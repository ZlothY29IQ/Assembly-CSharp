using System;
using System.Collections.Generic;
using System.Text;
using ExitGames.Client.Photon;
using GorillaLocomotion;
using Photon.Pun;
using TMPro;
using UnityEngine;

public class GhostReactorShiftManager : MonoBehaviourTick
{
	[Serializable]
	public class WarningPres
	{
		public int time;

		public AbilitySound sound;
	}

	public enum State
	{
		WaitingForConnect,
		WaitingForShiftStart,
		WaitingForFirstShiftStart,
		ReadyForShift,
		ShiftActive,
		PostShift,
		PreparingToDrill,
		Drilling
	}

	private const string EVENT_GOOD_KD = "GRShiftGoodKD";

	[SerializeField]
	public GhostReactor reactor;

	[SerializeField]
	private GRMetalEnergyGate frontGate;

	[SerializeField]
	private GameObject startShiftButton;

	[SerializeField]
	private TMP_Text shiftTimerText;

	[SerializeField]
	private TMP_Text shiftStatsText;

	[SerializeField]
	private TMP_Text shiftJugmentText;

	[SerializeField]
	private TMP_Text reactorTextMain;

	[SerializeField]
	private GameObject wrongStumpGoo;

	[SerializeField]
	private float shiftDurationMinutes = 20f;

	[SerializeField]
	private Transform playerTeleportTransform;

	[SerializeField]
	private Transform gatePlaneTransform;

	[SerializeField]
	private Transform gateBlockerTransform;

	[SerializeField]
	private AudioSource anomalyLoop1;

	[SerializeField]
	private AudioSource anomalyLoop2;

	[SerializeField]
	private AudioSource anomalyLoop3;

	[SerializeField]
	private AudioSource anomalyAlert;

	[SerializeField]
	private float anomalyAlertCountdownTimeToStartPlayingInMinutes = 3f;

	[SerializeField]
	private float roomCloseTimeSeconds = 60f;

	private bool isRoomClosed;

	[SerializeField]
	private int preShiftDuration = 10;

	private int preShiftDurationFirstArrive = 60;

	private int postShiftDuration = 10;

	[SerializeField]
	public int drillDuration = 50;

	private bool bIsStartingFloorAuthorityOnly;

	[Header("Drill Announcements")]
	[SerializeField]
	private AudioSource announceAudioSource;

	[SerializeField]
	private AudioSource announceBellAudioSource;

	public AbilitySound announcePrepareShift;

	public AbilitySound announceStartShift;

	public AbilitySound announceCompleteShift;

	public AbilitySound announceFailShift;

	public AbilitySound announcePrepareDrill;

	public AbilitySound announceTip;

	public AbilitySound announceBell;

	[Header("Warning")]
	public List<WarningPres> warnings;

	[SerializeField]
	private AudioClip warningAudio;

	[SerializeField]
	[Tooltip("Must be ordered from largest time (first played) to smallest time (last played)")]
	private List<int> warningClipPlayTimes = new List<int>();

	[Header("Ring")]
	[SerializeField]
	private Transform ringTransform;

	[SerializeField]
	private float ringClosingDuration = 3f;

	[SerializeField]
	private float ringClosingMaxRadius = 100f;

	[SerializeField]
	private float ringClosingMinRadius = 7f;

	[Header("Debug")]
	[SerializeField]
	private float debugFastForwardRate = 30f;

	[SerializeField]
	private bool debugFastForwarding;

	private bool shiftStarted;

	private bool shiftJustStarted;

	private double shiftStartNetworkTime;

	private double shiftEndNetworkTime;

	private float prevCountDownTotal;

	[SerializeField]
	private int shiftTotalEarned = -1;

	[SerializeField]
	private int shiftSanityMaximumEarned = 10000;

	public GhostReactorShiftDepthDisplay depthDisplay;

	public bool authorizedToDelveDeeper;

	public int shiftRewardCoresForMothership;

	public int coresRequiredToDelveDeeper;

	public int sentientCoresRequiredToDelveDeeper;

	public List<GREnemyCount> killsRequiredToDelveDeeper;

	public int maxPlayerDeaths;

	public int shiftRewardCredits;

	private bool localPlayerInside;

	private bool localPlayerOverlapping;

	private float totalPlayTime;

	private string gameIdGuid = "";

	public GRShiftStat shiftStats = new GRShiftStat();

	[NonSerialized]
	private GhostReactorManager grManager;

	[SerializeField]
	private TMP_Text shiftLeaderboardEfficiency;

	[SerializeField]
	private TMP_Text shiftLeaderboardSafety;

	private double lastLeaderboardRefreshTime;

	private float leaderboardUpdateFrequency = 0.5f;

	public double stateStartTime;

	private double lastReactorLogoAnimationTime;

