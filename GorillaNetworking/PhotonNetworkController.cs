using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExitGames.Client.Photon;
using Fusion;
using GorillaGameModes;
using GorillaLocomotion;
using GorillaNetworking.ScheduledEvents;
using GorillaTagScripts;
using Photon.Pun;
using PlayFab;
using UnityEngine;

namespace GorillaNetworking;

public class PhotonNetworkController : MonoBehaviour
{
	[OnEnterPlay_SetNull]
	public static volatile PhotonNetworkController Instance;

	public int incrementCounter;

	public PlayFabAuthenticator playFabAuthenticator;

	public string[] serverRegions;

	public bool isPrivate;

	public string customRoomID;

	public GameObject playerOffset;

	public SkinnedMeshRenderer[] offlineVRRig;

	public bool attemptingToConnect;

	private int currentRegionIndex;

	public string currentGameType;

	public bool roomCosmeticsInitialized;

	public GameObject photonVoiceObjectPrefab;

	public Dictionary<string, bool> playerCosmeticsLookup = new Dictionary<string, bool>();

	private float lastHeadRightHandDistance;

	private float lastHeadLeftHandDistance;

	private float pauseTime;

	private float disconnectTime = 120f;

	public bool disableAFKKick;

	private float headRightHandDistance;

	private float headLeftHandDistance;

	private Quaternion headQuat;

	private Quaternion lastHeadQuat;

	public GameObject[] disableOnStartup;

	public GameObject[] enableOnStartup;

	public bool updatedName;

	private int[] playersInRegion;

	private int[] pingInRegion;

	private List<string> friendIDList = new List<string>();

	private JoinType currentJoinType;

	private string friendToFollow;

	private string keyToFollow;

	public string shuffler;

	public string keyStr;

	private string platformTag = "OTHER";

	private string startLevel;

	[SerializeField]
	private GTZone startZone;

	private GorillaGeoHideShowTrigger startGeoTrigger;

	public GorillaNetworkJoinTrigger privateTrigger;

	internal string initialGameMode = "";

	public GorillaNetworkJoinTrigger currentJoinTrigger;

	public string autoJoinRoom;

	public int autoJoinRoomCap = 18;

	public string autoJoinGameMode;

	private bool deferredJoin;

	private float partyJoinDeferredUntilTimestamp;

	private DateTime? timeWhenApplicationPaused;

	[NetworkPrefab]
	[SerializeField]
	private NetworkObject testPlayerPrefab;

	private string roomToJoin = "";

	private int joinNextAttempt;

	private int maxNextAttempts = 10;

	private string LastRoomToJoin = "";

	private List<GorillaNetworkJoinTrigger> allJoinTriggers = new List<GorillaNetworkJoinTrigger>();

	public List<string> FriendIDList
	{
		get
		{
			return friendIDList;
		}
		set
		{
			friendIDList = value;
		}
	}

	public string StartLevel
	{
		get
		{
			return startLevel;
		}
		set
		{
			startLevel = value;
		}
	}

	public GTZone StartZone
	{
		get
		{
			return startZone;
		}
		set
		{
			startZone = value;
		}
	}

	public GTZone CurrentRoomZone
	{
		get
		{
			if (!(currentJoinTrigger != null))
			{
				return GTZone.none;
			}
			return currentJoinTrigger.zone;
		}
	}

	public GorillaGeoHideShowTrigger StartGeoTrigger
	{
		get
		{
			return startGeoTrigger;
		}
		set
		{
			startGeoTrigger = value;
		}
	}

