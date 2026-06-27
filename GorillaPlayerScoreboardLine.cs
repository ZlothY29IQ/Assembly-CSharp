using System;
using GorillaExtensions;
using GorillaNetworking;
using GorillaTagScripts.VirtualStumpCustomMaps;
using Photon.Realtime;
using Photon.Voice.Unity;
using UnityEngine;
using UnityEngine.UI;

public class GorillaPlayerScoreboardLine : MonoBehaviour
{
	private static int[] targetActors = new int[1] { -1 };

	public Text playerName;

	public Text playerLevel;

	public Text playerMMR;

	public Image playerSwatch;

	public Texture infectedTexture;

	public NetPlayer linePlayer;

	public VRRig playerVRRig;

	public string playerLevelValue;

	public string playerMMRValue;

	public string playerNameValue;

	public string playerNameVisible;

	public int playerActorNumber;

	public GorillaPlayerLineButton muteButton;

	public GorillaPlayerLineButton reportButton;

	[Space]
	public GameObject reportButtons;

	public GameObject hateSpeechButton;

	public GameObject toxicityButton;

	public GameObject cheatingButton;

	public GameObject cancelButton;

	[Space]
	public GameObject roomControlButtons;

	public GorillaPlayerLineButton muteForRoomButton;

	public GorillaPlayerLineButton kickButton;

	public GorillaPlayerLineButton banButton;

	[Space]
	public GameObject confirmRoomControlButtons;

	public GorillaPlayerLineButton confirmRoomControlButton;

	public GorillaPlayerLineButton cancelRoomControlButton;

	[Space]
	public SpriteRenderer speakerIcon;

	public bool canPressNextReportButton = true;

	public Text[] texts;

	public SpriteRenderer[] sprites;

	public MeshRenderer[] meshes;

	public Image[] images;

	private Recorder myRecorder;

	private bool isMuteManual;

	private int mute;

	private int emptyRigCount;

	public GameObject myRig;

	public bool reportedCheating;

	public bool reportedToxicity;

	public bool reportedHateSpeech;

	public bool reportInProgress;

	private string currentNickname;

	public bool doneReporting;

	public bool lastVisible = true;

	private GorillaPlayerLineButton _parentConfirmButton;

	private bool _attemptingKick;

	private bool _attemptingBan;

	public GorillaScoreBoard parentScoreboard;

	public float initTime;

	public float emptyRigCooldown = 10f;

	internal RigContainer rigContainer;

	public void Start()
	{
		emptyRigCount = 0;
		reportedCheating = false;
		reportedHateSpeech = false;
		reportedToxicity = false;
	}

	public void InitializeLine()
	{
		currentNickname = string.Empty;
		UpdatePlayerText();
		if (linePlayer == NetworkSystem.Instance.LocalPlayer)
		{
			muteButton.gameObject.SetActive(value: false);
			reportButton.gameObject.SetActive(value: false);
			reportButtons.SetActive(value: false);
			return;
		}
		muteButton.gameObject.SetActive(value: true);
		if (GorillaScoreboardTotalUpdater.instance != null && GorillaScoreboardTotalUpdater.instance.reportDict.ContainsKey(playerActorNumber))
		{
			GorillaScoreboardTotalUpdater.PlayerReports playerReports = GorillaScoreboardTotalUpdater.instance.reportDict[playerActorNumber];
			reportedCheating = playerReports.cheating;
			reportedHateSpeech = playerReports.hateSpeech;
			reportedToxicity = playerReports.toxicity;
			reportInProgress = playerReports.pressedReport;
		}
		else
		{
			reportedCheating = false;
			reportedHateSpeech = false;
			reportedToxicity = false;
			reportInProgress = false;
		}
		reportButton.isOn = reportedCheating || reportedHateSpeech || reportedToxicity;
		reportButton.UpdateColor();
		SwapToReportState(reportInProgress);
		muteButton.gameObject.SetActive(value: true);
		isMuteManual = PlayerPrefs.HasKey(linePlayer.UserId);
		mute = PlayerPrefs.GetInt(linePlayer.UserId, 0);
		muteButton.isOn = ((mute != 0) ? true : false);
		muteButton.isAutoOn = false;
		muteButton.UpdateColor();
		if (rigContainer != null)
		{
			rigContainer.hasManualMute = isMuteManual;
			rigContainer.SetMuted(RigContainer.MuteReason.Manual, (mute != 0) ? true : false);
		}
		_parentConfirmButton = null;
		_attemptingKick = false;
		_attemptingBan = false;
	}