	private int lastReactorLogoAnimFrame;

	private bool isPlayingLogoAnimation;

	private double lastReactorDisplayUpdate;

	private StringBuilder cachedStringBuilder = new StringBuilder(256);

	private bool nextRefreshLeaderboardSafety;

	private StringBuilder leaderboardDisplay = new StringBuilder(1024);

	public int ShiftTotalEarned => shiftTotalEarned;

	public bool ShiftActive => shiftStarted;

	public double ShiftStartNetworkTime => shiftStartNetworkTime;

	public bool LocalPlayerInside => localPlayerInside;

	public float TotalPlayTime => totalPlayTime;

	public string ShiftId => gameIdGuid;

	public State ShiftState { get; private set; }

	public void SetShiftId(string shiftId)
	{
		gameIdGuid = shiftId;
	}

	public void Init(GhostReactorManager grManager)
	{
		this.grManager = grManager;
		SetState(State.WaitingForConnect, force: true);
		depthDisplay.Setup();
	}

	public void RefreshShiftStatsDisplay()
	{
		shiftStatsText.text = "\n\n" + shiftStats.GetShiftStat(GRShiftStatType.EnemyDeaths).ToString("D2") + "\n" + shiftStats.GetShiftStat(GRShiftStatType.CoresCollected).ToString("D2") + "\n" + shiftStats.GetShiftStat(GRShiftStatType.SentientCoresCollected).ToString("D2") + "\n" + shiftStats.GetShiftStat(GRShiftStatType.PlayerDeaths).ToString("D2");
		depthDisplay.RefreshObjectives();
	}

	public void StartShiftButtonPressed()
	{
		RequestShiftStart();
	}

	public void RequestShiftStart()
	{
	}

	public void EndShift()
	{
		grManager.SendRequestShiftEndRPC();
	}

	public void ClearEntities()
	{
		Debug.LogError("Need to re-implement whatever this was doing");
	}

	public void RefreshShiftTimer()
	{
		if (shiftTimerText != null)
		{
			shiftTimerText.text = Mathf.FloorToInt(shiftDurationMinutes).ToString("D2") + ":00";
		}
	}

	public void UpdateLogoAnimations(List<TMP_Text> frames)
	{
		float num = 300f;
		float num2 = 0.5f;
		double time = PhotonNetwork.Time;
		if (frames.Count < 4)
		{
			return;
		}
		if (lastReactorLogoAnimationTime + (double)num < time || time < lastReactorLogoAnimationTime)
		{
			isPlayingLogoAnimation = true;
			lastReactorLogoAnimationTime = time;
		}
		if (isPlayingLogoAnimation)
		{
			if (lastReactorLogoAnimationTime + (double)num2 < time)
			{
				isPlayingLogoAnimation = false;
			}
			float f = Mathf.Clamp01((float)(time - lastReactorLogoAnimationTime) / num2) * 3.1415925f;
			int num3 = (int)(3.5f - Mathf.Abs(Mathf.Cos(f) * 3f));
			if (!isPlayingLogoAnimation)
			{
				num3 = 0;
			}
			if (lastReactorLogoAnimFrame != num3)
			{
				frames[lastReactorLogoAnimFrame].gameObject.SetActive(value: false);
				frames[num3].gameObject.SetActive(value: true);
				lastReactorLogoAnimFrame = num3;
			}
		}
	}

