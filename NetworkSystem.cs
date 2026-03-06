using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using GorillaNetworking;
using GorillaTag;
using Photon.Realtime;
using Photon.Voice.Unity;
using PlayFab;
using PlayFab.ClientModels;
using Steamworks;
using UnityEngine;

public abstract class NetworkSystem : MonoBehaviour
{
	public delegate void RPC(byte[] data);

	public delegate void StringRPC(string message);

	public delegate void StaticRPC(byte[] data);

	public delegate void StaticRPCPlaceholder(byte[] args);

	public static NetworkSystem Instance;

	public NetworkSystemConfig config;

	public bool changingSceneManually;

	public string[] regionNames;

	public int currentRegionIndex;

	private bool nonceRefreshed;

	protected bool isWrongVersion;

	private NetSystemState testState;

	protected List<NetPlayer> netPlayerCache = new List<NetPlayer>();

	protected Recorder localRecorder;

	protected Speaker localSpeaker;

	public List<GameObject> SceneObjectsToAttach = new List<GameObject>();

	protected SO_NetworkVoiceSettings VoiceSettings;

	protected List<Action<RemoteVoiceLink>> remoteVoiceAddedCallbacks = new List<Action<RemoteVoiceLink>>();

	public DelegateListProcessor OnJoinedRoomEvent = new DelegateListProcessor();

	public DelegateListProcessor OnMultiplayerStarted = new DelegateListProcessor();

	public DelegateListProcessor OnReturnedToSinglePlayer = new DelegateListProcessor();

	public DelegateListProcessor<NetPlayer> OnPlayerJoined = new DelegateListProcessor<NetPlayer>();

	public DelegateListProcessor<NetPlayer> OnPlayerLeft = new DelegateListProcessor<NetPlayer>();

	internal DelegateListProcessor<NetPlayer> OnMasterClientSwitchedEvent = new DelegateListProcessor<NetPlayer>();

	protected static readonly byte[] EmptyArgs = new byte[0];

	public const string roomCharacters = "ABCDEFGHIJKLMNPQRSTUVWXYZ123456789";

	public const string shuffleCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";

	private static StringBuilder shuffleStringBuilder = new StringBuilder(4);

	protected static StringBuilder reusableSB = new StringBuilder();

	[NonSerialized]
	public string groupJoinOverrideGameMode = "";

	public bool groupJoinInProgress { get; protected set; }

	public NetSystemState netState
	{
		get
		{
			return testState;
		}
		protected set
		{
			Debug.Log("netstate set to:" + value);
			testState = value;
		}
	}

	public NetPlayer LocalPlayer => netPlayerCache.Find((NetPlayer p) => p.IsLocal);

	public virtual bool IsMasterClient { get; }

	public virtual NetPlayer MasterClient => netPlayerCache.Find((NetPlayer p) => p.IsMasterClient);

	public Recorder LocalRecorder => localRecorder;

	public Speaker LocalSpeaker => localSpeaker;

	public bool WrongVersion => isWrongVersion;

	public abstract string CurrentPhotonBackend { get; }

	public abstract VoiceConnection VoiceConnection { get; }

	public abstract bool IsOnline { get; }

	public abstract bool InRoom { get; }

	public abstract string RoomName { get; }

	public abstract string GameModeString { get; }

	public abstract string CurrentRegion { get; }

	public abstract bool SessionIsPrivate { get; }

	public abstract bool SessionIsSubscription { get; }

	public abstract int LocalPlayerID { get; }

	public virtual NetPlayer[] AllNetPlayers => netPlayerCache.ToArray();

	public virtual NetPlayer[] PlayerListOthers => netPlayerCache.FindAll((NetPlayer p) => !p.IsLocal).ToArray();

	public abstract double SimTime { get; }

	public abstract float SimDeltaTime { get; }

	public abstract int SimTick { get; }

	public abstract int TickRate { get; }

	public abstract int ServerTimestamp { get; }

	public abstract int RoomPlayerCount { get; }

	public RoomConfig CurrentRoom { get; protected set; }

	public event Action<byte, object, int> OnRaiseEvent;