	public void SetLineData(NetPlayer netPlayer)
	{
		if (netPlayer.InRoom && netPlayer != linePlayer)
		{
			if (playerActorNumber != netPlayer.ActorNumber)
			{
				initTime = Time.time;
			}
			playerActorNumber = netPlayer.ActorNumber;
			linePlayer = netPlayer;
			playerNameValue = netPlayer.NickName ?? "";
			if (VRRigCache.Instance.TryGetVrrig(netPlayer, out var playerRig))
			{
				rigContainer = playerRig;
				playerVRRig = playerRig.Rig;
			}
			InitializeLine();
		}
	}

	public void UpdateLine()
	{
		if (linePlayer == null)
		{
			return;
		}
		if (playerNameVisible != playerVRRig.playerNameVisible)
		{
			UpdatePlayerText();
			parentScoreboard.SetDirty();
			if (playerVRRig.creator.IsMasterClient && GorillaComputer.instance.IsPlayerInVirtualStump())
			{
				CustomMapModeSelector.RefreshHostName();
			}
		}
		if (rigContainer == null)
		{
			return;
		}
		if (Time.time > initTime + emptyRigCooldown)
		{
			if (playerVRRig.netView != null)
			{
				emptyRigCount = 0;
			}
			else
			{
				emptyRigCount++;
				if (emptyRigCount > 30)
				{
					MonkeAgent.instance.SendReport("empty rig", linePlayer.UserId, linePlayer.NickName);
				}
			}
		}
		Material material = ((playerVRRig.setMatIndex == 0) ? playerVRRig.scoreboardMaterial : playerVRRig.materialsToChangeTo[playerVRRig.setMatIndex]);
		if (playerSwatch.material != material)
		{
			playerSwatch.material = material;
		}
		if (playerSwatch.color != playerVRRig.materialsToChangeTo[0].color)
		{
			playerSwatch.color = playerVRRig.materialsToChangeTo[0].color;
		}
		if (myRecorder == null)
		{
			myRecorder = NetworkSystem.Instance.LocalRecorder;
		}
		if (playerVRRig != null)
		{
			if (playerVRRig.remoteUseReplacementVoice || playerVRRig.localUseReplacementVoice || GorillaComputer.instance.voiceChatOn == "FALSE")
			{
				speakerIcon.enabled = playerVRRig.SpeakingLoudness > playerVRRig.replacementVoiceLoudnessThreshold && !rigContainer.IsMutedFor(~RigContainer.MuteReason.Auto);
			}
			else if ((rigContainer.Voice != null && rigContainer.Voice.IsSpeaking) || (playerVRRig.rigSerializer != null && playerVRRig.rigSerializer.IsLocallyOwned && myRecorder != null && myRecorder.IsCurrentlyTransmitting))
			{
				speakerIcon.enabled = true;
			}
			else
			{
				speakerIcon.enabled = false;
			}
		}
		else
		{
			speakerIcon.enabled = false;
		}
		if (!isMuteManual)
		{
			bool isPlayerAutoMuted = rigContainer.GetIsPlayerAutoMuted();
			if (muteButton.isAutoOn != isPlayerAutoMuted)
			{
				muteButton.isAutoOn = isPlayerAutoMuted;
				muteButton.UpdateColor();
			}
		}
		bool flag = RoomControls.MutedPlayers.ContainsKey(linePlayer.UserId);
		if (muteForRoomButton.isOn != flag)
		{
			muteForRoomButton.isOn = flag;
			muteForRoomButton.UpdateColor();
		}
	}

	private void UpdatePlayerText()
	{
		try
		{
			if (rigContainer.IsNull() || playerVRRig.IsNull())
			{
				playerNameVisible = NormalizeName(linePlayer.NickName != currentNickname, linePlayer.NickName);
				currentNickname = linePlayer.NickName;
			}
			else if (rigContainer.Initialized)
			{
				playerNameVisible = playerVRRig.playerNameVisible;
			}
			else if (currentNickname.IsNullOrEmpty() || GorillaComputer.instance.friendJoinCollider.playerIDsCurrentlyTouching.Contains(linePlayer.UserId))
			{
				playerNameVisible = NormalizeName(linePlayer.NickName != currentNickname, linePlayer.NickName);
			}
			currentNickname = linePlayer.NickName;
			playerName.text = (KIDManager.HasPermissionToUseFeature(EKIDFeatures.Custom_Nametags) ? playerNameVisible : linePlayer.DefaultName);
		}
		catch (Exception)
		{
			playerNameVisible = linePlayer.DefaultName;
			MonkeAgent.instance.SendReport("NmError", linePlayer.UserId, linePlayer.NickName);
		}
	}

	public bool IsLineActive()
	{
		return base.gameObject.activeInHierarchy;
	}

	public bool IsPlayerInRoom()
	{
		if (linePlayer != null)
		{
			return linePlayer.InRoom;
		}
		return false;
	}

