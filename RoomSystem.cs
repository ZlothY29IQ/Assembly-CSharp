using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Timers;
using ExitGames.Client.Photon;
using GorillaExtensions;
using GorillaGameModes;
using GorillaLocomotion;
using GorillaNetworking;
using GorillaTag;
using GorillaTag.Cosmetics;
using GorillaTagScripts;
using Photon.Pun;
using Photon.Realtime;
using TagEffects;
using UnityEngine;
using Voxels;

internal class RoomSystem : MonoBehaviour
{
	private class ImpactFxContainer : IFXContext
	{
		public VRRig targetRig;

		public Vector3 position;

		public Color colour;

		public int projectileIndex;

		public FXSystemSettings settings => targetRig.fxSettings;

		public virtual void OnPlayFX()
		{
			NetPlayer creator = targetRig.creator;
			ProjectileTracker.ProjectileInfo projectileInfo;
			if (targetRig.isOfflineVRRig)
			{
				projectileInfo = ProjectileTracker.GetLocalProjectile(projectileIndex);
			}
			else
			{
				(bool, ProjectileTracker.ProjectileInfo) andRemoveRemotePlayerProjectile = ProjectileTracker.GetAndRemoveRemotePlayerProjectile(creator, projectileIndex);
				if (!andRemoveRemotePlayerProjectile.Item1)
				{
					return;
				}
				projectileInfo = andRemoveRemotePlayerProjectile.Item2;
			}
			SlingshotProjectile projectileInstance = projectileInfo.projectileInstance;
			GameObject obj = (projectileInfo.hasImpactOverride ? projectileInstance.playerImpactEffectPrefab : playerImpactEffectPrefab);
			GameObject gameObject = ObjectPools.instance.Instantiate(obj, position);
			gameObject.transform.localScale = Vector3.one * targetRig.scaleFactor;
			if (gameObject.TryGetComponent<GorillaColorizableBase>(out var component))
			{
				component.SetColor(colour);
			}
			SurfaceImpactFX component2 = gameObject.GetComponent<SurfaceImpactFX>();
			if (component2 != null)
			{
				component2.SetScale(projectileInstance.transform.localScale.x * projectileInstance.impactEffectScaleMultiplier);
			}
			SoundBankPlayer component3 = gameObject.GetComponent<SoundBankPlayer>();
			if (component3 != null && !component3.playOnEnable)
			{
				component3.Play(projectileInstance.impactSoundVolumeOverride, projectileInstance.impactSoundPitchOverride);
			}
			if (projectileInstance.gameObject.activeSelf && projectileInstance.projectileOwner == creator)
			{
				projectileInstance.Deactivate();
			}
		}
	}

	private class LaunchProjectileContainer : ImpactFxContainer
	{
		public Vector3 velocity;

		public ProjectileSource projectileSource;

		public bool overridecolour;

		public PhotonMessageInfoWrapped messageInfo;

		private GameObject tempThrowableGO;

		private SnowballThrowable tempThrowableRef;

		public override void OnPlayFX()
		{
			GameObject gameObject = null;
			SlingshotProjectile slingshotProjectile = null;
			try
			{
				int num = -1;
				switch (projectileSource)
				{
				default:
					return;
				case ProjectileSource.ProjectileWeapon:
					if (targetRig.projectileWeapon.IsNotNull() && targetRig.projectileWeapon.IsNotNull())
					{
						velocity = targetRig.ClampVelocityRelativeToPlayerSafe(velocity, 70f);
						SlingshotProjectile slingshotProjectile2 = targetRig.projectileWeapon.LaunchNetworkedProjectile(position, velocity, projectileSource, projectileIndex, targetRig.scaleFactor, overridecolour, colour, messageInfo);
						if (slingshotProjectile2.IsNotNull())
						{
							ProjectileTracker.AddRemotePlayerProjectile(messageInfo.Sender, slingshotProjectile2, projectileIndex, messageInfo.SentServerTime, velocity, position, targetRig.scaleFactor);
						}
					}
					return;
				case ProjectileSource.LeftHand:
					tempThrowableGO = targetRig.myBodyDockPositions.GetLeftHandThrowable();
					break;
				case ProjectileSource.RightHand:
					tempThrowableGO = targetRig.myBodyDockPositions.GetRightHandThrowable();
					break;
				}
				if (!tempThrowableGO.IsNull() && tempThrowableGO.TryGetComponent<SnowballThrowable>(out tempThrowableRef) && !(tempThrowableRef is GrowingSnowballThrowable))
				{
					velocity = targetRig.ClampVelocityRelativeToPlayerSafe(velocity, 50f);
					num = tempThrowableRef.ProjectileHash;
					gameObject = ObjectPools.instance.Instantiate(num);
					slingshotProjectile = gameObject.GetComponent<SlingshotProjectile>();
					ProjectileTracker.AddRemotePlayerProjectile(targetRig.creator, slingshotProjectile, projectileIndex, messageInfo.SentServerTime, velocity, position, targetRig.scaleFactor);
					slingshotProjectile.Launch(position, velocity, messageInfo.Sender, blueTeam: false, orangeTeam: false, projectileIndex, targetRig.scaleFactor, overridecolour, colour);
				}
			}
			catch
			{
				if ((object)slingshotProjectile != null && (bool)slingshotProjectile)
				{
					slingshotProjectile.transform.position = Vector3.zero;
					slingshotProjectile.Deactivate();
				}
				else if (gameObject.IsNotNull())
				{
					ObjectPools.instance.Destroy(gameObject);
				}
			}
		}
	}

	internal enum ProjectileSource
	{
		ProjectileWeapon,
		LeftHand,
		RightHand
	}

	internal struct LavaSyncEventData
	{
		public byte zone;

		public byte state;

		public double stateStartTime;

		public float activationProgress;

		public int voteCount;

		public int senderActorNumber;

		public unsafe fixed int votes[20];
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	internal struct Events
	{
		public const byte PROJECTILE = 0;

		public const byte IMPACT = 1;

		public const byte STATUS_EFFECT = 2;

		public const byte SOUND_EFFECT = 3;

		public const byte NEARBY_JOIN = 4;

		public const byte PLAYER_TOUCHED = 5;

		public const byte PLAYER_EFFECT = 6;

		public const byte PARTY_JOIN = 7;

		public const byte PLAYER_LAUNCHED = 8;

		public const byte PLAYER_HIT = 9;

		public const byte ELEVATOR_JOIN = 10;

		public const byte SHUTTLE_JOIN = 11;

		public const byte LAVA_SYNC = 12;

		public const byte MONKE_BIZ_STATION__POINTS_REDEEMED = 13;

		public const byte VOX_REQ_WORLD = 100;

		public const byte VOX_REQ_OPERATION = 101;

		public const byte VOX_REQ_MINE = 102;

		public const byte VOX_START_CHUNK = 103;

		public const byte VOX_CONTINUE_CHUNK = 104;

		public const byte VOX_SET_DENSITY = 105;

		public const byte VOX_MINE = 106;

		public const byte RPC = byte.MaxValue;
	}

	public enum StatusEffects
	{
		TaggedTime,
		JoinedTaggedTime,
		SetSlowedTime,
		UnTagged,
		FrozenTime
	}

	public struct SoundEffect(int soundID, float soundVolume, bool _stopCurrentAudio)
	{
		public int id = soundID;

