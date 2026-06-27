using System;
using System.Collections.Generic;
using System.Text;
using GorillaGameModes;
using GorillaTagScripts;
using TMPro;
using UnityEngine;

public class GorillaScoreBoard : MonoBehaviour
{
	public GameObject scoreBoardLinePrefab;

	public int startingYValue;

	public int lineHeight;

	public bool includeMMR;

	public bool isActive;

	public GameObject leftPanel;

	public float leftPanelRoomControlXOffset = -36f;

	public GameObject rightPanel;

	[Space]
	public GameObject linesParent;

	public float bigRoomYOffset = 32.5f;

	[SerializeField]
	public List<GorillaPlayerScoreboardLine> lines;

	private List<RectTransform> linesRTs = new List<RectTransform>();

	public GameObject textsParent;

	public TextMeshPro boardText;

	public TextMeshPro buttonText;

	public TextMeshPro roomControlsText;

	public GameObject roomControlsToggle;

	public bool needsUpdate;

	public TextMeshPro notInRoomText;

	public string initialGameMode;

	private bool roomControlsActive;

	private string tempGmName;

	private string gmName;

	private const string error = "ERROR";

	private List<string> gmNames;

	private bool _isDirty = true;

	private StringBuilder stringBuilder = new StringBuilder(220);

	private StringBuilder buttonStringBuilder = new StringBuilder(720);

	public bool RoomControlsActive => roomControlsActive;

	public bool IsDirty
	{
		get
		{
			if (!_isDirty)
			{
				return string.IsNullOrEmpty(initialGameMode);
			}
			return true;
		}
	}

	public void SetSleepState(bool awake)
	{
		boardText.enabled = awake;
		buttonText.enabled = awake;
		if (linesParent != null)
		{
			linesParent.SetActive(awake);
		}
		ToggleRoomControlButton();
	}

	private string GetBeginningString()
	{
		string text = $" ({10})";
		if (NetworkSystem.Instance.SessionIsSubscription)
		{
			text = $" ({20})";
		}
		return "ROOM ID: " + (NetworkSystem.Instance.SessionIsPrivate ? "-PRIVATE- GAME: " : (NetworkSystem.Instance.RoomName + "   GAME: ")) + RoomType() + text + "\n  PLAYER     COLOR   MUTE  REPORT";
	}

	private string RoomType()
	{
		initialGameMode = RoomSystem.RoomGameMode;
		gmNames = GameMode.gameModeNames;
		gmName = "ERROR";
		int count = gmNames.Count;
		int num = initialGameMode.LastIndexOf('|');
		if (num >= 0)
		{
			tempGmName = initialGameMode.Substring(num + 1);
			for (int i = 0; i < count; i++)
			{
				if (tempGmName == gmNames[i])
				{
					gmName = tempGmName;
					break;
				}
			}
		}
		else
		{
			for (int j = 0; j < count; j++)
			{
				tempGmName = gmNames[j];
				if (initialGameMode.Contains(tempGmName))
				{
					gmName = tempGmName;
					break;
				}
			}
		}
		return gmName;
	}

	[ContextMenu("Toggle Room Controls")]
	public void ToggleRoomControls()
	{
		if (!RoomControls.CanModerate(out var cannotReason))
		{
			Debug.LogWarning("Cannot toggle room controls: " + cannotReason + ".");
			return;
		}
		roomControlsActive = !roomControlsActive;
		SetDirty();
	}

	public void CleanupRoomControls()
	{
		roomControlsActive = false;
		rightPanel.SetActive(value: false);
		for (int i = 0; i < lines.Count; i++)
		{
			lines[i].ToggleRoomControlButtons(toggle: false);
		}
	}

	private void ToggleRoomControlButton()
	{
		roomControlsToggle.gameObject.SetActive(RoomControls.CanModerate());
	}