	public void UpdateReactorDisplayMainShared(float countDownTotal)
	{
		if (reactorTextMain == null)
		{
			return;
		}
		double time = PhotonNetwork.Time;
		float num = 0.5f;
		if (lastReactorDisplayUpdate < time && lastReactorDisplayUpdate + (double)num > time)
		{
			return;
		}
		lastReactorDisplayUpdate = time;
		cachedStringBuilder.Clear();
		int num2 = Mathf.FloorToInt(countDownTotal / 60f);
		int num3 = Mathf.FloorToInt(countDownTotal % 60f);
		switch (ShiftState)
		{
		case State.WaitingForShiftStart:
		case State.WaitingForFirstShiftStart:
			cachedStringBuilder.AppendLine($"DEPTH {reactor.GetDepthLevel() * 1000 + 1000}m");
			cachedStringBuilder.AppendLine("STAND BY");
			depthDisplay.jumbotronTitle.text = $"<size=1>CURRENT DEPTH</size>\n{reactor.GetDepthLevel() * 1000 + 1000}m";
			break;
		case State.PreparingToDrill:
			cachedStringBuilder.AppendLine($"DEPTH {reactor.GetDepthLevel() * 1000}m");
			cachedStringBuilder.AppendLine("STAND BY");
			depthDisplay.jumbotronTitle.text = $"<size=1>CURRENT DEPTH</size>\n{reactor.GetDepthLevel() * 1000 + 1000}m";
			break;
		case State.Drilling:
		{
			int num8 = (int)((time - stateStartTime) / (double)GetDrillingDuration() * 1000.0);
			cachedStringBuilder.AppendLine($"DEPTH {reactor.GetDepthLevel() * 1000 + num8}m");
			cachedStringBuilder.AppendLine("DRILLING");
			depthDisplay.jumbotronTitle.text = $"<size=1>CURRENT DEPTH</size>\n{reactor.GetDepthLevel() * 1000 + num8}m";
			break;
		}
		case State.ShiftActive:
		{
			int shiftStat = shiftStats.GetShiftStat(GRShiftStatType.CoresCollected);
			int num4 = coresRequiredToDelveDeeper;
			depthDisplay.jumbotronTitle.text = $"<size=1>CURRENT DEPTH</size>\n{reactor.GetDepthLevel() * 1000 + 1000}m";
			cachedStringBuilder.AppendLine($"DEPTH {reactor.GetDepthLevel() * 1000 + 1000}m");
			cachedStringBuilder.AppendLine("ANOMALY COLLAPSE IN " + num2.ToString("D2") + ":" + num3.ToString("D2"));
			if (shiftStat >= num4)
			{
				cachedStringBuilder.Append("\nPOWER REQUIREMENTS MET\n");
			}
			else
			{
				cachedStringBuilder.Append($"\nCORES REQUIRED ({shiftStat}/{num4})\n");
			}
			int num5 = (int)((float)shiftStat / (float)num4 * 30f);
			if (shiftStat > 1 && num5 == 0)
			{
				num5 = 1;
			}
			int num6 = num5 / 3;
			int num7 = num5 - num6 * 3;
			for (int i = 0; i < 10; i++)
			{
				if (i < num6)
				{
					cachedStringBuilder.Append("▐█");
					continue;
				}
				if (i <= num6)
				{
					switch (num7)
					{
					case 0:
						break;
					case 1:
						cachedStringBuilder.Append("▐░");
						continue;
					default:
						cachedStringBuilder.Append("▐▌");
						continue;
					}
				}
				cachedStringBuilder.Append(" ░");
			}
			cachedStringBuilder.Append("\n");
			if (shiftStat > 0)
			{
				cachedStringBuilder.Append($"\nTOTAL BONUS EARNED: +⑭{shiftStat * 5}");
			}
			break;
		}
		}
		reactorTextMain.text = cachedStringBuilder.ToString();
	}

	public void OnShiftStarted(string gameId, double shiftStartTime, bool wasPlayerInAtStart, bool isFirstShift)
	{
		gameIdGuid = gameId;
		GRPlayer gRPlayer = GRPlayer.Get(VRRig.LocalRig);
		if (!shiftStarted && gRPlayer != null)
		{
			float num = (float)(PhotonNetwork.Time - shiftStartTime);
			gRPlayer.ResetTelemetryTracking(gameIdGuid, num);
			gRPlayer.IncrementShiftsPlayed(1);
			gRPlayer.SendFloorStartedTelemetry(num, wasPlayerInAtStart, reactor.GetDepthLevel(), reactor.GetCurrLevelGenConfig().name, "");
			if (gRPlayer.isFirstShift)
			{
				gRPlayer.SendGameStartedTelemetry(num, wasPlayerInAtStart, reactor.GetDepthLevel());
				gRPlayer.gameStartTime = (float)PhotonNetwork.Time;
			}
		}
		shiftStarted = true;
		shiftJustStarted = true;
		shiftStartNetworkTime = shiftStartTime;
		frontGate.OpenGate();
		ringTransform.gameObject.SetActive(value: false);
		anomalyLoop1.Stop();
		anomalyLoop2.Stop();
		anomalyLoop3.Stop();
		anomalyAlert.Stop();
		gateBlockerTransform.gameObject.SetActive(value: false);
		prevCountDownTotal = shiftDurationMinutes * 60f;
		shiftTotalEarned = -1;
		authorizedToDelveDeeper = false;
		ResetJoinTimes();
		reactor.RefreshScoreboards();
		reactor.RefreshDepth();
		isRoomClosed = false;
		if (gRPlayer != null)
		{
			gRPlayer.RefreshPlayerVisuals();
		}
	}