		public float volume = (volume = soundVolume);

		public bool stopCurrentAudio = _stopCurrentAudio;
	}

	[Serializable]
	public struct PlayerEffectConfig
	{
		public PlayerEffect type;

		public TagEffectPack tagEffectPack;
	}

	private static ImpactFxContainer impactEffect;

	private static LaunchProjectileContainer launchProjectile;

	public static GameObject playerImpactEffectPrefab;

	private static readonly object[] projectileSendData;

	private static readonly object[] impactSendData;

	private static readonly List<int> hashValues;

	[OnExitPlay_SetNull]
	internal static Action<LavaSyncEventData> OnLavaSyncReceived;

	private const int lavaSyncHeaderSize = 5;

	private const int lavaSyncTotalSize = 25;

	private static readonly object[] lavaSyncSendData;

	[OnExitPlay_SetNull]
	internal static Action<NetPlayer, int> OnMonkePointsRedeemedReceived;

	private const int monkePointsRedeemedMaxCount = 50;

	private static readonly object[] monkePointsRedeemedSendData;

	[SerializeField]
	private RoomSystemSettings roomSettings;

	[SerializeField]
	private string[] prefabsToInstantiateByPath;

	[SerializeField]
	private GameObject[] prefabsToInstantiate;

	private List<GameObject> prefabsInstantiated = new List<GameObject>();

	public static Dictionary<PlayerEffect, PlayerEffectConfig> playerEffectDictionary;

	private static RoomSystemSettings __roomSettings;

	[OnEnterPlay_SetNull]
	private static RoomSystem callbackInstance;

	private static byte m_roomSizeOnJoin;

	[OnEnterPlay_Clear]
	private static List<NetPlayer> netPlayersInRoom;

	[OnEnterPlay_Set("")]
	private static string roomGameMode;

	[OnEnterPlay_Set(false)]
	private static bool joinedRoom;

	[OnEnterPlay_SetNull]
	private static PhotonView[] sceneViews;

	[OnEnterPlay_SetNew]
	public static DelegateListProcessor LeftRoomEvent;

	[OnEnterPlay_SetNew]
	public static DelegateListProcessor JoinedRoomEvent;

	[OnEnterPlay_SetNew]
	public static DelegateListProcessor<NetPlayer> PlayerJoinedEvent;

	[OnEnterPlay_SetNew]
	public static DelegateListProcessor<NetPlayer> PlayerLeftEvent;

	[OnEnterPlay_SetNew]
	public static DelegateListProcessor PlayersChangedEvent;

	private static Timer disconnectTimer;

	[OnExitPlay_Clear]
	internal static readonly Dictionary<byte, Action<object[], PhotonMessageInfoWrapped>> netEventCallbacks;

	private static readonly object[] sendEventData;

	private static readonly object[] groupJoinSendData;

	private static readonly object[] reportTouchSendData;

	private static readonly object[] reportHitSendData;

	[OnExitPlay_SetNull]
	public static Action<NetPlayer, NetPlayer> playerTouchedCallback;

	private static CallLimiter playerLaunchedCallLimiter;

	private static CallLimiter hitPlayerCallLimiter;

	private static object[] statusSendData;

	public static Action<StatusEffects> statusEffectCallback;

	private static object[] soundSendData;

	private static object[] sendSoundDataOther;

	public static Action<SoundEffect, NetPlayer> soundEffectCallback;

	private static object[] playerEffectData;

	private static bool UseRoomSizeOverride { get; set; }

	public static byte RoomSizeOverride { get; set; }

	public static byte RoomSizeReduction { get; set; }

	public static List<NetPlayer> PlayersInRoom => netPlayersInRoom;

	public static string RoomGameMode => roomGameMode;

	public static bool JoinedRoom
	{
		get
		{
			if (NetworkSystem.Instance.InRoom)
			{
				return joinedRoom;
			}
			return false;
		}
	}

	public static bool AmITheHost
	{
		get
		{
			if (!NetworkSystem.Instance.IsMasterClient)
			{
				return !NetworkSystem.Instance.InRoom;
			}
			return true;
		}
	}

	public static bool IsVStumpRoom { get; private set; }

	public static bool WasRoomPrivate { get; private set; }

	public static bool WasRoomSubscription { get; private set; }

	public static GorillaNetworkJoinTrigger InitialJoinTrigger { get; private set; }

	internal static void DeserializeLaunchProjectile(object[] projectileData, PhotonMessageInfoWrapped info)
	{
		NetPlayer player = NetworkSystem.Instance.GetPlayer(info.senderID);
		MonkeAgent.IncrementRPCCall(info, "LaunchSlingshotProjectile");
		if (!VRRigCache.Instance.TryGetVrrig(player, out var playerRig))
		{
			return;
		}
		byte b = Convert.ToByte(projectileData[5]);
		byte b2 = Convert.ToByte(projectileData[6]);
		byte b3 = Convert.ToByte(projectileData[7]);
		byte b4 = Convert.ToByte(projectileData[8]);
		Color32 color = new Color32(b, b2, b3, b4);
		Vector3 v = (Vector3)projectileData[0];
		Vector3 v2 = (Vector3)projectileData[1];
		if (!v.IsValid(10000f) || !v2.IsValid(10000f) || !float.IsFinite((int)b) || !float.IsFinite((int)b2) || !float.IsFinite((int)b3) || !float.IsFinite((int)b4))
		{
			MonkeAgent.instance.SendReport("invalid projectile state", player.UserId, player.NickName);
			return;
		}
		ProjectileSource projectileSource = (ProjectileSource)Convert.ToInt32(projectileData[2]);
		int projectileIndex = Convert.ToInt32(projectileData[3]);
		bool overridecolour = Convert.ToBoolean(projectileData[4]);
		VRRig rig = playerRig.Rig;
		if (rig.isOfflineVRRig || rig.IsPositionInRange(v, 4f))
		{
			launchProjectile.targetRig = rig;
			launchProjectile.position = v;
			launchProjectile.velocity = v2;
			launchProjectile.overridecolour = overridecolour;
			launchProjectile.colour = color;
			launchProjectile.projectileIndex = projectileIndex;
			launchProjectile.projectileSource = projectileSource;
			launchProjectile.messageInfo = info;
			FXSystem.PlayFXForRig(FXType.Projectile, launchProjectile, info);
		}
	}

	internal static void SendLaunchProjectile(Vector3 position, Vector3 velocity, ProjectileSource projectileSource, int projectileCount, bool randomColour, byte r, byte g, byte b, byte a)
	{
		if (JoinedRoom)
		{
			projectileSendData[0] = position;
			projectileSendData[1] = velocity;
			projectileSendData[2] = projectileSource;
			projectileSendData[3] = projectileCount;
			projectileSendData[4] = randomColour;
			projectileSendData[5] = r;
			projectileSendData[6] = g;
			projectileSendData[7] = b;
			projectileSendData[8] = a;
			SendEvent(0, projectileSendData, in NetworkSystemRaiseEvent.neoOthers, reliable: false);
		}
	}

	internal static void ImpactEffect(VRRig targetRig, Vector3 position, float r, float g, float b, float a, int projectileCount, PhotonMessageInfoWrapped info = default(PhotonMessageInfoWrapped))
	{
		impactEffect.targetRig = targetRig;
		impactEffect.position = position;
		impactEffect.colour = new Color(r, g, b, a);
		impactEffect.projectileIndex = projectileCount;
		FXSystem.PlayFXForRig(FXType.Impact, impactEffect, info);
	}

