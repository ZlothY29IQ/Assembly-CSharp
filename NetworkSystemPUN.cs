using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ExitGames.Client.Photon;
using Fusion;
using GorillaNetworking;
using GorillaTag;
using GorillaTag.Audio;
using GorillaTagScripts;
using Photon.Pun;
using Photon.Realtime;
using Photon.Voice.PUN;
using Photon.Voice.Unity;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

[RequireComponent(typeof(PUNCallbackNotifier))]
public class NetworkSystemPUN : NetworkSystem
{
	private enum InternalState
	{
		AwaitingAuth,
		Authenticated,
		PingGathering,
		StateCheckFailed,
		ConnectingToMaster,
		ConnectedToMaster,
		Idle,
		Internal_Disconnecting,
		Internal_Disconnected,
		Searching_Connecting,
		Searching_Connected,
		Searching_Joining,
		Searching_Joined,
		Searching_JoinFailed_NotFound,
		Searching_JoinFailed_Full,
		Searching_JoinFailed_Other,
		Searching_Creating,
		Searching_Created,
		Searching_CreateFailed,
		Searching_Disconnecting,
		Searching_Disconnected
	}

	private NetworkRegionInfo[] regionData;

	private Task<NetJoinResult> roomTask;

	private ObjectPool<PunNetPlayer> playerPool;

	private NetPlayer[] m_allNetPlayers = new NetPlayer[0];

	private NetPlayer[] m_otherNetPlayers = new NetPlayer[0];

	private List<CancellationTokenSource> _taskCancelTokens = new List<CancellationTokenSource>();

	private PhotonVoiceNetwork punVoice;

	private GameObject VoiceNetworkObject;

	private InternalState currentState;

	private bool firstRoomJoin;

	public override NetPlayer[] AllNetPlayers => m_allNetPlayers;

	public override NetPlayer[] PlayerListOthers => m_otherNetPlayers;

	public override VoiceConnection VoiceConnection => punVoice;

	private int lowestPingRegionIndex
	{
		get
		{
			int num = 9999;
			int result = -1;
			for (int i = 0; i < regionData.Length; i++)
			{
				if (regionData[i].pingToRegion < num)
				{
					num = regionData[i].pingToRegion;
					result = i;
				}
			}
			return result;
		}
	}

	private InternalState internalState
	{
		get
		{
			return currentState;
		}
		set
		{
			currentState = value;
		}
	}

	public override string CurrentPhotonBackend => "PUN";

	public override bool IsOnline => InRoom;

	public override bool InRoom => PhotonNetwork.InRoom;

	public override string RoomName => PhotonNetwork.CurrentRoom?.Name ?? string.Empty;

	public override string GameModeString
	{
		get
		{
			PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("gameMode", out var value);
			return value?.ToString();
		}
	}

	public override string CurrentRegion => PhotonNetwork.CloudRegion;

	public override bool SessionIsPrivate
	{
		get
		{
			Room currentRoom = PhotonNetwork.CurrentRoom;
			if (currentRoom == null)
			{
				return false;
			}
			return !currentRoom.IsVisible;
		}
	}

	public override bool SessionIsSubscription => PhotonNetwork.CurrentRoom?.MaxPlayers > 10;

	public override int LocalPlayerID => PhotonNetwork.LocalPlayer.ActorNumber;

	public override int ServerTimestamp => PhotonNetwork.ServerTimestamp;

	public override double SimTime => PhotonNetwork.Time;

	public override float SimDeltaTime => Time.deltaTime;

	public override int SimTick => PhotonNetwork.ServerTimestamp;

	public override int TickRate => PhotonNetwork.SerializationRate;

	public override int RoomPlayerCount => PhotonNetwork.CurrentRoom.PlayerCount;

	public override bool IsMasterClient => PhotonNetwork.IsMasterClient;

	public override string RoomStringStripped()
	{
		Room currentRoom = PhotonNetwork.CurrentRoom;
		NetworkSystem.reusableSB.Clear();
		NetworkSystem.reusableSB.AppendFormat("Room: '{0}' ", (currentRoom.Name.Length < 20) ? currentRoom.Name : currentRoom.Name.Remove(20));
		NetworkSystem.reusableSB.AppendFormat("{0},{1} {3}/{2} players.", currentRoom.IsVisible ? "visible" : "hidden", currentRoom.IsOpen ? "open" : "closed", currentRoom.MaxPlayers, currentRoom.PlayerCount);
		NetworkSystem.reusableSB.Append("\ncustomProps: {");
		NetworkSystem.reusableSB.AppendFormat("joinedGameMode={0}, ", (RoomSystem.RoomGameMode.Length < 50) ? RoomSystem.RoomGameMode : RoomSystem.RoomGameMode.Remove(50));
		IDictionary customProperties = currentRoom.CustomProperties;
		AppendStringFromDict(customProperties, "gameMode", 50, NetworkSystem.reusableSB);
		NetworkSystem.reusableSB.Append(", ");
		AppendStringFromDict(customProperties, "platform", 10, NetworkSystem.reusableSB);
		NetworkSystem.reusableSB.Append(", ");
		AppendStringFromDict(customProperties, "queueName", 15, NetworkSystem.reusableSB);
		NetworkSystem.reusableSB.Append(", ");
		AppendStringFromDict(customProperties, "language", 15, NetworkSystem.reusableSB);
		NetworkSystem.reusableSB.Append(", ");
		AppendStringFromDict(customProperties, "fan_club", 6, NetworkSystem.reusableSB);
		NetworkSystem.reusableSB.Append(", ");
		AppendStringFromDict(customProperties, "mmrTier", 8, NetworkSystem.reusableSB);
		NetworkSystem.reusableSB.Append("}");
		return NetworkSystem.reusableSB.ToString();
	}