	public void OnShiftEnded(double shiftEndTime, bool isShiftActuallyEnding, ZoneClearReason zoneClearReason = ZoneClearReason.JoinZone)
	{
		if (shiftStarted)
		{
			GRPlayer component = VRRig.LocalRig.GetComponent<GRPlayer>();
			if (component != null)
			{
				component.SendFloorEndedTelemetry(isShiftActuallyEnding, (float)shiftStartNetworkTime, zoneClearReason, reactor.GetDepthLevel(), reactor.GetCurrLevelGenConfig().name, "", authorizedToDelveDeeper, ((reactor.GetDepthLevel() + 1) / 5).ToString(), authorizedToDelveDeeper ? (10 * reactor.GetDepthLevel()) : 0);
			}
		}
		shiftStarted = false;
		shiftEndNetworkTime = shiftEndTime;
		RefreshShiftTimer();
		frontGate.CloseGate();
		ringTransform.gameObject.SetActive(value: false);
		anomalyLoop1.Stop();
		anomalyLoop2.Stop();
		anomalyLoop3.Stop();
		anomalyAlert.Stop();
		TeleportLocalPlayerIfOutOfBounds();
		if (shiftEndNetworkTime > 0.0 && shiftStats.GetShiftStat(GRShiftStatType.EnemyDeaths) > shiftStats.GetShiftStat(GRShiftStatType.PlayerDeaths))
		{
			PlayerGameEvents.MiscEvent("GRShiftGoodKD");
		}
		if (PhotonNetwork.InRoom && !NetworkSystem.Instance.SessionIsPrivate && grManager.IsAuthority())
		{
			Hashtable hashtable = new Hashtable();
			hashtable.Add("ghostReactorShiftStarted", "false");
			PhotonNetwork.CurrentRoom.SetCustomProperties(hashtable);
			isRoomClosed = false;
		}
	}

	public override void Tick()
	{
		if (!(grManager == null))
		{
			double num = PhotonNetwork.Time - shiftStartNetworkTime;
			float num2 = 60f * shiftDurationMinutes - (float)num;
			if (grManager.IsAuthority())
			{
				AuthorityUpdate(num2);
			}
			num2 = Mathf.Clamp(num2, 0f, 60f * shiftDurationMinutes);
			SharedUpdate(num2);
			prevCountDownTotal = num2;
		}
	}

	private void AuthorityUpdate(float countDownTotal)
	{
		if (PhotonNetwork.InRoom && grManager.IsAuthority())
		{
			if (shiftStarted && !NetworkSystem.Instance.SessionIsPrivate && !isRoomClosed && 60f * shiftDurationMinutes - countDownTotal >= roomCloseTimeSeconds)
			{
				Hashtable hashtable = new Hashtable();
				hashtable.Add("ghostReactorShiftStarted", "true");
				PhotonNetwork.CurrentRoom.SetCustomProperties(hashtable);
				isRoomClosed = true;
			}
			if (shiftStarted && countDownTotal <= 0f)
			{
				grManager.RequestShiftEnd();
			}
			UpdateStateAuthority();
		}
	}

	private void SharedUpdate(float countDownTotal)
	{
		UpdateStateShared();
		UpdateReactorDisplayMainShared(countDownTotal);
		if (lastLeaderboardRefreshTime + (double)leaderboardUpdateFrequency < (double)Time.time || (double)Time.time < lastLeaderboardRefreshTime)
		{
			RefreshShiftLeaderboard();
			lastLeaderboardRefreshTime = Time.time;
		}
		if (shiftStarted)
		{
			if (debugFastForwarding)
			{
				float num = debugFastForwardRate * Time.deltaTime;
				shiftStartNetworkTime -= num;
			}
			int num2 = Mathf.FloorToInt(countDownTotal / 60f);
			int num3 = Mathf.FloorToInt(countDownTotal % 60f);
			shiftTimerText.text = num2.ToString("D2") + ":" + num3.ToString("D2");
			for (int i = 0; i < warnings.Count; i++)
			{
				if (countDownTotal < (float)warnings[i].time && prevCountDownTotal >= (float)warnings[i].time && !shiftJustStarted)
				{
					warnings[i].sound.Play(announceAudioSource);
					break;
				}
			}
			if (ShiftState == State.ShiftActive && countDownTotal > 0f && countDownTotal < anomalyAlertCountdownTimeToStartPlayingInMinutes * 60f && !anomalyAlert.isPlaying)
			{
				anomalyAlert.Play();
			}
			if (localPlayerInside)
			{
				if (countDownTotal >= 0f && countDownTotal < ringClosingDuration * 60f)
				{
					ringTransform.gameObject.SetActive(value: true);
					float num4 = Mathf.Lerp(ringClosingMinRadius, ringClosingMaxRadius, countDownTotal / (ringClosingDuration * 60f));
					ringTransform.localScale = new Vector3(num4, 1f, num4);
					Vector3 position = VRRig.LocalRig.bodyTransform.position;
					Vector3 vector = position - ringTransform.position;
					vector.y = 0f;
					Vector3 normalized = vector.normalized;
					Vector3 position2 = ringTransform.position + normalized * num4;
					Quaternion quaternion = Quaternion.AngleAxis(MathF.PI / 6f, Vector3.up);
					Quaternion quaternion2 = Quaternion.AngleAxis(0f - MathF.PI / 6f, Vector3.up);
					Vector3 position3 = ringTransform.position + quaternion * normalized * num4;
					Vector3 position4 = ringTransform.position + quaternion2 * normalized * num4;
					position2.y = position.y;
					position3.y = position.y;
					position4.y = position.y;
					anomalyLoop1.transform.position = position2;
					anomalyLoop2.transform.position = position3;
					anomalyLoop3.transform.position = position4;
					if (!anomalyLoop1.isPlaying)
					{
						anomalyLoop1.Play();
					}
					if (!anomalyLoop2.isPlaying)
					{
						anomalyLoop2.Play();
					}
					if (!anomalyLoop3.isPlaying)
					{
						anomalyLoop3.Play();
					}
					if (vector.sqrMagnitude > num4 * num4)
					{
						TeleportLocalPlayerIfOutOfBounds();
					}
				}
			}
			else if (ringTransform.gameObject.activeSelf)
			{
				ringTransform.gameObject.SetActive(value: false);
			}
			shiftJustStarted = false;
		}
		else if (!shiftStarted)
		{
			TeleportLocalPlayerIfOutOfBounds();
		}
	}