	internal static void DeserializeImpactEffect(object[] impactData, PhotonMessageInfoWrapped info)
	{
		NetPlayer player = NetworkSystem.Instance.GetPlayer(info.senderID);
		MonkeAgent.IncrementRPCCall(info, "SpawnSlingshotPlayerImpactEffect");
		if (VRRigCache.Instance.TryGetVrrig(player, out var playerRig) && !playerRig.Rig.projectileWeapon.IsNull())
		{
			float num = Convert.ToSingle(impactData[1]);
			float num2 = Convert.ToSingle(impactData[2]);
			float num3 = Convert.ToSingle(impactData[3]);
			float num4 = Convert.ToSingle(impactData[4]);
			Vector3 v = (Vector3)impactData[0];
			if (!v.IsValid(10000f) || !float.IsFinite(num) || !float.IsFinite(num2) || !float.IsFinite(num3) || !float.IsFinite(num4))
			{
				MonkeAgent.instance.SendReport("invalid impact state", player.UserId, player.NickName);
				return;
			}
			int projectileCount = Convert.ToInt32(impactData[5]);
			ImpactEffect(playerRig.Rig, v, num, num2, num3, num4, projectileCount, info);
		}
	}

	internal static void SendImpactEffect(Vector3 position, float r, float g, float b, float a, int projectileCount)
	{
		ImpactEffect(VRRigCache.Instance.localRig.Rig, position, r, g, b, a, projectileCount);
		if (joinedRoom)
		{
			impactSendData[0] = position;
			impactSendData[1] = r;
			impactSendData[2] = g;
			impactSendData[3] = b;
			impactSendData[4] = a;
			impactSendData[5] = projectileCount;
			SendEvent(1, impactSendData, in NetworkSystemRaiseEvent.neoOthers, reliable: false);
		}
	}

	internal static void SendLavaSync(byte zone, byte state, double stateStartTime, float activationProgress, int voteCount, int[] votePlayerIds)
	{
		if (joinedRoom)
		{
			PackLavaSyncData(zone, state, stateStartTime, activationProgress, voteCount, votePlayerIds);
			SendEvent(12, lavaSyncSendData, in NetworkSystemRaiseEvent.neoOthers, reliable: false);
		}
	}

	internal static void SendLavaSyncToPlayer(byte zone, byte state, double stateStartTime, float activationProgress, int voteCount, int[] votePlayerIds, NetPlayer target)
	{
		if (joinedRoom)
		{
			PackLavaSyncData(zone, state, stateStartTime, activationProgress, voteCount, votePlayerIds);
			SendEvent(12, lavaSyncSendData, in target, reliable: false);
		}
	}

	private static void PackLavaSyncData(byte zone, byte state, double stateStartTime, float activationProgress, int voteCount, int[] votePlayerIds)
	{
		lavaSyncSendData[0] = zone;
		lavaSyncSendData[1] = state;
		lavaSyncSendData[2] = stateStartTime;
		lavaSyncSendData[3] = activationProgress;
		lavaSyncSendData[4] = voteCount;
		for (int i = 0; i < 20; i++)
		{
			lavaSyncSendData[5 + i] = votePlayerIds[i];
		}
	}

	private unsafe static void DeserializeLavaSync(object[] data, PhotonMessageInfoWrapped info)
	{
		NetworkSystem.Instance.GetPlayer(info.senderID);
		MonkeAgent.IncrementRPCCall(info, "DeserializeLavaSync");
		if (!callbackInstance.roomSettings.LavaSyncLimiter.CheckCallServerTime(info.SentServerTime))
		{
			Debug.LogWarning($"[RoomSystem] LavaSync dropped by rate limiter: sender={info.senderID} sentTime={info.SentServerTime:F3} photonTime={PhotonNetwork.Time:F3}");
		}
		else
		{
			if (data == null || data.Length < 25 || !(data[0] is byte zone) || !(data[1] is byte b) || !(data[2] is double value) || !(data[3] is float value2) || !(data[4] is int value3))
			{
				return;
			}
			for (int i = 0; i < 20; i++)
			{
				if (!(data[5 + i] is int))
				{
					return;
				}
			}
			if (b <= 4)
			{
				LavaSyncEventData obj = default(LavaSyncEventData);
				obj.zone = zone;
				obj.state = b;
				obj.stateStartTime = value.GetFinite();
				obj.activationProgress = value2.ClampSafe(0f, 2f);
				obj.voteCount = Mathf.Clamp(value3, 0, 20);
				obj.senderActorNumber = info.senderID;
				for (int j = 0; j < 20; j++)
				{
					obj.votes[j] = (int)data[5 + j];
				}
				OnLavaSyncReceived?.Invoke(obj);
			}
		}
	}

	internal static void SendMonkePointsRedeemed(int redeemedPointCount)
	{
		if (joinedRoom)
		{
			monkePointsRedeemedSendData[0] = redeemedPointCount;
			SendEvent(13, monkePointsRedeemedSendData, in NetworkSystemRaiseEvent.neoOthers, reliable: false);
		}
	}

	private static void DeserializeMonkePointsRedeemed(object[] data, PhotonMessageInfoWrapped info)
	{
		MonkeAgent.IncrementRPCCall(info, "BroadcastRedeemQuestPoints");
		NetPlayer player = NetworkSystem.Instance.GetPlayer(info.senderID);
		if (player != null && data != null && data.Length >= 1 && data[0] is int value)
		{
			int arg = Mathf.Clamp(value, 0, 50);
			OnMonkePointsRedeemedReceived?.Invoke(player, arg);
		}
	}

	private void Awake()
	{
		base.transform.SetParent(null, worldPositionStays: true);
		UnityEngine.Object.DontDestroyOnLoad(this);
		playerImpactEffectPrefab = roomSettings.PlayerImpactEffect;
		callbackInstance = this;
		disconnectTimer.Interval = roomSettings.PausedDCTimer * 1000;
		playerEffectDictionary.Clear();
		foreach (PlayerEffectConfig playerEffect in roomSettings.PlayerEffects)
		{
			playerEffectDictionary.Add(playerEffect.type, playerEffect);
		}
		roomSettings.ResyncNetworkTimeTimer.callback = PhotonNetwork.FetchServerTimestamp;
		__roomSettings = roomSettings;
	}

	private void Start()
	{
		List<PhotonView> list = new List<PhotonView>(20);
		foreach (PhotonView item in PhotonNetwork.PhotonViewCollection)
		{
			if (item.IsRoomView)
			{
				list.Add(item);
			}
		}
		sceneViews = list.ToArray();
		NetworkSystem.Instance.OnRaiseEvent += OnEvent;
		NetworkSystem.Instance.OnPlayerLeft += new Action<NetPlayer>(OnPlayerLeftRoom);
		NetworkSystem.Instance.OnPlayerJoined += new Action<NetPlayer>(OnPlayerEnteredRoom);
		NetworkSystem.Instance.OnMultiplayerStarted += new Action(OnJoinedRoom);
		NetworkSystem.Instance.OnReturnedToSinglePlayer += new Action(OnLeftRoom);
	}