	private void AppendStringFromDict(IDictionary dict, string key, int maxStrLen, StringBuilder sb)
	{
		sb.AppendFormat("{0}=", key);
		if (!dict.Contains(key) || !(dict[key] is string text))
		{
			sb.Append("null");
		}
		else
		{
			sb.Append((text.Length < maxStrLen) ? text : text.Remove(maxStrLen));
		}
	}

	public override async void Initialise()
	{
		base.Initialise();
		base.netState = NetSystemState.Initialization;
		PhotonNetwork.PhotonServerSettings.AppSettings.AppVersion = NetworkSystemConfig.AppVersion;
		PhotonNetwork.PhotonServerSettings.AppSettings.UseNameServer = true;
		PhotonNetwork.EnableCloseConnection = false;
		PhotonNetwork.AutomaticallySyncScene = false;
		string playerName = PlayerPrefs.GetString("playerName", "gorilla" + UnityEngine.Random.Range(0, 9999).ToString().PadLeft(4, '0'));
		playerPool = new ObjectPool<PunNetPlayer>(20);
		UpdatePlayers();
		await CacheRegionInfo();
		UpdatePlayers();
		SetMyNickName(playerName);
	}

	private async Task CacheRegionInfo()
	{
		if (isWrongVersion)
		{
			return;
		}
		regionData = new NetworkRegionInfo[regionNames.Length];
		for (int i = 0; i < regionData.Length; i++)
		{
			regionData[i] = new NetworkRegionInfo();
		}
		if (!(await WaitForStateCheck(InternalState.Authenticated, float.PositiveInfinity)))
		{
			return;
		}
		base.netState = NetSystemState.PingRecon;
		int tryingRegionIndex = 0;
		while (tryingRegionIndex < regionNames.Length)
		{
			internalState = InternalState.ConnectingToMaster;
			PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = regionNames[tryingRegionIndex];
			currentRegionIndex = tryingRegionIndex;
			PhotonNetwork.ConnectUsingSettings();
			if (!(await WaitForStateCheck(InternalState.ConnectedToMaster)))
			{
				base.netState = NetSystemState.PingRecon;
			}
			else
			{
				regionData[currentRegionIndex].playersInRegion = PhotonNetwork.CountOfPlayers;
				regionData[currentRegionIndex].pingToRegion = PhotonNetwork.GetPing();
				Utils.Log("Ping for " + PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion.ToString() + " is " + PhotonNetwork.GetPing());
				internalState = InternalState.PingGathering;
				PhotonNetwork.Disconnect();
				if (!(await WaitForStateCheck(InternalState.Internal_Disconnected)))
				{
					return;
				}
			}
			int num = tryingRegionIndex + 1;
			tryingRegionIndex = num;
		}
		internalState = InternalState.Idle;
		base.netState = NetSystemState.Idle;
	}

	public override AuthenticationValues GetAuthenticationValues()
	{
		return PhotonNetwork.AuthValues;
	}

	public override void SetAuthenticationValues(AuthenticationValues authValues)
	{
		PhotonNetwork.AuthValues = authValues;
	}

	public override void FinishAuthenticating()
	{
		if (PhotonNetwork.AuthValues == null)
		{
			_taskCancelTokens.ForEach(delegate(CancellationTokenSource cts)
			{
				cts.Cancel();
				cts.Dispose();
			});
			_taskCancelTokens.Clear();
		}
		else
		{
			internalState = InternalState.Authenticated;
		}
	}

	private async Task WaitForState(CancellationToken ct, InternalState[] desiredStates, float timeout)
	{
		float timeoutTime = Time.realtimeSinceStartup + timeout;
		while (!Enumerable.Contains(desiredStates, this.internalState))
		{
			if (ct.IsCancellationRequested)
			{
				string text = "";
				InternalState[] array = desiredStates;
				foreach (InternalState internalState in array)
				{
					text += $"- {internalState}";
				}
				Debug.LogError("Got cancelation token while waiting for states " + text);
				this.internalState = InternalState.StateCheckFailed;
				break;
			}
			if (timeoutTime < Time.realtimeSinceStartup)
			{
				string text2 = "";
				InternalState[] array = desiredStates;
				foreach (InternalState internalState2 in array)
				{
					text2 += $"- {internalState2}";
				}
				Debug.LogError("Got stuck waiting for states " + text2);
				this.internalState = InternalState.StateCheckFailed;
				break;
			}
			await Task.Yield();
		}
	}

	private async Task<bool> WaitForStateCheck(InternalState[] desiredStates, float timeout = 10f)
	{
		(CancellationTokenSource, CancellationToken) token = GetCancellationToken();
		await WaitForState(token.Item2, desiredStates, timeout);
		_taskCancelTokens.Remove(token.Item1);
		token.Item1.Dispose();
		if (internalState != InternalState.StateCheckFailed)
		{
			return true;
		}
		ResetSystem();
		return false;
	}

	private Task<bool> WaitForStateCheck(InternalState desiredState, float timeout = 10f)
	{
		return WaitForStateCheck(new InternalState[1] { desiredState }, timeout);
	}

