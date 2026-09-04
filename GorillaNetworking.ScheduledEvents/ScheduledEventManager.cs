using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using PlayFab;
using UnityEngine;

namespace GorillaNetworking.ScheduledEvents;

[RequireComponent(typeof(PhotonView))]
public class ScheduledEventManager : MonoBehaviour, IGorillaSliceableSimple, IInRoomCallbacks, IPunObservable
{
	private enum StartKind
	{
		Unresolved,
		NoEvent,
		Scheduled
	}

	public const int SCHEDULED_EVENT_MAX_DELAY_MINUTES = 5;

	public const int SCHEDULED_EVENT_SEEN_COOLDOWN_HOURS = 12;

	[Header("Schedule")]
	[SerializeField]
	[Tooltip("PlayFab Title Data key whose value parses as a DateTime (date + time of day). Empty = no event configured.")]
	private string titleDataKey;

	[SerializeField]
	[Tooltip("PlayFab Title Data key whose value parses as an ElapsedTime (e.g. \"00:10:00\").")]
	private string graceTitleDataKey;

	[SerializeField]
	[Tooltip("If true, ignore titleDataKey and use forceEventTime instead. For local testing without editing PlayFab Title Data.")]
	private bool useForcedEventTime;

	[SerializeField]
	[Tooltip("Event start time in LOCAL time (parsed via DateTime.Parse). Only used when useForcedEventTime is true. Example: 2026-04-21 14:30:00")]
	private string forceEventTime;

	private DateTime scheduledStartUtc = DateTime.MinValue;

	private TimeSpan gracePeriodDuration = TimeSpan.FromMinutes(10.0);

	private bool scheduledStartKnown;

	private bool fetchInFlight;

	private StartKind startKind;

	private PhotonTimestamp scheduledStart;

	private string lastKnownState;

	private bool showEndedInRoom;

	private ScheduledEventPhase currentPhase;

	private int eventSubphase = -1;

	private readonly HashSet<ScheduledEventControlledObject> registered = new HashSet<ScheduledEventControlledObject>();

	public static ScheduledEventManager Instance { get; private set; }

	public TimeSpan GracePeriod => gracePeriodDuration;