	private void OnApplicationPause(bool paused)
	{
		if (!paused)
		{
			disconnectTimer.Stop();
		}
		else if (JoinedRoom)
		{
			disconnectTimer.Start();
		}
	}

	private void OnJoinedRoom()
	{
		joinedRoom = true;
		NetPlayer[] allNetPlayers = NetworkSystem.Instance.AllNetPlayers;
		foreach (NetPlayer item in allNetPlayers)
		{
			netPlayersInRoom.Add(item);
		}
		PlayerCosmeticsSystem.UpdatePlayerCosmetics(netPlayersInRoom);
		roomGameMode = NetworkSystem.Instance.GameModeString;
		WasRoomPrivate = NetworkSystem.Instance.SessionIsPrivate;
		WasRoomSubscription = NetworkSystem.Instance.SessionIsSubscription;
		IsVStumpRoom = NetworkSystem.Instance.RoomName.StartsWith(GorillaComputer.instance.VStumpRoomPrepend);
		InitialJoinTrigger = GorillaComputer.instance.GetJoinTriggerFromFullGameModeString(roomGameMode);
		if (!WasRoomPrivate)
		{
			WasRoomSubscription = PhotonNetwork.CurrentRoom.Name.EndsWith(":GTFC");
		}
		_ = WasRoomSubscription;
		if (NetworkSystem.Instance.IsMasterClient)
		{
			for (int j = 0; j < prefabsToInstantiateByPath.Length; j++)
			{
				prefabsInstantiated.Add(NetworkSystem.Instance.NetInstantiate(prefabsToInstantiate[j], Vector3.zero, Quaternion.identity, isRoomObject: true));
			}
		}
		try
		{
			m_roomSizeOnJoin = PhotonNetwork.CurrentRoom.MaxPlayers;
			roomSettings.ExpectedUsersTimer.Start();
			roomSettings.ResyncNetworkTimeTimer.Start();
			JoinedRoomEvent?.InvokeSafe();
			roomSettings.ResyncNetworkTimeTimer.OnTimedEvent();
		}
		catch (Exception)
		{
			Debug.LogError("RoomSystem failed invoking event");
		}
	}

	private void OnPlayerEnteredRoom(NetPlayer newPlayer)
	{
		if (newPlayer.IsLocal)
		{
			return;
		}
		Debug.Log($"Player {newPlayer?.ActorNumber} entered room");
		if (!netPlayersInRoom.Contains(newPlayer))
		{
			netPlayersInRoom.Add(newPlayer);
		}
		PlayerCosmeticsSystem.UpdatePlayerCosmetics(newPlayer);
		try
		{
			PlayerJoinedEvent?.InvokeSafe(in newPlayer);
			PlayersChangedEvent?.InvokeSafe();
		}
		catch (Exception)
		{
			Debug.LogError("RoomSystem failed invoking event");
		}
	}

	private void OnLeftRoom()
	{
		if (ApplicationQuittingState.IsQuitting)
		{
			return;
		}
		joinedRoom = false;
		netPlayersInRoom.Clear();
		roomGameMode = "";
		PlayerCosmeticsSystem.StaticReset();
		int actorNumber = NetworkSystem.Instance.LocalPlayer.ActorNumber;
		for (int i = 0; i < sceneViews.Length; i++)
		{
			sceneViews[i].ControllerActorNr = actorNumber;
			sceneViews[i].OwnerActorNr = actorNumber;
		}
		roomSettings.StatusEffectLimiter.Reset();
		roomSettings.SoundEffectLimiter.Reset();
		roomSettings.SoundEffectOtherLimiter.Reset();
		roomSettings.PlayerEffectLimiter.Reset();
		roomSettings.LavaSyncLimiter.Reset();
		try
		{
			m_roomSizeOnJoin = 0;
			roomSettings.ExpectedUsersTimer.Stop();
			roomSettings.ResyncNetworkTimeTimer.Stop();
			LeftRoomEvent?.InvokeSafe();
		}
		catch (Exception)
		{
			Debug.LogError("RoomSystem failed invoking event");
		}
		finally
		{
			WasRoomSubscription = false;
			InitialJoinTrigger = null;
		}
		GC.Collect(0);
	}

	private void OnPlayerLeftRoom(NetPlayer netPlayer)
	{
		if (netPlayer == null)
		{
			Debug.LogError("Player that left doesn't have a reference somehow...");
		}
		netPlayersInRoom.Remove(netPlayer);
		try
		{
			PlayerLeftEvent?.InvokeSafe(in netPlayer);
			PlayersChangedEvent?.InvokeSafe();
		}
		catch (Exception)
		{
			Debug.LogError("RoomSystem failed invoking event");
		}
	}

	static RoomSystem()
	{
		impactEffect = new ImpactFxContainer();
		launchProjectile = new LaunchProjectileContainer();
		playerImpactEffectPrefab = null;
		projectileSendData = new object[9];
		impactSendData = new object[6];
		hashValues = new List<int>(2);
		lavaSyncSendData = new object[25];
		monkePointsRedeemedSendData = new object[1];
		playerEffectDictionary = new Dictionary<PlayerEffect, PlayerEffectConfig>();
		netPlayersInRoom = new List<NetPlayer>(20);
		roomGameMode = "";
		joinedRoom = false;
		LeftRoomEvent = new DelegateListProcessor();
		JoinedRoomEvent = new DelegateListProcessor();
		PlayerJoinedEvent = new DelegateListProcessor<NetPlayer>();
		PlayerLeftEvent = new DelegateListProcessor<NetPlayer>();
		PlayersChangedEvent = new DelegateListProcessor();
		disconnectTimer = new Timer();
		netEventCallbacks = new Dictionary<byte, Action<object[], PhotonMessageInfoWrapped>>(20);
		sendEventData = new object[3];
		groupJoinSendData = new object[2];
		reportTouchSendData = new object[1];
		reportHitSendData = new object[3];
		playerLaunchedCallLimiter = new CallLimiter(3, 15f);
		hitPlayerCallLimiter = new CallLimiter(10, 2f);
		statusSendData = new object[1];
		soundSendData = new object[3];
		sendSoundDataOther = new object[4];
		playerEffectData = new object[2];
		disconnectTimer.Elapsed += TimerDC;
		disconnectTimer.AutoReset = false;
		StaticLoad();
	}

	[OnEnterPlay_Run]
	private static void StaticLoad()
	{
		netEventCallbacks[0] = DeserializeLaunchProjectile;
		netEventCallbacks[1] = DeserializeImpactEffect;
		netEventCallbacks[4] = SearchForNearby;
		netEventCallbacks[7] = SearchForParty;
		netEventCallbacks[10] = SearchForElevator;
		netEventCallbacks[11] = SearchForShuttle;
		netEventCallbacks[2] = DeserializeStatusEffect;
		netEventCallbacks[3] = DeserializeSoundEffect;
		netEventCallbacks[5] = DeserializeReportTouch;
		netEventCallbacks[8] = DeserializePlayerLaunched;
		netEventCallbacks[6] = DeserializePlayerEffect;
		netEventCallbacks[9] = DeserializePlayerHit;
		netEventCallbacks[12] = DeserializeLavaSync;
		netEventCallbacks[13] = DeserializeMonkePointsRedeemed;
		soundEffectCallback = OnPlaySoundEffect;
		statusEffectCallback = OnStatusEffect;
		VoxelManager.RegisterNetEventCallbacks();
	}