	private async Task<NetJoinResult> MakeOrFindRoom(string roomName, RoomConfig opts, int regionIndex = -1)
	{
		if (InRoom)
		{
			await InternalDisconnect();
		}
		currentRegionIndex = 0;
		bool flag = ((regionIndex >= 0) ? (await TryJoinRoomInRegion(roomName, opts, regionIndex)) : (await TryJoinRoom(roomName, opts)));
		bool flag2 = flag;
		if (internalState == InternalState.Searching_JoinFailed_Full)
		{
			return NetJoinResult.Failed_Full;
		}
		if (!flag2)
		{
			return await TryCreateRoom(roomName, opts);
		}
		if (internalState != InternalState.Searching_Joined)
		{
			return NetJoinResult.Failed_Other;
		}
		return NetJoinResult.Success;
	}

	private async Task<bool> TryJoinRoom(string roomName, RoomConfig opts)
	{
		while (currentRegionIndex < regionNames.Length)
		{
			if (await TryJoinRoomInRegion(roomName, opts, currentRegionIndex))
			{
				return true;
			}
			currentRegionIndex++;
		}
		return false;
	}

	private async Task<bool> TryJoinRoomInRegion(string roomName, RoomConfig opts, int regionIndex)
	{
		internalState = InternalState.ConnectingToMaster;
		string fixedRegion = regionNames[regionIndex];
		PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = fixedRegion;
		currentRegionIndex = regionIndex;
		UpdateZoneInfo(opts.isPublic);
		PhotonNetwork.ConnectUsingSettings();
		if (!(await WaitForStateCheck(InternalState.ConnectedToMaster)))
		{
			return false;
		}
		internalState = InternalState.Searching_Joining;
		PhotonNetwork.JoinRoom(roomName);
		if (!(await WaitForStateCheck(new InternalState[4]
		{
			InternalState.Searching_Joined,
			InternalState.Searching_JoinFailed_NotFound,
			InternalState.Searching_JoinFailed_Full,
			InternalState.Searching_JoinFailed_Other
		})))
		{
			return false;
		}
		bool foundRoom = internalState != InternalState.Searching_JoinFailed_NotFound;
		if (!foundRoom)
		{
			PhotonNetwork.Disconnect();
			internalState = InternalState.Searching_Disconnecting;
			if (!(await WaitForStateCheck(InternalState.Searching_Disconnected)))
			{
				return false;
			}
		}
		return foundRoom;
	}

	private async Task<NetJoinResult> TryCreateRoom(string roomName, RoomConfig opts)
	{
		Debug.Log("returning to best region to create room");
		internalState = InternalState.ConnectingToMaster;
		PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = regionNames[lowestPingRegionIndex];
		currentRegionIndex = lowestPingRegionIndex;
		UpdateZoneInfo(opts.isPublic);
		PhotonNetwork.ConnectUsingSettings();
		if (!(await WaitForStateCheck(InternalState.ConnectedToMaster)))
		{
			return NetJoinResult.Failed_Other;
		}
		internalState = InternalState.Searching_Creating;
		PhotonNetwork.CreateRoom(roomName, opts.ToPUNOpts());
		if (!(await WaitForStateCheck(new InternalState[2]
		{
			InternalState.Searching_Created,
			InternalState.Searching_CreateFailed
		})))
		{
			return NetJoinResult.Failed_Other;
		}
		if (internalState == InternalState.Searching_CreateFailed)
		{
			return NetJoinResult.Failed_Other;
		}
		return NetJoinResult.FallbackCreated;
	}

	private async Task<NetJoinResult> JoinRandomPublicRoom(RoomConfig opts)
	{
		if (InRoom)
		{
			await InternalDisconnect();
		}
		internalState = InternalState.ConnectingToMaster;
		if (!firstRoomJoin && opts.CustomProps.TryGetValue("gameMode", out var value) && !value.ToString().StartsWith("city"))
		{
			firstRoomJoin = true;
			PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = regionNames[lowestPingRegionIndex];
			currentRegionIndex = lowestPingRegionIndex;
		}
		else if (!opts.IsJoiningWithFriends)
		{
			PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = GetRandomWeightedRegion();
			currentRegionIndex = Array.IndexOf(regionNames, PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion);
		}
		UpdateZoneInfo(roomIsPublic: true, PhotonNetworkController.Instance.currentJoinTrigger.zone.GetName());
		PhotonNetwork.ConnectUsingSettings();
		if (!(await WaitForStateCheck(InternalState.ConnectedToMaster)))
		{
			return NetJoinResult.Failed_Other;
		}
		internalState = InternalState.Searching_Joining;
		if (opts.IsJoiningWithFriends)
		{
			PhotonNetwork.JoinRandomRoom(opts.EffectiveSearchFilter, opts.MaxPlayers, MatchmakingMode.RandomMatching, null, null, opts.joinFriendIDs.ToArray());
		}
		else
		{
			PhotonNetwork.JoinRandomRoom(opts.EffectiveSearchFilter, opts.MaxPlayers, MatchmakingMode.FillRoom, null, null);
		}
		if (!(await WaitForStateCheck(new InternalState[4]
		{
			InternalState.Searching_Joined,
			InternalState.Searching_JoinFailed_NotFound,
			InternalState.Searching_JoinFailed_Full,
			InternalState.Searching_JoinFailed_Other
		})))
		{
			return NetJoinResult.Failed_Other;
		}
		if (internalState != InternalState.Searching_Joined)
		{
			internalState = InternalState.Searching_Creating;
			string text = "";
			if (opts.MaxPlayers == 20 && opts.isPublic)
			{
				text = ":GTFC";
			}
			if (opts.IsJoiningWithFriends)
			{
				PhotonNetwork.CreateRoom(NetworkSystem.GetRandomRoomName() + text, opts.ToPUNOpts(), null, opts.joinFriendIDs);
			}
			else
			{
				PhotonNetwork.CreateRoom(NetworkSystem.GetRandomRoomName() + text, opts.ToPUNOpts());
			}
			if (!(await WaitForStateCheck(new InternalState[2]
			{
				InternalState.Searching_Created,
				InternalState.Searching_CreateFailed
			})))
			{
				return NetJoinResult.Failed_Other;
			}
			if (internalState == InternalState.Searching_CreateFailed)
			{
				return NetJoinResult.Failed_Other;
			}
			return NetJoinResult.FallbackCreated;
		}
		return NetJoinResult.Success;
	}