	public event Action<Dictionary<string, object>> OnCustomAuthenticationResponse;

	protected void JoinedNetworkRoom()
	{
		VRRigCache.Instance.OnJoinedRoom();
		OnJoinedRoomEvent?.InvokeSafe();
	}

	internal void MultiplayerStarted()
	{
		OnMultiplayerStarted?.InvokeSafe();
	}

	protected void SinglePlayerStarted()
	{
		try
		{
			OnReturnedToSinglePlayer?.InvokeSafe();
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
		}
		VRRigCache.Instance.OnLeftRoom();
	}

	protected void PlayerJoined(NetPlayer netPlayer)
	{
		if (IsOnline)
		{
			VRRigCache.Instance.OnPlayerEnteredRoom(netPlayer);
			OnPlayerJoined?.InvokeSafe(in netPlayer);
		}
	}

	protected void PlayerLeft(NetPlayer netPlayer)
	{
		try
		{
			OnPlayerLeft?.InvokeSafe(in netPlayer);
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
		}
		VRRigCache.Instance.OnPlayerLeftRoom(netPlayer);
	}

	protected void OnMasterClientSwitchedCallback(NetPlayer nMaster)
	{
		OnMasterClientSwitchedEvent?.InvokeSafe(in nMaster);
	}

	internal void RaiseEvent(byte eventCode, object data, int source)
	{
		this.OnRaiseEvent?.Invoke(eventCode, data, source);
	}

	internal void CustomAuthenticationResponse(Dictionary<string, object> response)
	{
		this.OnCustomAuthenticationResponse?.Invoke(response);
	}

	public virtual void Initialise()
	{
		Debug.Log("INITIALISING NETWORKSYSTEMS");
		if ((bool)Instance)
		{
			UnityEngine.Object.Destroy(base.gameObject);
			return;
		}
		Instance = this;
		NetCrossoverUtils.Prewarm();
	}

	protected virtual void Update()
	{
	}

	public void RegisterSceneNetworkItem(GameObject item)
	{
		if (!SceneObjectsToAttach.Contains(item))
		{
			SceneObjectsToAttach.Add(item);
		}
	}

	public virtual void AttachObjectInGame(GameObject item)
	{
		RegisterSceneNetworkItem(item);
	}

	public virtual void DetatchSceneObjectInGame(GameObject item)
	{
	}

	public virtual AuthenticationValues GetAuthenticationValues()
	{
		Debug.LogWarning("NetworkSystem.GetAuthenticationValues should be overridden");
		return new AuthenticationValues();
	}

	public virtual void SetAuthenticationValues(AuthenticationValues authValues)
	{
		Debug.LogWarning("NetworkSystem.SetAuthenticationValues should be overridden");
	}

	public abstract void FinishAuthenticating();

	public abstract Task<NetJoinResult> ConnectToRoom(string roomName, RoomConfig opts, int regionIndex = -1);

	public abstract Task JoinFriendsRoom(string userID, int actorID, string keyToFollow, string shufflerToFollow);

	public abstract Task ReturnToSinglePlayer();

	public abstract void JoinPubWithFriends();

	public void SetWrongVersion()
	{
		isWrongVersion = true;
	}

	public GameObject NetInstantiate(GameObject prefab, bool isRoomObject = false)
	{
		return NetInstantiate(prefab, Vector3.zero, Quaternion.identity);
	}

	public GameObject NetInstantiate(GameObject prefab, Vector3 position, bool isRoomObject = false)
	{
		return NetInstantiate(prefab, position, Quaternion.identity);
	}

	public abstract GameObject NetInstantiate(GameObject prefab, Vector3 position, Quaternion rotation, bool isRoomObject = false);

	public abstract GameObject NetInstantiate(GameObject prefab, Vector3 position, Quaternion rotation, int playerAuthID, bool isRoomObject = false);

	public abstract GameObject NetInstantiate(GameObject prefab, Vector3 position, Quaternion rotation, bool isRoomObject, byte group = 0, object[] data = null, NetworkRunner.OnBeforeSpawned callback = null);

	public abstract void SetPlayerObject(GameObject playerInstance, int? owningPlayerID = null);

