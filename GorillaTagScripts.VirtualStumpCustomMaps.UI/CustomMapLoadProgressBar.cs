using System.Text;
using TMPro;
using UnityEngine;

namespace GorillaTagScripts.VirtualStumpCustomMaps.UI;

public class CustomMapLoadProgressBar : MonoBehaviour
{
	private enum FillAxis
	{
		X,
		Y,
		Z
	}

	public enum BarState
	{
		Idle,
		Working,
		Ready,
		Failed
	}

	[Header("Bar")]
	[Tooltip("Scaled from 0 to 1 along Fill Axis. Its pivot must be at the empty end of the bar.")]
	[SerializeField]
	private Transform fillTransform;

	[SerializeField]
	private FillAxis fillAxis;

	[Tooltip("[Optional] Tinted per status when Tint Fill By Status is on")]
	[SerializeField]
	private Renderer fillRenderer;

	[SerializeField]
	private bool tintFillByStatus = true;

	[SerializeField]
	private string fillColorPropertyName = "_Color";

	[SerializeField]
	private Color workingColor;

	[SerializeField]
	private Color readyColor;

	[SerializeField]
	private Color failedColor;

	[Header("Text")]
	[Tooltip("The phase - like DOWNLOADING.")]
	[SerializeField]
	private TMP_Text statusLabel;

	[Tooltip("The percentage - Hidden during phases that have no measurable progress")]
	[SerializeField]
	private TMP_Text percentLabel;

	[Tooltip("The detail message from the loader - like LOADING MAP SCENE.")]
	[SerializeField]
	private TMP_Text detailLabel;

	[SerializeField]
	private bool tintStatusLabelByStatus = true;

	[SerializeField]
	private string preparingString = "PREPARING";

	[SerializeField]
	private string downloadingString = "DOWNLOADING";

	[SerializeField]
	private string installingString = "INSTALLING";

	[SerializeField]
	private string loadingString = "LOADING";

	[SerializeField]
	private string unloadingString = "UNLOADING";

	[SerializeField]
	private string readyString = "READY";

	[SerializeField]
	private string failedString = "FAILED";

	[SerializeField]
	private string readyDetailString = "";

	[Header("Visibility")]
	[Tooltip("Toggled off while there is nothing to report.Leave empty to keep the bar visible at all times")]
	[SerializeField]
	private GameObject contentRoot;

	[Tooltip("How long READY / FAILED stays up before the bar hides itself. 0 keeps it up until the next load.")]
	[SerializeField]
	private float hideDelayAfterFinished = 6f;

	[Tooltip("How quickly the bar catches up to a new value")]
	[SerializeField]
	private float fillLerpSpeed = 5f;

	[Tooltip("Seconds per dot of the animated ellipsis shown while a phase has no measurable progress.")]
	[SerializeField]
	private float ellipsisSecondsPerDot = 0.35f;

	private static readonly StringBuilder SharedStringBuilder = new StringBuilder(32);

	private int fillColorPropertyId = -1;

	private MaterialPropertyBlock fillPropertyBlock;

	private float targetFill;

	private float displayedFill;

	private bool showEllipsis;

	private int ellipsisDotCount = -1;

	private string detailTextWithoutEllipsis = "";

	private bool finished;

	private float finishedAtTime;

	public BarState State { get; private set; }

	public MapLoadStatus Phase { get; private set; }

	public int PercentComplete { get; private set; }

	public bool HasMeasurablePercent { get; private set; }

	public float NormalizedProgress => targetFill;

	public float DisplayedProgress => displayedFill;

	public string DetailMessage => detailTextWithoutEllipsis;

	private void OnEnable()
	{
		CustomMapManager.OnMapLoadStatusChanged.AddListener(OnMapLoadStatusChanged);
		CustomMapManager.OnMapLoadComplete.AddListener(OnMapLoadComplete);
		CustomMapManager.OnMapUnloadComplete.AddListener(OnMapUnloadComplete);
		SyncToCurrentState();
	}