	public override async Task<NetJoinResult> ConnectToRoom(string roomName, RoomConfig opts, int regionIndex = -1)
	{
		if (isWrongVersion)
		{
			return NetJoinResult.Failed_Other;
		}
		if (base.netState != NetSystemState.Idle && base.netState != NetSystemState.InGame)
		{
			return NetJoinResult.Failed_Other;
		}
		if (InRoom && roomName == RoomName)
		{
			return NetJoinResult.AlreadyInRoom;
		}
		if (roomTask != null && !roomTask.IsCompleted)
		{
			return NetJoinResult.Failed_Other;
		}
		base.netState = NetSystemState.Connecting;
		NetJoinResult netJoinResult;
		if (roomName != null)
		{
			roomTask = MakeOrFindRoom(roomName, opts, regionIndex);
			netJoinResult = await roomTask;
			roomTask = null;
		}
		else
		{
			roomTask = JoinRandomPublicRoom(opts);
			netJoinResult = await roomTask;
			roomTask = null;
		}
		switch (netJoinResult)
		{
		case NetJoinResult.Failed_Full:
			GorillaComputer.instance.roomFull = true;
			GorillaComputer.instance.UpdateScreen();
			ResetSystem();
			roomTask = null;
			return netJoinResult;
		case NetJoinResult.Failed_Other:
			ResetSystem();
			roomTask = null;
			return netJoinResult;
		case NetJoinResult.AlreadyInRoom:
			base.netState = NetSystemState.InGame;
			roomTask = null;
			return netJoinResult;
		default:
			if (!InRoom)
			{
				GTDev.LogError("NetworkSystem: room joined success but we have disconnected");
				return NetJoinResult.Failed_Other;
			}
			base.netState = NetSystemState.InGame;
			PlayerJoined(base.LocalPlayer);
			localRecorder.StartRecording();
			return netJoinResult;
		}
	}

	public override async Task JoinFriendsRoom(string userID, int actorIDToFollow, string keyToFollow, string shufflerToFollow)
	{
		bool foundFriend = false;
		float searchStartTime = Time.realtimeSinceStartup;
		float timeToSpendSearching = 15f;
		Dictionary<string, PlayFab.ClientModels.SharedGroupDataRecord> dummyData = new Dictionary<string, PlayFab.ClientModels.SharedGroupDataRecord>();
		bool failedToJoinFriend = false;
		try
		{
			base.groupJoinInProgress = true;
			while (!foundFriend && searchStartTime + timeToSpendSearching > Time.realtimeSinceStartup)
			{
				Dictionary<string, PlayFab.ClientModels.SharedGroupDataRecord> data = dummyData;
				bool callbackFinished = false;
				PlayFabClientAPI.GetSharedGroupData(new PlayFab.ClientModels.GetSharedGroupDataRequest
				{
					Keys = new List<string> { keyToFollow },
					SharedGroupId = userID
				}, delegate(GetSharedGroupDataResult getSharedGroupDataResult)
				{
					data = getSharedGroupDataResult.Data;
					Debug.Log($"Got friend follow data, {data.Count} entries");
					callbackFinished = true;
				}, delegate(PlayFabError error)
				{
					Debug.Log($"GetSharedGroupData returns error: {error}");
					callbackFinished = true;
				});
				while (!callbackFinished)
				{
					await Task.Yield();
				}
				foreach (KeyValuePair<string, PlayFab.ClientModels.SharedGroupDataRecord> item in data)
				{
					if (!(item.Key == keyToFollow))
					{
						continue;
					}
					string[] array = item.Value.Value.Split("|");
					if (array.Length != 2)
					{
						continue;
					}
					string roomID = NetworkSystem.ShuffleRoomName(array[0], shufflerToFollow.Substring(2, 8), encode: false);
					string value = NetworkSystem.ShuffleRoomName(array[1], shufflerToFollow.Substring(0, 2), encode: false);
					int regionIndex = "ABCDEFGHIJKLMNPQRSTUVWXYZ123456789".IndexOf(value);
					if (regionIndex < 0 || regionIndex >= NetworkSystem.Instance.regionNames.Length)
					{
						continue;
					}
					foundFriend = true;
					if (InRoom && PhotonNetwork.CurrentRoom.Players.TryGetValue(actorIDToFollow, out var value2) && value2 != null)
					{
						MonkeAgent.instance.SendReport("possible kick attempt", value2.UserId, value2.NickName);
					}
					else if (RoomName != roomID)
					{
						await ReturnToSinglePlayer();
						RoomConfig roomConfig = new RoomConfig();
						roomConfig.createIfMissing = false;
						roomConfig.isPublic = true;
						roomConfig.isJoinable = true;
						Task<NetJoinResult> ConnectToRoomTask = ConnectToRoom(roomID, roomConfig, regionIndex);
						await ConnectToRoomTask;
						NetJoinResult result = ConnectToRoomTask.Result;
						failedToJoinFriend = result != NetJoinResult.Success;
						if (result == NetJoinResult.Success)
						{
							groupJoinOverrideGameMode = NetworkSystem.Instance.GameModeString;
						}
					}
				}
				await Task.Delay(500);
			}
		}
		finally
		{
			base.groupJoinInProgress = false;
			if (failedToJoinFriend)
			{
				FriendshipGroupDetection.Instance.OnFailedToFollowParty();
			}
		}
	}