	public bool IsReportButtonActive()
	{
		return reportButton.isActiveAndEnabled;
	}

	public bool IsConfirmButtonsActive()
	{
		return confirmRoomControlButtons.activeInHierarchy;
	}

	public bool IsConfirmParentKick()
	{
		return _parentConfirmButton == kickButton;
	}

	public bool IsConfirmParentBan()
	{
		return _parentConfirmButton == banButton;
	}

	public void PressButton(bool isOn, GorillaPlayerLineButton.ButtonType buttonType)
	{
		switch (buttonType)
		{
		case GorillaPlayerLineButton.ButtonType.Mute:
			if (linePlayer != null && playerVRRig != null)
			{
				isMuteManual = true;
				muteButton.isAutoOn = false;
				mute = (isOn ? 1 : 0);
				PlayerPrefs.SetInt(linePlayer.UserId, mute);
				if (rigContainer != null)
				{
					rigContainer.hasManualMute = isMuteManual;
					rigContainer.SetMuted(RigContainer.MuteReason.Manual, (mute != 0) ? true : false);
				}
				PlayerPrefs.Save();
				muteButton.UpdateColor();
				GorillaScoreboardTotalUpdater.ReportMute(linePlayer, mute);
			}
			break;
		case GorillaPlayerLineButton.ButtonType.Report:
			SetReportState(reportState: true, buttonType);
			break;
		case GorillaPlayerLineButton.ButtonType.MuteAllRoom:
			Debug.Log("Pressed Mute Room Control button for player " + linePlayer.UserId);
			AttemptRoomControlMute();
			break;
		case GorillaPlayerLineButton.ButtonType.KickRoom:
			if (_attemptingKick || _attemptingBan)
			{
				Debug.Log("Ban or kick for player " + linePlayer.UserId + " is already in progress!");
				break;
			}
			Debug.Log("Pressed Kick Room Control button for player " + linePlayer.UserId);
			ShowConfirmButtons(kickButton);
			break;
		case GorillaPlayerLineButton.ButtonType.BanRoom:
			if (_attemptingKick || _attemptingBan)
			{
				Debug.Log("Ban or kick for player " + linePlayer.UserId + " is already in progress!");
				break;
			}
			Debug.Log("Pressed Ban Room Control button for player " + linePlayer.UserId);
			ShowConfirmButtons(banButton);
			break;
		case GorillaPlayerLineButton.ButtonType.Confirm:
			if (IsConfirmParentKick())
			{
				if (AttemptRoomControlKick())
				{
					HideConfirmButtons();
				}
			}
			else if (IsConfirmParentBan() && AttemptRoomControlBan())
			{
				HideConfirmButtons();
			}
			break;
		case GorillaPlayerLineButton.ButtonType.Cancel:
			if (_parentConfirmButton == null)
			{
				SetReportState(reportState: false, buttonType);
			}
			else
			{
				HideConfirmButtons();
			}
			break;
		default:
			SetReportState(reportState: false, buttonType);
			break;
		}
	}

	public void SetReportState(bool reportState, GorillaPlayerLineButton.ButtonType buttonType)
	{
		canPressNextReportButton = buttonType != GorillaPlayerLineButton.ButtonType.Toxicity && buttonType != GorillaPlayerLineButton.ButtonType.Report;
		reportInProgress = reportState;
		SwapToReportState(reportState);
		if (!reportState && linePlayer != null && buttonType != GorillaPlayerLineButton.ButtonType.Cancel)
		{
			if ((!reportedHateSpeech && buttonType == GorillaPlayerLineButton.ButtonType.HateSpeech) || (!reportedToxicity && buttonType == GorillaPlayerLineButton.ButtonType.Toxicity) || (!reportedCheating && buttonType == GorillaPlayerLineButton.ButtonType.Cheating))
			{
				ReportPlayer(linePlayer.UserId, buttonType, playerNameVisible);
				doneReporting = true;
			}
			reportedCheating = reportedCheating || buttonType == GorillaPlayerLineButton.ButtonType.Cheating;
			reportedToxicity = reportedToxicity || buttonType == GorillaPlayerLineButton.ButtonType.Toxicity;
			reportedHateSpeech = reportedHateSpeech || buttonType == GorillaPlayerLineButton.ButtonType.HateSpeech;
			reportButton.isOn = true;
			reportButton.UpdateColor();
		}
		if (GorillaScoreboardTotalUpdater.instance != null)
		{
			GorillaScoreboardTotalUpdater.instance.UpdateLineState(this);
		}
		parentScoreboard.RedrawPlayerLines();
	}

