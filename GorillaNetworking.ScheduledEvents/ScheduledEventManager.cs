using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using PlayFab;
using UnityEngine;

namespace GorillaNetworking.ScheduledEvents;

[RequireComponent(typeof(PhotonView))]
public class ScheduledEventManager : MonoBehaviour, IInRoomCallbacks, IPunObservable
{
	private enum StartKind
	{
		Unresolved,
		NoEvent,
		Scheduled
	}

	public const int SCHEDULED_EVENT_MAX_DELAY_MINUTES = 5;

	public const int SCHEDULED_EVENT_GRACE_PERIOD_MINUTES = 15;

	public const int SCHEDULED_EVENT_SEEN_COOLDOWN_HOURS = 12;

	[Header("Schedule")]
	[SerializeField]
	[Tooltip("PlayFab Title Data key whose value parses as a DateTime (date + time of day). Empty = no event configured.")]
	private string titleDataKey;

	[SerializeField]
	[Tooltip("If true, ignore titleDataKey and use forceEventTime instead. For local testing without editing PlayFab Title Data.")]
	private bool useForcedEventTime;

	[SerializeField]
	[Tooltip("Event start time in LOCAL time (parsed via DateTime.Parse). Only used when useForcedEventTime is true. Example: 2026-04-21 14:30:00")]
	private string forceEventTime;

	private DateTime scheduledStartUtc = DateTime.MinValue;

	private bool scheduledStartKnown;

	private bool fetchInFlight;

	private StartKind startKind;

	private PhotonTimestamp scheduledStart;

	private string lastKnownState;

	private bool showEndedInRoom;

	private Coroutine graceEndWatcher;

	private ScheduledEventPhase currentPhase;

	private readonly HashSet<ScheduledEventControlledObject> registered = new HashSet<ScheduledEventControlledObject>();

	public static ScheduledEventManager Instance { get; private set; }

	public bool IsResolved => startKind != StartKind.Unresolved;

	public bool HasEvent => startKind == StartKind.Scheduled;

	public double SecondsUntilEventStart
	{
		get
		{
			if (!HasEvent)
			{
				return 0.0;
			}
			return Math.Max(0.0, PhotonTimestamp.Now.SecondsUntil(scheduledStart));
		}
	}

	public ScheduledEventPhase CurrentPhase => currentPhase;

	public event Action OnChanged;