	public bool DataReady => !fetchInFlight;

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
			return PhotonTimestamp.Now.SecondsUntil(scheduledStart);
		}
	}

	public ScheduledEventPhase CurrentPhase => currentPhase;

	public int EventSubphase => eventSubphase;

	public DateTime PreviousEventSubphaseStartTime { get; private set; }

	public DateTime EventSubphaseStartTime { get; private set; }

	public event Action OnChanged;

	public event Action<ScheduledEventPhase> OnPhaseChanged;

	public event Action<int> OnSubphaseChanged;

	public void SetEventSubphase(int subphase)
	{
		if (subphase != eventSubphase && (!PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient))
		{
			eventSubphase = subphase;
			PreviousEventSubphaseStartTime = EventSubphaseStartTime;
			EventSubphaseStartTime = DateTime.Now;
			this.OnSubphaseChanged?.Invoke(subphase);
		}
	}

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			UnityEngine.Object.Destroy(this);
			return;
		}
		Instance = this;
		currentPhase = ScheduledEventPhase.None;
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

	private void OnEnable()
	{
		if (Instance == this)
		{
			GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
		}
	}

	private void OnDisable()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	public void SliceUpdate()
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
		MaintainRoomStateAsMaster();
		ScheduledEventPhase scheduledEventPhase = ComputePhase();
		if (scheduledEventPhase != currentPhase)
		{
			if (scheduledEventPhase == ScheduledEventPhase.During && currentPhase == ScheduledEventPhase.Before && NetworkSystem.Instance != null && NetworkSystem.Instance.InRoom && NetworkSystem.Instance.IsMasterClient && lastKnownState == "regular")
			{
				SetRoomState("event-in-progress");
			}
			currentPhase = scheduledEventPhase;
			if (scheduledEventPhase == ScheduledEventPhase.During)
			{
				SetEventSubphase(0);
			}
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
		if (NetworkSystem.Instance != null && NetworkSystem.Instance.InRoom)
		{
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
		return ComputeOfflinePhase();
	}

	private ScheduledEventPhase ComputeOfflinePhase()
	{
		if (!useForcedEventTime && string.IsNullOrEmpty(titleDataKey))
		{
			return ScheduledEventPhase.NoEvent;
		}
		if (!scheduledStartKnown)
		{
			return ScheduledEventPhase.Before;
		}
		DateTime serverTime = GorillaComputer.instance.GetServerTime();
		ScheduledEventInfo current = GetCurrent(serverTime);
		bool creatorSeenRecently = ScheduledEventMatchmaking.HasSeenScheduledEventRecently(serverTime);
		if (ScheduledEventMatchmaking.ResolveCreateState(current, serverTime, creatorSeenRecently) == "post-event")
		{
			return ScheduledEventPhase.After;
		}
		if (!current.isActive)
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
		PlayFabTitleDataCache.Instance.GetTitleData(graceTitleDataKey, OnGraceTitleData, OnGraceTitleDataError);
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

	private void OnGraceTitleData(string raw)
	{
		fetchInFlight = false;
		if (!TimeSpan.TryParse(raw, out gracePeriodDuration))
		{
			Debug.Log("ScheduledEventManager :: could not parse time interval '" + raw + "' for key " + graceTitleDataKey);
		}
	}

	private void OnGraceTitleDataError(PlayFabError error)
	{
		fetchInFlight = false;
		Debug.Log($"ScheduledEventManager :: grace title data fetch failed: {error}");
		gracePeriodDuration = TimeSpan.FromMinutes(10.0);
	}

	public ScheduledEventInfo GetCurrent(DateTime serverNow)
	{
		if (!scheduledStartKnown)
		{
			return ScheduledEventInfo.None;
		}
		DateTime dateTime = scheduledStartUtc + gracePeriodDuration;
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
		SetStartState(StartKind.Unresolved, default(PhotonTimestamp));
		RefreshPhase();
	}

	public void OnShowEnded()
	{
		if (currentPhase == ScheduledEventPhase.During)
		{
			PreviousEventSubphaseStartTime = EventSubphaseStartTime;
			EventSubphaseStartTime = DateTime.Now;
		}
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
			Hashtable propertiesToSet = new Hashtable { { "scheduledEventState", state } };
			PhotonNetwork.CurrentRoom.SetCustomProperties(propertiesToSet);
		}
	}

	void IInRoomCallbacks.OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
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
			RefreshPhase();
		}
	}

	void IInRoomCallbacks.OnMasterClientSwitched(Player newMasterClient)
	{
	}

	void IInRoomCallbacks.OnPlayerEnteredRoom(Player newPlayer)
	{
	}

	void IInRoomCallbacks.OnPlayerLeftRoom(Player otherPlayer)
	{
	}

	void IInRoomCallbacks.OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
	{
	}

	private void MaintainRoomStateAsMaster()
	{
		if (!(NetworkSystem.Instance == null) && NetworkSystem.Instance.InRoom && NetworkSystem.Instance.IsMasterClient && !(lastKnownState != "post-event"))
		{
			DateTime serverTime = GorillaComputer.instance.GetServerTime();
			if (ScheduledEventMatchmaking.GracePeriodEnded(GetCurrent(serverTime), serverTime))
			{
				SetRoomState("regular");
				lastKnownState = "regular";
			}
		}
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
			stream.SendNext(eventSubphase);
			return;
		}
		double num = (double)stream.ReceiveNext();
		if (!double.IsFinite(num))
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
		int num2 = (int)stream.ReceiveNext();
		if (num2 != eventSubphase)
		{
			eventSubphase = num2;
			this.OnSubphaseChanged?.Invoke(eventSubphase);
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
