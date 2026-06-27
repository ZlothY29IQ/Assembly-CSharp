using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Fusion;
using GorillaGameModes;
using GorillaLocomotion;
using GorillaNetworking;
using GorillaTag;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public abstract class GorillaGameManager : MonoBehaviourPunCallbacks, ITickSystemTick, IWrappedSerializable, INetworkStruct
{
	public delegate void OnTouchDelegate(NetPlayer taggedPlayer, NetPlayer taggingPlayer);

	protected const string GAME_MODE_NONE_KEY = "GAME_MODE_NONE";

	protected const string GAME_MODE_CASUAL_ROOM_LABEL_KEY = "GAME_MODE_CASUAL_ROOM_LABEL";

	protected const string GAME_MODE_INFECTION_ROOM_LABEL_KEY = "GAME_MODE_INFECTION_ROOM_LABEL";

	protected const string GAME_MODE_HUNT_ROOM_LABEL_KEY = "GAME_MODE_HUNT_ROOM_LABEL";

	protected const string GAME_MODE_PAINTBRAWL_ROOM_LABEL_KEY = "GAME_MODE_PAINTBRAWL_ROOM_LABEL";

	protected const string GAME_MODE_SUPER_INFECTION_ROOM_LABEL_KEY = "GAME_MODE_SUPER_INFECTION_ROOM_LABEL";

	protected const string GAME_MODE_SUPER_CASUAL_ROOM_LABEL_KEY = "GAME_MODE_SUPER_CASUAL_ROOM_LABEL";

	protected const string GAME_MODE_NONE_ROOM_LABEL_KEY = "GAME_MODE_NONE_ROOM_LABEL";

	protected const string GAME_MODE_CUSTOM_ROOM_LABEL_KEY = "GAME_MODE_CUSTOM_ROOM_LABEL";

	protected const string GAME_MODE_GHOST_ROOM_LABEL_KEY = "GAME_MODE_GHOST_ROOM_LABEL";

	protected const string GAME_MODE_AMBUSH_ROOM_LABEL_KEY = "GAME_MODE_AMBUSH_ROOM_LABEL";

	protected const string GAME_MODE_FREEZE_TAG_ROOM_LABEL_KEY = "GAME_MODE_FREEZE_TAG_ROOM_LABEL";

	protected const string GAME_MODE_GUARDIAN_ROOM_LABEL_KEY = "GAME_MODE_GUARDIAN_ROOM_LABEL";

	protected const string GAME_MODE_PROP_HUNT_ROOM_LABEL_KEY = "GAME_MODE_PROP_HUNT_ROOM_LABEL";

	protected const string GAME_MODE_COMP_INF_ROOM_LABEL_KEY = "GAME_MODE_COMP_INF_ROOM_LABEL";

	public const int k_defaultMatIndex = 0;

	public float fastJumpLimit;

	public float fastJumpMultiplier;

	public float slowJumpLimit;

	public float slowJumpMultiplier;

	public float lastCheck;

	public float checkCooldown = 3f;

	public float tagDistanceThreshold = 4f;

	private NetPlayer outPlayer;

	private int outInt;

	private VRRig tempRig;

	public NetPlayer[] currentNetPlayerArray;

	public float[] playerSpeed = new float[2];

	public Dictionary<int, int> lastTaggedActorNr = new Dictionary<int, int>();

	private string _gameModeName;

	private static Action onInstanceReady;

	private static bool replicatedClientReady;

	private static Action onReplicatedClientReady;

	private GameModeSerializer serializer;

	public static GorillaGameManager instance => GorillaGameModes.GameMode.ActiveGameMode;

	bool ITickSystemTick.TickRunning { get; set; }

	internal GameModeSerializer Serializer => serializer;

	public static event OnTouchDelegate OnTouch;

	public static string GameModeEnumToName(GameModeType gameMode)
	{
		return gameMode.ToString();
	}

	public virtual void Awake()
	{
	}

	private new void OnEnable()
	{
	}

	private new void OnDisable()
	{
	}

	public virtual void Tick()
	{
		if (lastCheck + checkCooldown < Time.time)
		{
			lastCheck = Time.time;
			if (NetworkSystem.Instance.IsMasterClient && !ValidGameMode())
			{
				GorillaGameModes.GameMode.ChangeGameFromProperty();
			}
			else
			{
				InfrequentUpdate();
			}
		}
	}

	public virtual void InfrequentUpdate()
	{
		GorillaGameModes.GameMode.RefreshPlayers();
		currentNetPlayerArray = NetworkSystem.Instance.AllNetPlayers;
	}

	public virtual string GameModeName()
	{
		if (_gameModeName == null)
		{
			_gameModeName = GameType().ToString().ToUpper();
		}
		return _gameModeName;
	}

	public virtual string GameModeNameRoomLabel()
	{
		if (!LocalisationManager.TryGetKeyForCurrentLocale("GAME_MODE_NONE_ROOM_LABEL", out var result, "(NONE GAME)"))
		{
			Debug.LogError("[LOCALIZATION::GORILLA_GAME_MANAGER] Failed to get key for Game Mode [GAME_MODE_NONE_ROOM_LABEL]");
		}
		return result;
	}

	public virtual void LocalTag(NetPlayer taggedPlayer, NetPlayer taggingPlayer, bool bodyHit, bool leftHand)
	{
	}

	public virtual void ReportTag(NetPlayer taggedPlayer, NetPlayer taggingPlayer)
	{
	}

	public virtual void HitPlayer(NetPlayer player)
	{
	}

	public virtual bool CanAffectPlayer(NetPlayer player, bool thisFrame)
	{
		return false;
	}

	public virtual void HandleHandTap(NetPlayer tappingPlayer, Tappable hitTappable, bool leftHand, Vector3 handVelocity, Vector3 tapSurfaceNormal)
	{
	}

	public virtual bool CanJoinFrienship(NetPlayer player)
	{
		return true;
	}

	public virtual bool CanPlayerParticipate(NetPlayer player)
	{
		return true;
	}

	public virtual void HandleRoundComplete()
	{
		PlayerGameEvents.GameModeCompleteRound();
	}

	public virtual void HandleTagBroadcast(NetPlayer taggedPlayer, NetPlayer taggingPlayer)
	{
	}

	public virtual void HandleTagBroadcast(NetPlayer taggedPlayer, NetPlayer taggingPlayer, double tagTime)
	{
	}

	public virtual void NewVRRig(NetPlayer player, int vrrigPhotonViewID, bool didTutorial)
	{
	}

	public virtual bool LocalCanTag(NetPlayer myPlayer, NetPlayer otherPlayer)
	{
		return false;
	}

	public virtual bool LocalIsTagged(NetPlayer player)
	{
		return false;
	}

	public virtual VRRig FindPlayerVRRig(NetPlayer player)
	{
		if (player != null && VRRigCache.Instance.TryGetVrrig(player, out var playerRig))
		{
			return playerRig.Rig;
		}
		return null;
	}

	public static VRRig StaticFindRigForPlayer(NetPlayer player)
	{
		VRRig result = null;
		RigContainer playerRig;
		if (instance != null)
		{
			result = instance.FindPlayerVRRig(player);
		}
		else if (VRRigCache.Instance.TryGetVrrig(player, out playerRig))
		{
			result = playerRig.Rig;
		}
		return result;
	}

	public virtual float[] LocalPlayerSpeed()
	{
		playerSpeed[0] = slowJumpLimit;
		playerSpeed[1] = slowJumpMultiplier;
		return playerSpeed;
	}

	public virtual void UpdatePlayerAppearance(VRRig rig)
	{
		ScienceExperimentManager scienceExperimentManager = ScienceExperimentManager.instance;
		if (scienceExperimentManager != null && scienceExperimentManager.GetMaterialIfPlayerInGame(rig.creator.ActorNumber, out var materialIndex))
		{
			rig.ChangeMaterialLocal(materialIndex);
			return;
		}
		int materialIndex2 = MyMatIndex(rig.creator);
		rig.ChangeMaterialLocal(materialIndex2);
	}

	public virtual int MyMatIndex(NetPlayer forPlayer)
	{
		return 0;
	}

	public virtual int SpecialHandFX(NetPlayer player, RigContainer rigContainer)
	{
		return -1;
	}

	public virtual bool ValidGameMode()
	{
		if (!NetworkSystem.Instance.InRoom)
		{
			return false;
		}
		if (!NetworkSystem.Instance.SessionIsPrivate || !RoomSystem.IsVStumpRoom)
		{
			return GameModeString.DoesPropertyStringContainGameMode(NetworkSystem.Instance.GameModeString, GameTypeName());
		}
		return true;
	}

	public static void OnInstanceReady(Action action)
	{
		GorillaParent.OnReplicatedClientReady(delegate
		{
			if ((bool)instance)
			{
				action();
			}
			else
			{
				onInstanceReady = (Action)Delegate.Combine(onInstanceReady, action);
			}
		});
	}

	public static void ReplicatedClientReady()
	{
		replicatedClientReady = true;
	}

	public static void OnReplicatedClientReady(Action action)
	{
		if (replicatedClientReady)
		{
			action();
		}
		else
		{
			onReplicatedClientReady = (Action)Delegate.Combine(onReplicatedClientReady, action);
		}
	}

	internal virtual void NetworkLinkSetup(GameModeSerializer netSerializer)
	{
		serializer = netSerializer;
	}

	internal virtual void NetworkLinkDestroyed(GameModeSerializer netSerializer)
	{
		if (serializer == netSerializer)
		{
			serializer = null;
		}
	}

	public abstract GameModeType GameType();

	public string GameTypeName()
	{
		return GameType().ToString();
	}

	public abstract void AddFusionDataBehaviour(NetworkObject behaviour);

	public abstract void OnSerializeRead(object newData);

	public abstract object OnSerializeWrite();

	public abstract void OnSerializeRead(PhotonStream stream, PhotonMessageInfo info);

	public abstract void OnSerializeWrite(PhotonStream stream, PhotonMessageInfo info);

	public virtual void ResetGame()
	{
	}

	public virtual void StartPlaying()
	{
		TickSystem<object>.AddTickCallback(this);
		NetworkSystem.Instance.OnPlayerJoined += new Action<NetPlayer>(OnPlayerEnteredRoom);
		NetworkSystem.Instance.OnPlayerLeft += new Action<NetPlayer>(OnPlayerLeftRoom);
		NetworkSystem.Instance.OnMasterClientSwitchedEvent += new Action<NetPlayer>(OnMasterClientSwitched);
		currentNetPlayerArray = NetworkSystem.Instance.AllNetPlayers;
		GorillaTelemetry.PostGameModeEvent(GTGameModeEventType.game_mode_start, GameType());
	}

	public virtual void StopPlaying()
	{
		TickSystem<object>.RemoveTickCallback(this);
		NetworkSystem.Instance.OnPlayerJoined -= new Action<NetPlayer>(OnPlayerEnteredRoom);
		NetworkSystem.Instance.OnPlayerLeft -= new Action<NetPlayer>(OnPlayerLeftRoom);
		NetworkSystem.Instance.OnMasterClientSwitchedEvent -= new Action<NetPlayer>(OnMasterClientSwitched);
		lastCheck = 0f;
	}

	public new virtual void OnMasterClientSwitched(Player newMaster)
	{
	}

	public new virtual void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
	{
	}

	public new virtual void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
	{
	}

	public virtual void OnPlayerLeftRoom(NetPlayer otherPlayer)
	{
		currentNetPlayerArray = NetworkSystem.Instance.AllNetPlayers;
		if (lastTaggedActorNr.ContainsKey(otherPlayer.ActorNumber))
		{
			lastTaggedActorNr.Remove(otherPlayer.ActorNumber);
		}
	}

	public virtual void OnPlayerEnteredRoom(NetPlayer newPlayer)
	{
		currentNetPlayerArray = NetworkSystem.Instance.AllNetPlayers;
	}

	public virtual void OnMasterClientSwitched(NetPlayer newMaster)
	{
	}

	internal static void ForceStopGame_DisconnectAndDestroy()
	{
		Application.Quit();
		NetworkSystem.Instance?.ReturnToSinglePlayer();
		UnityEngine.Object.DestroyImmediate(PhotonNetworkController.Instance);
		UnityEngine.Object.DestroyImmediate(GTPlayer.Instance);
		GameObject[] array = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
		for (int i = 0; i < array.Length; i++)
		{
			UnityEngine.Object.Destroy(array[i]);
		}
	}

	public void AddLastTagged(NetPlayer taggedPlayer, NetPlayer taggingPlayer)
	{
		if (lastTaggedActorNr.ContainsKey(taggedPlayer.ActorNumber))
		{
			lastTaggedActorNr[taggedPlayer.ActorNumber] = taggingPlayer.ActorNumber;
		}
		else
		{
			lastTaggedActorNr.Add(taggedPlayer.ActorNumber, taggingPlayer.ActorNumber);
		}
	}

	public void WriteLastTagged(PhotonStream stream)
	{
		stream.SendNext(lastTaggedActorNr.Count);
		foreach (KeyValuePair<int, int> item in lastTaggedActorNr)
		{
			stream.SendNext(item.Key);
			stream.SendNext(item.Value);
		}
	}

	public void ReadLastTagged(PhotonStream stream)
	{
		lastTaggedActorNr.Clear();
		int num = Mathf.Min((int)stream.ReceiveNext(), 20);
		for (int i = 0; i < num; i++)
		{
			lastTaggedActorNr.Add((int)stream.ReceiveNext(), (int)stream.ReceiveNext());
		}
	}
}