	public event Action<ScheduledEventPhase> OnPhaseChanged;

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			UnityEngine.Object.Destroy(this);
		}
		else
		{
			Instance = this;
		}
	}

	private async void Start()
	{
		if (useForcedEventTime)
		{
			ApplyForcedEventTime();
		}
		else if (!string.IsNullOrEmpty(titleDataKey))
		{
			await FetchReferenceDate();
		}
		PhotonNetwork.AddCallbackTarget(this);
		if (NetworkSystem.Instance != null)
		{
			NetworkSystem.Instance.OnMultiplayerStarted += new Action(OnMultiplayerStarted);
			NetworkSystem.Instance.OnReturnedToSinglePlayer += new Action(OnReturnedToSinglePlayer);
			if (NetworkSystem.Instance.InRoom)
			{
				OnMultiplayerStarted();
			}
		}
		if (GorillaComputer.instance != null)
		{
			GorillaComputer instance = GorillaComputer.instance;
			instance.OnServerTimeUpdated = (Action)Delegate.Combine(instance.OnServerTimeUpdated, new Action(OnServerTimeUpdated));
		}
	}

	private void OnDestroy()
	{
		PhotonNetwork.RemoveCallbackTarget(this);
		if (NetworkSystem.Instance != null)
		{
			NetworkSystem.Instance.OnMultiplayerStarted -= new Action(OnMultiplayerStarted);
			NetworkSystem.Instance.OnReturnedToSinglePlayer -= new Action(OnReturnedToSinglePlayer);
		}
		StopGraceEndWatcher();
		if (GorillaComputer.instance != null)
		{
			GorillaComputer instance = GorillaComputer.instance;
			instance.OnServerTimeUpdated = (Action)Delegate.Remove(instance.OnServerTimeUpdated, new Action(OnServerTimeUpdated));
		}
		if (Instance == this)
		{
			Instance = null;
		}
	}

	private void Update()
	{
		RefreshPhase();
	}

	public void Register(ScheduledEventControlledObject obj)
	{
		if (!(obj == null))
		{
			registered.Add(obj);
			ApplyPhaseTo(obj);
		}
	}

	public void Unregister(ScheduledEventControlledObject obj)
	{
		registered.Remove(obj);
	}

	private void ApplyPhaseTo(ScheduledEventControlledObject obj)
	{
		if (!(obj == null) && !(obj.gameObject == null))
		{
			bool flag = obj.MatchesPhase(currentPhase);
			if (obj.gameObject.activeSelf != flag)
			{
				obj.gameObject.SetActive(flag);
			}
		}
	}

	private void ApplyPhaseToAll()
	{
		foreach (ScheduledEventControlledObject item in registered)
		{
			ApplyPhaseTo(item);
		}
	}

	private void RefreshPhase()
	{
		ScheduledEventPhase scheduledEventPhase = ComputePhase();
		if (scheduledEventPhase != currentPhase)
		{
			if (scheduledEventPhase == ScheduledEventPhase.During && currentPhase == ScheduledEventPhase.Before && NetworkSystem.Instance != null && NetworkSystem.Instance.InRoom && NetworkSystem.Instance.IsMasterClient && lastKnownState == "regular")
			{
				SetRoomState("event-in-progress");
			}
			currentPhase = scheduledEventPhase;
			this.OnPhaseChanged?.Invoke(scheduledEventPhase);
			ApplyPhaseToAll();
		}
	}

	private ScheduledEventPhase ComputePhase()
	{
		if (showEndedInRoom)
		{
			return ScheduledEventPhase.After;
		}
		if (lastKnownState == "event-in-progress")
		{
			return ScheduledEventPhase.During;
		}
		if (HasEvent)
		{
			if (!(PhotonTimestamp.Now >= scheduledStart))
			{
				return ScheduledEventPhase.Before;
			}
			return ScheduledEventPhase.During;
		}
		if (!useForcedEventTime && string.IsNullOrEmpty(titleDataKey))
		{
			return ScheduledEventPhase.NoEvent;
		}
		if (IsResolved)
		{
			return ScheduledEventPhase.After;
		}
		return ScheduledEventPhase.Before;
	}

	private void OnServerTimeUpdated()
	{
		if (!useForcedEventTime && !string.IsNullOrEmpty(titleDataKey) && !fetchInFlight)
		{
			FetchReferenceDate();
		}
	}

	private void ApplyForcedEventTime()
	{
		if (string.IsNullOrEmpty(forceEventTime))
		{
			Debug.Log("ScheduledEventManager :: useForcedEventTime is true but forceEventTime is empty");
			return;
		}
		if (!DateTime.TryParse(forceEventTime, out var result))
		{
			Debug.Log("ScheduledEventManager :: could not parse forceEventTime '" + forceEventTime + "'");
			return;
		}
		scheduledStartUtc = ((result.Kind == DateTimeKind.Utc) ? result : DateTime.SpecifyKind(result, DateTimeKind.Local)).ToUniversalTime();
		scheduledStartKnown = true;
	}

	private async Task FetchReferenceDate()
	{
		fetchInFlight = true;
		while (PlayFabTitleDataCache.Instance == null)
		{
			await Task.Yield();
		}
		PlayFabTitleDataCache.Instance.GetTitleData(titleDataKey, OnTitleData, OnTitleDataError);
	}

	private void OnTitleData(string raw)
	{
		fetchInFlight = false;
		if (!DateTime.TryParse(raw, out var result))
		{
			Debug.Log("ScheduledEventManager :: could not parse title data '" + raw + "' for key " + titleDataKey);
			return;
		}
		scheduledStartUtc = ((result.Kind == DateTimeKind.Unspecified) ? DateTime.SpecifyKind(result, DateTimeKind.Utc) : result.ToUniversalTime());
		scheduledStartKnown = true;
	}

	private void OnTitleDataError(PlayFabError error)
	{
		fetchInFlight = false;
		Debug.Log($"ScheduledEventManager :: title data fetch failed: {error}");
	}

	public ScheduledEventInfo GetCurrent(DateTime serverNow)
	{
		if (!scheduledStartKnown)
		{
			return ScheduledEventInfo.None;
		}
		DateTime dateTime = scheduledStartUtc + TimeSpan.FromMinutes(15.0);
		if (serverNow >= dateTime)
		{
			return ScheduledEventInfo.None;
		}
		return new ScheduledEventInfo
		{
			isActive = true,
			scheduledStart = scheduledStartUtc
		};
	}

	private void OnMultiplayerStarted()
	{
		lastKnownState = ReadRoomState();
		showEndedInRoom = lastKnownState == "post-event";
		EnsureGraceEndWatcher();
		if (NetworkSystem.Instance.IsMasterClient && startKind == StartKind.Unresolved)
		{
			PhotonTimestamp? photonTimestamp = ComputeStartTime();
			if (photonTimestamp.HasValue)
			{
				SetStartState(StartKind.Scheduled, photonTimestamp.Value);
			}
			else
			{
				SetStartState(StartKind.NoEvent, default(PhotonTimestamp));
			}
		}
		RefreshPhase();
	}

	private void OnReturnedToSinglePlayer()
	{
		lastKnownState = null;
		showEndedInRoom = false;
		StopGraceEndWatcher();
		SetStartState(StartKind.Unresolved, default(PhotonTimestamp));
		RefreshPhase();
	}

	public void OnShowEnded()
	{
		if (NetworkSystem.Instance.InRoom && NetworkSystem.Instance.IsMasterClient)
		{
			SetRoomState("post-event");
		}
	}

	public void DebugStartCountdown()
	{
		if (NetworkSystem.Instance.InRoom && NetworkSystem.Instance.IsMasterClient)
		{
			showEndedInRoom = false;
			if (lastKnownState != "regular")
			{
				SetRoomState("regular");
			}
			SetStartState(StartKind.Scheduled, PhotonTimestamp.Now + 5.0);
		}
	}

	private string ReadRoomState()
	{
		if (PhotonNetwork.CurrentRoom == null)
		{
			return null;
		}
		if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("scheduledEventState", out var value))
		{
			return value as string;
		}
		return null;
	}

	private void SetRoomState(string state)
	{
		if (PhotonNetwork.CurrentRoom != null)
		{
			ExitGames.Client.Photon.Hashtable propertiesToSet = new ExitGames.Client.Photon.Hashtable { { "scheduledEventState", state } };
			PhotonNetwork.CurrentRoom.SetCustomProperties(propertiesToSet);
		}
	}

	void IInRoomCallbacks.OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
	{
		if (propertiesThatChanged.TryGetValue("scheduledEventState", out var value))
		{
			string text = lastKnownState;
			string text2 = (lastKnownState = value as string);
			if (text == "event-in-progress" && text2 == "post-event")
			{
				ScheduledEventMatchmaking.MarkSeenScheduledEventNow(GorillaComputer.instance.GetServerTime());
			}
			if (text2 == "post-event")
			{
				showEndedInRoom = true;
			}
			else if (text2 == "event-in-progress")
			{
				showEndedInRoom = false;
			}
			EnsureGraceEndWatcher();
			RefreshPhase();
		}
	}

	void IInRoomCallbacks.OnMasterClientSwitched(Player newMasterClient)
	{
		EnsureGraceEndWatcher();
	}

	void IInRoomCallbacks.OnPlayerEnteredRoom(Player newPlayer)
	{
	}

	void IInRoomCallbacks.OnPlayerLeftRoom(Player otherPlayer)
	{
	}

	void IInRoomCallbacks.OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
	{
	}

	private void EnsureGraceEndWatcher()
	{
	}

	private void StopGraceEndWatcher()
	{
		if (graceEndWatcher != null)
		{
			StopCoroutine(graceEndWatcher);
			graceEndWatcher = null;
		}
	}

	private IEnumerator GraceEndWatcherCoroutine()
	{
		WaitForSeconds wait = new WaitForSeconds(30f);
		while (true)
		{
			if (!NetworkSystem.Instance.InRoom || !NetworkSystem.Instance.IsMasterClient || lastKnownState != "post-event")
			{
				yield break;
			}
			DateTime serverTime = GorillaComputer.instance.GetServerTime();
			if (ScheduledEventMatchmaking.GracePeriodEnded(GetCurrent(serverTime), serverTime))
			{
				break;
			}
			yield return wait;
		}
		SetRoomState("regular");
	}

	private PhotonTimestamp? ComputeStartTime()
	{
		if (lastKnownState == "event-in-progress" || lastKnownState == "post-event")
		{
			return null;
		}
		DateTime serverTime = GorillaComputer.instance.GetServerTime();
		ScheduledEventInfo current = GetCurrent(serverTime);
		if (!current.isActive)
		{
			return null;
		}
		double totalSeconds = (current.scheduledStart - serverTime).TotalSeconds;
		double val = 300.0;
		double num = Math.Max(totalSeconds, val);
		return PhotonTimestamp.Now + num;
	}

	void IPunObservable.OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		if (stream.IsWriting)
		{
			stream.SendNext(startKind switch
			{
				StartKind.Unresolved => double.NaN, 
				StartKind.NoEvent => -1.0, 
				StartKind.Scheduled => scheduledStart.Value, 
				_ => double.NaN, 
			});
			return;
		}
		double num = (double)stream.ReceiveNext();
		if (double.IsNaN(num))
		{
			SetStartState(StartKind.Unresolved, default(PhotonTimestamp));
		}
		else if (num < 0.0)
		{
			SetStartState(StartKind.NoEvent, default(PhotonTimestamp));
		}
		else
		{
			SetStartState(StartKind.Scheduled, new PhotonTimestamp(num));
		}
	}

	private void SetStartState(StartKind kind, PhotonTimestamp ts)
	{
		if (kind != startKind || (kind == StartKind.Scheduled && ts.Value != scheduledStart.Value))
		{
			startKind = kind;
			scheduledStart = ts;
			this.OnChanged?.Invoke();
			RefreshPhase();
		}
	}
}