	public override void JoinPubWithFriends()
	{
		throw new NotImplementedException();
	}

	public override string GetRandomWeightedRegion()
	{
		float value = UnityEngine.Random.value;
		int num = 0;
		for (int i = 0; i < regionData.Length; i++)
		{
			num += regionData[i].playersInRegion;
		}
		float num2 = 0f;
		int num3;
		for (num3 = -1; num2 < value; num2 += (float)regionData[num3].playersInRegion / (float)num)
		{
			if (num3 >= regionData.Length - 1)
			{
				break;
			}
			num3++;
		}
		return regionNames[num3];
	}

	public override async Task ReturnToSinglePlayer()
	{
		if (base.netState == NetSystemState.InGame || base.netState == NetSystemState.Connecting)
		{
			base.netState = NetSystemState.Disconnecting;
			_taskCancelTokens.ForEach(delegate(CancellationTokenSource cts)
			{
				cts.Cancel();
				cts.Dispose();
			});
			_taskCancelTokens.Clear();
			await InternalDisconnect();
			base.netState = NetSystemState.Idle;
		}
	}

	private async Task InternalDisconnect()
	{
		internalState = InternalState.Internal_Disconnecting;
		PhotonNetwork.Disconnect();
		if (!(await WaitForStateCheck(InternalState.Internal_Disconnected)))
		{
			Debug.LogError("Failed to achieve internal disconnected state");
		}
		UnityEngine.Object.Destroy(VoiceNetworkObject);
		UpdatePlayers();
		SinglePlayerStarted();
	}

	private void AddVoice()
	{
		SetupVoice();
	}

	private void SetupVoice()
	{
		try
		{
			punVoice = PhotonVoiceNetwork.Instance;
			VoiceNetworkObject = punVoice.gameObject;
			VoiceNetworkObject.name = "VoiceNetworkObject";
			VoiceNetworkObject.transform.parent = base.transform;
			VoiceNetworkObject.transform.localPosition = Vector3.zero;
			punVoice.LogLevel = VoiceSettings.LogLevel;
			punVoice.GlobalRecordersLogLevel = VoiceSettings.GlobalRecordersLogLevel;
			punVoice.GlobalSpeakersLogLevel = VoiceSettings.GlobalSpeakersLogLevel;
			punVoice.AutoConnectAndJoin = VoiceSettings.AutoConnectAndJoin;
			punVoice.AutoLeaveAndDisconnect = VoiceSettings.AutoLeaveAndDisconnect;
			punVoice.WorkInOfflineMode = VoiceSettings.WorkInOfflineMode;
			punVoice.AutoCreateSpeakerIfNotFound = VoiceSettings.CreateSpeakerIfNotFound;
			AppSettings appSettings = new AppSettings();
			appSettings.AppIdRealtime = PhotonNetwork.PhotonServerSettings.AppSettings.AppIdRealtime;
			appSettings.AppIdVoice = PhotonNetwork.PhotonServerSettings.AppSettings.AppIdVoice;
			punVoice.Settings = appSettings;
			remoteVoiceAddedCallbacks.ForEach(delegate(Action<RemoteVoiceLink> callback)
			{
				punVoice.RemoteVoiceAdded += callback;
			});
			localRecorder = VoiceNetworkObject.GetComponent<GTRecorder>();
			if (localRecorder == null)
			{
				localRecorder = VoiceNetworkObject.AddComponent<GTRecorder>();
				if (VRRigCache.Instance != null && VRRigCache.Instance.localRig != null)
				{
					LoudSpeakerActivator[] componentsInChildren = VRRigCache.Instance.localRig.GetComponentsInChildren<LoudSpeakerActivator>();
					for (int num = 0; num < componentsInChildren.Length; num++)
					{
						componentsInChildren[num].SetRecorder((GTRecorder)localRecorder);
					}
				}
			}
			localRecorder.LogLevel = VoiceSettings.LogLevel;
			localRecorder.RecordOnlyWhenEnabled = VoiceSettings.RecordOnlyWhenEnabled;
			localRecorder.RecordOnlyWhenJoined = VoiceSettings.RecordOnlyWhenJoined;
			localRecorder.StopRecordingWhenPaused = VoiceSettings.StopRecordingWhenPaused;
			localRecorder.TransmitEnabled = VoiceSettings.TransmitEnabled;
			localRecorder.AutoStart = VoiceSettings.AutoStart;
			localRecorder.Encrypt = VoiceSettings.Encrypt;
			localRecorder.FrameDuration = VoiceSettings.FrameDuration;
			localRecorder.InterestGroup = VoiceSettings.InterestGroup;
			localRecorder.SourceType = VoiceSettings.InputSourceType;
			localRecorder.MicrophoneType = VoiceSettings.MicrophoneType;
			localRecorder.UseMicrophoneTypeFallback = VoiceSettings.UseFallback;
			localRecorder.VoiceDetection = VoiceSettings.Detect;
			localRecorder.VoiceDetectionThreshold = VoiceSettings.Threshold;
			localRecorder.VoiceDetectionDelayMs = VoiceSettings.Delay;
			localRecorder.DebugEchoMode = VoiceSettings.DebugEcho;
			if (!SubscriptionManager.IsLocalSubscribed())
			{
				localRecorder.SamplingRate = VoiceSettings.SamplingRate;
				localRecorder.Bitrate = VoiceSettings.Bitrate;
			}
			else
			{
				localRecorder.SamplingRate = VoiceSettings.SubsSamplingRate;
				localRecorder.Bitrate = VoiceSettings.SubsBitrate;
			}
			VoiceNetworkObject.AddComponent<VoiceToLoudness>();
			punVoice.PrimaryRecorder = localRecorder;
		}
		catch (Exception ex)
		{
			Debug.LogError("An exception was thrown when trying to setup photon voice, please check microphone permissions:\n" + ex.ToString());
		}
	}

