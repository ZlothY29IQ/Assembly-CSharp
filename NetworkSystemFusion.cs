using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Fusion;
using Fusion.Photon.Realtime;
using Fusion.Sockets;
using GorillaGameModes;
using GorillaNetworking;
using GorillaTag;
using GorillaTag.Audio;
using Photon.Realtime;
using Photon.Voice.Unity;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkSystemFusion : NetworkSystem
{
	private enum InternalState
	{
		AwaitingAuth,
		Idle,
		Searching_Joining,
		Searching_Joined,
		Searching_JoinFailed,
		Searching_Disconnecting,
		Searching_Disconnected,
		ConnectingToRoom,
		ConnectedToRoom,
		JoinRoomFailed,
		Disconnecting,
		Disconnected,
		StateCheckFailed
	}

	private InternalState internalState;

	private FusionInternalRPCs internalRPCProvider;

	private FusionCallbackHandler callbackHandler;

	private FusionRegionCrawler regionCrawler;

	private GameObject volatileNetObj;

	private Fusion.Photon.Realtime.AuthenticationValues cachedPlayfabAuth;

	private const string playerPropertiesPath = "P_FusionProperties";

	private bool lastConnectAttempt_WasFull;

	private VoiceConnection FusionVoice;

	private CustomObjectProvider myObjectProvider;

	private ObjectPool<FusionNetPlayer> playerPool;

	public List<NetworkObject> cachedNetSceneObjects = new List<NetworkObject>();

	private List<INetworkRunnerCallbacks> objectsThatNeedCallbacks = new List<INetworkRunnerCallbacks>();

	private Queue<NetworkObject> registrationQueue = new Queue<NetworkObject>();

	private bool isProcessingQueue;

	public NetworkRunner runner { get; private set; }

	public override bool IsOnline
	{
		get
		{
			if (runner != null)
			{
				return !runner.IsSinglePlayer;
			}
			return false;
		}
	}

	public override bool InRoom
	{
		get
		{
			if (runner != null && runner.State != NetworkRunner.States.Shutdown && !runner.IsSinglePlayer)
			{
				return runner.IsConnectedToServer;
			}
			return false;
		}
	}

	public override string RoomName => runner.SessionInfo?.Name;

	public override string GameModeString
	{
		get
		{
			runner.SessionInfo.Properties.TryGetValue("gameMode", out var value);
			if (value != null)
			{
				return (string)value.PropertyValue;
			}
			return null;
		}
	}

	public override string CurrentRegion => runner.SessionInfo?.Region;

	public override bool SessionIsPrivate
	{
		get
		{
			NetworkRunner networkRunner = runner;
			bool? obj;
			if ((object)networkRunner == null)
			{
				obj = null;
			}
			else
			{
				SessionInfo sessionInfo = networkRunner.SessionInfo;
				obj = ((sessionInfo != null) ? new bool?(!sessionInfo.IsVisible) : ((bool?)null));
			}
			bool? flag = obj;
			return flag == true;
		}
	}

	public override bool SessionIsSubscription
	{
		get
		{
			NetworkRunner networkRunner = runner;
			if ((object)networkRunner == null)
			{
				return false;
			}
			return networkRunner.SessionInfo?.MaxPlayers > 10;
		}
	}

	public override int LocalPlayerID => runner.LocalPlayer.PlayerId;

	public override string CurrentPhotonBackend => "Fusion";

	public override double SimTime => runner.SimulationTime;

	public override float SimDeltaTime => runner.DeltaTime;

	public override int SimTick => runner.Tick.Raw;

	public override int TickRate => runner.TickRate;

	public override int ServerTimestamp => runner.Tick.Raw;

	public override int RoomPlayerCount => runner.SessionInfo.PlayerCount;

	public override VoiceConnection VoiceConnection => FusionVoice;

	public override bool IsMasterClient => runner?.IsSharedModeMasterClient ?? true;

	public override NetPlayer MasterClient
	{
		get
		{
			if (!(runner != null) || !runner.IsSharedModeMasterClient)
			{
				if (!(GorillaGameModes.GameMode.ActiveNetworkHandler != null))
				{
					return null;
				}
				return GetPlayer(GorillaGameModes.GameMode.ActiveNetworkHandler.Object.StateAuthority);
			}
			return GetPlayer(runner.LocalPlayer);
		}
	}

	public override string RoomStringStripped()
	{
		SessionInfo sessionInfo = runner.SessionInfo;
		NetworkSystem.reusableSB.Clear();
		NetworkSystem.reusableSB.AppendFormat("Room: '{0}' ", (sessionInfo.Name.Length < 20) ? sessionInfo.Name : sessionInfo.Name.Remove(20));
		NetworkSystem.reusableSB.AppendFormat("{0},{1} {3}/{2} players.", sessionInfo.IsVisible ? "visible" : "hidden", sessionInfo.IsOpen ? "open" : "closed", sessionInfo.MaxPlayers, sessionInfo.PlayerCount);
		NetworkSystem.reusableSB.Append("\ncustomProps: {");
		NetworkSystem.reusableSB.AppendFormat("joinedGameMode={0}, ", (RoomSystem.RoomGameMode.Length < 50) ? RoomSystem.RoomGameMode : RoomSystem.RoomGameMode.Remove(50));
		IDictionary properties = sessionInfo.Properties;
		UnityEngine.Debug.Log(RoomSystem.RoomGameMode.ToString());
		if (properties.Contains("gameMode"))
		{
			object obj = properties["gameMode"];
			if (obj == null)
			{
				NetworkSystem.reusableSB.AppendFormat("gameMode=null}");
			}
			else if (obj is string text)
			{
				NetworkSystem.reusableSB.AppendFormat("gameMode={0}", (text.Length < 50) ? text : text.Remove(50));
			}
		}
		NetworkSystem.reusableSB.Append("}");
		UnityEngine.Debug.Log(NetworkSystem.reusableSB.ToString());
		return NetworkSystem.reusableSB.ToString();
	}

	public override async void Initialise()
	{
		base.Initialise();
		myObjectProvider = new CustomObjectProvider();
		base.netState = NetSystemState.Initialization;
		internalState = InternalState.Idle;
		await ReturnToSinglePlayer();
		AwaitAuth();
		CreateRegionCrawler();
		GameModeSerializer.FusionGameModeOwnerChanged = (Action<NetPlayer>)Delegate.Combine(GameModeSerializer.FusionGameModeOwnerChanged, new Action<NetPlayer>(base.OnMasterClientSwitchedCallback));
		OnMasterClientSwitchedEvent += new Action<NetPlayer>(OnMasterSwitch);
		base.netState = NetSystemState.Idle;
		playerPool = new ObjectPool<FusionNetPlayer>(20);
		UpdatePlayers();
	}

	private void CreateRegionCrawler()
	{
		GameObject gameObject = new GameObject("[Network Crawler]");
		gameObject.transform.SetParent(base.transform);
		regionCrawler = gameObject.AddComponent<FusionRegionCrawler>();
	}

	private async Task AwaitAuth()
	{
		internalState = InternalState.AwaitingAuth;
		while (cachedPlayfabAuth == null)
		{
			await Task.Yield();
		}
		internalState = InternalState.Idle;
		base.netState = NetSystemState.Idle;
	}

	public override void FinishAuthenticating()
	{
		if (cachedPlayfabAuth != null)
		{
			UnityEngine.Debug.Log("AUTHED");
		}
		else
		{
			UnityEngine.Debug.LogError("Authentication Failed");
		}
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
		base.netState = NetSystemState.Connecting;
		Utils.Log("Connecting to:" + (string.IsNullOrEmpty(roomName) ? "random room" : roomName));
		NetJoinResult result;
		if (!string.IsNullOrEmpty(roomName))
		{
			Task<NetJoinResult> makeOrJoinTask = MakeOrJoinRoom(roomName, opts);
			await makeOrJoinTask;
			result = makeOrJoinTask.Result;
		}
		else
		{
			Task<NetJoinResult> makeOrJoinTask = JoinRandomPublicRoom(opts);
			await makeOrJoinTask;
			result = makeOrJoinTask.Result;
		}
		switch (result)
		{
		case NetJoinResult.Failed_Full:
		case NetJoinResult.Failed_Other:
			ResetSystem();
			return result;
		case NetJoinResult.AlreadyInRoom:
			base.netState = NetSystemState.InGame;
			return result;
		default:
			UpdatePlayers();
			base.netState = NetSystemState.InGame;
			Utils.Log("Connect to room result: " + result);
			return result;
		}
	}

	private async Task<bool> Connect(Fusion.GameMode mode, string targetSessionName, RoomConfig opts)
	{
		if (runner != null)
		{
			bool goingBetweenRooms = InRoom && mode != Fusion.GameMode.Single;
			await CloseRunner();
			await Task.Yield();
			if (goingBetweenRooms)
			{
				SinglePlayerStarted();
				await Task.Yield();
			}
		}
		if ((bool)volatileNetObj)
		{
			UnityEngine.Debug.LogError("Volatile net obj should not exist - destroying and recreating");
			UnityEngine.Object.Destroy(volatileNetObj);
		}
		volatileNetObj = new GameObject("VolatileFusionObj");
		volatileNetObj.transform.parent = base.transform;
		runner = volatileNetObj.AddComponent<NetworkRunner>();
		internalRPCProvider = runner.AddBehaviour<FusionInternalRPCs>();
		callbackHandler = volatileNetObj.AddComponent<FusionCallbackHandler>();
		callbackHandler.Setup(this);
		AttachCallbackTargets();
		lastConnectAttempt_WasFull = false;
		internalState = InternalState.ConnectingToRoom;
		Dictionary<string, SessionProperty> sessionProperties = opts.CustomProps?.ToPropDict();
		myObjectProvider.SceneObjects = SceneObjectsToAttach;
		NetworkSceneManagerDefault sceneManager = volatileNetObj.AddComponent<NetworkSceneManagerDefault>();
		Task<Fusion.StartGameResult> startupTask = runner.StartGame(new StartGameArgs
		{
			IsVisible = opts.isPublic,
			IsOpen = opts.isJoinable,
			GameMode = mode,
			SessionName = targetSessionName,
			PlayerCount = opts.MaxPlayers,
			SceneManager = sceneManager,
			AuthValues = cachedPlayfabAuth,
			SessionProperties = sessionProperties,
			EnableClientSessionCreation = opts.createIfMissing,
			ObjectProvider = myObjectProvider
		});
		await startupTask;
		Utils.Log("Startuptask finished : " + startupTask.Result.ToString());
		if (!startupTask.Result.Ok)
		{
			base.CurrentRoom = null;
			return startupTask.Result.Ok;
		}
		if (cachedNetSceneObjects.Count > 0)
		{
			foreach (NetworkObject cachedNetSceneObject in cachedNetSceneObjects)
			{
				registrationQueue.Enqueue(cachedNetSceneObject);
			}
		}
		AttachSceneObjects();
		AddVoice();
		base.CurrentRoom = opts;
		if (IsTotalAuthority() || runner.IsSharedModeMasterClient)
		{
			opts.SetFusionOpts(runner);
		}
		SetMyNickName(GorillaComputer.instance.savedName);
		return startupTask.Result.Ok;
	}

	private async Task<NetJoinResult> MakeOrJoinRoom(string roomName, RoomConfig opts)
	{
		int currentRegionIndex = 0;
		bool flag = false;
		opts.createIfMissing = false;
		while (currentRegionIndex < regionNames.Length && !flag)
		{
			try
			{
				PhotonAppSettings.Global.AppSettings.FixedRegion = regionNames[currentRegionIndex];
				internalState = InternalState.Searching_Joining;
				Task<bool> connectTask = Connect(Fusion.GameMode.Shared, roomName, opts);
				await connectTask;
				flag = connectTask.Result;
				if (!flag)
				{
					if (lastConnectAttempt_WasFull)
					{
						Utils.Log("Found room but it was full");
						break;
					}
					Utils.Log("Region incrimenting");
					int num = currentRegionIndex + 1;
					currentRegionIndex = num;
				}
			}
			catch (Exception ex)
			{
				UnityEngine.Debug.LogError("MakeOrJoinRoom - message: " + ex.Message + "\nStacktrace : " + ex.StackTrace);
				return NetJoinResult.Failed_Other;
			}
		}
		if (lastConnectAttempt_WasFull)
		{
			PhotonAppSettings.Global.AppSettings.FixedRegion = "";
			return NetJoinResult.Failed_Full;
		}
		if (!flag)
		{
			PhotonAppSettings.Global.AppSettings.FixedRegion = "";
			opts.createIfMissing = true;
			Task<bool> connectTask = Connect(Fusion.GameMode.Shared, roomName, opts);
			await connectTask;
			Utils.Log("made room?");
			if (!connectTask.Result)
			{
				UnityEngine.Debug.LogError("NS-FUS] Failed to create private room");
				return NetJoinResult.Failed_Other;
			}
			while (!runner.SessionInfo.IsValid)
			{
				await Task.Yield();
			}
			return NetJoinResult.FallbackCreated;
		}
		return NetJoinResult.Success;
	}

	private async Task<NetJoinResult> JoinRandomPublicRoom(RoomConfig opts)
	{
		bool shouldCreateIfNone = opts.createIfMissing;
		PhotonAppSettings.Global.AppSettings.FixedRegion = "";
		internalState = InternalState.Searching_Joining;
		opts.createIfMissing = false;
		Task<bool> connectTask = Connect(Fusion.GameMode.Shared, null, opts);
		await connectTask;
		if (!connectTask.Result && shouldCreateIfNone)
		{
			opts.createIfMissing = shouldCreateIfNone;
			string randomRoomName = NetworkSystem.GetRandomRoomName();
			Task<bool> createTask = Connect(Fusion.GameMode.Shared, randomRoomName, opts);
			await createTask;
			if (!createTask.Result)
			{
				UnityEngine.Debug.LogError("NS-FUS] Failed to create public room");
				return NetJoinResult.Failed_Other;
			}
			opts.SetFusionOpts(runner);
			return NetJoinResult.FallbackCreated;
		}
		return NetJoinResult.Success;
	}

	public override async Task JoinFriendsRoom(string userID, int actorIDToFollow, string keyToFollow, string shufflerToFollow)
	{
		bool foundFriend = false;
		float searchStartTime = Time.realtimeSinceStartup;
		float timeToSpendSearching = 15f;
		Dictionary<string, PlayFab.ClientModels.SharedGroupDataRecord> dummyData = new Dictionary<string, PlayFab.ClientModels.SharedGroupDataRecord>();
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
				}, delegate(GetSharedGroupDataResult result)
				{
					data = result.Data;
					UnityEngine.Debug.Log($"Got friend follow data, {data.Count} entries");
					callbackFinished = true;
				}, delegate(PlayFabError error)
				{
					UnityEngine.Debug.Log($"GetSharedGroupData returns error: {error}");
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
					if (regionIndex >= 0 && regionIndex < NetworkSystem.Instance.regionNames.Length)
					{
						foundFriend = true;
						NetPlayer player = GetPlayer(actorIDToFollow);
						if (InRoom && GetPlayer(actorIDToFollow) != null)
						{
							MonkeAgent.instance.SendReport("possible kick attempt", player.UserId, player.NickName);
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
							_ = ConnectToRoomTask.Result;
						}
					}
				}
				await Task.Delay(500);
			}
		}
		finally
		{
			base.groupJoinInProgress = false;
		}
	}

	public override void JoinPubWithFriends()
	{
		throw new NotImplementedException();
	}

	public override async Task ReturnToSinglePlayer()
	{
		if (base.netState == NetSystemState.InGame || base.netState == NetSystemState.Initialization)
		{
			base.netState = NetSystemState.Disconnecting;
			Utils.Log("Returning to single player");
			if ((bool)runner)
			{
				await CloseRunner();
				await Task.Yield();
				Utils.Log("Connect in return to single player");
			}
			base.netState = NetSystemState.Idle;
			internalState = InternalState.Idle;
			SinglePlayerStarted();
		}
	}

	private async Task CloseRunner(ShutdownReason reason = ShutdownReason.Ok)
	{
		internalState = InternalState.Disconnecting;
		try
		{
			await runner.Shutdown(destroyGameObject: true, reason);
		}
		catch (Exception ex)
		{
			StackFrame frame = new StackTrace(ex, fNeedFileInfo: true).GetFrame(0);
			int fileLineNumber = frame.GetFileLineNumber();
			UnityEngine.Debug.LogError(ex.Message + " File:" + frame.GetFileName() + " line: " + fileLineNumber);
		}
		if (Application.isPlaying)
		{
			UnityEngine.Object.Destroy(volatileNetObj);
		}
		else
		{
			UnityEngine.Object.DestroyImmediate(volatileNetObj);
		}
		internalState = InternalState.Disconnected;
	}

	public async void MigrateHost(NetworkRunner runner, HostMigrationToken hostMigrationToken)
	{
		Utils.Log("HOSTTEST : MigrateHostTriggered, returning to single player!");
		await ReturnToSinglePlayer();
	}

	public async void ResetSystem()
	{
		if (Application.isPlaying)
		{
			StopAllCoroutines();
			await Connect(Fusion.GameMode.Single, "--", RoomConfig.SPConfig());
			Utils.Log("Connect in return to single player");
			base.netState = NetSystemState.Idle;
			internalState = InternalState.Idle;
		}
	}

	private void AddVoice()
	{
		SetupVoice();
	}

	private void SetupVoice()
	{
		Utils.Log("<color=orange>Adding Voice Stuff</color>");
		FusionVoice = volatileNetObj.AddComponent<VoiceConnection>();
		FusionVoice.LogLevel = VoiceSettings.LogLevel;
		FusionVoice.GlobalRecordersLogLevel = VoiceSettings.GlobalRecordersLogLevel;
		FusionVoice.GlobalSpeakersLogLevel = VoiceSettings.GlobalSpeakersLogLevel;
		FusionVoice.AutoCreateSpeakerIfNotFound = VoiceSettings.CreateSpeakerIfNotFound;
		Photon.Realtime.AppSettings appSettings = new Photon.Realtime.AppSettings();
		appSettings.AppIdFusion = PhotonAppSettings.Global.AppSettings.AppIdFusion;
		appSettings.AppIdVoice = PhotonAppSettings.Global.AppSettings.AppIdVoice;
		FusionVoice.Settings = appSettings;
		remoteVoiceAddedCallbacks.ForEach(delegate(Action<RemoteVoiceLink> callback)
		{
			FusionVoice.RemoteVoiceAdded += callback;
		});
		localRecorder = volatileNetObj.AddComponent<Recorder>();
		localRecorder.LogLevel = VoiceSettings.LogLevel;
		localRecorder.RecordOnlyWhenEnabled = VoiceSettings.RecordOnlyWhenEnabled;
		localRecorder.RecordOnlyWhenJoined = VoiceSettings.RecordOnlyWhenJoined;
		localRecorder.StopRecordingWhenPaused = VoiceSettings.StopRecordingWhenPaused;
		localRecorder.TransmitEnabled = VoiceSettings.TransmitEnabled;
		localRecorder.AutoStart = VoiceSettings.AutoStart;
		localRecorder.Encrypt = VoiceSettings.Encrypt;
		localRecorder.FrameDuration = VoiceSettings.FrameDuration;
		localRecorder.SamplingRate = VoiceSettings.SamplingRate;
		localRecorder.InterestGroup = VoiceSettings.InterestGroup;
		localRecorder.SourceType = VoiceSettings.InputSourceType;
		localRecorder.MicrophoneType = VoiceSettings.MicrophoneType;
		localRecorder.UseMicrophoneTypeFallback = VoiceSettings.UseFallback;
		localRecorder.VoiceDetection = VoiceSettings.Detect;
		localRecorder.VoiceDetectionThreshold = VoiceSettings.Threshold;
		localRecorder.Bitrate = VoiceSettings.Bitrate;
		localRecorder.VoiceDetectionDelayMs = VoiceSettings.Delay;
		localRecorder.DebugEchoMode = VoiceSettings.DebugEcho;
		localRecorder.UserData = runner.UserId;
		FusionVoice.PrimaryRecorder = localRecorder;
		volatileNetObj.AddComponent<VoiceToLoudness>();
	}

	public override void AddRemoteVoiceAddedCallback(Action<RemoteVoiceLink> callback)
	{
		remoteVoiceAddedCallbacks.Add(callback);
	}

	private void AttachCallbackTargets()
	{
		runner.AddCallbacks(objectsThatNeedCallbacks.ToArray());
	}

	public void RegisterForNetworkCallbacks(INetworkRunnerCallbacks callbacks)
	{
		if (!objectsThatNeedCallbacks.Contains(callbacks))
		{
			objectsThatNeedCallbacks.Add(callbacks);
		}
		if (runner != null)
		{
			runner.AddCallbacks(callbacks);
		}
	}

	private async void AttachSceneObjects(bool onlyCached = false)
	{
		if (!onlyCached)
		{
			SceneObjectsToAttach.ForEach(delegate(GameObject obj)
			{
				if (!cachedNetSceneObjects.Exists((NetworkObject o) => o.gameObject == obj.gameObject))
				{
					NetworkObject component = obj.GetComponent<NetworkObject>();
					if (component == null)
					{
						UnityEngine.Debug.LogWarning("no network object on scene item - " + obj.name);
					}
					else
					{
						cachedNetSceneObjects.Add(component);
						registrationQueue.Enqueue(component);
					}
				}
			});
		}
		await Task.Delay(5);
		ProcessRegistrationQueue();
	}

	public override void AttachObjectInGame(GameObject item)
	{
		base.AttachObjectInGame(item);
		NetworkObject component = item.GetComponent<NetworkObject>();
		if ((component != null && !cachedNetSceneObjects.Contains(component)) || !component.IsValid)
		{
			cachedNetSceneObjects.AddIfNew(component);
			registrationQueue.Enqueue(component);
			ProcessRegistrationQueue();
		}
	}

	private void ProcessRegistrationQueue()
	{
		if (isProcessingQueue)
		{
			UnityEngine.Debug.LogError("Queue is still processing");
			return;
		}
		isProcessingQueue = true;
		List<NetworkObject> list = new List<NetworkObject>();
		SceneRef scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
		while (registrationQueue.Count > 0)
		{
			NetworkObject networkObject = registrationQueue.Dequeue();
			if (InRoom && !networkObject.IsValid && !networkObject.Id.IsValid && networkObject.Runner == null)
			{
				try
				{
					list.Add(networkObject);
				}
				catch (Exception exception)
				{
					UnityEngine.Debug.LogException(exception);
					isProcessingQueue = false;
					runner.RegisterSceneObjects(scene, list.ToArray());
					ProcessRegistrationQueue();
					break;
				}
			}
		}
		runner.RegisterSceneObjects(scene, list.ToArray());
		isProcessingQueue = false;
	}

	public override GameObject NetInstantiate(GameObject prefab, Vector3 position, Quaternion rotation, bool isRoomObject = false)
	{
		Utils.Log("Net instantiate Fusion: " + prefab.name);
		try
		{
			return runner.Spawn(prefab, position, rotation, runner.LocalPlayer).gameObject;
		}
		catch (Exception message)
		{
			UnityEngine.Debug.LogError(message);
		}
		return null;
	}

	public override GameObject NetInstantiate(GameObject prefab, Vector3 position, Quaternion rotation, int playerAuthID, bool isRoomObject = false)
	{
		foreach (PlayerRef activePlayer in runner.ActivePlayers)
		{
			if (activePlayer.PlayerId == playerAuthID)
			{
				Utils.Log("Net instantiate Fusion: " + prefab.name);
				return runner.Spawn(prefab, position, rotation, activePlayer).gameObject;
			}
		}
		UnityEngine.Debug.LogError($"Couldn't find player with ID: {playerAuthID}, cancelling requested spawn...");
		return null;
	}

	public override GameObject NetInstantiate(GameObject prefab, Vector3 position, Quaternion rotation, bool isRoomObject, byte group = 0, object[] data = null, NetworkRunner.OnBeforeSpawned callback = null)
	{
		Utils.Log("Net instantiate Fusion: " + prefab.name);
		return runner.Spawn(prefab, position, rotation, runner.LocalPlayer, callback).gameObject;
	}

	public override void NetDestroy(GameObject instance)
	{
		if (instance.TryGetComponent<NetworkObject>(out var component))
		{
			runner.Despawn(component);
		}
		else
		{
			UnityEngine.Object.Destroy(instance);
		}
	}

	public override bool ShouldSpawnLocally(int playerID)
	{
		if (runner.GameMode == Fusion.GameMode.Shared)
		{
			if (runner.LocalPlayer.PlayerId != playerID)
			{
				if (playerID == -1)
				{
					return runner.IsSharedModeMasterClient;
				}
				return false;
			}
			return true;
		}
		return runner.GameMode != Fusion.GameMode.Client;
	}

	public override void CallRPC(MonoBehaviour component, RPC rpcMethod, bool sendToSelf = true)
	{
		Utils.Log(rpcMethod.GetDelegateName() + "RPC called!");
		foreach (PlayerRef activePlayer in runner.ActivePlayers)
		{
			if (!sendToSelf)
			{
				_ = activePlayer != runner.LocalPlayer;
			}
		}
	}

	public override void CallRPC<T>(MonoBehaviour component, RPC rpcMethod, RPCArgBuffer<T> args, bool sendToSelf = true)
	{
		Utils.Log(rpcMethod.GetDelegateName() + "RPC called!");
		NetCrossoverUtils.SerializeToRPCData(ref args);
		foreach (PlayerRef activePlayer in runner.ActivePlayers)
		{
			if (!sendToSelf)
			{
				_ = activePlayer != runner.LocalPlayer;
			}
		}
	}

	public override void CallRPC(MonoBehaviour component, StringRPC rpcMethod, string message, bool sendToSelf = true)
	{
		foreach (PlayerRef activePlayer in runner.ActivePlayers)
		{
			if (!sendToSelf)
			{
				_ = activePlayer != runner.LocalPlayer;
			}
		}
	}

	public override void CallRPC(int targetPlayerID, MonoBehaviour component, RPC rpcMethod)
	{
		GetPlayerRef(targetPlayerID);
		Utils.Log(rpcMethod.GetDelegateName() + "RPC called!");
	}

	public override void CallRPC<T>(int targetPlayerID, MonoBehaviour component, RPC rpcMethod, RPCArgBuffer<T> args)
	{
		Utils.Log(rpcMethod.GetDelegateName() + "RPC called!");
		GetPlayerRef(targetPlayerID);
	}

	public override void CallRPC(int targetPlayerID, MonoBehaviour component, StringRPC rpcMethod, string message)
	{
		GetPlayerRef(targetPlayerID);
	}

	public override void NetRaiseEventReliable(byte eventCode, object data)
	{
		byte[] byteData = data.ByteSerialize();
		FusionCallbackHandler.RPC_OnEventRaisedReliable(runner, eventCode, byteData, hasOps: false, null);
	}

	public override void NetRaiseEventUnreliable(byte eventCode, object data)
	{
		byte[] byteData = data.ByteSerialize();
		FusionCallbackHandler.RPC_OnEventRaisedUnreliable(runner, eventCode, byteData, hasOps: false, null);
	}

	public override void NetRaiseEventReliable(byte eventCode, object data, NetEventOptions opts)
	{
		byte[] byteData = data.ByteSerialize();
		byte[] netOptsData = opts.ByteSerialize();
		FusionCallbackHandler.RPC_OnEventRaisedReliable(runner, eventCode, byteData, hasOps: true, netOptsData);
	}

	public override void NetRaiseEventUnreliable(byte eventCode, object data, NetEventOptions opts)
	{
		byte[] byteData = data.ByteSerialize();
		byte[] netOptsData = opts.ByteSerialize();
		FusionCallbackHandler.RPC_OnEventRaisedUnreliable(runner, eventCode, byteData, hasOps: true, netOptsData);
	}

	public override string GetRandomWeightedRegion()
	{
		throw new NotImplementedException();
	}

	public override async Task AwaitSceneReady()
	{
		while (runner.SceneManager.IsBusy)
		{
			await Task.Yield();
		}
		for (float counter = 0f; counter < 0.5f; counter += Time.deltaTime)
		{
			await Task.Yield();
		}
	}

	public void OnJoinedSession()
	{
	}

	public void OnJoinFailed(NetConnectFailedReason reason)
	{
		switch (reason)
		{
		case NetConnectFailedReason.ServerFull:
			lastConnectAttempt_WasFull = true;
			break;
		case NetConnectFailedReason.Timeout:
		case NetConnectFailedReason.ServerRefused:
			break;
		}
	}

	public void OnDisconnectedFromSession()
	{
		Utils.Log("On Disconnected");
		internalState = InternalState.Disconnected;
		UpdatePlayers();
	}

	public void OnRunnerShutDown()
	{
		Utils.Log("Runner shutdown callback");
		if (internalState == InternalState.Disconnecting)
		{
			internalState = InternalState.Disconnected;
		}
	}

	public void OnFusionPlayerJoined(PlayerRef player)
	{
		AwaitJoiningPlayerClientReady(player);
	}

	private async Task AwaitJoiningPlayerClientReady(PlayerRef player)
	{
		UpdatePlayers();
		if (runner != null && player == runner.LocalPlayer && !runner.IsSinglePlayer)
		{
			Utils.Log("JoinedNetworkRoom");
			await Task.Delay(8);
			JoinedNetworkRoom();
		}
		if (runner != null && player == runner.LocalPlayer && runner.IsSinglePlayer)
		{
			SinglePlayerStarted();
		}
		await Task.Delay(200);
		NetPlayer joiningPlayer = GetPlayer(player);
		if (joiningPlayer == null)
		{
			UnityEngine.Debug.LogError("Joining player doesnt have a NetPlayer somehow, this shouldnt happen");
		}
		while (joiningPlayer.NickName.IsNullOrEmpty())
		{
			await Task.Delay(1);
		}
		PlayerJoined(joiningPlayer);
	}

	public void OnFusionPlayerLeft(PlayerRef player)
	{
		if (IsTotalAuthority())
		{
			NetworkObject playerObject = runner.GetPlayerObject(player);
			if (playerObject != null)
			{
				Utils.Log("Destroying player object for leaving player!");
				NetDestroy(playerObject.gameObject);
			}
			else
			{
				Utils.Log("Player left without destroying an avatar for it somehow?");
			}
		}
		NetPlayer player2 = GetPlayer(player);
		if (player2 == null)
		{
			UnityEngine.Debug.LogError("Joining player doesnt have a NetPlayer somehow, this shouldnt happen");
		}
		PlayerLeft(player2);
		UpdatePlayers();
	}

	protected override void UpdateNetPlayerList()
	{
		if (runner == null && (netPlayerCache.Count > 1 || !netPlayerCache.Exists((NetPlayer p) => p.IsLocal)))
		{
			netPlayerCache.ForEach(delegate(NetPlayer p)
			{
				playerPool.Return((FusionNetPlayer)p);
			});
			netPlayerCache.Clear();
			netPlayerCache.Add(new FusionNetPlayer(default(PlayerRef)));
			return;
		}
		NetPlayer[] array;
		NetPlayer[] array2;
		if (runner.IsSinglePlayer)
		{
			if (netPlayerCache.Count == 1 && netPlayerCache[0].IsLocal)
			{
				return;
			}
			bool flag = false;
			array = netPlayerCache.ToArray();
			if (netPlayerCache.Count > 0)
			{
				array2 = array;
				foreach (NetPlayer netPlayer in array2)
				{
					if (((FusionNetPlayer)netPlayer).PlayerRef == runner.LocalPlayer)
					{
						flag = true;
						continue;
					}
					playerPool.Return((FusionNetPlayer)netPlayer);
					netPlayerCache.Remove(netPlayer);
				}
			}
			if (!flag)
			{
				FusionNetPlayer fusionNetPlayer = playerPool.Take();
				fusionNetPlayer.InitPlayer(runner.LocalPlayer);
				netPlayerCache.Add(fusionNetPlayer);
			}
		}
		foreach (PlayerRef activePlayer in runner.ActivePlayers)
		{
			bool flag2 = false;
			for (int num2 = 0; num2 < netPlayerCache.Count; num2++)
			{
				if (activePlayer == ((FusionNetPlayer)netPlayerCache[num2]).PlayerRef)
				{
					flag2 = true;
				}
			}
			if (!flag2)
			{
				FusionNetPlayer fusionNetPlayer2 = playerPool.Take();
				fusionNetPlayer2.InitPlayer(activePlayer);
				netPlayerCache.Add(fusionNetPlayer2);
			}
		}
		array = netPlayerCache.ToArray();
		array2 = array;
		foreach (NetPlayer netPlayer2 in array2)
		{
			bool flag3 = false;
			foreach (PlayerRef activePlayer2 in runner.ActivePlayers)
			{
				if (activePlayer2 == ((FusionNetPlayer)netPlayer2).PlayerRef)
				{
					flag3 = true;
				}
			}
			if (!flag3)
			{
				playerPool.Return((FusionNetPlayer)netPlayer2);
				netPlayerCache.Remove(netPlayer2);
			}
		}
	}

	public override void SetPlayerObject(GameObject playerInstance, int? owningPlayerID = null)
	{
		PlayerRef player = runner.LocalPlayer;
		if (owningPlayerID.HasValue)
		{
			player = GetPlayerRef(owningPlayerID.Value);
		}
		runner.SetPlayerObject(player, playerInstance.GetComponent<NetworkObject>());
	}

	private PlayerRef GetPlayerRef(int playerID)
	{
		if (runner == null)
		{
			UnityEngine.Debug.LogWarning("There is no runner yet - returning default player ref");
			return default(PlayerRef);
		}
		foreach (PlayerRef activePlayer in runner.ActivePlayers)
		{
			if (activePlayer.PlayerId == playerID)
			{
				return activePlayer;
			}
		}
		UnityEngine.Debug.LogWarning($"GetPlayerRef - Couldn't find active player with ID #{playerID}");
		return default(PlayerRef);
	}

	public override NetPlayer GetLocalPlayer()
	{
		if (netPlayerCache.Count == 0 || netPlayerCache.Count != runner.SessionInfo.PlayerCount)
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
		UnityEngine.Debug.LogError("Somehow there is no local NetPlayer. This shoulnd't happen.");
		return null;
	}

	public override NetPlayer GetPlayer(int PlayerID)
	{
		if (PlayerID == -1)
		{
			UnityEngine.Debug.LogWarning("Attempting to get NetPlayer for local -1 ID.");
			return null;
		}
		foreach (NetPlayer item in netPlayerCache)
		{
			if (item.ActorNumber == PlayerID)
			{
				return item;
			}
		}
		if (netPlayerCache.Count == 0 || netPlayerCache.Count != runner.SessionInfo.PlayerCount)
		{
			UpdatePlayers();
			foreach (NetPlayer item2 in netPlayerCache)
			{
				if (item2.ActorNumber == PlayerID)
				{
					return item2;
				}
			}
		}
		UnityEngine.Debug.LogError("Failed to find the player, before and after resyncing the player cache, this probably shoulnd't happen...");
		return null;
	}

	public override void SetMyNickName(string name)
	{
		if (!KIDManager.HasPermissionToUseFeature(EKIDFeatures.Custom_Nametags) && !name.StartsWith("gorilla"))
		{
			UnityEngine.Debug.Log("[KID] Trying to set custom nickname but that permission has been disallowed");
			if (InRoom && GorillaTagger.Instance.rigSerializer != null)
			{
				GorillaTagger.Instance.rigSerializer.nickName = "gorilla";
			}
		}
		else
		{
			PlayerPrefs.SetString("playerName", name);
			if (InRoom && GorillaTagger.Instance.rigSerializer != null)
			{
				GorillaTagger.Instance.rigSerializer.nickName = name;
			}
		}
	}

	public override string GetMyNickName()
	{
		return PlayerPrefs.GetString("playerName");
	}

	public override string GetMyDefaultName()
	{
		return "gorilla" + UnityEngine.Random.Range(0, 9999).ToString().PadLeft(4, '0');
	}

	public override string GetNickName(int playerID)
	{
		NetPlayer player = GetPlayer(playerID);
		return GetNickName(player);
	}

	public override string GetNickName(NetPlayer player)
	{
		if (player == null)
		{
			UnityEngine.Debug.LogError("Cant get nick name as playerID doesnt have a NetPlayer...");
			return "";
		}
		VRRigCache.Instance.TryGetVrrig(player, out var playerRig);
		if (!KIDManager.HasPermissionToUseFeature(EKIDFeatures.Custom_Nametags))
		{
			return playerRig.Rig.rigSerializer.defaultName.Value ?? "";
		}
		return playerRig.Rig.rigSerializer.nickName.Value ?? "";
	}

	public override string GetMyUserID()
	{
		return runner.GetPlayerUserId(runner.LocalPlayer);
	}

	public override string GetUserID(int playerID)
	{
		if (runner == null)
		{
			return string.Empty;
		}
		return runner.GetPlayerUserId(GetPlayerRef(playerID));
	}

	public override string GetUserID(NetPlayer player)
	{
		if (runner == null)
		{
			return string.Empty;
		}
		return runner.GetPlayerUserId(((FusionNetPlayer)player).PlayerRef);
	}

	public override void SetMyTutorialComplete()
	{
		if (!(PlayerPrefs.GetString("didTutorial", "nope") == "done"))
		{
			PlayerPrefs.SetString("didTutorial", "done");
			PlayerPrefs.Save();
		}
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
			UnityEngine.Debug.LogError("Player not found");
			return false;
		}
		VRRigCache.Instance.TryGetVrrig(player, out var playerRig);
		if (playerRig == null)
		{
			UnityEngine.Debug.LogError("VRRig not found for player");
			return false;
		}
		if (playerRig.Rig.rigSerializer == null)
		{
			UnityEngine.Debug.LogWarning("Vr rig serializer is not set up on the rig yet");
			return false;
		}
		return playerRig.Rig.rigSerializer.tutorialComplete;
	}

	public override int GlobalPlayerCount()
	{
		if (regionCrawler == null)
		{
			return 0;
		}
		return regionCrawler.PlayerCountGlobal;
	}

	public override int GetOwningPlayerID(GameObject obj)
	{
		if (obj.TryGetComponent<NetworkObject>(out var component))
		{
			if (runner.GameMode == Fusion.GameMode.Shared)
			{
				return component.StateAuthority.PlayerId;
			}
			return component.InputAuthority.PlayerId;
		}
		return -1;
	}

	public override bool IsObjectLocallyOwned(GameObject obj)
	{
		if (obj.TryGetComponent<NetworkObject>(out var component))
		{
			if (runner.GameMode == Fusion.GameMode.Shared)
			{
				return component.StateAuthority == runner.LocalPlayer;
			}
			return component.InputAuthority == runner.LocalPlayer;
		}
		return false;
	}

	public override bool IsTotalAuthority()
	{
		if (runner.Mode != SimulationModes.Server && runner.Mode != SimulationModes.Host && runner.GameMode != Fusion.GameMode.Single)
		{
			return runner.IsSharedModeMasterClient;
		}
		return true;
	}

	public override bool ShouldWriteObjectData(GameObject obj)
	{
		if (obj.TryGetComponent<NetworkObject>(out var component))
		{
			return component.HasStateAuthority;
		}
		return false;
	}

	public override bool ShouldUpdateObject(GameObject obj)
	{
		if (obj.TryGetComponent<NetworkObject>(out var component))
		{
			if (IsTotalAuthority())
			{
				return true;
			}
			if (component.InputAuthority.IsRealPlayer && !component.InputAuthority.IsRealPlayer)
			{
				return component.InputAuthority == runner.LocalPlayer;
			}
			return runner.IsSharedModeMasterClient;
		}
		return true;
	}

	public override bool IsObjectRoomObject(GameObject obj)
	{
		if (obj.TryGetComponent<NetworkObject>(out var _))
		{
			UnityEngine.Debug.LogWarning("Fusion currently automatically passes false for roomobject check.");
			return false;
		}
		return false;
	}

	private void OnMasterSwitch(NetPlayer player)
	{
		if (runner.IsSharedModeMasterClient)
		{
			Dictionary<string, SessionProperty> customProperties = new Dictionary<string, SessionProperty> { 
			{
				"MasterClient",
				base.LocalPlayer.ActorNumber
			} };
			runner.SessionInfo.UpdateCustomProperties(customProperties);
		}
	}
}