	private static void TimerDC(object sender, ElapsedEventArgs args)
	{
		disconnectTimer.Stop();
		if (joinedRoom)
		{
			PhotonNetwork.Disconnect();
			PhotonNetwork.SendAllOutgoingCommands();
		}
	}

	public static byte GetMaxRoomSize()
	{
		return (byte)__roomSettings.GetRoomCount(privateRoom: true, sub: true);
	}

	public static byte GetCurrentRoomExpectedSize()
	{
		if (!joinedRoom)
		{
			return 10;
		}
		if (IsVStumpRoom)
		{
			if (m_roomSizeOnJoin >= 10)
			{
				return 10;
			}
			return m_roomSizeOnJoin;
		}
		NetPlayer lowestActorNumberPlayer = GetLowestActorNumberPlayer();
		if (lowestActorNumberPlayer == null || !VRRigCache.Instance.TryGetVrrig(lowestActorNumberPlayer, out var playerRig))
		{
			return 10;
		}
		byte b = 20;
		bool flag = false;
		flag = SubscriptionManager.GetSubscriptionDetails(lowestActorNumberPlayer).active;
		if (WasRoomPrivate)
		{
			Room currentRoom = PhotonNetwork.CurrentRoom;
			if (!playerRig.Rig.InitializedCosmetics)
			{
				b = currentRoom.MaxPlayers;
				if (b >= 20)
				{
					return 20;
				}
				return b;
			}
			b = (byte)__roomSettings.GetRoomCount(privateRoom: true, flag);
			if (!flag && PhotonNetwork.CurrentRoom.PlayerCount > 10)
			{
				b = PhotonNetwork.CurrentRoom.PlayerCount;
			}
		}
		else
		{
			GorillaNetworkJoinTrigger initialJoinTrigger = InitialJoinTrigger;
			GTZone zone = GTZone.none;
			if (initialJoinTrigger.IsNotNull())
			{
				zone = initialJoinTrigger.zone;
			}
			b = (byte)__roomSettings.GetRoomCount(zone, GameMode.CurrentGameModeType, privateRoom: false, WasRoomSubscription);
		}
		if (b >= 20)
		{
			return 20;
		}
		return b;
	}

	public static byte GetRoomSizeForCreate(GTZone zone, GameModeType mode, bool privateRoom, bool sub)
	{
		if (UseRoomSizeOverride)
		{
			return RoomSizeOverride;
		}
		return (byte)__roomSettings.GetRoomCount(zone, mode, privateRoom, sub);
	}

	public static void OverrideRoomSize(byte size)
	{
		if (size < 1)
		{
			size = 1;
		}
		else if (size > 10)
		{
			size = 10;
		}
		if (size == 10)
		{
			UseRoomSizeOverride = false;
		}
		else
		{
			UseRoomSizeOverride = true;
		}
		RoomSizeOverride = size;
	}

	public static byte GetOverridenRoomSize()
	{
		if (UseRoomSizeOverride)
		{
			return RoomSizeOverride;
		}
		return 10;
	}

	public static void ClearOverridenRoomSize()
	{
		UseRoomSizeOverride = false;
		RoomSizeOverride = 10;
	}

	public static void MakeRoomMultiplayer(byte roomSize)
	{
		if (joinedRoom && m_roomSizeOnJoin <= 1)
		{
			if (roomSize > 20)
			{
				roomSize = 20;
			}
			m_roomSizeOnJoin = roomSize;
			PhotonNetwork.CurrentRoom.MaxPlayers = roomSize;
		}
	}

	public static NetPlayer GetLowestActorNumberPlayer()
	{
		if (!joinedRoom || netPlayersInRoom.Count == 0)
		{
			return null;
		}
		NetPlayer netPlayer = netPlayersInRoom[0];
		for (int i = 1; i < netPlayersInRoom.Count; i++)
		{
			NetPlayer netPlayer2 = netPlayersInRoom[i];
			if (netPlayer2.ActorNumber < netPlayer.ActorNumber)
			{
				netPlayer = netPlayer2;
			}
		}
		return netPlayer;
	}

	internal static void SendEvent(byte code, object[] evData, in NetPlayer target, bool reliable)
	{
		NetworkSystemRaiseEvent.neoTarget.TargetActors[0] = target.ActorNumber;
		SendEvent(code, evData, in NetworkSystemRaiseEvent.neoTarget, reliable);
	}

	internal static void SendEvent(byte code, object[] evData, in NetEventOptions neo, bool reliable)
	{
		sendEventData[0] = NetworkSystem.Instance.ServerTimestamp;
		sendEventData[1] = code;
		sendEventData[2] = evData;
		NetworkSystemRaiseEvent.RaiseEvent(3, sendEventData, neo, reliable);
	}

	private static void OnEvent(EventData data)
	{
		OnEvent(data.Code, data.CustomData, data.Sender);
	}

	private static void OnEvent(byte code, object data, int source)
	{
		if (code != 3 || !Utils.PlayerInRoom(source, out NetPlayer player))
		{
			return;
		}
		try
		{
			object[] array = (object[])data;
			int tick = Convert.ToInt32(array[0]);
			byte key = Convert.ToByte(array[1]);
			object[] arg = null;
			if (array.Length > 2)
			{
				object obj = array[2];
				arg = ((obj == null) ? null : ((object[])obj));
			}
			PhotonMessageInfoWrapped arg2 = new PhotonMessageInfoWrapped(player.ActorNumber, tick);
			if (netEventCallbacks.TryGetValue(key, out var value))
			{
				value(arg, arg2);
			}
		}
		catch
		{
		}
	}

	internal static void SearchForNearby(object[] shuffleData, PhotonMessageInfoWrapped info)
	{
		NetPlayer player = NetworkSystem.Instance.GetPlayer(info.senderID);
		MonkeAgent.IncrementRPCCall(info, "JoinPubWithNearby");
		if (!VRRigCache.Instance.TryGetVrrig(player, out var playerRig) || !FXSystem.CheckCallSpam(playerRig.Rig.fxSettings, 23, NetworkSystem.Instance.SimTime))
		{
			return;
		}
		string shufflerStr = (string)shuffleData[0];
		string newKeyStr = (string)shuffleData[1];
		bool flag = KIDManager.HasPermissionToUseFeature(EKIDFeatures.Groups);
		if (GorillaComputer.instance.friendJoinCollider.playerIDsCurrentlyTouching.Contains(NetworkSystem.Instance.LocalPlayer.UserId))
		{
			if (flag && WasRoomPrivate)
			{
				PhotonNetworkController.Instance.AttemptToFollowIntoPub(player.UserId, player.ActorNumber, newKeyStr, shufflerStr, JoinType.FollowingNearby);
			}
		}
		else
		{
			MonkeAgent.instance.SendReport("possible kick attempt", player.UserId, player.NickName);
		}
	}