	public override void AddRemoteVoiceAddedCallback(Action<RemoteVoiceLink> callback)
	{
		remoteVoiceAddedCallbacks.Add(callback);
	}

	public override GameObject NetInstantiate(GameObject prefab, Vector3 position, Quaternion rotation, bool isRoomObject = false)
	{
		if (PhotonNetwork.CurrentRoom == null)
		{
			return null;
		}
		if (isRoomObject)
		{
			return PhotonNetwork.InstantiateRoomObject(prefab.name, position, rotation, 0);
		}
		return PhotonNetwork.Instantiate(prefab.name, position, rotation, 0);
	}

	public override GameObject NetInstantiate(GameObject prefab, Vector3 position, Quaternion rotation, int playerAuthID, bool isRoomObject = false)
	{
		return NetInstantiate(prefab, position, rotation, isRoomObject);
	}

	public override GameObject NetInstantiate(GameObject prefab, Vector3 position, Quaternion rotation, bool isRoomObject, byte group = 0, object[] data = null, NetworkRunner.OnBeforeSpawned callback = null)
	{
		if (PhotonNetwork.CurrentRoom == null)
		{
			return null;
		}
		if (isRoomObject)
		{
			return PhotonNetwork.InstantiateRoomObject(prefab.name, position, rotation, group, data);
		}
		return PhotonNetwork.Instantiate(prefab.name, position, rotation, group, data);
	}

	public override void NetDestroy(GameObject instance)
	{
		if (instance.TryGetComponent<PhotonView>(out var component) && component.AmOwner)
		{
			PhotonNetwork.Destroy(instance);
		}
		else
		{
			UnityEngine.Object.Destroy(instance);
		}
	}

	public override void SetPlayerObject(GameObject playerInstance, int? owningPlayerID = null)
	{
	}

	public override void CallRPC(MonoBehaviour component, RPC rpcMethod, bool sendToSelf = true)
	{
		RpcTarget target = ((!sendToSelf) ? RpcTarget.Others : RpcTarget.All);
		PhotonView.Get(component).RPC(rpcMethod.Method.Name, target, NetworkSystem.EmptyArgs);
	}

	public override void CallRPC<T>(MonoBehaviour component, RPC rpcMethod, RPCArgBuffer<T> args, bool sendToSelf = true)
	{
		RpcTarget target = ((!sendToSelf) ? RpcTarget.Others : RpcTarget.All);
		NetCrossoverUtils.SerializeToRPCData(ref args);
		PhotonView.Get(component).RPC(rpcMethod.Method.Name, target, args.Data);
	}

	public override void CallRPC(MonoBehaviour component, StringRPC rpcMethod, string message, bool sendToSelf = true)
	{
		RpcTarget target = ((!sendToSelf) ? RpcTarget.Others : RpcTarget.All);
		PhotonView.Get(component).RPC(rpcMethod.Method.Name, target, message);
	}

	public override void CallRPC(int targetPlayerID, MonoBehaviour component, RPC rpcMethod)
	{
		Player player = PhotonNetwork.CurrentRoom.GetPlayer(targetPlayerID);
		PhotonView.Get(component).RPC(rpcMethod.Method.Name, player, NetworkSystem.EmptyArgs);
	}

	public override void CallRPC<T>(int targetPlayerID, MonoBehaviour component, RPC rpcMethod, RPCArgBuffer<T> args)
	{
		Player player = PhotonNetwork.CurrentRoom.GetPlayer(targetPlayerID);
		NetCrossoverUtils.SerializeToRPCData(ref args);
		PhotonView.Get(component).RPC(rpcMethod.Method.Name, player, args.Data);
	}

	public override void CallRPC(int targetPlayerID, MonoBehaviour component, StringRPC rpcMethod, string message)
	{
		Player player = PhotonNetwork.CurrentRoom.GetPlayer(targetPlayerID);
		PhotonView.Get(component).RPC(rpcMethod.Method.Name, player, message);
	}

	public override async Task AwaitSceneReady()
	{
		while (PhotonNetwork.LevelLoadingProgress < 1f)
		{
			await Task.Yield();
		}
	}

	public override NetPlayer GetLocalPlayer()
	{
		if (netPlayerCache.Count == 0)
		{
			UpdatePlayers();
		}
		foreach (NetPlayer item in netPlayerCache)
		{
			if (item.IsLocal)
			{
				return item;
			}
		}
		Debug.LogError("Somehow no local net players found. This shouldn't happen");
		return null;
	}

	public override NetPlayer GetPlayer(int PlayerID)
	{
		if (InRoom && !PhotonNetwork.CurrentRoom.Players.ContainsKey(PlayerID))
		{
			return null;
		}
		foreach (NetPlayer item in netPlayerCache)
		{
			if (item.ActorNumber == PlayerID)
			{
				return item;
			}
		}
		UpdatePlayers();
		foreach (NetPlayer item2 in netPlayerCache)
		{
			if (item2.ActorNumber == PlayerID)
			{
				return item2;
			}
		}
		GTDev.LogWarning("There is no NetPlayer with this ID currently in game. Passed ID: " + PlayerID);
		return null;
	}