	public void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else if (Instance != this)
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
		updatedName = false;
		playersInRegion = new int[serverRegions.Length];
		pingInRegion = new int[serverRegions.Length];
	}

	public void Start()
	{
		StartCoroutine(DisableOnStart());
		NetworkSystem.Instance.OnJoinedRoomEvent += new Action(OnJoinedRoom);
		NetworkSystem.Instance.OnReturnedToSinglePlayer += new Action(OnDisconnected);
		PhotonNetwork.NetworkingClient.LoadBalancingPeer.ReuseEventInstance = true;
	}

	private IEnumerator DisableOnStart()
	{
		ZoneManagement.SetActiveZone(StartZone);
		yield break;
	}

	public void FixedUpdate()
	{
		headRightHandDistance = (GTPlayer.Instance.headCollider.transform.position - GTPlayer.Instance.GetControllerTransform(isLeftHand: false).position).magnitude;
		headLeftHandDistance = (GTPlayer.Instance.headCollider.transform.position - GTPlayer.Instance.GetControllerTransform(isLeftHand: true).position).magnitude;
		headQuat = GTPlayer.Instance.headCollider.transform.rotation;
		if (!disableAFKKick && Quaternion.Angle(headQuat, lastHeadQuat) <= 0.01f && Mathf.Abs(headRightHandDistance - lastHeadRightHandDistance) < 0.001f && Mathf.Abs(headLeftHandDistance - lastHeadLeftHandDistance) < 0.001f && pauseTime + disconnectTime < Time.realtimeSinceStartup)
		{
			pauseTime = Time.realtimeSinceStartup;
			NetworkSystem.Instance.ReturnToSinglePlayer();
		}
		else if (Quaternion.Angle(headQuat, lastHeadQuat) > 0.01f || Mathf.Abs(headRightHandDistance - lastHeadRightHandDistance) >= 0.001f || Mathf.Abs(headLeftHandDistance - lastHeadLeftHandDistance) >= 0.001f)
		{
			pauseTime = Time.realtimeSinceStartup;
		}
		lastHeadRightHandDistance = headRightHandDistance;
		lastHeadLeftHandDistance = headLeftHandDistance;
		lastHeadQuat = headQuat;
		if (!deferredJoin || !(Time.realtimeSinceStartup >= partyJoinDeferredUntilTimestamp))
		{
			return;
		}
		if ((partyJoinDeferredUntilTimestamp != 0f || NetworkSystem.Instance.netState == NetSystemState.Idle) && currentJoinTrigger != null)
		{
			deferredJoin = false;
			partyJoinDeferredUntilTimestamp = 0f;
			if (currentJoinTrigger == privateTrigger)
			{
				if (customRoomID == roomToJoin || customRoomID == autoJoinRoom || customRoomID == LastRoomToJoin)
				{
					AttemptToAutoJoinSpecificRoom(customRoomID, FriendshipGroupDetection.Instance.IsInParty ? JoinType.JoinWithParty : JoinType.Solo);
				}
				else
				{
					AttemptToJoinSpecificRoom(customRoomID, FriendshipGroupDetection.Instance.IsInParty ? JoinType.JoinWithParty : JoinType.Solo);
				}
			}
			else
			{
				AttemptToJoinPublicRoom(currentJoinTrigger, currentJoinType);
			}
		}
		else if (NetworkSystem.Instance.netState != NetSystemState.PingRecon && NetworkSystem.Instance.netState != NetSystemState.Initialization && NetworkSystem.Instance.netState != NetSystemState.Disconnecting)
		{
			deferredJoin = false;
			partyJoinDeferredUntilTimestamp = 0f;
		}
	}

	public void DeferJoining(float duration)
	{
		partyJoinDeferredUntilTimestamp = Mathf.Max(partyJoinDeferredUntilTimestamp, Time.realtimeSinceStartup + duration);
	}

	public void ClearDeferredJoin()
	{
		partyJoinDeferredUntilTimestamp = 0f;
		deferredJoin = false;
	}

	public void AttemptToJoinPublicRoom(GorillaNetworkJoinTrigger triggeredTrigger, JoinType roomJoinType = JoinType.Solo, List<(string, string)> additionalCustomProperties = null, bool filterSubscribed = false)
	{
		AttemptToJoinPublicRoomAsync(triggeredTrigger, roomJoinType, additionalCustomProperties, filterSubscribed);
	}

	private async void AttemptToJoinPublicRoomAsync(GorillaNetworkJoinTrigger triggeredTrigger, JoinType roomJoinType, List<(string, string)> additionalCustomProperties, bool filterSubscribed)
	{
		if ((KIDManager.KidEnabledAndReady && !KIDManager.CheckFeatureOptIn(EKIDFeatures.Multiplayer).hasOptedInPreviously) || !base.enabled || NetworkSystem.Instance.netState == NetSystemState.Connecting || NetworkSystem.Instance.netState == NetSystemState.Disconnecting)
		{
			return;
		}
		if (NetworkSystem.Instance.netState == NetSystemState.Initialization || NetworkSystem.Instance.netState == NetSystemState.PingRecon || Time.realtimeSinceStartup < partyJoinDeferredUntilTimestamp)
		{
			currentJoinTrigger = triggeredTrigger;
			currentJoinType = roomJoinType;
			deferredJoin = true;
			return;
		}
		deferredJoin = false;
		string desiredGameMode = triggeredTrigger.GetFullDesiredGameModeString();
		if (NetworkSystem.Instance.InRoom)
		{
			if (NetworkSystem.Instance.SessionIsPrivate)
			{
				if (roomJoinType != JoinType.JoinWithNearby && roomJoinType != JoinType.ForceJoinWithParty)
				{
					return;
				}
			}
			else
			{
				_ = roomJoinType;
				_ = 3;
				if ((!filterSubscribed || (PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.MaxPlayers > 10)) && (NetworkSystem.Instance.GameModeString.StartsWith(desiredGameMode) || triggeredTrigger.SameZoneAsOverride()))
				{
					return;
				}
			}
		}
		if (roomJoinType == JoinType.JoinWithParty || roomJoinType == JoinType.ForceJoinWithParty)
		{
			await SendPartyFollowCommands();
		}
		currentJoinTrigger = triggeredTrigger;
		currentJoinType = roomJoinType;
		if (PlayFabClientAPI.IsClientLoggedIn())
		{
			playFabAuthenticator.SetDisplayName(NetworkSystem.Instance.GetMyNickName());
		}
		RoomConfig roomConfig = RoomConfig.AnyPublicConfig();
		if (currentJoinType == JoinType.JoinWithNearby || currentJoinType == JoinType.JoinWithElevator)
		{
			roomConfig.SetFriendIDs(FriendIDList);
		}
		else if (currentJoinType == JoinType.JoinWithParty || currentJoinType == JoinType.ForceJoinWithParty)
		{
			roomConfig.SetFriendIDs(FriendshipGroupDetection.Instance.PartyMemberIDs.ToList());
		}
		bool flag = filterSubscribed && SubscriptionManager.IsLocalSubscribed();
		ExitGames.Client.Photon.Hashtable hashtable = new ExitGames.Client.Photon.Hashtable
		{
			{ "gameMode", desiredGameMode },
			{ "platform", platformTag },
			{
				"queueName",
				GorillaComputer.instance.currentQueue
			},
			{
				"language",
				LocalisationManager.CurrentLanguage.ToString()
			},
			{
				"fan_club",
				flag ? "true" : "false"
			}
		};
		if (additionalCustomProperties != null)
		{
			foreach (var additionalCustomProperty in additionalCustomProperties)
			{
				hashtable.Add(additionalCustomProperty.Item1, additionalCustomProperty.Item2);
			}
		}
		ScheduledEventMatchmaking.ApplyScheduledEventStateToHashes(hashtable, out var searchFilter);
		roomConfig.CustomProps = hashtable;
		roomConfig.SearchFilter = searchFilter;
		roomConfig.MaxPlayers = currentJoinTrigger.GetRoomSize(flag);
		Debug.Log($"AttemptToJoinPublicRoom: MaxPlayers: {roomConfig.MaxPlayers}   FanClub: {flag}");
		await NetworkSystem.Instance.ConnectToRoom(null, roomConfig);
	}

	public void AttemptToJoinRankedPublicRoom(GorillaNetworkJoinTrigger triggeredTrigger, JoinType roomJoinType = JoinType.Solo)
	{
		string mmrTier = RankedProgressionManager.Instance.GetRankedMatchmakingTier().ToString();
		string text = "Quest";
		text = "PC";
		AttemptToJoinRankedPublicRoomAsync(triggeredTrigger, mmrTier, text, roomJoinType);
	}

	private async void AttemptToJoinRankedPublicRoomAsync(GorillaNetworkJoinTrigger triggeredTrigger, string mmrTier, string platform, JoinType roomJoinType)
	{
		if ((KIDManager.KidEnabledAndReady && !KIDManager.CheckFeatureOptIn(EKIDFeatures.Multiplayer).hasOptedInPreviously) || !base.enabled || NetworkSystem.Instance.netState == NetSystemState.Connecting || NetworkSystem.Instance.netState == NetSystemState.Disconnecting)
		{
			return;
		}
		if (NetworkSystem.Instance.netState == NetSystemState.Initialization || NetworkSystem.Instance.netState == NetSystemState.PingRecon || Time.realtimeSinceStartup < partyJoinDeferredUntilTimestamp)
		{
			currentJoinTrigger = triggeredTrigger;
			currentJoinType = roomJoinType;
			deferredJoin = true;
			return;
		}
		deferredJoin = false;
		string fullDesiredGameModeString = triggeredTrigger.GetFullDesiredGameModeString();
		if (!NetworkSystem.Instance.InRoom)
		{
			currentJoinTrigger = triggeredTrigger;
			currentJoinType = roomJoinType;
			if (PlayFabClientAPI.IsClientLoggedIn())
			{
				playFabAuthenticator.SetDisplayName(NetworkSystem.Instance.GetMyNickName());
			}
			RoomConfig roomConfig = RoomConfig.AnyPublicConfig();
			ExitGames.Client.Photon.Hashtable hashtable = new ExitGames.Client.Photon.Hashtable
			{
				{ "gameMode", fullDesiredGameModeString },
				{ "mmrTier", mmrTier },
				{ "platform", platform }
			};
			ScheduledEventMatchmaking.ApplyScheduledEventStateToHashes(hashtable, out var searchFilter);
			roomConfig.CustomProps = hashtable;
			roomConfig.SearchFilter = searchFilter;
			roomConfig.MaxPlayers = currentJoinTrigger.GetRoomSize(subscribed: false);
			await NetworkSystem.Instance.ConnectToRoom(null, roomConfig);
		}
	}

	private async Task SendPartyFollowCommands()
	{
		Instance.shuffler = UnityEngine.Random.Range(0, 99).ToString().PadLeft(2, '0') + UnityEngine.Random.Range(0, 99999999).ToString().PadLeft(8, '0');
		Instance.keyStr = UnityEngine.Random.Range(0, 99999999).ToString().PadLeft(8, '0');
		RoomSystem.SendPartyFollowCommand(Instance.shuffler, Instance.keyStr);
		PhotonNetwork.SendAllOutgoingCommands();
		await Task.Delay(200);
	}

	private void AttemptToAutoJoinRoomCallback(NetJoinResult obj)
	{
		LastRoomToJoin = roomToJoin;
		switch (obj)
		{
		case NetJoinResult.AlreadyInRoom:
			break;
		case NetJoinResult.Failed_Full:
			break;
		case NetJoinResult.Success:
			break;
		case NetJoinResult.FallbackCreated:
			break;
		}
	}

	public void AttemptToAutoJoinSpecificRoom(string roomID, JoinType roomJoinType)
	{
		roomToJoin = roomID;
		AttemptToJoinSpecificRoomAsync(roomID, roomJoinType, AttemptToAutoJoinRoomCallback);
	}

	public void AttemptToJoinSpecificRoom(string roomID, JoinType roomJoinType)
	{
		AttemptToJoinSpecificRoomAsync(roomID, roomJoinType, null);
	}

	public void AttemptToJoinSpecificRoomWithCallback(string roomID, JoinType roomJoinType, Action<NetJoinResult> callback)
	{
		AttemptToJoinSpecificRoomAsync(roomID, roomJoinType, callback);
	}

	public async Task AttemptToJoinSpecificRoomAsync(string roomID, JoinType roomJoinType, Action<NetJoinResult> callback)
	{
		if (await KIDManager.UseKID() && !KIDManager.HasPermissionToUseFeature(EKIDFeatures.Multiplayer))
		{
			return;
		}
		if (NetworkSystem.Instance.netState == NetSystemState.Initialization || NetworkSystem.Instance.netState == NetSystemState.PingRecon)
		{
			deferredJoin = true;
			customRoomID = roomID;
			currentJoinType = roomJoinType;
			currentJoinTrigger = privateTrigger;
		}
		else if (NetworkSystem.Instance.netState == NetSystemState.Idle || NetworkSystem.Instance.netState == NetSystemState.InGame)
		{
			customRoomID = roomID;
			currentJoinType = roomJoinType;
			currentJoinTrigger = privateTrigger;
			deferredJoin = false;
			if (currentJoinType == JoinType.JoinWithParty || currentJoinType == JoinType.ForceJoinWithParty)
			{
				await SendPartyFollowCommands();
			}
			string fullDesiredGameModeString = currentJoinTrigger.GetFullDesiredGameModeString();
			ExitGames.Client.Photon.Hashtable customProps = new ExitGames.Client.Photon.Hashtable
			{
				{ "gameMode", fullDesiredGameModeString },
				{ "platform", platformTag },
				{
					"queueName",
					GorillaComputer.instance.currentQueue
				}
			};
			RoomConfig roomConfig = new RoomConfig();
			roomConfig.createIfMissing = true;
			roomConfig.isJoinable = true;
			roomConfig.isPublic = false;
			if (roomJoinType == JoinType.FriendStationPublic)
			{
				roomConfig.isPublic = true;
			}
			byte roomSizeForCreate = RoomSystem.GetRoomSizeForCreate(currentJoinTrigger.zone, Enum.Parse<GameModeType>(GorillaComputer.instance.currentGameMode.Value, ignoreCase: true), roomConfig.isPublic, SubscriptionManager.IsLocalSubscribed());
			roomConfig.MaxPlayers = roomSizeForCreate;
			Debug.Log($"[AttemptToJoinSpecificRoomAsync] Room MaxPlayers = {roomConfig.MaxPlayers}");
			roomConfig.CustomProps = customProps;
			if (PlayFabClientAPI.IsClientLoggedIn())
			{
				playFabAuthenticator.SetDisplayName(NetworkSystem.Instance.GetMyNickName());
			}
			Task<NetJoinResult> connectToRoomTask = NetworkSystem.Instance.ConnectToRoom(roomID, roomConfig);
			if (callback != null)
			{
				await connectToRoomTask;
				Debug.Log("AttemptToJoinSpecificRoomAsync ConnectToRoom Result: " + connectToRoomTask.Result);
				callback(connectToRoomTask.Result);
			}
		}
	}

	private void DisconnectCleanup()
	{
		if (ApplicationQuittingState.IsQuitting)
		{
			return;
		}
		if (GorillaParent.instance != null)
		{
			GorillaScoreboardSpawner[] componentsInChildren = GorillaParent.instance.GetComponentsInChildren<GorillaScoreboardSpawner>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].OnLeftRoom();
			}
		}
		attemptingToConnect = true;
		SkinnedMeshRenderer[] array = offlineVRRig;
		foreach (SkinnedMeshRenderer skinnedMeshRenderer in array)
		{
			if (skinnedMeshRenderer != null)
			{
				skinnedMeshRenderer.enabled = true;
			}
		}
		if (GorillaComputer.instance != null && !ApplicationQuittingState.IsQuitting)
		{
			UpdateTriggerScreens();
		}
		GTPlayer.Instance.maxJumpSpeed = 6.5f;
		GTPlayer.Instance.jumpMultiplier = 1.1f;
		MonkeAgent.instance.currentMasterClient = null;
		GorillaTagger.Instance.offlineVRRig.huntComputer.SetActive(value: false);
		initialGameMode = "";
	}

	public void OnJoinedRoom()
	{
		if (NetworkSystem.Instance.GameModeString.IsNullOrEmpty())
		{
			NetworkSystem.Instance.ReturnToSinglePlayer();
		}
		initialGameMode = NetworkSystem.Instance.GameModeString;
		if (NetworkSystem.Instance.SessionIsPrivate)
		{
			currentJoinTrigger = privateTrigger;
			Instance.UpdateTriggerScreens();
		}
		else if (currentJoinType != JoinType.FollowingParty)
		{
			bool flag = false;
			for (int i = 0; i < GorillaComputer.instance.allowedMapsToJoin.Length; i++)
			{
				if (NetworkSystem.Instance.GameModeString.StartsWith(GorillaComputer.instance.allowedMapsToJoin[i]))
				{
					flag = true;
					break;
				}
			}
			if (flag && GorillaComputer.instance.friendJoinCollider != null && !GorillaComputer.instance.friendJoinCollider.playerIDsCurrentlyTouching.Contains(NetworkSystem.Instance.LocalPlayer.UserId))
			{
				GorillaNetworkJoinTrigger joinTriggerFromFullGameModeString = GorillaComputer.instance.GetJoinTriggerFromFullGameModeString(NetworkSystem.Instance.GameModeString);
				if (!joinTriggerFromFullGameModeString.groupJoinRequiredZonesAB.HasAnyFlag(VRRig.LocalRig.zoneEntity.currentNode.groupZoneAB))
				{
					Debug.Log($"NOT ALLOWED IN ROOM: Joined {ParseZoneFromGameMode(NetworkSystem.Instance.GameModeString)} room but physically in {VRRig.LocalRig.zoneEntity.currentNode.groupZoneAB} zone");
					PersistLog.Log($"NOT ALLOWED IN ROOM - FAILED JOIN: [PlayerZone:{VRRig.LocalRig.zoneEntity.currentNode.groupZoneAB} TriggerZone:{joinTriggerFromFullGameModeString.groupJoinRequiredZonesAB}  Trigger:{joinTriggerFromFullGameModeString.name}]");
					flag = false;
				}
			}
			if (!flag)
			{
				GorillaComputer.instance.roomNotAllowed = true;
				NetworkSystem.Instance.ReturnToSinglePlayer();
				return;
			}
		}
		NetworkSystem.Instance.SetMyTutorialComplete();
		VRRigCache.Instance.InstantiateNetworkObject();
		if (NetworkSystem.Instance.IsMasterClient)
		{
			GorillaGameModes.GameMode.LoadGameModeFromProperty(initialGameMode);
		}
		GorillaComputer.instance.roomFull = false;
		GorillaComputer.instance.roomNotAllowed = false;
		if (currentJoinType == JoinType.JoinWithParty || currentJoinType == JoinType.JoinWithNearby || currentJoinType == JoinType.ForceJoinWithParty || currentJoinType == JoinType.JoinWithElevator)
		{
			keyToFollow = NetworkSystem.Instance.LocalPlayer.UserId + keyStr;
			NetworkSystem.Instance.BroadcastMyRoom(create: true, keyToFollow, shuffler);
		}
		MonkeAgent.instance.currentMasterClient = null;
		UpdateCurrentJoinTrigger();
		UpdateTriggerScreens();
		NetworkSystem.Instance.MultiplayerStarted();
	}

	public void RegisterJoinTrigger(GorillaNetworkJoinTrigger trigger)
	{
		allJoinTriggers.Add(trigger);
	}

	private void UpdateCurrentJoinTrigger()
	{
		GorillaNetworkJoinTrigger joinTriggerFromFullGameModeString = GorillaComputer.instance.GetJoinTriggerFromFullGameModeString(NetworkSystem.Instance.GameModeString);
		if (joinTriggerFromFullGameModeString != null)
		{
			currentJoinTrigger = joinTriggerFromFullGameModeString;
		}
		else if (NetworkSystem.Instance.SessionIsPrivate)
		{
			if (currentJoinTrigger != privateTrigger)
			{
				Debug.LogError("IN a private game but private trigger isnt current");
			}
		}
		else
		{
			Debug.LogError("Not in private room and unabel tp update jointrigger.");
		}
	}

	public void UpdateTriggerScreens()
	{
		foreach (GorillaNetworkJoinTrigger allJoinTrigger in allJoinTriggers)
		{
			allJoinTrigger.UpdateUI();
		}
	}

	public void AttemptToFollowIntoPub(string userIDToFollow, int actorNumberToFollow, string newKeyStr, string shufflerStr, JoinType joinType)
	{
		friendToFollow = userIDToFollow;
		keyToFollow = userIDToFollow + newKeyStr;
		shuffler = shufflerStr;
		currentJoinType = joinType;
		ClearDeferredJoin();
		if (NetworkSystem.Instance.InRoom)
		{
			NetworkSystem.Instance.JoinFriendsRoom(friendToFollow, actorNumberToFollow, keyToFollow, shuffler);
		}
	}

	public void OnDisconnected()
	{
		DisconnectCleanup();
	}

	public void OnApplicationQuit()
	{
		if (PhotonNetwork.IsConnected)
		{
			_ = PhotonNetwork.PhotonServerSettings.AppSettings.AppVersion != "dev";
		}
	}

	private string ReturnRoomName()
	{
		if (isPrivate)
		{
			return customRoomID;
		}
		return RandomRoomName();
	}

	private string RandomRoomName()
	{
		string text = "";
		for (int i = 0; i < 4; i++)
		{
			text += "ABCDEFGHIJKLMNPQRSTUVWXYZ123456789".Substring(UnityEngine.Random.Range(0, "ABCDEFGHIJKLMNPQRSTUVWXYZ123456789".Length), 1);
		}
		if (GorillaComputer.instance.CheckAutoBanListForName(text))
		{
			return text;
		}
		return RandomRoomName();
	}

	private string GetRegionWithLowestPing()
	{
		int num = 10000;
		int num2 = 0;
		for (int i = 0; i < serverRegions.Length; i++)
		{
			Debug.Log("ping in region " + serverRegions[i] + " is " + pingInRegion[i]);
			if (pingInRegion[i] < num && pingInRegion[i] > 0)
			{
				num = pingInRegion[i];
				num2 = i;
			}
		}
		return serverRegions[num2];
	}

	public int TotalUsers()
	{
		int num = 0;
		int[] array = playersInRegion;
		foreach (int num2 in array)
		{
			num += num2;
		}
		return num;
	}

	public string CurrentState()
	{
		if (NetworkSystem.Instance == null)
		{
			Debug.Log("Null netsys!!!");
		}
		return NetworkSystem.Instance.netState.ToString();
	}

	private void OnApplicationPause(bool pause)
	{
		if (pause)
		{
			timeWhenApplicationPaused = DateTime.Now;
			return;
		}
		if ((DateTime.Now - (timeWhenApplicationPaused ?? DateTime.Now)).TotalSeconds > (double)disconnectTime)
		{
			timeWhenApplicationPaused = null;
			NetworkSystem.Instance?.ReturnToSinglePlayer();
		}
		if (NetworkSystem.Instance != null && !NetworkSystem.Instance.InRoom && NetworkSystem.Instance.netState == NetSystemState.InGame)
		{
			NetworkSystem.Instance?.ReturnToSinglePlayer();
		}
	}

	private void OnApplicationFocus(bool focus)
	{
		if (!focus && NetworkSystem.Instance != null && !NetworkSystem.Instance.InRoom && NetworkSystem.Instance.netState == NetSystemState.InGame)
		{
			NetworkSystem.Instance?.ReturnToSinglePlayer();
		}
	}

	private GTZone ParseZoneFromGameMode(string gameMode)
	{
		if (string.IsNullOrEmpty(gameMode))
		{
			return GTZone.none;
		}
		foreach (GTZone value in Enum.GetValues(typeof(GTZone)))
		{
			if (value != GTZone.none && gameMode.StartsWith(value.ToString(), StringComparison.OrdinalIgnoreCase))
			{
				return value;
			}
		}
		return GTZone.none;
	}
}