	private void OnDisable()
	{
		CustomMapManager.OnMapLoadStatusChanged.RemoveListener(OnMapLoadStatusChanged);
		CustomMapManager.OnMapLoadComplete.RemoveListener(OnMapLoadComplete);
		CustomMapManager.OnMapUnloadComplete.RemoveListener(OnMapUnloadComplete);
	}

	private void Update()
	{
		if (!Mathf.Approximately(displayedFill, targetFill))
		{
			displayedFill = ((fillLerpSpeed > 0f) ? Mathf.MoveTowards(displayedFill, targetFill, fillLerpSpeed * Time.deltaTime) : targetFill);
			ApplyFill(displayedFill);
		}
		if (showEllipsis)
		{
			UpdateEllipsis();
		}
		if (finished && hideDelayAfterFinished > 0f && Time.time - finishedAtTime >= hideDelayAfterFinished)
		{
			ShowIdle();
		}
	}

	public void SyncToCurrentState()
	{
		MapLoadStatus currentLoadStatus = CustomMapManager.CurrentLoadStatus;
		if (currentLoadStatus != MapLoadStatus.None)
		{
			ApplyStatus(currentLoadStatus, CustomMapManager.CurrentLoadProgress, CustomMapManager.CurrentLoadMessage);
			displayedFill = targetFill;
			ApplyFill(displayedFill);
		}
		else if (CustomMapManager.IsUnloading())
		{
			ApplyStatus(MapLoadStatus.Unloading, 0, "");
		}
		else if (CustomMapLoader.IsMapLoaded())
		{
			ShowFinished(readyString, readyDetailString, readyColor, 1f, succeeded: true);
		}
		else
		{
			ShowIdle();
		}
	}

	private void OnMapLoadStatusChanged(MapLoadStatus status, int progress, string message)
	{
		ApplyStatus(status, progress, message);
	}

	private void OnMapLoadComplete(bool success)
	{
		if (success)
		{
			ShowFinished(readyString, readyDetailString, readyColor, 1f, succeeded: true);
		}
		else
		{
			ShowFinished(failedString, CustomMapManager.CurrentLoadMessage, failedColor, displayedFill, succeeded: false);
		}
	}

	private void OnMapUnloadComplete()
	{
		ShowIdle();
	}

	private void ApplyStatus(MapLoadStatus status, int progress, string message)
	{
		Phase = status;
		switch (status)
		{
		case MapLoadStatus.Downloading:
			SetWorking(downloadingString, message, progress, progress > 0);
			break;
		case MapLoadStatus.Installing:
			SetWorking(installingString, message, progress, progress > 0);
			break;
		case MapLoadStatus.Loading:
			SetWorking((progress > 0) ? loadingString : preparingString, message, progress, progress > 0);
			break;
		case MapLoadStatus.Unloading:
			SetWorking(unloadingString, message, 100, hasMeasurableProgress: false);
			break;
		case MapLoadStatus.Error:
			ShowFinished(failedString, message, failedColor, displayedFill, succeeded: false);
			break;
		case MapLoadStatus.None:
			ShowIdle();
			break;
		}
	}

	private void SetWorking(string status, string detail, int percent, bool hasMeasurableProgress)
	{
		finished = false;
		SetContentActive(active: true);
		State = BarState.Working;
		HasMeasurablePercent = hasMeasurableProgress;
		PercentComplete = Mathf.Clamp(percent, 0, 100);
		SetText(statusLabel, status);
		if (tintStatusLabelByStatus && statusLabel != null)
		{
			statusLabel.color = workingColor;
		}
		if (percentLabel != null)
		{
			percentLabel.gameObject.SetActive(hasMeasurableProgress);
			if (hasMeasurableProgress)
			{
				SharedStringBuilder.Clear();
				SharedStringBuilder.Append(PercentComplete);
				SharedStringBuilder.Append('%');
				percentLabel.SetText(SharedStringBuilder);
			}
		}
		SetDetail(detail, !hasMeasurableProgress);
		targetFill = (float)PercentComplete / 100f;
		SetFillColor(workingColor);
	}