	public override void SetMyNickName(string id)
	{
		if (!KIDManager.HasPermissionToUseFeature(EKIDFeatures.Custom_Nametags) && !id.StartsWith("gorilla"))
		{
			Debug.Log("[KID] Trying to set custom nickname but that permission has been disallowed");
			PhotonNetwork.LocalPlayer.NickName = "gorilla";
		}
		else
		{
			PlayerPrefs.SetString("playerName", id);
			_ = PhotonNetwork.LocalPlayer.NickName;
			PhotonNetwork.LocalPlayer.NickName = id;
		}
	}

	public override string GetMyNickName()
	{
		return PhotonNetwork.LocalPlayer.NickName;
	}

	public override string GetMyDefaultName()
	{
		return PhotonNetwork.LocalPlayer.DefaultName;
	}

	public override string GetNickName(int playerID)
	{
		return GetPlayer(playerID)?.NickName;
	}

	public override string GetNickName(NetPlayer player)
	{
		return player.NickName;
	}

	public override void SetMyTutorialComplete()
	{
		bool flag = PlayerPrefs.GetString("didTutorial", "nope") == "done";
		if (!flag)
		{
			PlayerPrefs.SetString("didTutorial", "done");
			PlayerPrefs.Save();
		}
		ExitGames.Client.Photon.Hashtable hashtable = new ExitGames.Client.Photon.Hashtable();
		hashtable.Add("didTutorial", flag);
		PhotonNetwork.LocalPlayer.SetCustomProperties(hashtable);
	}

	public override bool GetMyTutorialCompletion()
	{
		return PlayerPrefs.GetString("didTutorial", "nope") == "done";
	}

	public override bool GetPlayerTutorialCompletion(int playerID)
	{
		NetPlayer player = GetPlayer(playerID);
		if (player == null)
		{
			return false;
		}
		Player player2 = PhotonNetwork.CurrentRoom.GetPlayer(player.ActorNumber);
		if (player2 == null)
		{
			return false;
		}
		if (player2.CustomProperties.TryGetValue("didTutorial", out var value))
		{
			bool flag = default(bool);
			int num;
			if (value is bool)
			{
				flag = (bool)value;
				num = ((1 == 0) ? 1 : 0);
			}
			else
			{
				num = 1;
			}
			return (byte)((uint)num | (flag ? 1u : 0u)) != 0;
		}
		return false;
	}

	public override string GetMyUserID()
	{
		return PhotonNetwork.LocalPlayer.UserId;
	}

	public override string GetUserID(int playerID)
	{
		return GetPlayer(playerID)?.UserId;
	}

	public override string GetUserID(NetPlayer netPlayer)
	{
		return ((PunNetPlayer)netPlayer).PlayerRef?.UserId;
	}

	public override int GlobalPlayerCount()
	{
		int num = 0;
		NetworkRegionInfo[] array = regionData;
		foreach (NetworkRegionInfo networkRegionInfo in array)
		{
			num += networkRegionInfo.playersInRegion;
		}
		return num;
	}

	public override bool IsObjectLocallyOwned(GameObject obj)
	{
		if (!IsOnline)
		{
			return true;
		}
		if (obj.TryGetComponent<PhotonView>(out var component))
		{
			return component.IsMine;
		}
		return true;
	}

	protected override void UpdateNetPlayerList()
	{
		if (!IsOnline)
		{
			bool flag = false;
			PunNetPlayer punNetPlayer = null;
			if (netPlayerCache.Count > 0)
			{
				for (int i = 0; i < netPlayerCache.Count; i++)
				{
					NetPlayer netPlayer = netPlayerCache[i];
					if (netPlayer.IsLocal)
					{
						punNetPlayer = (PunNetPlayer)netPlayer;
						flag = true;
					}
					else
					{
						playerPool.Return((PunNetPlayer)netPlayer);
					}
				}
				netPlayerCache.Clear();
			}
			if (!flag)
			{
				punNetPlayer = playerPool.Take();
				punNetPlayer.InitPlayer(PhotonNetwork.LocalPlayer);
			}
			netPlayerCache.Add(punNetPlayer);
		}
		else
		{
			Dictionary<int, Player>.ValueCollection values = PhotonNetwork.CurrentRoom.Players.Values;
			foreach (Player item in values)
			{
				bool flag2 = false;
				for (int j = 0; j < netPlayerCache.Count; j++)
				{
					if (item == ((PunNetPlayer)netPlayerCache[j]).PlayerRef)
					{
						flag2 = true;
						break;
					}
				}
				if (!flag2)
				{
					PunNetPlayer punNetPlayer2 = playerPool.Take();
					punNetPlayer2.InitPlayer(item);
					netPlayerCache.Add(punNetPlayer2);
				}
			}
			for (int k = 0; k < netPlayerCache.Count; k++)
			{
				PunNetPlayer punNetPlayer3 = (PunNetPlayer)netPlayerCache[k];
				bool flag3 = false;
				foreach (Player item2 in values)
				{
					if (item2 == punNetPlayer3.PlayerRef)
					{
						flag3 = true;
						break;
					}
				}
				if (!flag3)
				{
					playerPool.Return(punNetPlayer3);
					netPlayerCache.Remove(punNetPlayer3);
				}
			}
		}
		m_allNetPlayers = netPlayerCache.ToArray();
		m_otherNetPlayers = new NetPlayer[m_allNetPlayers.Length - 1];
		int num = 0;
		for (int l = 0; l < m_allNetPlayers.Length; l++)
		{
			NetPlayer netPlayer2 = m_allNetPlayers[l];
			if (netPlayer2.IsLocal)
			{
				num++;
				continue;
			}
			int num2 = l - num;
			if (num2 != m_otherNetPlayers.Length)
			{
				m_otherNetPlayers[num2] = netPlayer2;
				continue;
			}
			break;
		}
	}