	private void TeleportLocalPlayerIfOutOfBounds()
	{
		if (localPlayerInside || (localPlayerOverlapping && Vector3.Dot(GTPlayer.Instance.headCollider.transform.position - gatePlaneTransform.position, gatePlaneTransform.forward) < 0f))
		{
			grManager.ReportLocalPlayerHit();
			GRPlayer component = VRRig.LocalRig.GetComponent<GRPlayer>();
			component.ChangePlayerState(GRPlayer.GRPlayerState.Ghost, grManager);
			GTPlayer.Instance.TeleportTo(playerTeleportTransform);
			localPlayerInside = false;
			localPlayerOverlapping = false;
			component.caughtByAnomaly = true;
		}
	}

	public void RevealJudgment(int evaluation)
	{
		if (evaluation <= 0)
		{
			shiftJugmentText.text = "DON'T QUIT YOUR DAY JOB.";
			return;
		}
		switch (evaluation)
		{
		case 1:
			shiftJugmentText.text = "YOU'RE LEARNING. GOOD.";
			return;
		case 2:
			shiftJugmentText.text = "YOU MIGHT EARN A PROMOTION.";
			return;
		case 3:
			shiftJugmentText.text = "YOU DID A MANAGER-TIER JOB.";
			return;
		case 4:
			shiftJugmentText.text = "NICE. YOU GET EXTRA SHIFTS.";
			return;
		}
		shiftJugmentText.text = "YOU WORK FOR US NOW.";
		if (wrongStumpGoo != null)
		{
			wrongStumpGoo.SetActive(value: true);
		}
	}

	public void ResetJudgment()
	{
		shiftJugmentText.text = "";
		if (wrongStumpGoo != null)
		{
			wrongStumpGoo.SetActive(value: false);
		}
	}

	public void ResetJoinTimes()
	{
		int count = reactor.vrRigs.Count;
		totalPlayTime = 0f;
		for (int i = 0; i < count; i++)
		{
			GRPlayer.Get(reactor.vrRigs[i]).shiftJoinTime = shiftStartNetworkTime;
		}
	}

	public void CalculatePlayerPercentages()
	{
		int count = reactor.vrRigs.Count;
		totalPlayTime = 0f;
		for (int i = 0; i < count; i++)
		{
			GRPlayer gRPlayer = GRPlayer.Get(reactor.vrRigs[i]);
			if (reactor.vrRigs[i] != null && gRPlayer != null)
			{
				if (reactor.vrRigs[i].OwningNetPlayer == null)
				{
					gRPlayer.ShiftPlayTime = 0.1f;
				}
				else if (shiftStarted)
				{
					gRPlayer.ShiftPlayTime = Mathf.Min(shiftDurationMinutes * 60f, (float)(PhotonNetwork.Time - gRPlayer.shiftJoinTime));
				}
				else
				{
					gRPlayer.ShiftPlayTime = Mathf.Min(shiftDurationMinutes * 60f, (float)(shiftEndNetworkTime - gRPlayer.shiftJoinTime));
				}
				totalPlayTime += gRPlayer.ShiftPlayTime;
			}
		}
	}

