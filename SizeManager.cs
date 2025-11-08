using System.Collections.Generic;
using GorillaLocomotion;
using UnityEngine;

public class SizeManager : MonoBehaviour
{
	public enum SizeChangerType
	{
		LocalOffline,
		LocalOnline,
		OtherOnline
	}

	public List<SizeChanger> touchingChangers;

	private LineRenderer[] lineRenderers;

	private List<float> initLineScalar = new List<float>();

	public VRRig targetRig;

	public GTPlayer targetPlayer;

	public float magnitudeThreshold = 0.01f;

	public float rate = 650f;

	public Transform mainCameraTransform;

	public SizeChangerType myType;

	public float lastScale;

	private bool buildInitialized;

	private const float returnToNormalEasing = 0.33f;

	private float smallThreshold = 0.6f;

	private float largeThreshold = 1.5f;

	private bool isSmall;

	private bool isLarge;

	public float currentScale
	{
		get
		{
			if (targetRig != null)
			{
				return targetRig.ScaleMultiplier;
			}
			if (targetPlayer != null)
			{
				return targetPlayer.ScaleMultiplier;
			}
			return 1f;
		}
	}

	public int currentSizeLayerMaskValue
	{
		get
		{
			if ((bool)targetPlayer)
			{
				return targetPlayer.sizeLayerMask;
			}
			if ((bool)targetRig)
			{
				return targetRig.SizeLayerMask;
			}
			return 1;
		}
		set
		{
			if ((bool)targetPlayer)
			{
				targetPlayer.sizeLayerMask = value;
				if (targetRig != null)
				{
					targetRig.SizeLayerMask = value;
				}
			}
			else if ((bool)targetRig)
			{
				targetRig.SizeLayerMask = value;
			}
		}
	}

	private void OnDisable()
	{
		touchingChangers.Clear();
		currentSizeLayerMaskValue = 1;
		SizeManagerManager.UnregisterSM(this);
	}

	private void OnEnable()
	{
		SizeManagerManager.RegisterSM(this);
	}

	private void CollectLineRenderers(GameObject obj)
	{
		lineRenderers = obj.GetComponentsInChildren<LineRenderer>(includeInactive: true);
		_ = lineRenderers.Length;
		LineRenderer[] array = lineRenderers;
		foreach (LineRenderer lineRenderer in array)
		{
			initLineScalar.Add(lineRenderer.widthMultiplier);
		}
	}

	public void BuildInitialize()
	{
		rate = 650f;
		if (targetRig != null)
		{
			CollectLineRenderers(targetRig.gameObject);
		}
		else if (targetPlayer != null)
		{
			CollectLineRenderers(GorillaTagger.Instance.offlineVRRig.gameObject);
		}
		mainCameraTransform = Camera.main.transform;
		if (targetPlayer != null)
		{
			myType = SizeChangerType.LocalOffline;
		}
		else if (targetRig != null && !targetRig.isOfflineVRRig && targetRig.netView != null && targetRig.netView.Owner != NetworkSystem.Instance.LocalPlayer)
		{
			myType = SizeChangerType.OtherOnline;
		}
		else
		{
			myType = SizeChangerType.LocalOnline;
		}
		buildInitialized = true;
	}

	private void Awake()
	{
		if (!buildInitialized)
		{
			BuildInitialize();
		}
		SizeManagerManager.RegisterSM(this);
	}

	public void InvokeFixedUpdate()
	{
		float num = 1f;
		SizeChanger sizeChanger = ControllingChanger(targetRig.transform);
		switch (myType)
		{
		case SizeChangerType.LocalOnline:
			num = ScaleFromChanger(sizeChanger, targetRig.transform, Time.fixedDeltaTime);
			targetRig.ScaleMultiplier = ((num == 1f) ? SizeOverTime(num, 0.33f, Time.fixedDeltaTime) : num);
			break;
		case SizeChangerType.OtherOnline:
			num = ScaleFromChanger(sizeChanger, targetRig.transform, Time.fixedDeltaTime);
			targetRig.ScaleMultiplier = ((num == 1f) ? SizeOverTime(num, 0.33f, Time.fixedDeltaTime) : num);
			break;
		case SizeChangerType.LocalOffline:
			num = ScaleFromChanger(sizeChanger, mainCameraTransform, Time.fixedDeltaTime);
			targetPlayer.SetScaleMultiplier((num == 1f) ? SizeOverTime(num, 0.33f, Time.fixedDeltaTime) : num);
			break;
		}
		if (num != lastScale)
		{
			for (int i = 0; i < lineRenderers.Length; i++)
			{
				lineRenderers[i].widthMultiplier = num * initLineScalar[i];
			}
			if (sizeChanger != null && sizeChanger.TryGetScaleCenterPoint(out var centerPoint))
			{
				if (myType == SizeChangerType.LocalOffline)
				{
					targetPlayer.ScaleAwayFromPoint(lastScale, num, centerPoint);
				}
				else if (myType == SizeChangerType.LocalOnline)
				{
					GTPlayer.Instance.ScaleAwayFromPoint(lastScale, num, centerPoint);
				}
			}
			if (myType == SizeChangerType.LocalOffline)
			{
				CheckSizeChangeEvents(num);
			}
		}
		lastScale = num;
	}

	private SizeChanger ControllingChanger(Transform t)
	{
		for (int num = touchingChangers.Count - 1; num >= 0; num--)
		{
			SizeChanger sizeChanger = touchingChangers[num];
			if (!(sizeChanger == null) && sizeChanger.gameObject.activeInHierarchy && (sizeChanger.SizeLayerMask & currentSizeLayerMaskValue) != 0 && (sizeChanger.alwaysControlWhenEntered || (sizeChanger.ClosestPoint(t.position) - t.position).magnitude < magnitudeThreshold))
			{
				return sizeChanger;
			}
		}
		return null;
	}

	private float ScaleFromChanger(SizeChanger sC, Transform t, float deltaTime)
	{
		if (sC == null)
		{
			return 1f;
		}
		switch (sC.MyType)
		{
		case SizeChanger.ChangerType.Continuous:
		{
			Vector3 vector = Vector3.Project(t.position - sC.StartPos.position, sC.EndPos.position - sC.StartPos.position);
			return Mathf.Clamp(sC.MaxScale - vector.magnitude / (sC.StartPos.position - sC.EndPos.position).magnitude * (sC.MaxScale - sC.MinScale), sC.MinScale, sC.MaxScale);
		}
		case SizeChanger.ChangerType.Static:
			return SizeOverTime(sC.MinScale, sC.StaticEasing, deltaTime);
		default:
			return 1f;
		}
	}

	private float SizeOverTime(float targetSize, float easing, float deltaTime)
	{
		if (easing <= 0f || Mathf.Abs(targetRig.ScaleMultiplier - targetSize) < 0.05f)
		{
			return targetSize;
		}
		return Mathf.MoveTowards(targetRig.ScaleMultiplier, targetSize, deltaTime / easing);
	}

	private void CheckSizeChangeEvents(float newSize)
	{
		if (newSize < smallThreshold)
		{
			if (!isSmall)
			{
				isSmall = true;
				isLarge = false;
				PlayerGameEvents.MiscEvent("SizeSmall");
			}
		}
		else if (newSize > largeThreshold)
		{
			if (!isLarge)
			{
				isLarge = true;
				isSmall = false;
				PlayerGameEvents.MiscEvent("SizeLarge");
			}
		}
		else
		{
			isLarge = false;
			isSmall = false;
		}
	}
}