	public abstract void NetDestroy(GameObject instance);

	public abstract void CallRPC(MonoBehaviour component, RPC rpcMethod, bool sendToSelf = true);

	public abstract void CallRPC<T>(MonoBehaviour component, RPC rpcMethod, RPCArgBuffer<T> args, bool sendToSelf = true) where T : struct;

	public abstract void CallRPC(MonoBehaviour component, StringRPC rpcMethod, string message, bool sendToSelf = true);

	public abstract void CallRPC(int targetPlayerID, MonoBehaviour component, RPC rpcMethod);

	public abstract void CallRPC<T>(int targetPlayerID, MonoBehaviour component, RPC rpcMethod, RPCArgBuffer<T> args) where T : struct;

	public abstract void CallRPC(int targetPlayerID, MonoBehaviour component, StringRPC rpcMethod, string message);

	public static string GetRandomRoomName()
	{
		string text = "";
		for (int i = 0; i < 4; i++)
		{
			text += "ABCDEFGHIJKLMNPQRSTUVWXYZ123456789".Substring(UnityEngine.Random.Range(0, "ABCDEFGHIJKLMNPQRSTUVWXYZ123456789".Length), 1);
		}
		if (GorillaComputer.instance.IsPlayerInVirtualStump())
		{
			text = GorillaComputer.instance.VStumpRoomPrepend + text;
		}
		if (GorillaComputer.instance.CheckAutoBanListForName(text))
		{
			return text;
		}
		return GetRandomRoomName();
	}

	public abstract string GetRandomWeightedRegion();

	protected async Task RefreshNonce()
	{
		nonceRefreshed = false;
		PlayFabAuthenticator.instance.RefreshSteamAuthTicketForPhoton(GetSteamAuthTicketSuccessCallback, GetSteamAuthTicketFailureCallback);
		while (!nonceRefreshed)
		{
			await Task.Yield();
		}
	}

	private void GetSteamAuthTicketSuccessCallback(string ticket)
	{
		AuthenticationValues authenticationValues = GetAuthenticationValues();
		if (authenticationValues?.AuthPostData is Dictionary<string, object> dictionary)
		{
			dictionary["Nonce"] = ticket;
			authenticationValues.SetAuthPostData(dictionary);
			SetAuthenticationValues(authenticationValues);
			nonceRefreshed = true;
		}
	}

	private void GetSteamAuthTicketFailureCallback(EResult result)
	{
		StartCoroutine(ReGetNonce());
	}

	private IEnumerator ReGetNonce()
	{
		yield return new WaitForSecondsRealtime(3f);
		PlayFabAuthenticator.instance.RefreshSteamAuthTicketForPhoton(GetSteamAuthTicketSuccessCallback, GetSteamAuthTicketFailureCallback);
		yield return null;
	}

	public void BroadcastMyRoom(bool create, string key, string shuffler)
	{
		string roomToJoin = ShuffleRoomName(Instance.RoomName, shuffler.Substring(2, 8), encode: true) + "|" + ShuffleRoomName("ABCDEFGHIJKLMNPQRSTUVWXYZ123456789".Substring(Instance.currentRegionIndex, 1), shuffler.Substring(0, 2), encode: true);
		GorillaServer.Instance.BroadcastMyRoom(new BroadcastMyRoomRequest
		{
			KeyToFollow = key,
			RoomToJoin = roomToJoin,
			Set = create
		}, delegate
		{
		}, delegate
		{
		});
	}

	public bool InstantCheckGroupData(string userID, string keyToFollow)
	{
		bool success = false;
		PlayFabClientAPI.GetSharedGroupData(new PlayFab.ClientModels.GetSharedGroupDataRequest
		{
			Keys = new List<string> { keyToFollow },
			SharedGroupId = userID
		}, delegate(GetSharedGroupDataResult result)
		{
			if (result.Data.Count > 0)
			{
				success = true;
			}
		}, delegate
		{
		});
		return success;
	}

	public NetPlayer GetNetPlayerByID(int playerActorNumber)
	{
		return netPlayerCache.Find((NetPlayer a) => a.ActorNumber == playerActorNumber);
	}