	public void CalculateShiftTotal()
	{
		shiftTotalEarned = 0;
		int count = reactor.vrRigs.Count;
		double num = 0.0;
		for (int i = 0; i < count; i++)
		{
			GRPlayer gRPlayer = GRPlayer.Get(reactor.vrRigs[i]);
			if (reactor.vrRigs[i] != null && gRPlayer != null)
			{
				shiftTotalEarned += gRPlayer.ShiftCredits;
				if (reactor.vrRigs[i].OwningNetPlayer == null)
				{
					gRPlayer.ShiftPlayTime = 0.1f;
				}
				else
				{
					gRPlayer.ShiftPlayTime = Mathf.Min(shiftDurationMinutes * 60f, (float)(PhotonNetwork.Time - gRPlayer.shiftJoinTime));
				}
				num += (double)gRPlayer.ShiftPlayTime;
			}
		}
		shiftTotalEarned = Mathf.Clamp(shiftTotalEarned, 0, shiftSanityMaximumEarned);
		num = Mathf.Clamp((float)num, 0.1f, shiftDurationMinutes * 10f * 60f);
		for (int j = 0; j < count; j++)
		{
			GRPlayer gRPlayer2 = GRPlayer.Get(reactor.vrRigs[j]);
			if (reactor.vrRigs[j] != null && gRPlayer2 != null && depthDisplay != null)
			{
				int rewardXP = depthDisplay.GetRewardXP();
				if (authorizedToDelveDeeper)
				{
					gRPlayer2.LastShiftCut = rewardXP;
					gRPlayer2.CollectShiftCut();
				}
			}
		}
		reactor.RefreshScoreboards();
		reactor.promotionBot.Refresh();
		reactor.RefreshDepth();
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other == GTPlayer.Instance.headCollider)
		{
			localPlayerOverlapping = true;
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (other == GTPlayer.Instance.headCollider)
		{
			bool flag = Vector3.Dot(other.transform.position - gatePlaneTransform.position, gatePlaneTransform.forward) < 0f;
			localPlayerInside = flag;
			localPlayerOverlapping = false;
		}
	}

	public void OnButtonDelveDeeper()
	{
		if (ShiftActive)
		{
			_ = authorizedToDelveDeeper;
		}
	}

	public void OnButtonDEBUGResetDepth()
	{
		grManager.RequestPlayerAction(GhostReactorManager.GRPlayerAction.DEBUG_ResetDepth);
	}

	public void OnButtonDEBUGDelveDeeper()
	{
		grManager.RequestPlayerAction(GhostReactorManager.GRPlayerAction.DEBUG_DelveDeeper);
	}

	public void OnButtonDEBUGDelveShallower()
	{
		grManager.RequestPlayerAction(GhostReactorManager.GRPlayerAction.DEBUG_DelveShallower);
	}

	public void RequestState(State newState)
	{
		if (grManager.IsAuthority())
		{
			grManager.RequestPlayerAction(GhostReactorManager.GRPlayerAction.DelveState, (int)newState);
		}
	}

	public void SetState(State newState, bool force = false)
	{
		if (ShiftState == newState && !force)
		{
			return;
		}
		switch (ShiftState)
		{
		case State.ReadyForShift:
			if (startShiftButton != null)
			{
				startShiftButton.SetActive(value: false);
			}
			break;
		case State.Drilling:
			reactor.shiftManager.depthDisplay.StopDelveDeeperFX();
			break;
		}
		ShiftState = newState;
		stateStartTime = PhotonNetwork.Time;
		switch (ShiftState)
		{
		case State.ShiftActive:
			announceStartShift.Play(announceAudioSource);
			foreach (VRRig vrRig in reactor.vrRigs)
			{
				GRPlayer component = vrRig.GetComponent<GRPlayer>();
				if (component != null)
				{
					component.startingShiftCreditCache = component.ShiftCredits;
				}
			}
			break;
		case State.WaitingForFirstShiftStart:
			announceBell.Play(announceBellAudioSource);
			announceTip.Play(announceAudioSource);
			break;
		case State.WaitingForShiftStart:
			announceBell.Play(announceBellAudioSource);
			announceTip.Play(announceAudioSource);
			break;
		case State.PostShift:
			if (authorizedToDelveDeeper)
			{
				announceCompleteShift.Play(announceAudioSource);
				if (!string.IsNullOrEmpty(ShiftId))
				{
					ProgressionManager.Instance.EndOfShiftReward(ShiftId);
					int count = reactor.vrRigs.Count;
					for (int i = 0; i < count; i++)
					{
						GRPlayer gRPlayer = GRPlayer.Get(reactor.vrRigs[i]);
						if (gRPlayer != null)
						{
							gRPlayer.IncrementSynchronizedSessionStat(GRPlayer.SynchronizedSessionStat.EarnedCredits, shiftRewardCredits);
						}
					}
				}
				Debug.LogError("ShiftId is null or empty, skipping reward of end of shift.");
			}
			else
			{
				announceFailShift.Play(announceAudioSource);
			}
			break;
		case State.PreparingToDrill:
			announcePrepareDrill.Play(announceAudioSource);
			break;
		case State.Drilling:
			reactor.DelveToNextDepth();
			reactor.shiftManager.depthDisplay.StartDelveDeeperFX();
			break;
		}
		RefreshDepthDisplay();
	}