	public static void ReportPlayer(string PlayerID, GorillaPlayerLineButton.ButtonType buttonType, string OtherPlayerNickName)
	{
		if (OtherPlayerNickName.Length > 12)
		{
			OtherPlayerNickName.Remove(12);
		}
		WebFlags flags = new WebFlags(3);
		NetEventOptions options = new NetEventOptions
		{
			Flags = flags,
			TargetActors = targetActors
		};
		byte code = 50;
		object[] data = new object[6]
		{
			PlayerID,
			buttonType,
			OtherPlayerNickName,
			NetworkSystem.Instance.LocalPlayer.NickName,
			!NetworkSystem.Instance.SessionIsPrivate,
			NetworkSystem.Instance.RoomStringStripped()
		};
		NetworkSystemRaiseEvent.RaiseEvent(code, data, options, reliable: true);
	}

	public void ToggleRoomControlButtons(bool toggle, bool hideConfirm = true)
	{
		roomControlButtons.gameObject.SetActive(toggle);
		if (hideConfirm && !toggle)
		{
			HideConfirmButtons();
		}
	}

	public void ShowConfirmButtons(GorillaPlayerLineButton parentButton)
	{
		kickButton.gameObject.SetActive(value: false);
		banButton.gameObject.SetActive(value: false);
		confirmRoomControlButtons.SetActive(value: true);
		confirmRoomControlButton.SetTouchTime(0.5f);
		cancelRoomControlButton.SetTouchTime(0.5f);
		confirmRoomControlButtons.transform.localPosition = new Vector2(parentButton.transform.localPosition.x, 0f);
		_parentConfirmButton = parentButton;
		parentScoreboard.RedrawPlayerLines();
	}

	public void HideConfirmButtons()
	{
		kickButton.gameObject.SetActive(value: true);
		banButton.gameObject.SetActive(value: true);
		confirmRoomControlButtons.SetActive(value: false);
		_parentConfirmButton = null;
		parentScoreboard.RedrawPlayerLines();
	}

	private string NormalizeName(bool doIt, string text)
	{
		if (doIt)
		{
			int length = text.Length;
			text = new string(Array.FindAll(text.ToCharArray(), (char c) => Utils.IsASCIILetterOrDigit(c)));
			int length2 = text.Length;
			if (length2 > 0 && length == length2 && GorillaComputer.instance.CheckAutoBanListForName(text))
			{
				if (text.Length > 12)
				{
					text = text.Substring(0, 12);
				}
				text = text.ToUpper();
			}
			else
			{
				text = "BADGORILLA";
				MonkeAgent.instance.SendReport("evading the name ban", linePlayer.UserId, linePlayer.NickName);
			}
		}
		return text;
	}

	public void ResetData()
	{
		emptyRigCount = 0;
		playerActorNumber = -1;
		linePlayer = null;
		playerNameValue = string.Empty;
		currentNickname = string.Empty;
	}

	private void OnEnable()
	{
		GorillaScoreboardTotalUpdater.RegisterSL(this);
	}

	private void OnDisable()
	{
		GorillaScoreboardTotalUpdater.UnregisterSL(this);
	}

	private void SwapToReportState(bool reportInProgress)
	{
		reportButton.gameObject.SetActive(!reportInProgress);
		reportButtons.SetActive(reportInProgress);
	}

	private bool AttemptRoomControlMute()
	{
		if (!RoomControls.CanModerate(out var cannotReason))
		{
			Debug.LogWarning("Cannot mute player in room: " + cannotReason + ".");
			return false;
		}
		if (RoomControls.MutedPlayers.ContainsKey(linePlayer.UserId))
		{
			Debug.Log("Attempting UNMUTE in room for player " + linePlayer.UserId);
			RoomControls.UnmutePlayer(linePlayer.UserId);
		}
		else
		{
			Debug.Log("Attempting MUTE in room for player " + linePlayer.UserId);
			RoomControls.MutePlayer(linePlayer.ActorNumber);
		}
		return true;
	}

	private bool AttemptRoomControlKick()
	{
		if (!RoomControls.CanModerate(out var cannotReason))
		{
			Debug.LogWarning("Cannot kick player from room: " + cannotReason + ".");
			return false;
		}
		Debug.Log("Attempting KICK from room for player " + linePlayer.UserId);
		RoomControls.KickPlayer(linePlayer.ActorNumber);
		_attemptingKick = true;
		return true;
	}

	private bool AttemptRoomControlBan()
	{
		if (!RoomControls.CanModerate(out var cannotReason))
		{
			Debug.LogWarning("Cannot ban player from room: " + cannotReason + ".");
			return false;
		}
		Debug.Log("Attempting BAN from room for player " + linePlayer.UserId);
		RoomControls.KickAndBlockPlayer(linePlayer.ActorNumber);
		_attemptingBan = true;
		return true;
	}
}