	internal static void SearchForParty(object[] shuffleData, PhotonMessageInfoWrapped info)
	{
		MonkeAgent.IncrementRPCCall(info, "PARTY_JOIN");
		if (!VRRigCache.Instance.TryGetVrrig(info.Sender, out var playerRig) || !FXSystem.CheckCallSpam(playerRig.Rig.fxSettings, 23, NetworkSystem.Instance.SimTime))
		{
			return;
		}
		string shufflerStr = (string)shuffleData[0];
		string newKeyStr = (string)shuffleData[1];
		if (FriendshipGroupDetection.Instance.IsInMyGroup(info.Sender.UserId))
		{
			if (!PlayFabAuthenticator.instance.GetSafety())
			{
				PhotonNetworkController.Instance.AttemptToFollowIntoPub(info.Sender.UserId, info.Sender.ActorNumber, newKeyStr, shufflerStr, JoinType.FollowingParty);
			}
		}
		else
		{
			MonkeAgent.instance.SendReport("possible kick attempt", info.Sender.UserId, info.Sender.NickName);
		}
	}

	internal static void SearchForElevator(object[] shuffleData, PhotonMessageInfoWrapped info)
	{
		NetPlayer player = NetworkSystem.Instance.GetPlayer(info.senderID);
		MonkeAgent.IncrementRPCCall(info, "JoinPubWithElevator");
		if (!VRRigCache.Instance.TryGetVrrig(player, out var playerRig) || !FXSystem.CheckCallSpam(playerRig.Rig.fxSettings, 23, NetworkSystem.Instance.SimTime))
		{
			return;
		}
		string shufflerStr = (string)shuffleData[0];
		string newKeyStr = (string)shuffleData[1];
		bool flag = KIDManager.HasPermissionToUseFeature(EKIDFeatures.Groups);
		if (GRElevatorManager.ValidElevatorNetworking(info.Sender.ActorNumber) && GRElevatorManager.ValidElevatorNetworking(NetworkSystem.Instance.LocalPlayer.ActorNumber))
		{
			if (!flag)
			{
				GRElevatorManager.JoinPublicRoom();
			}
			else
			{
				PhotonNetworkController.Instance.AttemptToFollowIntoPub(player.UserId, player.ActorNumber, newKeyStr, shufflerStr, JoinType.JoinWithElevator);
			}
		}
	}

	internal static void SearchForShuttle(object[] shuffleData, PhotonMessageInfoWrapped info)
	{
		NetPlayer player = NetworkSystem.Instance.GetPlayer(info.senderID);
		MonkeAgent.IncrementRPCCall(info, "JoinPubWithElevator");
		if (!VRRigCache.Instance.TryGetVrrig(player, out var playerRig) || !FXSystem.CheckCallSpam(playerRig.Rig.fxSettings, 23, NetworkSystem.Instance.SimTime))
		{
			return;
		}
		string shufflerStr = (string)shuffleData[0];
		string newKeyStr = (string)shuffleData[1];
		bool flag = KIDManager.HasPermissionToUseFeature(EKIDFeatures.Groups);
		bool num = GRElevatorManager.ValidShuttleNetworking(info.Sender.ActorNumber);
		bool flag2 = GRElevatorManager.ValidShuttleNetworking(NetworkSystem.Instance.LocalPlayer.ActorNumber);
		if (num && flag2)
		{
			if (!flag)
			{
				GRElevatorManager.JoinPublicRoom();
			}
			else
			{
				PhotonNetworkController.Instance.AttemptToFollowIntoPub(player.UserId, player.ActorNumber, newKeyStr, shufflerStr, JoinType.JoinWithElevator);
			}
		}
	}

	internal static void SendNearbyFollowCommand(GorillaFriendCollider friendCollider, string shuffler, string keyStr)
	{
		groupJoinSendData[0] = shuffler;
		groupJoinSendData[1] = keyStr;
		NetEventOptions neo = new NetEventOptions
		{
			TargetActors = new int[1]
		};
		foreach (NetPlayer item in PlayersInRoom)
		{
			if (friendCollider.playerIDsCurrentlyTouching.Contains(item.UserId) && item != NetworkSystem.Instance.LocalPlayer)
			{
				neo.TargetActors[0] = item.ActorNumber;
				SendEvent(4, groupJoinSendData, in neo, reliable: false);
			}
		}
	}

	internal static void SendPartyFollowCommand(string shuffler, string keyStr)
	{
		groupJoinSendData[0] = shuffler;
		groupJoinSendData[1] = keyStr;
		NetEventOptions neo = new NetEventOptions
		{
			TargetActors = new int[1]
		};
		foreach (RigContainer activeRigContainer in VRRigCache.ActiveRigContainers)
		{
			VRRig rig = activeRigContainer.Rig;
			if (rig.IsLocalPartyMember && rig.creator != NetworkSystem.Instance.LocalPlayer)
			{
				neo.TargetActors[0] = rig.creator.ActorNumber;
				SendEvent(7, groupJoinSendData, in neo, reliable: false);
			}
		}
	}

	internal static void SendElevatorFollowCommand(string shuffler, string keyStr, GorillaFriendCollider sourceFriendCollider, GorillaFriendCollider targetFriendCollider)
	{
		SendGroupJoinFollowCommand(10, shuffler, keyStr, sourceFriendCollider, targetFriendCollider);
	}

	internal static void SendShuttleFollowCommand(string shuffler, string keyStr, GorillaFriendCollider sourceFriendCollider, GorillaFriendCollider targetFriendCollider)
	{
		SendGroupJoinFollowCommand(11, shuffler, keyStr, sourceFriendCollider, targetFriendCollider);
	}

	internal static void SendGroupJoinFollowCommand(byte eventType, string shuffler, string keyStr, GorillaFriendCollider sourceFriendCollider, GorillaFriendCollider targetFriendCollider)
	{
		groupJoinSendData[0] = shuffler;
		groupJoinSendData[1] = keyStr;
		NetEventOptions neo = new NetEventOptions
		{
			TargetActors = new int[1]
		};
		foreach (NetPlayer item in PlayersInRoom)
		{
			if (sourceFriendCollider.playerIDsCurrentlyTouching.Contains(item.UserId) || (targetFriendCollider.playerIDsCurrentlyTouching.Contains(item.UserId) && item != NetworkSystem.Instance.LocalPlayer))
			{
				neo.TargetActors[0] = item.ActorNumber;
				SendEvent(eventType, groupJoinSendData, in neo, reliable: false);
			}
		}
	}

	private static void DeserializeReportTouch(object[] data, PhotonMessageInfoWrapped info)
	{
		if (NetworkSystem.Instance.IsMasterClient)
		{
			NetPlayer arg = (NetPlayer)data[0];
			NetPlayer player = NetworkSystem.Instance.GetPlayer(info.senderID);
			playerTouchedCallback?.Invoke(arg, player);
		}
	}

	internal static void SendReportTouch(NetPlayer touchedNetPlayer)
	{
		if (NetworkSystem.Instance.IsMasterClient)
		{
			playerTouchedCallback?.Invoke(touchedNetPlayer, NetworkSystem.Instance.LocalPlayer);
			return;
		}
		reportTouchSendData[0] = touchedNetPlayer;
		SendEvent(5, reportTouchSendData, in NetworkSystemRaiseEvent.neoMaster, reliable: false);
	}

	internal static void LaunchPlayer(NetPlayer player, Vector3 velocity)
	{
		reportTouchSendData[0] = velocity;
		SendEvent(8, reportTouchSendData, in player, reliable: false);
	}