	public State GetState()
	{
		return ShiftState;
	}

	public bool IsSoaking()
	{
		if (GhostReactorSoak.instance != null)
		{
			return GhostReactorSoak.instance.IsSoaking();
		}
		return false;
	}

	private int GetPreShiftDuration()
	{
		if (IsSoaking())
		{
			return 5;
		}
		return preShiftDuration;
	}

	private int GetPreShiftDurationFirstArrive()
	{
		if (IsSoaking())
		{
			return 5;
		}
		return preShiftDurationFirstArrive;
	}

	private int GetPostShiftDuration()
	{
		if (IsSoaking())
		{
			return 5;
		}
		return postShiftDuration;
	}

	private int GetPreparingToDrillDuration()
	{
		IsSoaking();
		return 5;
	}

	public int GetDrillingDuration()
	{
		if (IsSoaking())
		{
			return 5;
		}
		return drillDuration;
	}

	private void UpdateStateAuthority()
	{
		if (!grManager.IsAuthority())
		{
			return;
		}
		double time = PhotonNetwork.Time;
		switch (ShiftState)
		{
		case State.WaitingForConnect:
			if (reactor.grManager.IsZoneReady())
			{
				RequestState(State.WaitingForFirstShiftStart);
			}
			break;
		case State.WaitingForFirstShiftStart:
			if (time - stateStartTime > (double)GetPreShiftDurationFirstArrive())
			{
				reactor.grManager.RequestShiftStartAuthority(isFirstShift: true);
			}
			break;
		case State.WaitingForShiftStart:
			if (time - stateStartTime > (double)GetPreShiftDuration())
			{
				reactor.grManager.RequestShiftStartAuthority(isFirstShift: false);
			}
			break;
		case State.PostShift:
			if (time - stateStartTime > (double)GetPostShiftDuration())
			{
				if (authorizedToDelveDeeper)
				{
					reactor.grManager.RequestPlayerAction(GhostReactorManager.GRPlayerAction.DelveDeeper);
					RequestState(State.PreparingToDrill);
				}
				else
				{
					RequestState(State.WaitingForShiftStart);
				}
			}
			break;
		case State.PreparingToDrill:
			if (time - stateStartTime > (double)GetPreparingToDrillDuration())
			{
				RequestState(State.Drilling);
			}
			break;
		case State.Drilling:
			if (time - stateStartTime > (double)GetDrillingDuration())
			{
				RequestState(State.WaitingForShiftStart);
			}
			break;
		case State.ReadyForShift:
		case State.ShiftActive:
			break;
		}
	}

	private void UpdateStateShared()
	{
		double time = PhotonNetwork.Time;
		switch (ShiftState)
		{
		case State.WaitingForFirstShiftStart:
		{
			int b5 = GetPreShiftDurationFirstArrive() - Mathf.FloorToInt((float)(time - stateStartTime));
			b5 = Mathf.Max(0, b5);
			shiftTimerText.text = ":" + b5.ToString("D2");
			break;
		}
		case State.WaitingForShiftStart:
		{
			int b4 = GetPreShiftDuration() - Mathf.FloorToInt((float)(time - stateStartTime));
			b4 = Mathf.Max(0, b4);
			shiftTimerText.text = ":" + b4.ToString("D2");
			break;
		}
		case State.PostShift:
		{
			int b3 = GetPostShiftDuration() - Mathf.FloorToInt((float)(time - stateStartTime));
			b3 = Mathf.Max(0, b3);
			shiftTimerText.text = ":" + b3.ToString("D2");
			break;
		}
		case State.PreparingToDrill:
		{
			int b2 = 5 - Mathf.FloorToInt((float)(time - stateStartTime));
			b2 = Mathf.Max(0, b2);
			shiftTimerText.text = ":" + b2.ToString("D2");
			break;
		}
		case State.Drilling:
		{
			int b = GetDrillingDuration() - Mathf.FloorToInt((float)(time - stateStartTime));
			b = Mathf.Max(0, b);
			shiftTimerText.text = ":" + b.ToString("D2");
			UpdateLogoAnimations(depthDisplay.logoFrames);
			break;
		}
		case State.ReadyForShift:
		case State.ShiftActive:
			break;
		}
	}