	private void ShowFinished(string status, string detail, Color color, float fill, bool succeeded)
	{
		finished = true;
		finishedAtTime = Time.time;
		SetContentActive(active: true);
		State = (succeeded ? BarState.Ready : BarState.Failed);
		Phase = ((!succeeded) ? MapLoadStatus.Error : MapLoadStatus.None);
		HasMeasurablePercent = false;
		PercentComplete = Mathf.RoundToInt(Mathf.Clamp01(fill) * 100f);
		SetText(statusLabel, status);
		if (tintStatusLabelByStatus && statusLabel != null)
		{
			statusLabel.color = color;
		}
		if (percentLabel != null)
		{
			percentLabel.gameObject.SetActive(value: false);
		}
		SetDetail(detail, animateEllipsis: false);
		targetFill = Mathf.Clamp01(fill);
		SetFillColor(color);
	}

	private void ShowIdle()
	{
		finished = false;
		showEllipsis = false;
		State = BarState.Idle;
		Phase = MapLoadStatus.None;
		PercentComplete = 0;
		HasMeasurablePercent = false;
		targetFill = 0f;
		displayedFill = 0f;
		ApplyFill(0f);
		if (contentRoot != null)
		{
			SetContentActive(active: false);
			return;
		}
		SetText(statusLabel, "");
		if (percentLabel != null)
		{
			percentLabel.gameObject.SetActive(value: false);
		}
		SetDetail("", animateEllipsis: false);
	}

	private void SetContentActive(bool active)
	{
		if (contentRoot != null && contentRoot.activeSelf != active)
		{
			contentRoot.SetActive(active);
		}
	}

	private void SetDetail(string detail, bool animateEllipsis)
	{
		detailTextWithoutEllipsis = detail ?? "";
		showEllipsis = animateEllipsis && detailLabel != null && detailTextWithoutEllipsis.Length > 0;
		ellipsisDotCount = -1;
		if (!(detailLabel == null))
		{
			detailLabel.gameObject.SetActive(detailTextWithoutEllipsis.Length > 0);
			if (showEllipsis)
			{
				UpdateEllipsis();
			}
			else
			{
				detailLabel.SetText(detailTextWithoutEllipsis);
			}
		}
	}

	private void UpdateEllipsis()
	{
		int num = (int)(Time.time / Mathf.Max(0.01f, ellipsisSecondsPerDot)) % 4;
		if (num != ellipsisDotCount)
		{
			ellipsisDotCount = num;
			SharedStringBuilder.Clear();
			SharedStringBuilder.Append(detailTextWithoutEllipsis);
			for (int i = 0; i < num; i++)
			{
				SharedStringBuilder.Append('.');
			}
			detailLabel.SetText(SharedStringBuilder);
		}
	}

	private static void SetText(TMP_Text label, string text)
	{
		if (!(label == null))
		{
			label.gameObject.SetActive(!string.IsNullOrEmpty(text));
			label.SetText(text);
		}
	}

	private void ApplyFill(float fill)
	{
		if (!(fillTransform == null))
		{
			Vector3 localScale = fillTransform.localScale;
			switch (fillAxis)
			{
			case FillAxis.X:
				localScale.x = fill;
				break;
			case FillAxis.Y:
				localScale.y = fill;
				break;
			case FillAxis.Z:
				localScale.z = fill;
				break;
			}
			fillTransform.localScale = localScale;
		}
	}

	private void SetFillColor(Color color)
	{
		if (tintFillByStatus && !(fillRenderer == null) && !string.IsNullOrEmpty(fillColorPropertyName))
		{
			if (fillPropertyBlock == null)
			{
				fillPropertyBlock = new MaterialPropertyBlock();
			}
			if (fillColorPropertyId < 0)
			{
				fillColorPropertyId = Shader.PropertyToID(fillColorPropertyName);
			}
			fillRenderer.GetPropertyBlock(fillPropertyBlock);
			fillPropertyBlock.SetColor(fillColorPropertyId, color);
			fillRenderer.SetPropertyBlock(fillPropertyBlock);
		}
	}
}