	private static void DeserializePlayerLaunched(object[] data, PhotonMessageInfoWrapped info)
	{
		MonkeAgent.IncrementRPCCall(info, "DeserializePlayerLaunched");
		GorillaGameManager activeGameMode = GameMode.ActiveGameMode;
		if ((object)activeGameMode != null && activeGameMode.GameType() == GameModeType.Guardian && info.Sender == NetworkSystem.Instance.MasterClient && data[0] is Vector3 v && v.IsValid(10000f) && !(v.magnitude > 20f) && playerLaunchedCallLimiter.CheckCallTime(Time.time))
		{
			GTPlayer.Instance.DoLaunch(v);
		}
	}

	internal static void HitPlayer(NetPlayer player, Vector3 direction, float strength)
	{
		reportHitSendData[0] = direction;
		reportHitSendData[1] = strength;
		reportHitSendData[2] = player.ActorNumber;
		SendEvent(9, reportHitSendData, in NetworkSystemRaiseEvent.neoOthers, reliable: false);
		if (VRRigCache.Instance.TryGetVrrig(player, out var playerRig))
		{
			playerRig.Rig.DisableHitWithKnockBack();
		}
	}

	private static void DeserializePlayerHit(object[] data, PhotonMessageInfoWrapped info)
	{
		if (!(data[0] is Vector3 v) || !(data[1] is float value) || !(data[2] is int num) || !v.IsValid(10000f) || !VRRigCache.Instance.TryGetVrrig(info.Sender, out var playerRig) || !FXSystem.CheckCallSpam(playerRig.Rig.fxSettings, 20, info.SentServerTime))
		{
			return;
		}
		float num2 = value.ClampSafe(0f, 10f);
		MonkeAgent.IncrementRPCCall(info, "DeserializePlayerHit");
		if (num == NetworkSystem.Instance.LocalPlayer.ActorNumber)
		{
			CosmeticEffectsOnPlayers.CosmeticEffect value3;
			if (GorillaTagger.Instance.offlineVRRig.TemporaryCosmeticEffects.TryGetValue(CosmeticEffectsOnPlayers.EFFECTTYPE.TagWithKnockback, out var value2))
			{
				if (!value2.IsGameModeAllowed())
				{
					return;
				}
				float num3 = (num2 * value2.knockbackStrength * value2.knockbackStrengthMultiplier).ClampSafe(value2.minKnockbackStrength, value2.maxKnockbackStrength);
				if (value2.applyScaleToKnockbackStrength)
				{
					num3 *= GTPlayer.Instance.scale;
				}
				GTPlayer.Instance.ApplyKnockback(v.normalized, num3, value2.forceOffTheGround);
			}
			else if (GorillaTagger.Instance.offlineVRRig.TemporaryCosmeticEffects.TryGetValue(CosmeticEffectsOnPlayers.EFFECTTYPE.InstantKnockback, out value3))
			{
				if (!value3.IsGameModeAllowed())
				{
					return;
				}
				float num4 = (num2 * value3.knockbackStrength * value3.knockbackStrengthMultiplier).ClampSafe(value3.minKnockbackStrength, value3.maxKnockbackStrength);
				if (value2.applyScaleToKnockbackStrength)
				{
					num4 *= GTPlayer.Instance.scale;
				}
				GTPlayer.Instance.ApplyKnockback(v.normalized, num4, value3.forceOffTheGround);
			}
		}
		NetPlayer player = NetworkSystem.Instance.GetPlayer(num);
		if (player != null && VRRigCache.Instance.TryGetVrrig(player, out var playerRig2))
		{
			playerRig2.Rig.DisableHitWithKnockBack();
		}
	}

	private static void SetSlowedTime()
	{
		if (GorillaTagger.Instance.currentStatus != GorillaTagger.StatusEffect.Slowed)
		{
			GorillaTagger.Instance.StartVibration(forLeftController: true, GorillaTagger.Instance.taggedHapticStrength, GorillaTagger.Instance.taggedHapticDuration);
			GorillaTagger.Instance.StartVibration(forLeftController: false, GorillaTagger.Instance.taggedHapticStrength, GorillaTagger.Instance.taggedHapticDuration);
		}
		GorillaTagger.Instance.ApplyStatusEffect(GorillaTagger.StatusEffect.Slowed, GorillaTagger.Instance.slowCooldown);
		GorillaTagger.Instance.offlineVRRig.PlayTaggedEffect();
	}

	private static void SetTaggedTime()
	{
		GorillaTagger.Instance.ApplyStatusEffect(GorillaTagger.StatusEffect.Frozen, GorillaTagger.Instance.tagCooldown);
		GorillaTagger.Instance.StartVibration(forLeftController: true, GorillaTagger.Instance.taggedHapticStrength, GorillaTagger.Instance.taggedHapticDuration);
		GorillaTagger.Instance.StartVibration(forLeftController: false, GorillaTagger.Instance.taggedHapticStrength, GorillaTagger.Instance.taggedHapticDuration);
		GorillaTagger.Instance.offlineVRRig.PlayTaggedEffect();
	}

	private static void SetFrozenTime()
	{
		if (GameMode.ActiveGameMode is GorillaFreezeTagManager gorillaFreezeTagManager)
		{
			GorillaTagger.Instance.ApplyStatusEffect(GorillaTagger.StatusEffect.Slowed, gorillaFreezeTagManager.freezeDuration);
			GorillaTagger.Instance.StartVibration(forLeftController: true, GorillaTagger.Instance.taggedHapticStrength, GorillaTagger.Instance.taggedHapticDuration);
			GorillaTagger.Instance.StartVibration(forLeftController: false, GorillaTagger.Instance.taggedHapticStrength, GorillaTagger.Instance.taggedHapticDuration);
			GorillaTagger.Instance.offlineVRRig.PlayTaggedEffect();
		}
	}

	private static void SetJoinedTaggedTime()
	{
		GorillaTagger.Instance.StartVibration(forLeftController: true, GorillaTagger.Instance.taggedHapticStrength, GorillaTagger.Instance.taggedHapticDuration);
		GorillaTagger.Instance.StartVibration(forLeftController: false, GorillaTagger.Instance.taggedHapticStrength, GorillaTagger.Instance.taggedHapticDuration);
	}

	private static void SetUntaggedTime()
	{
		GorillaTagger.Instance.ApplyStatusEffect(GorillaTagger.StatusEffect.None, 0f);
		GorillaTagger.Instance.StartVibration(forLeftController: true, GorillaTagger.Instance.taggedHapticStrength, GorillaTagger.Instance.taggedHapticDuration);
		GorillaTagger.Instance.StartVibration(forLeftController: false, GorillaTagger.Instance.taggedHapticStrength, GorillaTagger.Instance.taggedHapticDuration);
	}

	private static void OnStatusEffect(StatusEffects status)
	{
		switch (status)
		{
		case StatusEffects.TaggedTime:
			SetTaggedTime();
			break;
		case StatusEffects.JoinedTaggedTime:
			SetJoinedTaggedTime();
			break;
		case StatusEffects.SetSlowedTime:
			SetSlowedTime();
			break;
		case StatusEffects.UnTagged:
			SetUntaggedTime();
			break;
		case StatusEffects.FrozenTime:
			SetFrozenTime();
			break;
		}
	}

	private static void DeserializeStatusEffect(object[] data, PhotonMessageInfoWrapped info)
	{
		NetPlayer player = NetworkSystem.Instance.GetPlayer(info.senderID);
		MonkeAgent.IncrementRPCCall(info, "DeserializeStatusEffect");
		if (!player.IsMasterClient)
		{
			MonkeAgent.instance.SendReport("invalid status", player.UserId, player.NickName);
		}
		else if (callbackInstance.roomSettings.StatusEffectLimiter.CheckCallServerTime(info.SentServerTime))
		{
			StatusEffects obj = (StatusEffects)Convert.ToInt32(data[0]);
			statusEffectCallback?.Invoke(obj);
		}
	}