	public void RefreshDepthDisplay()
	{
		GhostReactorLevelGenConfig currLevelGenConfig = reactor.GetCurrLevelGenConfig();
		int num = reactor.GetDepthLevel() + 1;
		int num2 = num / 4 + 1 + ((num % 5 == 4) ? 2 : 0);
		shiftRewardCoresForMothership = currLevelGenConfig.coresRequired + num2;
		coresRequiredToDelveDeeper = ((currLevelGenConfig.coresRequired > 0) ? ((int)(reactor.difficultyScalingForCurrentFloor * (float)currLevelGenConfig.coresRequired) + num2) : 0);
		killsRequiredToDelveDeeper = currLevelGenConfig.minEnemyKills;
		shiftRewardCredits = currLevelGenConfig.coresRequired * 5;
		sentientCoresRequiredToDelveDeeper = (int)(reactor.difficultyScalingForCurrentFloor * (float)currLevelGenConfig.sentientCoresRequired);
		shiftDurationMinutes = currLevelGenConfig.shiftDuration / 60;
		if (IsSoaking())
		{
			shiftDurationMinutes = UnityEngine.Random.Range(1, 3);
		}
		maxPlayerDeaths = currLevelGenConfig.maxPlayerDeaths;
		if (depthDisplay != null)
		{
			depthDisplay.RefreshDisplay();
		}
		RefreshShiftTimer();
	}

	public void RefreshShiftLeaderboard()
	{
		if (nextRefreshLeaderboardSafety)
		{
			RefreshShiftLeaderboard_Safety();
		}
		else
		{
			RefreshShiftLeaderboard_Efficiency();
		}
		nextRefreshLeaderboardSafety = !nextRefreshLeaderboardSafety;
	}

	public void RefreshShiftLeaderboard_Safety()
	{
		if (shiftLeaderboardSafety == null)
		{
			return;
		}
		int count = reactor.vrRigs.Count;
		totalPlayTime = 0f;
		leaderboardDisplay.Clear();
		leaderboardDisplay.Append("<color=#c0c0c0c0><size=-0.4>SAFETY          GHOSTS   WORKPLACE  TEAM    CHAOS\nREPORT          BANISHED INCIDENTS  ASSISTS EXPOSURE\n----------------------------------------------------</size></color>\n");
		for (int i = 0; i < count; i++)
		{
			GRPlayer gRPlayer = GRPlayer.Get(reactor.vrRigs[i]);
			if (!(reactor.vrRigs[i] == null) && !(gRPlayer == null) && !(gRPlayer.gamePlayer == null))
			{
				string playerNameVisible = gRPlayer.gamePlayer.rig.playerNameVisible;
				int num = (int)gRPlayer.synchronizedSessionStats[4];
				int num2 = (int)gRPlayer.synchronizedSessionStats[5];
				int num3 = (int)gRPlayer.synchronizedSessionStats[6];
				float num4 = gRPlayer.synchronizedSessionStats[7];
				int num5 = (int)num4 / 60;
				int num6 = (int)num4 % 60;
				leaderboardDisplay.Append((i % 2 == 0) ? "<color=#e0e0ff>" : "<color=#a0a0ff>");
				leaderboardDisplay.Append($"{playerNameVisible,-12}{num2,5}{num,7}{num3,7}{$"{num5,3}:{num6:00}",10}");
				leaderboardDisplay.Append("</color>\n");
			}
		}
		shiftLeaderboardSafety.text = leaderboardDisplay.ToString();
	}

	public void RefreshShiftLeaderboard_Efficiency()
	{
		if (shiftLeaderboardEfficiency == null)
		{
			return;
		}
		int count = reactor.vrRigs.Count;
		totalPlayTime = 0f;
		leaderboardDisplay.Clear();
		leaderboardDisplay.Append("<color=#c0c0c0c0><size=-0.4>KEY PERFORMANCE   CORES   EARNED   SPENT    DISTANCE\nINDICATORS        FOUND   CREDITS  CREDITS  TRAVELED\n----------------------------------------------------</size></color>\n");
		for (int i = 0; i < count; i++)
		{
			GRPlayer gRPlayer = GRPlayer.Get(reactor.vrRigs[i]);
			if (!(reactor.vrRigs[i] == null) && !(gRPlayer == null) && !(gRPlayer.gamePlayer == null))
			{
				string playerNameVisible = gRPlayer.gamePlayer.rig.playerNameVisible;
				int num = (int)gRPlayer.synchronizedSessionStats[0];
				int num2 = (int)gRPlayer.synchronizedSessionStats[1];
				int num3 = (int)gRPlayer.synchronizedSessionStats[2];
				int num4 = (int)gRPlayer.synchronizedSessionStats[3];
				leaderboardDisplay.Append((i % 2 == 0) ? "<color=#e0e0ff>" : "<color=#a0a0ff>");
				leaderboardDisplay.Append($"{playerNameVisible,-12}{num,6}{num2,7}{num3,7}{num4,8}");
				leaderboardDisplay.Append("</color>\n");
			}
		}
		shiftLeaderboardEfficiency.text = leaderboardDisplay.ToString();
	}
}