	public override bool IsObjectRoomObject(GameObject obj)
	{
		PhotonView component = obj.GetComponent<PhotonView>();
		if (component == null)
		{
			Debug.LogError("No photonview found on this Object, this shouldn't happen");
			return false;
		}
		return component.IsRoomView;
	}

	public override bool ShouldUpdateObject(GameObject obj)
	{
		return IsObjectLocallyOwned(obj);
	}

	public override bool ShouldWriteObjectData(GameObject obj)
	{
		return IsObjectLocallyOwned(obj);
	}

	public override int GetOwningPlayerID(GameObject obj)
	{
		if (obj.TryGetComponent<PhotonView>(out var component) && component.Owner != null)
		{
			return component.Owner.ActorNumber;
		}
		return -1;
	}

	public override bool ShouldSpawnLocally(int playerID)
	{
		if (LocalPlayerID != playerID)
		{
			if (playerID == -1)
			{
				return PhotonNetwork.MasterClient.IsLocal;
			}
			return false;
		}
		return true;
	}

	public override bool IsTotalAuthority()
	{
		return false;
	}

	public void OnConnectedtoMaster()
	{
		if (internalState == InternalState.ConnectingToMaster)
		{
			internalState = InternalState.ConnectedToMaster;
		}
		UpdatePlayers();
	}

	public void OnJoinedRoom()
	{
		if (internalState == InternalState.Searching_Joining)
		{
			internalState = InternalState.Searching_Joined;
		}
		else if (internalState == InternalState.Searching_Creating)
		{
			internalState = InternalState.Searching_Created;
		}
		AddVoice();
		UpdatePlayers();
		JoinedNetworkRoom();
	}

	public void OnJoinRoomFailed(short returnCode, string message)
	{
		PersistLog.Log("OnJoinRoomFailed " + returnCode + " " + message);
		if (internalState == InternalState.Searching_Joining)
		{
			switch (returnCode)
			{
			case 32758:
				internalState = InternalState.Searching_JoinFailed_NotFound;
				break;
			case 32765:
				internalState = InternalState.Searching_JoinFailed_Full;
				break;
			default:
				internalState = InternalState.Searching_JoinFailed_Other;
				break;
			}
		}
	}

	public void OnCreateRoomFailed(short returnCode, string message)
	{
		PersistLog.Log("OnCreateRoomFailed " + returnCode + " " + message);
		if (internalState == InternalState.Searching_Creating)
		{
			internalState = InternalState.Searching_CreateFailed;
		}
	}

	public void OnPlayerEnteredRoom(Player newPlayer)
	{
		UpdatePlayers();
		NetPlayer player = GetPlayer(newPlayer);
		PlayerJoined(player);
	}

	public void OnPlayerLeftRoom(Player otherPlayer)
	{
		NetPlayer player = GetPlayer(otherPlayer);
		UpdatePlayers();
		PlayerLeft(player);
	}

	public async void OnDisconnected(DisconnectCause cause)
	{
		if (!ApplicationQuittingState.IsQuitting)
		{
			groupJoinOverrideGameMode = "";
			await RefreshNonce();
			if (internalState == InternalState.Searching_Disconnecting)
			{
				internalState = InternalState.Searching_Disconnected;
				return;
			}
			if (internalState == InternalState.PingGathering)
			{
				internalState = InternalState.Internal_Disconnected;
				return;
			}
			if (internalState == InternalState.Internal_Disconnecting)
			{
				internalState = InternalState.Internal_Disconnected;
				return;
			}
			UpdatePlayers();
			SinglePlayerStarted();
		}
	}

	public void OnMasterClientSwitched(Player newMasterClient)
	{
		OnMasterClientSwitchedCallback(newMasterClient);
	}

	private (CancellationTokenSource, CancellationToken) GetCancellationToken()
	{
		CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
		CancellationToken token = cancellationTokenSource.Token;
		_taskCancelTokens.Add(cancellationTokenSource);
		return (cancellationTokenSource, token);
	}

	public void ResetSystem()
	{
		if ((bool)VoiceNetworkObject)
		{
			UnityEngine.Object.Destroy(VoiceNetworkObject);
		}
		PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = regionNames[lowestPingRegionIndex];
		currentRegionIndex = lowestPingRegionIndex;
		PhotonNetwork.Disconnect();
		_taskCancelTokens.ForEach(delegate(CancellationTokenSource token)
		{
			token.Cancel();
			token.Dispose();
		});
		_taskCancelTokens.Clear();
		internalState = InternalState.Idle;
		base.netState = NetSystemState.Idle;
	}

	private void UpdateZoneInfo(bool roomIsPublic, string zoneName = null)
	{
		AuthenticationValues authenticationValues = GetAuthenticationValues();
		if (authenticationValues?.AuthPostData is Dictionary<string, object> dictionary)
		{
			dictionary["Zone"] = ((zoneName != null) ? zoneName : ((ZoneManagement.instance.activeZones.Count > 0) ? ZoneManagement.instance.activeZones.First().GetName() : ""));
			dictionary["SubZone"] = GTSubZone.none.GetName();
			dictionary["IsPublic"] = roomIsPublic;
			authenticationValues.SetAuthPostData(dictionary);
			SetAuthenticationValues(authenticationValues);
		}
	}
}