	internal static void SendStatusEffectAll(StatusEffects status)
	{
		statusEffectCallback?.Invoke(status);
		if (joinedRoom)
		{
			statusSendData[0] = (int)status;
			SendEvent(2, statusSendData, in NetworkSystemRaiseEvent.neoOthers, reliable: false);
		}
	}

	internal static void SendStatusEffectToPlayer(StatusEffects status, NetPlayer target)
	{
		if (target.IsLocal)
		{
			statusEffectCallback?.Invoke(status);
			return;
		}
		statusSendData[0] = (int)status;
		SendEvent(2, statusSendData, in target, reliable: false);
	}

	internal static void PlaySoundEffect(int soundIndex, float soundVolume, bool stopCurrentAudio)
	{
		VRRigCache.Instance.localRig.Rig.PlayTagSoundLocal(soundIndex, soundVolume, stopCurrentAudio);
	}

	internal static void PlaySoundEffect(int soundIndex, float soundVolume, bool stopCurrentAudio, NetPlayer target)
	{
		if (VRRigCache.Instance.TryGetVrrig(target, out var playerRig))
		{
			playerRig.Rig.PlayTagSoundLocal(soundIndex, soundVolume, stopCurrentAudio);
		}
	}

	private static void OnPlaySoundEffect(SoundEffect sound, NetPlayer target)
	{
		if (target.IsLocal)
		{
			PlaySoundEffect(sound.id, sound.volume, sound.stopCurrentAudio);
		}
		else
		{
			PlaySoundEffect(sound.id, sound.volume, sound.stopCurrentAudio, target);
		}
	}

	private static void DeserializeSoundEffect(object[] data, PhotonMessageInfoWrapped info)
	{
		NetPlayer player = NetworkSystem.Instance.GetPlayer(info.senderID);
		MonkeAgent.IncrementRPCCall(info, "DeserializeSoundEffect");
		if (!player.Equals(GetLowestActorNumberPlayer()))
		{
			MonkeAgent.instance.SendReport("invalid sound effect", player.UserId, player.NickName);
			return;
		}
		SoundEffect arg = default(SoundEffect);
		arg.id = Convert.ToInt32(data[0]);
		arg.volume = Convert.ToSingle(data[1]);
		arg.stopCurrentAudio = Convert.ToBoolean(data[2]);
		if (!float.IsFinite(arg.volume))
		{
			return;
		}
		NetPlayer netPlayer;
		if (data.Length > 3)
		{
			if (!callbackInstance.roomSettings.SoundEffectOtherLimiter.CheckCallServerTime(info.SentServerTime))
			{
				return;
			}
			int playerID = Convert.ToInt32(data[3]);
			netPlayer = NetworkSystem.Instance.GetPlayer(playerID);
		}
		else
		{
			if (!callbackInstance.roomSettings.SoundEffectLimiter.CheckCallServerTime(info.SentServerTime))
			{
				return;
			}
			netPlayer = NetworkSystem.Instance.LocalPlayer;
		}
		if (netPlayer != null)
		{
			soundEffectCallback(arg, netPlayer);
		}
	}

	internal static void SendSoundEffectAll(int soundIndex, float soundVolume, bool stopCurrentAudio = false)
	{
		SendSoundEffectAll(new SoundEffect(soundIndex, soundVolume, stopCurrentAudio));
	}

	internal static void SendSoundEffectAll(SoundEffect sound)
	{
		soundEffectCallback?.Invoke(sound, NetworkSystem.Instance.LocalPlayer);
		if (joinedRoom)
		{
			soundSendData[0] = sound.id;
			soundSendData[1] = sound.volume;
			soundSendData[2] = sound.stopCurrentAudio;
			SendEvent(3, soundSendData, in NetworkSystemRaiseEvent.neoOthers, reliable: false);
		}
	}

	internal static void SendSoundEffectToPlayer(int soundIndex, float soundVolume, NetPlayer player, bool stopCurrentAudio = false)
	{
		SendSoundEffectToPlayer(new SoundEffect(soundIndex, soundVolume, stopCurrentAudio), player);
	}

	internal static void SendSoundEffectToPlayer(SoundEffect sound, NetPlayer player)
	{
		if (player.IsLocal)
		{
			soundEffectCallback?.Invoke(sound, player);
		}
		else if (joinedRoom)
		{
			soundSendData[0] = sound.id;
			soundSendData[1] = sound.volume;
			soundSendData[2] = sound.stopCurrentAudio;
			SendEvent(3, soundSendData, in player, reliable: false);
		}
	}

	internal static void SendSoundEffectOnOther(int soundIndex, float soundvolume, NetPlayer target, bool stopCurrentAudio = false)
	{
		SendSoundEffectOnOther(new SoundEffect(soundIndex, soundvolume, stopCurrentAudio), target);
	}

	internal static void SendSoundEffectOnOther(SoundEffect sound, NetPlayer target)
	{
		soundEffectCallback?.Invoke(sound, target);
		if (joinedRoom)
		{
			sendSoundDataOther[0] = sound.id;
			sendSoundDataOther[1] = sound.volume;
			sendSoundDataOther[2] = sound.stopCurrentAudio;
			sendSoundDataOther[3] = target.ActorNumber;
			SendEvent(3, sendSoundDataOther, in NetworkSystemRaiseEvent.neoOthers, reliable: false);
		}
	}

	internal static void OnPlayerEffect(PlayerEffect effect, NetPlayer target)
	{
		if (target != null && playerEffectDictionary.TryGetValue(effect, out var value) && VRRigCache.Instance.TryGetVrrig(target, out var playerRig) && playerRig != null && playerRig.Rig != null && value.tagEffectPack != null)
		{
			TagEffectsLibrary.PlayEffect(playerRig.Rig.transform, isLeftHand: false, playerRig.Rig.scaleFactor, (!target.IsLocal) ? TagEffectsLibrary.EffectType.THIRD_PERSON : TagEffectsLibrary.EffectType.FIRST_PERSON, value.tagEffectPack, value.tagEffectPack, playerRig.Rig.transform.rotation);
		}
	}

	private static void DeserializePlayerEffect(object[] data, PhotonMessageInfoWrapped info)
	{
		MonkeAgent.IncrementRPCCall(info, "DeserializePlayerEffect");
		if (callbackInstance.roomSettings.PlayerEffectLimiter.CheckCallServerTime(info.SentServerTime))
		{
			int playerID = Convert.ToInt32(data[0]);
			int effect = Convert.ToInt32(data[1]);
			NetPlayer player = NetworkSystem.Instance.GetPlayer(playerID);
			OnPlayerEffect((PlayerEffect)effect, player);
		}
	}

	internal static void SendPlayerEffect(PlayerEffect effect, NetPlayer target)
	{
		OnPlayerEffect(effect, target);
		if (joinedRoom)
		{
			playerEffectData[0] = target.ActorNumber;
			playerEffectData[1] = effect;
			SendEvent(6, playerEffectData, in NetworkSystemRaiseEvent.neoOthers, reliable: false);
		}
	}
}