	public void RedrawPlayerLines()
	{
		stringBuilder.Clear();
		stringBuilder.Append(GetBeginningString());
		buttonStringBuilder.Clear();
		bool flag = KIDManager.HasPermissionToUseFeature(EKIDFeatures.Custom_Nametags);
		int num = 0;
		for (int i = 0; i < lines.Count; i++)
		{
			if (lines[i].gameObject.activeInHierarchy)
			{
				num++;
			}
		}
		if (roomControlsActive)
		{
			leftPanel.transform.localScale = new Vector3(0.7f, 1f, 1f);
			leftPanel.transform.localPosition = new Vector3(leftPanelRoomControlXOffset, 0f);
			rightPanel.SetActive(value: true);
		}
		else
		{
			leftPanel.transform.localScale = Vector3.one;
			leftPanel.transform.localPosition = Vector3.zero;
			rightPanel.SetActive(value: false);
		}
		if (num > 10)
		{
			linesParent.transform.localScale = new Vector3(1f, 0.5f, 1f);
			linesParent.transform.localPosition = new Vector3(0f, bigRoomYOffset, 0f);
			textsParent.transform.localScale = new Vector3(1f, 0.5f, 1f);
		}
		else
		{
			linesParent.transform.localScale = Vector3.one;
			linesParent.transform.localPosition = Vector3.zero;
			textsParent.transform.localScale = Vector3.one;
		}
		for (int j = 0; j < lines.Count; j++)
		{
			if (!lines[j].IsLineActive())
			{
				continue;
			}
			linesRTs[j].localPosition = new Vector3(0f, startingYValue - lineHeight * j, 0f);
			if (!lines[j].IsPlayerInRoom())
			{
				continue;
			}
			stringBuilder.Append("\n ");
			SubscriptionManager.SubscriptionDetails subscriptionDetails = SubscriptionManager.GetSubscriptionDetails(lines[j].linePlayer);
			if (subscriptionDetails.active && subscriptionDetails.tier > 0)
			{
				stringBuilder.Append("<color=#ffc600>");
			}
			else
			{
				stringBuilder.Append("<color=#ffffff>");
			}
			stringBuilder.Append(flag ? lines[j].playerNameVisible : lines[j].linePlayer.DefaultName);
			stringBuilder.Append("</color>");
			if (lines[j].linePlayer != NetworkSystem.Instance.LocalPlayer)
			{
				bool flag2 = lines[j].IsReportButtonActive();
				if (flag2)
				{
					if (!roomControlsActive)
					{
						buttonStringBuilder.Append("MUTE                                REPORT\n");
					}
					else if (!lines[j].IsConfirmButtonsActive())
					{
						buttonStringBuilder.Append("MUTE                                REPORT                              SILENCE                      KICK                      BLOCK\n");
					}
					else if (lines[j].IsConfirmParentKick())
					{
						buttonStringBuilder.Append("MUTE                                REPORT                              SILENCE           CONFIRM            CANCEL\n");
					}
					else
					{
						buttonStringBuilder.Append("MUTE                                REPORT                              SILENCE                                      CONFIRM            CANCEL\n");
					}
				}
				else
				{
					buttonStringBuilder.Append("MUTE                HATE SPEECH    TOXICITY     CHEATING       CANCEL\n");
				}
				lines[j].ToggleRoomControlButtons(roomControlsActive && flag2, hideConfirm: false);
			}
			else
			{
				buttonStringBuilder.Append("\n");
			}
		}
		boardText.text = stringBuilder.ToString();
		buttonText.text = buttonStringBuilder.ToString();
		_isDirty = false;
	}

	private void Awake()
	{
		linesRTs.Clear();
		for (int i = 0; i < lines.Count; i++)
		{
			linesRTs.Add(lines[i].GetComponent<RectTransform>());
		}
	}

	private void Start()
	{
		GorillaScoreboardTotalUpdater.RegisterScoreboard(this);
	}

	private void OnEnable()
	{
		GorillaScoreboardTotalUpdater.RegisterScoreboard(this);
		SetDirty();
		SubscriptionManager.OnSubscriptionData = (Action)Delegate.Remove(SubscriptionManager.OnSubscriptionData, new Action(OnSubscribeReady));
		SubscriptionManager.OnSubscriptionData = (Action)Delegate.Combine(SubscriptionManager.OnSubscriptionData, new Action(OnSubscribeReady));
		NetworkSystem.Instance.OnMasterClientSwitchedEvent -= new Action<NetPlayer>(OnMasterClientSwitched);
		NetworkSystem.Instance.OnMasterClientSwitchedEvent += new Action<NetPlayer>(OnMasterClientSwitched);
		RoomControls.OnRoomControlsEnabledChanged -= new Action<bool>(OnRoomControlsEnabledChanged);
		RoomControls.OnRoomControlsEnabledChanged += new Action<bool>(OnRoomControlsEnabledChanged);
		roomControlsToggle.gameObject.SetActive(value: false);
	}

	private void OnDisable()
	{
		GorillaScoreboardTotalUpdater.UnregisterScoreboard(this);
		SubscriptionManager.OnSubscriptionData = (Action)Delegate.Remove(SubscriptionManager.OnSubscriptionData, new Action(OnSubscribeReady));
		NetworkSystem.Instance.OnMasterClientSwitchedEvent -= new Action<NetPlayer>(OnMasterClientSwitched);
		RoomControls.OnRoomControlsEnabledChanged -= new Action<bool>(OnRoomControlsEnabledChanged);
	}

	private void OnSubscribeReady()
	{
		RefreshRoomControlUI();
	}

	private void OnMasterClientSwitched(NetPlayer newMasterClient)
	{
		RefreshRoomControlUI();
	}

	private void OnRoomControlsEnabledChanged(bool enabled)
	{
		RefreshRoomControlUI();
	}

	private void RefreshRoomControlUI()
	{
		if (roomControlsActive && !RoomControls.CanModerate())
		{
			roomControlsActive = false;
		}
		SetDirty();
		ToggleRoomControlButton();
	}

	public void SetDirty()
	{
		_isDirty = true;
	}
}