	public virtual void NetRaiseEventReliable(byte eventCode, object data)
	{
	}

	public virtual void NetRaiseEventUnreliable(byte eventCode, object data)
	{
	}

	public virtual void NetRaiseEventReliable(byte eventCode, object data, NetEventOptions options)
	{
	}

	public virtual void NetRaiseEventUnreliable(byte eventCode, object data, NetEventOptions options)
	{
	}

	public static string ShuffleRoomName(string room, string shuffle, bool encode)
	{
		shuffleStringBuilder.Clear();
		if (!int.TryParse(shuffle, out var _))
		{
			Debug.Log("Shuffle room failed");
			return "";
		}
		for (int i = 0; i < room.Length; i++)
		{
			int num = int.Parse(shuffle.Substring(i * 2 % (shuffle.Length - 1), 2));
			int index = mod("ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".IndexOf(room[i]) + (encode ? num : (-num)), "ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".Length);
			shuffleStringBuilder.Append("ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890"[index]);
		}
		return shuffleStringBuilder.ToString();
	}

	public static int mod(int x, int m)
	{
		return (x % m + m) % m;
	}

	public abstract Task AwaitSceneReady();

	public abstract NetPlayer GetLocalPlayer();

	public abstract NetPlayer GetPlayer(int PlayerID);

	public NetPlayer GetPlayer(Player punPlayer)
	{
		if (punPlayer == null)
		{
			return null;
		}
		NetPlayer netPlayer = FindPlayer(punPlayer);
		if (netPlayer == null)
		{
			UpdatePlayers();
			netPlayer = FindPlayer(punPlayer);
			if (netPlayer == null)
			{
				Debug.LogError($"There is no NetPlayer with this ID currently in game. Passed ID: {punPlayer.ActorNumber} nickname {punPlayer.NickName}");
				return null;
			}
		}
		return netPlayer;
	}

	private NetPlayer FindPlayer(Player punPlayer)
	{
		for (int i = 0; i < netPlayerCache.Count; i++)
		{
			if (netPlayerCache[i].GetPlayerRef() == punPlayer)
			{
				return netPlayerCache[i];
			}
		}
		return null;
	}

	public NetPlayer GetPlayer(PlayerRef playerRef)
	{
		return null;
	}

	public abstract void SetMyNickName(string name);

	public abstract string GetMyNickName();

	public abstract string GetMyDefaultName();

	public abstract string GetNickName(int playerID);

	public abstract string GetNickName(NetPlayer player);

	public abstract string GetMyUserID();

	public abstract string GetUserID(int playerID);

	public abstract string GetUserID(NetPlayer player);

	public abstract void SetMyTutorialComplete();

	public abstract bool GetMyTutorialCompletion();

	public abstract bool GetPlayerTutorialCompletion(int playerID);

	public void AddVoiceSettings(SO_NetworkVoiceSettings settings)
	{
		VoiceSettings = settings;
	}

	public abstract void AddRemoteVoiceAddedCallback(Action<RemoteVoiceLink> callback);

	public abstract string RoomStringStripped();

	public string RoomString()
	{
		return string.Format("Room: '{0}' {1},{2} {4}/{3} players.\ncustomProps: {5}", RoomName, CurrentRoom.isPublic ? "visible" : "hidden", CurrentRoom.isJoinable ? "open" : "closed", CurrentRoom.MaxPlayers, RoomPlayerCount, CurrentRoom.CustomProps.ToStringFull());
	}

	protected abstract void UpdateNetPlayerList();

	public void UpdatePlayers()
	{
		UpdateNetPlayerList();
	}

	public abstract int GlobalPlayerCount();

	public abstract bool IsObjectLocallyOwned(GameObject obj);

	public abstract bool IsObjectRoomObject(GameObject obj);

	public abstract bool ShouldUpdateObject(GameObject obj);

	public abstract bool ShouldWriteObjectData(GameObject obj);

	public abstract int GetOwningPlayerID(GameObject obj);

	public abstract bool ShouldSpawnLocally(int playerID);

	public abstract bool IsTotalAuthority();
}
