using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Cysharp.Text;
using Fusion;
using GorillaExtensions;
using GorillaLocomotion;
using GorillaNetworking;
using GorillaTag;
using Ionic.Zlib;
using Photon.Pun;
using Photon.Realtime;
using Unity.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

[NetworkBehaviourWeaved(0)]
public class GameEntityManager : NetworkComponent, IRequestableOwnershipGuardCallbacks, ITickSystemTick, IGorillaSliceableSimple
{
	public delegate void ZoneStartEvent(GTZone zoneId);

	public delegate void ZoneClearEvent(GTZone zoneId);

	private enum ZoneState
	{
		WaitingToEnterZone,
		WaitingToRequestState,
		WaitingForState,
		Active
	}

	private struct ZoneStateRequest
	{
		public Player player;

		public GTZone zone;

		public bool completed;
	}

	private class ZoneStateData
	{
		public ZoneState state;

		public double stateStartTime;

		public List<ZoneStateRequest> zoneStateRequests;

		public List<Player> zonePlayers;

		[HideInInspector]
		public byte[] recievedStateBytes;

		[HideInInspector]
		public int numRecievedStateBytes;
	}

	public enum RPC
	{
		CreateItem,
		CreateItems,
		DestroyItem,
		ApplyState,
		GrabEntity,
		ThrowEntity,
		SendTableData,
		HitEntity,
		PlayerLeftZone
	}

	private struct ScenePlacedRecord
	{
		public GameEntity entity;

		public Vector3 position;

		public Quaternion rotation;

		public float uniformScale;
	}

	private struct AttachmentData
	{
		public int entityNetId;

		public int attachToEntityNetId;

		public Vector3 localPosition;

		public Quaternion localRotation;
	}

	private const string preLog = "[GT/GameEntityManager]  ";

	private const string preErr = "[GT/GameEntityManager]  ERROR!!!  ";

	private const string preErrBeta = "[GT/GameEntityManager]  ERROR!!!  (beta only log) ";

	private const int MAX_STATE_BYTES = 15360;

	private const int MAX_CHUNK_BYTES = 512;

	private const int MAX_JOINWITHITEMS_BYTES = 255;

	public const float MAX_LOCAL_MAGNITUDE_SQ = 6400f;

	public const float MAX_DISTANCE_FROM_HAND = 16f;

	public const float MAX_ENTITY_DIST = 16f;

	public const float MAX_THROW_SPEED_SQ = 1600f;

	public const int MAX_ENTITY_COUNT_PER_TYPE = 100;

	public const int INVALID_ID = -1;

	public const int INVALID_INDEX = -1;

	private static List<GameEntityManager> allManagers = new List<GameEntityManager>(8);

	internal static readonly Dictionary<int, GameEntityManager> managersByZone = new Dictionary<int, GameEntityManager>(8);

	public GTZone zone;

	public PhotonView photonView;

	public RequestableOwnershipGuard guard;

	public Player prevAuthorityPlayer;

	[FormerlySerializedAs("zoneLimit")]
	public BoxCollider boundsBoxCollider;

	public bool useRandomCheckForAuthority;

	public GameAgentManager gameAgentManager;

	public GhostReactorManager ghostReactorManager;

	public CustomMapsGameManager customMapsManager;

	public SuperInfectionManager superInfectionManager;

	protected List<IGameEntityZoneComponent> zoneComponents;

	private List<GameEntity> entities;

	private int entitiesActiveCount;

	private List<GameEntityData> gameEntityData;

	public List<GameEntity> tempFactoryItems;

	private Dictionary<int, GameObject> itemPrefabFactory;

	private Dictionary<int, int> priceLookupByEntityId;

	private List<GameEntity> tempEntities = new List<GameEntity>();

	private List<int> netIdsForCreate;

	private List<int> entityTypeIdsForCreate;

	private List<int> packedRotationsForCreate;

	private List<long> packedPositionsForCreate;

	private List<long> createDataForCreate;

	private List<int> createdByEntityNetIdForCreate;

	private float createCooldown = 0.24f;

	private float lastCreateSent;

	private List<int> netIdsForDelete;

	private float destroyCooldown = 0.25f;

	private float lastDestroySent;

	private List<int> netIdsForState;

	private List<long> statesForState;

	private float lastStateSent;

	private float stateCooldown;

	private Dictionary<int, int> netIdToIndex;

	private NativeArray<int> netIds;

	private Dictionary<int, int> createdItemTypeCount;

	private const float ZONE_MIGRATION_RECOVERY_DURATION = 10f;

	private readonly Dictionary<int, float> playerZoneJoinTimes = new Dictionary<int, float>(20);

	private ZoneClearReason zoneClearReason;

	[NonSerialized]
	public Action<GameEntity> OnEntityRemoved;

	[NonSerialized]
	public Action<GameEntity> OnEntityAdded;

	private int pendingTableDataSetFrame;

	[DebugReadout]
	private ZoneStateData zoneStateData;

	private int nextNetId = 1;

	public CallLimitersList<CallLimiter, RPC> m_RpcSpamChecks = new CallLimitersList<CallLimiter, RPC>();

	private bool scenePlacedEntitiesRegistered;

	private float scenePlacedBoundsCheckTimer;

	private int _lastUpdateZoneStateAuthLogSig = int.MinValue;

	private readonly List<ScenePlacedRecord> scenePlacedEntities = new List<ScenePlacedRecord>(16);

	private readonly List<GameEntityId> _leavingItemScratch = new List<GameEntityId>(4);

	private List<Collider> _collidersList = new List<Collider>(16);

	private static List<VRRig> tempRigs = new List<VRRig>(32);

	private static List<GameEntity> tempEntitiesToSerialize = new List<GameEntity>(512);

	private static List<AttachmentData> tempAttachments = new List<AttachmentData>(512);

	private byte[] tempSerializeGameState = new byte[15360];

	[OnEnterPlay_Clear]
	private static readonly Dictionary<string, List<GameEntity>> s_scenePlacedEntities = new Dictionary<string, List<GameEntity>>();

	[OnEnterPlay_Clear]
	private static readonly Dictionary<int, string> s_scenePlacedHomeScenes = new Dictionary<int, string>();

	public static GameEntityManager activeManager { get; private set; }

	public bool TickRunning { get; set; }

	public bool PendingTableData { get; private set; }

	public event ZoneStartEvent onZoneStart;

	public event ZoneClearEvent onZoneClear;

	protected override void Awake()
	{
		base.Awake();
		entities = new List<GameEntity>(64);
		entitiesActiveCount = 0;
		gameEntityData = new List<GameEntityData>(64);
		netIdToIndex = new Dictionary<int, int>(16384);
		netIds = new NativeArray<int>(16384, Unity.Collections.Allocator.Persistent);
		createdItemTypeCount = new Dictionary<int, int>();
		OnEntityRemoved = (Action<GameEntity>)Delegate.Combine(OnEntityRemoved, new Action<GameEntity>(CustomGameMode.OnGameEntityRemoved));
		zoneStateData = new ZoneStateData
		{
			zoneStateRequests = new List<ZoneStateRequest>(),
			zonePlayers = new List<Player>(),
			recievedStateBytes = new byte[15360],
			numRecievedStateBytes = 0
		};
		guard.AddCallbackTarget(this);
		netIdsForCreate = new List<int>();
		entityTypeIdsForCreate = new List<int>();
		packedPositionsForCreate = new List<long>();
		packedRotationsForCreate = new List<int>();
		createDataForCreate = new List<long>();
		createdByEntityNetIdForCreate = new List<int>();
		netIdsForDelete = new List<int>();
		netIdsForState = new List<int>();
		statesForState = new List<long>();
		zoneComponents = new List<IGameEntityZoneComponent>(8);
		if (ghostReactorManager != null)
		{
			zoneComponents.Add(ghostReactorManager);
		}
		if (customMapsManager != null)
		{
			zoneComponents.Add(customMapsManager);
		}
		if (superInfectionManager != null)
		{
			zoneComponents.Add(superInfectionManager);
		}
		BuildFactory();
		allManagers.Add(this);
		managersByZone[(int)zone] = this;
		if (base.transform.parent != null)
		{
			base.transform.SetParent(null, worldPositionStays: true);
		}
		UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
	}

	internal void RegisterScenePlacedEntities()
	{
		if (scenePlacedEntitiesRegistered)
		{
			return;
		}
		scenePlacedEntitiesRegistered = true;
		string zoneSceneName = GetZoneSceneName();
		s_scenePlacedEntities.TryGetValue(zoneSceneName, out var value);
		if (value != null)
		{
			for (int i = 0; i < value.Count; i++)
			{
				RegisterSingleScenePlacedEntity(value[i]);
			}
		}
	}

	private void RegisterSingleScenePlacedEntity(GameEntity entity)
	{
		if (entity == null)
		{
			return;
		}
		XSceneRefTarget component;
		int num = ((!entity.TryGetComponent<XSceneRefTarget>(out component) || component.UniqueID <= 0) ? ComputeNetIdFromHierarchyForCustomMaps(entity.transform) : NetIdFromXSceneRefId(component.UniqueID));
		if (netIdToIndex.TryGetValue(num, out var value))
		{
			GameEntity gameEntity = ((value >= 0 && value < entities.Count) ? entities[value] : null);
			if (gameEntity == entity)
			{
				EnsureScenePlacedRecord(entity);
				return;
			}
			if (!(gameEntity == null))
			{
				Debug.LogError("[GT/GameEntityManager]  ERROR!!!  RegisterSingleScenePlacedEntity" + $": NetId collision for scene-placed entity '{entity.name}' (netId={num})" + $" with live entity '{gameEntity.name}' at index {value}. Skipping.");
				return;
			}
			netIdToIndex.Remove(num);
		}
		if (!entity.scenePlacedInitialized)
		{
			entity.scenePlacedHomePosition = entity.transform.position;
			entity.scenePlacedHomeRotation = entity.transform.rotation;
			entity.scenePlacedHomeScale = entity.transform.lossyScale.x;
			if (!entity.gameObject.activeSelf)
			{
				entity.gameObject.SetActive(value: true);
			}
			entity.IsScenePlaced = true;
			entity.Create(this, num, -2147483647);
			entity.Init(0L, -1);
			AddGameEntity(num, entity);
			entity.scenePlacedInitialized = true;
		}
		else
		{
			GameEntityManager manager = entity.manager;
			if (manager != null && manager != this)
			{
				manager.RemoveGameEntity(entity);
				if (entity.builtInEntities != null)
				{
					for (int i = 0; i < entity.builtInEntities.Count; i++)
					{
						manager.RemoveGameEntity(entity.builtInEntities[i]);
					}
				}
			}
			entity.manager = this;
			AddGameEntity(num, entity);
			if (entity.builtInEntities != null)
			{
				bool flag = num < -1 && num != int.MinValue;
				for (int j = 0; j < entity.builtInEntities.Count; j++)
				{
					int netId = (flag ? (num - 1 - j) : (num + 1 + j));
					entity.builtInEntities[j].manager = this;
					AddGameEntity(netId, entity.builtInEntities[j]);
				}
			}
			if (!entity.gameObject.activeSelf)
			{
				entity.gameObject.SetActive(value: true);
			}
		}
		s_scenePlacedHomeScenes[num] = entity.gameObject.scene.name;
		EnsureScenePlacedRecord(entity);
	}

	private void EnsureScenePlacedRecord(GameEntity entity)
	{
		for (int i = 0; i < scenePlacedEntities.Count; i++)
		{
			if (scenePlacedEntities[i].entity == entity)
			{
				return;
			}
		}
		scenePlacedEntities.Add(new ScenePlacedRecord
		{
			entity = entity,
			position = entity.scenePlacedHomePosition,
			rotation = entity.scenePlacedHomeRotation,
			uniformScale = entity.scenePlacedHomeScale
		});
	}

	private static void ResetScenePlacedTransform(GameEntity entity, in ScenePlacedRecord record)
	{
		bool num = entity.transform.parent != null;
		bool flag = entity.IsHeld() || entity.snappedByActorNumber != -1;
		if (num || flag)
		{
			if (entity.manager != null)
			{
				entity.manager.ReleaseScenePlacedHold(entity);
			}
			else
			{
				DetachScenePlacedFromRig(entity);
			}
		}
		entity.transform.SetPositionAndRotation(record.position, record.rotation);
		entity.transform.localScale = Vector3.one * record.uniformScale;
		Rigidbody component = entity.GetComponent<Rigidbody>();
		if (component != null)
		{
			component.linearVelocity = Vector3.zero;
			component.angularVelocity = Vector3.zero;
		}
	}

	internal void ReleaseScenePlacedHold(GameEntity entity)
	{
		if (entity == null || !entity.IsScenePlaced)
		{
			return;
		}
		int heldByActorNumber = entity.heldByActorNumber;
		int snappedByActorNumber = entity.snappedByActorNumber;
		if (heldByActorNumber != -1 && GamePlayer.TryGetGamePlayer(heldByActorNumber, out var out_gamePlayer))
		{
			out_gamePlayer.ClearGrabbedIfHeld(entity.id, this);
			if (out_gamePlayer.IsLocal())
			{
				GamePlayerLocal.instance?.ClearGrabbedIfHeld(entity.id, this);
			}
		}
		if (snappedByActorNumber != -1 && GamePlayer.TryGetGamePlayer(snappedByActorNumber, out var out_gamePlayer2))
		{
			out_gamePlayer2.ClearSnappedIfSnapped(entity.id, this);
		}
		DetachScenePlacedFromRig(entity);
		MoveScenePlacedToHomeScene(entity);
		bool num = entity.heldByActorNumber != -1 || entity.snappedByActorNumber != -1 || entity.attachedToEntityId != GameEntityId.Invalid;
		entity.heldByActorNumber = -1;
		entity.heldByHandIndex = -1;
		entity.snappedByActorNumber = -1;
		entity.snappedJoint = SnapJointType.None;
		entity.attachedToEntityId = GameEntityId.Invalid;
		if (num)
		{
			entity.OnReleased?.Invoke();
		}
	}

	private static void DetachScenePlacedFromRig(GameEntity entity)
	{
		Transform parent = entity.transform.parent;
		if (!(parent == null) && parent.GetComponentInParent<VRRig>() != null)
		{
			entity.transform.SetParent(null, worldPositionStays: true);
		}
	}

	private static void MoveScenePlacedToHomeScene(GameEntity entity)
	{
		if (!(entity == null) && entity.IsScenePlaced && !(entity.manager == null) && s_scenePlacedHomeScenes.TryGetValue(entity.GetNetId(), out var value))
		{
			Scene sceneByName = SceneManager.GetSceneByName(value);
			if (sceneByName.IsValid() && sceneByName.isLoaded && !(entity.gameObject.scene == sceneByName))
			{
				SceneManager.MoveGameObjectToScene(entity.gameObject, sceneByName);
			}
		}
	}

	internal override void OnEnable()
	{
		NetworkBehaviourUtils.InternalOnEnable(this);
		base.OnEnable();
		TickSystem<object>.AddTickCallback(this);
		VRRigCache.OnRigDeactivated += OnRigDeactivated;
		VRRigCache.OnActiveRigsChanged += RefreshRigList;
		RoomSystem.JoinedRoomEvent += new Action(OnNetworkJoinedRoom);
		RoomSystem.LeftRoomEvent += new Action(OnNetworkLeftRoom);
		RoomSystem.PlayerLeftEvent += new Action<NetPlayer>(OnNetworkPlayerLeft);
		RefreshRigList();
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	internal override void OnDisable()
	{
		NetworkBehaviourUtils.InternalOnDisable(this);
		base.OnDisable();
		TickSystem<object>.RemoveTickCallback(this);
		VRRigCache.OnRigDeactivated -= OnRigDeactivated;
		VRRigCache.OnActiveRigsChanged -= RefreshRigList;
		RoomSystem.JoinedRoomEvent -= new Action(OnNetworkJoinedRoom);
		RoomSystem.LeftRoomEvent -= new Action(OnNetworkLeftRoom);
		RoomSystem.PlayerLeftEvent -= new Action<NetPlayer>(OnNetworkPlayerLeft);
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	private void OnDestroy()
	{
		NetworkBehaviourUtils.InternalOnDestroy(this);
		netIds.Dispose();
		allManagers.Remove(this);
		if (managersByZone.TryGetValue((int)zone, out var value) && value == this)
		{
			managersByZone.Remove((int)zone);
		}
	}

	public static GameEntityManager GetManagerForZone(GTZone zone)
	{
		for (int i = 0; i < allManagers.Count; i++)
		{
			if (allManagers[i].zone == zone)
			{
				return allManagers[i];
			}
		}
		return null;
	}

	public void SliceUpdate()
	{
	}

	public void Tick()
	{
		if (ApplicationQuittingState.IsQuitting)
		{
			return;
		}
		UpdateZoneState();
		if (!IsZoneActive())
		{
			if (netIdsForCreate.Count > 0 || netIdsForDelete.Count > 0 || netIdsForState.Count > 0)
			{
				ClearPendingRPCBatches();
			}
			if (PendingTableData)
			{
				if (Time.frameCount - pendingTableDataSetFrame > 90)
				{
					ResolveTableData();
				}
				else
				{
					_ = Time.frameCount % 300;
				}
			}
			return;
		}
		float time = Time.time;
		for (int i = 0; i < entities.Count; i++)
		{
			GameEntity gameEntity = entities[i];
			if (gameEntity != null && gameEntity.LastTickTime + gameEntity.MinTimeBetweenTicks < time && gameEntity.isActiveAndEnabled)
			{
				gameEntity.OnTick?.Invoke();
				gameEntity.LastTickTime = time;
			}
		}
		if (!IsAuthority())
		{
			return;
		}
		if (scenePlacedEntities.Count > 0)
		{
			scenePlacedBoundsCheckTimer -= Time.deltaTime;
			if (scenePlacedBoundsCheckTimer <= 0f)
			{
				scenePlacedBoundsCheckTimer = 1f;
				for (int j = 0; j < scenePlacedEntities.Count; j++)
				{
					ScenePlacedRecord record = scenePlacedEntities[j];
					if (!(record.entity == null))
					{
						Vector3 position = record.entity.transform.position;
						if (!((position - record.position).sqrMagnitude < 0.0625f) && !IsPositionInManagerBounds(position))
						{
							ResetScenePlacedTransform(record.entity, in record);
						}
					}
				}
			}
		}
		if (netIdsForCreate.Count > 0 && Time.time > lastCreateSent + createCooldown)
		{
			lastCreateSent = Time.time;
			photonView.RPC("CreateItemRPC", RpcTarget.Others, netIdsForCreate.ToArray(), entityTypeIdsForCreate.ToArray(), packedPositionsForCreate.ToArray(), packedRotationsForCreate.ToArray(), createDataForCreate.ToArray(), createdByEntityNetIdForCreate.ToArray());
			netIdsForCreate.Clear();
			entityTypeIdsForCreate.Clear();
			packedPositionsForCreate.Clear();
			packedRotationsForCreate.Clear();
			createDataForCreate.Clear();
			createdByEntityNetIdForCreate.Clear();
		}
		if (netIdsForDelete.Count > 0 && Time.time > lastDestroySent + destroyCooldown)
		{
			lastDestroySent = Time.time;
			photonView.RPC("DestroyItemRPC", RpcTarget.Others, netIdsForDelete.ToArray());
			netIdsForDelete.Clear();
		}
		if (netIdsForState.Count > 0 && Time.time > lastStateSent + stateCooldown)
		{
			lastStateSent = Time.time;
			photonView.RPC("ApplyStateRPC", RpcTarget.All, netIdsForState.ToArray(), statesForState.ToArray());
			netIdsForState.Clear();
			statesForState.Clear();
		}
	}

	public GameEntityId AddGameEntity(GameEntity gameEntity)
	{
		return AddGameEntity(CreateNetId(1 + gameEntity.builtInEntities.Count), gameEntity);
	}

	public GameEntityId AddGameEntity(int netId, GameEntity gameEntity)
	{
		if (netId == -1)
		{
			Debug.LogError("[GT/GameEntityManager]  ERROR!!!  AddGameEntity: Aborting. Invalid netId for GameEntity '" + gameEntity?.name + "'.", gameEntity);
			return GameEntityId.Invalid;
		}
		if (netIdToIndex.TryGetValue(netId, out var value) && value != -1)
		{
			GameEntity gameEntity2 = GetGameEntity(value);
			if (gameEntity2 != null)
			{
				if (gameEntity2 == gameEntity)
				{
					return gameEntity.id;
				}
				Debug.LogError("[GT/GameEntityManager]  ERROR!!!  AddGameEntity" + $": NetId {netId} collision: " + "'" + gameEntity2.name + "' replaced by '" + gameEntity.name + "'. Destroying old entity to prevent zombie.");
				DestroyItemLocal(gameEntity2.id);
			}
		}
		int num = FindNewEntityIndex();
		entities[num] = gameEntity;
		entitiesActiveCount++;
		gameEntityData.Add(default(GameEntityData));
		gameEntity.id = new GameEntityId
		{
			index = num
		};
		netIdToIndex[netId] = num;
		netIds[num] = netId;
		OnEntityAdded?.Invoke(gameEntity);
		return gameEntity.id;
	}

	private int FindNewEntityIndex()
	{
		for (int i = 0; i < entities.Count; i++)
		{
			if (entities[i] == null)
			{
				return i;
			}
		}
		entities.Add(null);
		return entities.Count - 1;
	}

	public void RemoveGameEntity(GameEntity entity)
	{
		netIdToIndex.Remove(entity.GetNetId());
		int index = entity.id.index;
		if (index < 0 || index >= entities.Count)
		{
			return;
		}
		if (entities[index] == entity)
		{
			entities[index] = null;
			entitiesActiveCount--;
		}
		else
		{
			for (int i = 0; i < entities.Count; i++)
			{
				if (entities[i] == entity)
				{
					entities[i] = null;
					entitiesActiveCount--;
					break;
				}
			}
		}
		OnEntityRemoved?.Invoke(entity);
	}

	public List<GameEntity> GetGameEntities()
	{
		return entities;
	}

	public bool IsValidNetId(int netId)
	{
		if (netIdToIndex.TryGetValue(netId, out var value) && value >= 0)
		{
			return value < entities.Count;
		}
		return false;
	}

	public int FindOpenIndex()
	{
		for (int i = 0; i < netIds.Length; i++)
		{
			if (netIds[i] != -1)
			{
				return i;
			}
		}
		return -1;
	}

	public GameEntityId GetEntityIdFromNetId(int netId)
	{
		if (netIdToIndex.TryGetValue(netId, out var value))
		{
			return new GameEntityId
			{
				index = value
			};
		}
		return GameEntityId.Invalid;
	}

	public int GetNetIdFromEntityId(GameEntityId id)
	{
		if (id.index < 0 || id.index >= netIds.Length)
		{
			return -1;
		}
		return netIds[id.index];
	}

	private void ClearPendingRPCBatches()
	{
		netIdsForCreate.Clear();
		entityTypeIdsForCreate.Clear();
		packedPositionsForCreate.Clear();
		packedRotationsForCreate.Clear();
		createDataForCreate.Clear();
		createdByEntityNetIdForCreate.Clear();
		netIdsForDelete.Clear();
		netIdsForState.Clear();
		statesForState.Clear();
	}

	public virtual bool IsAuthority()
	{
		if (NetworkSystem.Instance.InRoom)
		{
			return guard.isTrulyMine;
		}
		return true;
	}

	public bool IsAuthorityPlayer(NetPlayer player)
	{
		if (player != null)
		{
			return IsAuthorityPlayer(player.GetPlayerRef());
		}
		return false;
	}

	public bool IsAuthorityPlayer(Player player)
	{
		if (player != null && guard.actualOwner != null)
		{
			return player.ActorNumber == guard.actualOwner.GetPlayerRef()?.ActorNumber;
		}
		return false;
	}

	public bool IsZoneAuthority()
	{
		return IsAuthority();
	}

	public bool HasAuthority()
	{
		return GetAuthorityPlayer() != null;
	}

	public Player GetAuthorityPlayer()
	{
		if (guard.actualOwner != null)
		{
			return guard.actualOwner.GetPlayerRef();
		}
		return null;
	}

	public virtual bool IsZoneActive()
	{
		if (GorillaComputer.instance != null && GorillaComputer.instance.IsPlayerInVirtualStump() && IsSuppressZonesInVStumpEnabled())
		{
			if (CustomMapLoader.CanLoadEntities && zone == GTZone.customMaps)
			{
				return zoneStateData.state == ZoneState.Active;
			}
			return false;
		}
		return zoneStateData.state == ZoneState.Active;
	}

	private static bool IsSuppressZonesInVStumpEnabled()
	{
		GorillaServer instance = GorillaServer.Instance;
		if (instance != null)
		{
			return instance.CheckIsSuppressZonesInVStumpEnabled();
		}
		return false;
	}

	public virtual bool IsPositionInManagerBounds(Vector3 pos)
	{
		if (boundsBoxCollider != null)
		{
			return boundsBoxCollider.bounds.Contains(pos);
		}
		ZoneGraphBSP instance = ZoneGraphBSP.Instance;
		if (instance != null && instance.HasCompiledTree())
		{
			ZoneDef zoneDef = instance.FindZoneAtPoint(pos);
			return zoneDef != null && zoneDef.zoneId == zone;
		}
		return true;
	}

	public virtual bool IsValidClientRPC(Player sender)
	{
		bool num = IsAuthorityPlayer(sender);
		bool flag = IsZoneActive();
		bool flag2 = sender.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber;
		if (num)
		{
			return flag || flag2;
		}
		return false;
	}

	public bool IsValidClientRPC(Player sender, int entityNetId)
	{
		if (IsValidClientRPC(sender))
		{
			return IsValidNetId(entityNetId);
		}
		return false;
	}

	public bool IsValidClientRPC(Player sender, int entityNetId, Vector3 pos)
	{
		if (IsValidClientRPC(sender, entityNetId))
		{
			return IsPositionInManagerBounds(pos);
		}
		return false;
	}

	public bool IsValidClientRPC(Player sender, Vector3 pos)
	{
		if (IsValidClientRPC(sender))
		{
			return IsPositionInManagerBounds(pos);
		}
		return false;
	}

	public bool IsValidAuthorityRPC(Player sender)
	{
		if (IsAuthority())
		{
			if (!IsZoneActive())
			{
				return sender.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber;
			}
			return true;
		}
		return false;
	}

	public bool IsValidAuthorityRPC(Player sender, int entityNetId)
	{
		if (IsValidAuthorityRPC(sender))
		{
			return IsValidNetId(entityNetId);
		}
		return false;
	}

	public bool IsValidAuthorityRPC(Player sender, int entityNetId, Vector3 pos)
	{
		if (IsValidAuthorityRPC(sender, entityNetId))
		{
			return IsPositionInManagerBounds(pos);
		}
		return false;
	}

	public bool IsValidAuthorityRPC(Player sender, Vector3 pos)
	{
		if (IsValidAuthorityRPC(sender))
		{
			return IsPositionInManagerBounds(pos);
		}
		return false;
	}

	public bool IsValidEntity(GameEntityId id)
	{
		return GetGameEntity(id) != null;
	}

	public GameEntity GetGameEntity(GameEntityId id)
	{
		if (!id.IsValid())
		{
			return null;
		}
		return GetGameEntity(id.index);
	}

	public GameEntity GetGameEntityFromNetId(int netId)
	{
		if (netIdToIndex.TryGetValue(netId, out var value))
		{
			return GetGameEntity(value);
		}
		return null;
	}

	private GameEntity GetGameEntity(int index)
	{
		if (index == -1)
		{
			return null;
		}
		if (index < 0 || index >= entities.Count)
		{
			return null;
		}
		return entities[index];
	}

	public T GetGameComponent<T>(GameEntityId id) where T : Component
	{
		GameEntity gameEntity = GetGameEntity(id);
		if (gameEntity == null)
		{
			return null;
		}
		return gameEntity.GetComponent<T>();
	}

	public bool LocalValidateMigrationRecoveryItem(int entityTypeId, ref long createData)
	{
		GameObject gameObject = FactoryPrefabById(entityTypeId);
		if (gameObject == null)
		{
			return false;
		}
		GameEntity component = gameObject.GetComponent<GameEntity>();
		if (component != null)
		{
			for (int i = 0; i < zoneComponents.Count; i++)
			{
				createData = zoneComponents[i].ProcessMigratedGameEntityCreateData(component, createData);
			}
		}
		int actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
		for (int j = 0; j < zoneComponents.Count; j++)
		{
			if (!zoneComponents[j].ValidateMigratedGameEntity(0, entityTypeId, Vector3.zero, Quaternion.identity, createData, actorNumber))
			{
				return false;
			}
		}
		return true;
	}

	public bool IsEntityValidToMigrate(GameEntity entity)
	{
		if (entity == null)
		{
			return false;
		}
		Vector3 position = VRRig.LocalRig.transform.position;
		int actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
		bool flag = true;
		for (int i = 0; i < zoneComponents.Count && flag; i++)
		{
			flag &= zoneComponents[i].ValidateMigratedGameEntity(GetNetIdFromEntityId(entity.id), entity.typeId, position, Quaternion.identity, entity.createData, actorNumber);
		}
		return flag;
	}

	private void BuildFactory()
	{
		using Utf16ValueStringBuilder utf16ValueStringBuilder = ZString.CreateStringBuilder(notNested: true);
		string value = "[GT/GameEntityManager]  BuildFactory: Entity names and typeIds for manager \"" + base.name + "\":";
		utf16ValueStringBuilder.AppendLine(value);
		foreach (IGameEntityZoneComponent zoneComponent in zoneComponents)
		{
			if (!(zoneComponent is IFactoryItemProvider factoryItemProvider))
			{
				continue;
			}
			foreach (GameEntity factoryItem in factoryItemProvider.GetFactoryItems())
			{
				if (!tempFactoryItems.Contains(factoryItem))
				{
					tempFactoryItems.Add(factoryItem);
				}
			}
		}
		itemPrefabFactory = new Dictionary<int, GameObject>(1024);
		priceLookupByEntityId = new Dictionary<int, int>();
		for (int i = 0; i < tempFactoryItems.Count; i++)
		{
			GameObject gameObject = tempFactoryItems[i].gameObject;
			int staticHash = gameObject.name.GetStaticHash();
			if ((bool)gameObject.GetComponent<GRToolLantern>())
			{
				priceLookupByEntityId.Add(staticHash, 50);
			}
			else if ((bool)gameObject.GetComponent<GRToolCollector>())
			{
				priceLookupByEntityId.Add(staticHash, 50);
			}
			itemPrefabFactory.Add(staticHash, gameObject);
			utf16ValueStringBuilder.AppendFormat("    - name=\"{0}\", typeId={1}\n", gameObject.name, staticHash);
			if (utf16ValueStringBuilder.Length > 5000)
			{
				utf16ValueStringBuilder.Append("... (continued in next log message) ...");
				utf16ValueStringBuilder.Clear();
				if (i + 1 < tempFactoryItems.Count)
				{
					utf16ValueStringBuilder.Append(value);
					utf16ValueStringBuilder.Append(" ... CONTINUED FROM PREVIOUS ...\n");
				}
			}
		}
	}

	private int CreateNetId(int numToCreate)
	{
		int result = nextNetId;
		nextNetId += numToCreate;
		return result;
	}

	private void RecalculateNextNetId()
	{
		int num = 0;
		for (int i = 0; i < entities.Count; i++)
		{
			if (!(entities[i] != null))
			{
				continue;
			}
			int num2 = netIds[i];
			if (num2 >= 0)
			{
				int num3 = num2 + entities[i].builtInEntities.Count;
				if (num3 > num)
				{
					num = num3;
				}
			}
		}
		nextNetId = num + 1;
	}

	public GameEntityId RequestCreateItem(int entityTypeId, Vector3 position, Quaternion rotation, long createData)
	{
		return RequestCreateItem(entityTypeId, position, rotation, createData, GameEntityId.Invalid);
	}

	public GameEntityId RequestCreateItem(int entityTypeId, Vector3 position, Quaternion rotation, long createData, GameEntityId createdByEntityId)
	{
		if (!IsZoneAuthority() || !IsZoneActive() || !IsPositionInManagerBounds(position))
		{
			return GameEntityId.Invalid;
		}
		int netIdFromEntityId = GetNetIdFromEntityId(createdByEntityId);
		for (int i = 0; i < zoneComponents.Count; i++)
		{
			if (!zoneComponents[i].ValidateCreateItem(0, entityTypeId, position, rotation, createData, netIdFromEntityId))
			{
				if (zoneComponents[i] is MonoBehaviour monoBehaviour)
				{
					_ = monoBehaviour.name;
				}
				return GameEntityId.Invalid;
			}
		}
		long item = BitPackUtils.PackWorldPosForNetwork(position);
		int item2 = BitPackUtils.PackQuaternionForNetwork(rotation);
		int numToCreate = 1 + FactoryGetBuiltInEntityCountById(entityTypeId);
		int num = CreateNetId(numToCreate);
		netIdsForCreate.Add(num);
		entityTypeIdsForCreate.Add(entityTypeId);
		packedPositionsForCreate.Add(item);
		packedRotationsForCreate.Add(item2);
		createDataForCreate.Add(createData);
		createdByEntityNetIdForCreate.Add(netIdFromEntityId);
		return CreateAndInitItemLocal(num, entityTypeId, position, rotation, createData, netIdFromEntityId);
	}

	[PunRPC]
	public void CreateItemRPC(int[] netId, int[] entityTypeId, long[] packedPos, int[] packedRot, long[] createData, int[] createdByEntityNetId, PhotonMessageInfo info)
	{
		if (!IsValidClientRPC(info.Sender) || m_RpcSpamChecks.IsSpamming(RPC.CreateItem) || netId == null || entityTypeId == null || packedPos == null || createData == null || createdByEntityNetId == null || netId.Length != entityTypeId.Length || netId.Length != packedPos.Length || netId.Length != packedRot.Length || netId.Length != createData.Length || netId.Length != createdByEntityNetId.Length)
		{
			return;
		}
		if (netId.Length > 1)
		{
			for (int i = 0; i < zoneComponents.Count; i++)
			{
				if (!zoneComponents[i].ValidateCreateItemBatchSize(netId.Length))
				{
					return;
				}
			}
		}
		for (int j = 0; j < netId.Length; j++)
		{
			if (IsScenePlacedNetId(netId[j]))
			{
				continue;
			}
			Vector3 v = BitPackUtils.UnpackWorldPosFromNetwork(packedPos[j]);
			Quaternion q = BitPackUtils.UnpackQuaternionFromNetwork(packedRot[j]);
			if (!v.IsValid(10000f) || !q.IsValid() || !FactoryHasEntity(entityTypeId[j]) || !IsPositionInManagerBounds(v))
			{
				break;
			}
			int num = netId[j];
			int entityTypeId2 = entityTypeId[j];
			long createData2 = createData[j];
			int createdByEntityNetId2 = createdByEntityNetId[j];
			bool flag = true;
			for (int k = 0; k < zoneComponents.Count; k++)
			{
				if (!zoneComponents[k].ValidateCreateItem(num, entityTypeId2, v, q, createData2, createdByEntityNetId2))
				{
					flag = false;
				}
			}
			if (flag)
			{
				CreateAndInitItemLocal(num, entityTypeId2, v, q, createData2, createdByEntityNetId2);
			}
		}
	}

	public void RequestCreateItems(List<GameEntityCreateData> entityData)
	{
		if (!IsZoneAuthority() || !IsZoneActive())
		{
			GTDev.LogError($"[GameEntityManager::RequestCreateItems] Cannot create items. Zone Auth: {IsZoneAuthority()} " + $"| Zone Active: {IsZoneActive()}");
			return;
		}
		ClearByteBuffer(tempSerializeGameState);
		MemoryStream memoryStream = new MemoryStream(tempSerializeGameState);
		BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
		binaryWriter.Write(entityData.Count);
		for (int i = 0; i < entityData.Count; i++)
		{
			GameEntityCreateData gameEntityCreateData = entityData[i];
			int numToCreate = 1 + FactoryGetBuiltInEntityCountById(gameEntityCreateData.entityTypeId);
			int value = CreateNetId(numToCreate);
			long value2 = BitPackUtils.PackWorldPosForNetwork(gameEntityCreateData.position);
			int value3 = BitPackUtils.PackQuaternionForNetwork(gameEntityCreateData.rotation);
			binaryWriter.Write(value);
			binaryWriter.Write(gameEntityCreateData.entityTypeId);
			binaryWriter.Write(value2);
			binaryWriter.Write(value3);
			binaryWriter.Write(gameEntityCreateData.createData);
			binaryWriter.Write(gameEntityCreateData.createdByEntityId);
		}
		_ = memoryStream.Position;
		byte[] array = GZipStream.CompressBuffer(tempSerializeGameState);
		photonView.RPC("CreateItemsRPC", RpcTarget.All, (int)zone, array);
	}

	[PunRPC]
	public void CreateItemsRPC(int zoneId, byte[] stateData, PhotonMessageInfo info)
	{
		if (!IsValidClientRPC(info.Sender) || stateData == null || stateData.Length >= 15360 || m_RpcSpamChecks.IsSpamming(RPC.CreateItems))
		{
			return;
		}
		try
		{
			using MemoryStream input = new MemoryStream(GZipStream.UncompressBuffer(stateData));
			using BinaryReader binaryReader = new BinaryReader(input);
			int num = binaryReader.ReadInt32();
			for (int i = 0; i < zoneComponents.Count; i++)
			{
				if (!zoneComponents[i].ValidateCreateMultipleItems(zoneId, stateData, num))
				{
					return;
				}
			}
			for (int j = 0; j < num; j++)
			{
				int num2 = binaryReader.ReadInt32();
				int entityTypeId = binaryReader.ReadInt32();
				long data = binaryReader.ReadInt64();
				int data2 = binaryReader.ReadInt32();
				long createData = binaryReader.ReadInt64();
				int createdByEntityNetId = binaryReader.ReadInt32();
				Vector3 v = BitPackUtils.UnpackWorldPosFromNetwork(data);
				Quaternion q = BitPackUtils.UnpackQuaternionFromNetwork(data2);
				if (v.IsValid(10000f) && q.IsValid() && FactoryHasEntity(entityTypeId) && IsPositionInManagerBounds(v))
				{
					bool flag = true;
					for (int k = 0; k < zoneComponents.Count; k++)
					{
						flag &= zoneComponents[k].ValidateCreateItem(num2, entityTypeId, v, q, createData, createdByEntityNetId);
					}
					if (flag)
					{
						CreateAndInitItemLocal(num2, entityTypeId, v, q, createData, createdByEntityNetId);
					}
				}
			}
		}
		catch (Exception)
		{
		}
	}

	public void RequestMigrationRecovery(List<GameEntityCreateData> entityData)
	{
		if (entityData == null || entityData.Count == 0)
		{
			return;
		}
		ClearByteBuffer(tempSerializeGameState);
		using MemoryStream output = new MemoryStream(tempSerializeGameState);
		using BinaryWriter binaryWriter = new BinaryWriter(output);
		binaryWriter.Write(entityData.Count);
		for (int i = 0; i < entityData.Count; i++)
		{
			GameEntityCreateData gameEntityCreateData = entityData[i];
			_JoinWithItems_WriteOne(binaryWriter, gameEntityCreateData.entityTypeId, gameEntityCreateData.position, gameEntityCreateData.rotation, gameEntityCreateData.createData, gameEntityCreateData.createdByEntityId, gameEntityCreateData.slotIndex);
		}
		byte[] array = GZipStream.CompressBuffer(tempSerializeGameState);
		photonView.RPC("JoinWithItemsRPC", GetAuthorityPlayer(), array, Array.Empty<int>(), PhotonNetwork.LocalPlayer.ActorNumber);
	}

	public void JoinWithItems(List<GameEntity> entities)
	{
		if (entities.Count == 0)
		{
			return;
		}
		ClearByteBuffer(tempSerializeGameState);
		MemoryStream memoryStream = new MemoryStream(tempSerializeGameState);
		BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
		int num = 0;
		for (int i = 0; i < entities.Count; i++)
		{
			if (entities[i] != null)
			{
				num++;
			}
		}
		binaryWriter.Write(num);
		for (int j = 0; j < entities.Count; j++)
		{
			GameEntity gameEntity = entities[j];
			if (!(gameEntity == null))
			{
				long createData = gameEntity.createData;
				for (int k = 0; k < zoneComponents.Count; k++)
				{
					createData = zoneComponents[k].ProcessMigratedGameEntityCreateData(gameEntity, createData);
				}
				_JoinWithItems_WriteOne(binaryWriter, gameEntity.typeId, gameEntity.transform.localPosition, gameEntity.transform.localRotation, createData, GetNetIdFromEntityId(gameEntity.createdByEntityId), gameEntity.slotIndex);
			}
		}
		_ = memoryStream.Position;
		byte[] array = GZipStream.CompressBuffer(tempSerializeGameState);
		photonView.RPC("JoinWithItemsRPC", GetAuthorityPlayer(), array, Array.Empty<int>(), PhotonNetwork.LocalPlayer.ActorNumber);
	}

	[PunRPC]
	public void PlayerLeftZoneRPC(PhotonMessageInfo info)
	{
		if (m_RpcSpamChecks.IsSpamming(RPC.PlayerLeftZone))
		{
			return;
		}
		GamePlayer gamePlayer = GamePlayer.GetGamePlayer(info.Sender);
		if (gamePlayer == null)
		{
			return;
		}
		if (NetworkSystem.Instance.SessionIsPrivate)
		{
			gamePlayer.DidJoinWithItems = false;
		}
		_leavingItemScratch.Clear();
		foreach (GameEntityId item in gamePlayer.IterateHeldAndSnappedItems(this))
		{
			_leavingItemScratch.Add(item);
		}
		for (int i = 0; i < _leavingItemScratch.Count; i++)
		{
			GameEntityId gameEntityId = _leavingItemScratch[i];
			GameEntity gameEntity = GetGameEntity(gameEntityId);
			if (gameEntity != null && gameEntity.IsScenePlaced)
			{
				ReleaseScenePlacedHold(gameEntity);
				if (IsAuthority() && TryGetScenePlacedRecord(gameEntity, out var record))
				{
					ResetScenePlacedTransform(gameEntity, in record);
				}
			}
			else
			{
				if (!netIdsForDelete.Contains(GetNetIdFromEntityId(gameEntityId)))
				{
					netIdsForDelete.Add(GetNetIdFromEntityId(gameEntityId));
				}
				DestroyItemLocal(gameEntityId);
			}
		}
		_leavingItemScratch.Clear();
		playerZoneJoinTimes.Remove(info.Sender.ActorNumber);
		gamePlayer.OnPlayerLeftZone?.Invoke();
	}

	private bool TryGetScenePlacedRecord(GameEntity entity, out ScenePlacedRecord record)
	{
		for (int i = 0; i < scenePlacedEntities.Count; i++)
		{
			if (scenePlacedEntities[i].entity == entity)
			{
				record = scenePlacedEntities[i];
				return true;
			}
		}
		record = default(ScenePlacedRecord);
		return false;
	}

	[PunRPC]
	public void JoinWithItemsRPC(byte[] stateData, int[] netIds, int joiningActorNum, PhotonMessageInfo info)
	{
		bool isAuthority = IsAuthority();
		if (isAuthority)
		{
			if (!IsValidAuthorityRPC(info.Sender))
			{
				return;
			}
		}
		else if (!IsAuthorityPlayer(info.Sender))
		{
			return;
		}
		float value;
		bool flag = playerZoneJoinTimes.TryGetValue(joiningActorNum, out value) && Time.unscaledTime - value < 10f;
		GamePlayer joiningPlayer;
		bool num = GamePlayer.TryGetGamePlayer(joiningActorNum, out joiningPlayer);
		bool flag2 = !isAuthority && GetAuthorityPlayer()?.ActorNumber != info.Sender.ActorNumber;
		bool flag3 = isAuthority && info.Sender.ActorNumber != joiningActorNum;
		bool flag4 = stateData == null || stateData.Length >= 255;
		bool flag5 = num && joiningPlayer.DidJoinWithItems && !flag;
		if (!num || flag2 || flag3 || flag4 || flag5 || !IsInZone())
		{
			return;
		}
		if (isAuthority)
		{
			joiningPlayer.DidJoinWithItems = true;
		}
		Action createItemsCallback = null;
		createItemsCallback = delegate
		{
			try
			{
				GamePlayer gamePlayer2 = joiningPlayer;
				gamePlayer2.OnPlayerInitialized = (Action)Delegate.Remove(gamePlayer2.OnPlayerInitialized, createItemsCallback);
				using MemoryStream input = new MemoryStream(GZipStream.UncompressBuffer(stateData));
				using BinaryReader binaryReader = new BinaryReader(input);
				int num2 = binaryReader.ReadInt32();
				if (num2 <= 4 && (isAuthority || netIds.Length == num2))
				{
					if (isAuthority)
					{
						netIds = new int[num2];
					}
					for (int i = 0; i < num2; i++)
					{
						_JoinWithItems_ReadOne(binaryReader, out var entityTypeId, out var localPos, out var localRot, out var createData, out var createdByEntityNetId, out var slotIndex);
						if (!joiningPlayer.TryGetSlotXform(slotIndex, out var slotXform))
						{
							Debug.LogError("[GT/GameEntityManager]  ERROR!!!  " + $"JoinWithItemsRPC: No slot transform for item's slot, {slotIndex}.");
						}
						else
						{
							if (isAuthority)
							{
								int numToCreate = 1 + FactoryGetBuiltInEntityCountById(entityTypeId);
								netIds[i] = CreateNetId(numToCreate);
							}
							int netId = netIds[i];
							Vector3 pos = slotXform.TransformPoint(localPos);
							if (localPos.IsValid(10000f) && localRot.IsValid() && FactoryHasEntity(entityTypeId) && IsPositionInManagerBounds(pos))
							{
								bool flag6 = true;
								for (int j = 0; j < zoneComponents.Count && flag6; j++)
								{
									flag6 &= zoneComponents[j].ValidateMigratedGameEntity(netId, entityTypeId, joiningPlayer.rig.transform.position, Quaternion.identity, createData, joiningActorNum);
								}
								if (flag6)
								{
									GameEntityId gameEntityId = CreateAndInitItemLocal(netId, entityTypeId, joiningPlayer.rig.transform.position, Quaternion.identity, createData, createdByEntityNetId);
									bool isLeftHand = slotIndex == 0;
									SnapJointType snapIndexToJoint = GameSnappable.GetSnapIndexToJoint(slotIndex);
									if (snapIndexToJoint != SnapJointType.None)
									{
										SnapEntityLocal(gameEntityId, isLeftHand, localPos, localRot, (int)snapIndexToJoint, joiningPlayer.rig.Creator);
									}
									else
									{
										GrabEntityOnCreate(gameEntityId, isLeftHand, localPos, localRot, joiningPlayer.rig.Creator);
									}
								}
							}
						}
					}
					if (isAuthority)
					{
						photonView.RPC("JoinWithItemsRPC", RpcTarget.Others, stateData, netIds, joiningActorNum);
					}
				}
			}
			catch (Exception)
			{
			}
		};
		if (joiningPlayer.AdditionalDataInitialized)
		{
			createItemsCallback();
			return;
		}
		GamePlayer gamePlayer = joiningPlayer;
		gamePlayer.OnPlayerInitialized = (Action)Delegate.Combine(gamePlayer.OnPlayerInitialized, createItemsCallback);
	}

	private static void _JoinWithItems_WriteOne(BinaryWriter writer, int typeId, Vector3 localPos, Quaternion localRot, long createData, int createdByEntityId, int slotIndex)
	{
		writer.Write(typeId);
		writer.Write(BitPackUtils.PackWorldPosForNetwork(localPos));
		writer.Write(BitPackUtils.PackQuaternionForNetwork(localRot));
		writer.Write(createData);
		writer.Write(createdByEntityId);
		writer.Write((byte)(slotIndex + 1));
	}

	private static void _JoinWithItems_ReadOne(BinaryReader reader, out int entityTypeId, out Vector3 localPos, out Quaternion localRot, out long createData, out int createdByEntityNetId, out int slotIndex)
	{
		entityTypeId = reader.ReadInt32();
		localPos = BitPackUtils.UnpackWorldPosFromNetwork(reader.ReadInt64());
		localRot = BitPackUtils.UnpackQuaternionFromNetwork(reader.ReadInt32());
		createData = reader.ReadInt64();
		createdByEntityNetId = reader.ReadInt32();
		slotIndex = reader.ReadByte() - 1;
	}

	public bool FactoryHasEntity(int entityTypeId)
	{
		GameObject value;
		return itemPrefabFactory.TryGetValue(entityTypeId, out value);
	}

	public GameObject FactoryPrefabById(int entityTypeId)
	{
		if (itemPrefabFactory.TryGetValue(entityTypeId, out var value))
		{
			return value;
		}
		return null;
	}

	public GameEntity FactoryEntityById(int entityTypeId)
	{
		if (itemPrefabFactory.TryGetValue(entityTypeId, out var value))
		{
			return value.GetComponent<GameEntity>();
		}
		return null;
	}

	public int FactoryGetBuiltInEntityCountById(int entityTypeId)
	{
		GameEntity gameEntity = FactoryEntityById(entityTypeId);
		if (gameEntity == null || gameEntity.builtInEntities == null)
		{
			return 0;
		}
		return gameEntity.builtInEntities.Count;
	}

	public bool PriceLookup(int entityTypeId, out int price)
	{
		if (priceLookupByEntityId.TryGetValue(entityTypeId, out price))
		{
			return true;
		}
		price = -1;
		return false;
	}

	private void ValidateThatNetIdIsNotAlreadyUsed(int netId, int newTypeId)
	{
		for (int i = 0; i < netIds.Length; i++)
		{
			if (i < entities.Count && netIds[i] == netId)
			{
				_ = entities[i] == null;
			}
		}
	}

	public GameEntityId CreateAndInitItemLocal(int netId, int entityTypeId, Vector3 position, Quaternion rotation, long createData, int createdByEntityNetId)
	{
		GameEntity gameEntity = CreateItemLocal(netId, entityTypeId, position, rotation);
		if (gameEntity == null)
		{
			return GameEntityId.Invalid;
		}
		InitItemLocal(gameEntity, createData, createdByEntityNetId);
		return gameEntity.id;
	}

	public GameEntity CreateItemLocal(int netId, int entityTypeId, Vector3 position, Quaternion rotation)
	{
		if (entityTypeId == -1)
		{
			return null;
		}
		nextNetId = Mathf.Max(netId + 1, nextNetId);
		if (!itemPrefabFactory.TryGetValue(entityTypeId, out var value))
		{
			return null;
		}
		if (!createdItemTypeCount.ContainsKey(entityTypeId))
		{
			createdItemTypeCount[entityTypeId] = 0;
		}
		if (createdItemTypeCount[entityTypeId] > 100)
		{
			return null;
		}
		createdItemTypeCount[entityTypeId]++;
		GameEntity componentInChildren = UnityEngine.Object.Instantiate(value, position, rotation).GetComponentInChildren<GameEntity>();
		AddGameEntity(netId, componentInChildren);
		componentInChildren.Create(this, netId, entityTypeId);
		return componentInChildren;
	}

	public void InitItemLocal(GameEntity entity, long createData, int createdByEntityNetId)
	{
		entity.Init(createData, createdByEntityNetId);
		for (int i = 0; i < zoneComponents.Count; i++)
		{
			zoneComponents[i].OnCreateGameEntity(entity);
		}
	}

	public void RequestDestroyItem(GameEntityId entityId)
	{
		if (!IsAuthority())
		{
			return;
		}
		GameEntity gameEntity = GetGameEntity(entityId);
		if (!(gameEntity != null) || !gameEntity.IsScenePlaced)
		{
			int netIdFromEntityId = GetNetIdFromEntityId(entityId);
			if (!netIdsForDelete.Contains(netIdFromEntityId))
			{
				netIdsForDelete.Add(netIdFromEntityId);
			}
			int num = netIdsForState.IndexOf(netIdFromEntityId);
			if (num >= 0)
			{
				netIdsForState.RemoveAt(num);
				statesForState.RemoveAt(num);
			}
			DestroyItemLocal(entityId);
		}
	}

	public void RequestDestroyItems(List<GameEntityId> entityIds)
	{
		if (IsAuthority())
		{
			List<int> list = new List<int>();
			for (int i = 0; i < entityIds.Count; i++)
			{
				list.Add(GetNetIdFromEntityId(entityIds[i]));
			}
			if (PhotonNetwork.InRoom)
			{
				photonView.RPC("DestroyItemRPC", RpcTarget.All, list.ToArray());
			}
		}
	}

	[PunRPC]
	public void DestroyItemRPC(int[] entityNetId, PhotonMessageInfo info)
	{
		if (entityNetId == null || m_RpcSpamChecks.IsSpamming(RPC.DestroyItem))
		{
			return;
		}
		for (int i = 0; i < entityNetId.Length && IsValidClientRPC(info.Sender, entityNetId[i]); i++)
		{
			if (!IsScenePlacedNetId(entityNetId[i]))
			{
				DestroyItemLocal(GetEntityIdFromNetId(entityNetId[i]));
			}
		}
	}

	public void DestroyItemLocal(GameEntityId entityId)
	{
		GameEntity gameEntity = GetGameEntity(entityId);
		if (gameEntity == null)
		{
			return;
		}
		if (!createdItemTypeCount.ContainsKey(gameEntity.typeId))
		{
			createdItemTypeCount[gameEntity.typeId] = 1;
		}
		createdItemTypeCount[gameEntity.typeId]--;
		if (GamePlayer.TryGetGamePlayer(gameEntity.heldByActorNumber, out var out_gamePlayer))
		{
			if (out_gamePlayer.IsLocal())
			{
				GamePlayerLocal.instance.ClearGrabbedIfHeld(gameEntity.id, this);
			}
			out_gamePlayer.ClearGrabbedIfHeld(gameEntity.id, this);
		}
		if (GamePlayer.TryGetGamePlayer(gameEntity.snappedByActorNumber, out var out_gamePlayer2))
		{
			out_gamePlayer2.ClearSnappedIfSnapped(gameEntity.id, this);
		}
		RemoveGameEntity(gameEntity);
		if (gameEntity.isBuiltIn || gameEntity.IsScenePlaced)
		{
			gameEntity.gameObject.SetActive(value: false);
		}
		else
		{
			UnityEngine.Object.Destroy(gameEntity.gameObject);
		}
	}

	public void RequestState(GameEntityId entityId, long newState)
	{
		if (IsAuthority())
		{
			RequestStateAuthority(entityId, newState);
			return;
		}
		photonView.RPC("RequestStateRPC", GetAuthorityPlayer(), GetNetIdFromEntityId(entityId), newState);
	}

	private void RequestStateAuthority(GameEntityId entityId, long newState)
	{
		if (!IsAuthority())
		{
			return;
		}
		int netIdFromEntityId = GetNetIdFromEntityId(entityId);
		if (IsValidNetId(netIdFromEntityId))
		{
			if (netIdsForState.Contains(netIdFromEntityId))
			{
				statesForState[netIdsForState.IndexOf(netIdFromEntityId)] = newState;
				return;
			}
			netIdsForState.Add(netIdFromEntityId);
			statesForState.Add(newState);
		}
	}

	[PunRPC]
	public void RequestStateRPC(int entityNetId, long newState, PhotonMessageInfo info)
	{
		if (!IsValidAuthorityRPC(info.Sender, entityNetId) || !GamePlayer.TryGetGamePlayer(info.Sender, out var gamePlayer) || !gamePlayer.netStateLimiter.CheckCallTime(Time.unscaledTime))
		{
			return;
		}
		GameEntityId entityIdFromNetId = GetEntityIdFromNetId(entityNetId);
		GameEntity gameEntity = GetGameEntity(entityIdFromNetId);
		if (gameEntity == null || gameEntity.IsNull())
		{
			return;
		}
		bool flag = false;
		GRToolClub component = gameEntity.GetComponent<GRToolClub>();
		GRToolCollector component2 = gameEntity.GetComponent<GRToolCollector>();
		GRToolRevive component3 = gameEntity.GetComponent<GRToolRevive>();
		GRToolLantern component4 = gameEntity.GetComponent<GRToolLantern>();
		GRToolFlash component5 = gameEntity.GetComponent<GRToolFlash>();
		GRToolDirectionalShield component6 = gameEntity.GetComponent<GRToolDirectionalShield>();
		GRToolShieldGun component7 = gameEntity.GetComponent<GRToolShieldGun>();
		if (component == null && component2 == null && component3 == null && component4 == null && component5 == null && component6 == null && component7 == null)
		{
			flag = IsAuthorityPlayer(info.Sender);
		}
		bool flag2 = gamePlayer.IsHoldingEntity(entityIdFromNetId, isLeftHand: false) || gamePlayer.IsHoldingEntity(entityIdFromNetId, isLeftHand: true);
		bool flag3 = gameEntity.lastHeldByActorNumber == info.Sender.ActorNumber;
		if (!flag && (flag2 || flag3))
		{
			if (component4 != null)
			{
				flag = component4.CanChangeState(newState);
			}
			if (component5 != null)
			{
				flag = component5.CanChangeState(newState);
			}
			if (component != null || component2 != null || component3 != null || component6 != null || component7 != null)
			{
				flag = true;
			}
		}
		if (!flag)
		{
			bool flag4 = gameEntity.snappedByActorNumber == gamePlayer.rig.OwningNetPlayer.ActorNumber;
			if (gameEntity.canHoldingPlayerUpdateState && flag2)
			{
				flag = true;
			}
			else if (gameEntity.canLastHoldingPlayerUpdateState && flag3)
			{
				flag = true;
			}
			else if (gameEntity.canSnapPlayerUpdateState && flag4)
			{
				flag = true;
			}
		}
		IGameEntityCustomStateChange component8 = gameEntity.GetComponent<IGameEntityCustomStateChange>();
		if (component8 != null)
		{
			flag = component8.CanChangeState(newState, info.Sender.ActorNumber);
		}
		if (flag)
		{
			if (netIdsForState.Contains(entityNetId))
			{
				statesForState[netIdsForState.IndexOf(entityNetId)] = newState;
				return;
			}
			netIdsForState.Add(entityNetId);
			statesForState.Add(newState);
		}
	}

	[PunRPC]
	public void ApplyStateRPC(int[] netId, long[] newState, PhotonMessageInfo info)
	{
		if (netId == null || newState == null || netId.Length != newState.Length || m_RpcSpamChecks.IsSpamming(RPC.ApplyState))
		{
			return;
		}
		for (int i = 0; i < netId.Length && IsValidClientRPC(info.Sender, netId[i]); i++)
		{
			GameEntityId entityIdFromNetId = GetEntityIdFromNetId(netId[i]);
			GameEntity gameEntity = entities[entityIdFromNetId.index];
			if (gameEntity != null)
			{
				gameEntity.SetState(newState[i]);
			}
		}
	}

	public void RequestGrabEntity(GameEntityId gameEntityId, bool isLeftHand, Vector3 localPosition, Quaternion localRotation)
	{
		bool inRoom = PhotonNetwork.InRoom;
		if (!IsAuthority() || !inRoom)
		{
			GrabEntityLocal(gameEntityId, isLeftHand, localPosition, localRotation, NetPlayer.Get(PhotonNetwork.LocalPlayer));
		}
		if (inRoom)
		{
			long num = BitPackUtils.PackHandPosRotForNetwork(localPosition, localRotation);
			photonView.RPC("RequestGrabEntityRPC", GetAuthorityPlayer(), GetNetIdFromEntityId(gameEntityId), isLeftHand, num);
		}
	}

	[PunRPC]
	public void RequestGrabEntityRPC(int entityNetId, bool isLeftHand, long packedPosRot, PhotonMessageInfo info)
	{
		if (!IsValidAuthorityRPC(info.Sender, entityNetId))
		{
			return;
		}
		BitPackUtils.UnpackHandPosRotFromNetwork(packedPosRot, out var localPos, out var handRot);
		if (localPos.IsValid(10000f) && handRot.IsValid() && !(localPos.sqrMagnitude > 6400f) && GamePlayer.TryGetGamePlayer(info.Sender, out var gamePlayer) && IsPlayerHandNearEntity(gamePlayer, entityNetId, isLeftHand, checkBothHands: false) && !IsValidEntity(gamePlayer.GetGameEntityId(isLeftHand)) && gamePlayer.netGrabLimiter.CheckCallTime(Time.time) && !gamePlayer.IsHoldingEntity(this, isLeftHand))
		{
			GameEntity gameEntity = GetGameEntity(GetEntityIdFromNetId(entityNetId));
			if (!(gameEntity == null) && ValidateGrab(gameEntity, info.Sender.ActorNumber, isLeftHand))
			{
				photonView.RPC("GrabEntityRPC", RpcTarget.All, entityNetId, isLeftHand, packedPosRot, info.Sender);
				PhotonNetwork.SendAllOutgoingCommands();
			}
		}
	}

	[PunRPC]
	public void GrabEntityRPC(int entityNetId, bool isLeftHand, long packedPosRot, Player grabbedByPlayer, PhotonMessageInfo info)
	{
		if (IsValidClientRPC(info.Sender, entityNetId) && !m_RpcSpamChecks.IsSpamming(RPC.GrabEntity))
		{
			BitPackUtils.UnpackHandPosRotFromNetwork(packedPosRot, out var localPos, out var handRot);
			if (localPos.IsValid(10000f) && handRot.IsValid() && !(localPos.sqrMagnitude > 6400f))
			{
				GrabEntityLocal(GetEntityIdFromNetId(entityNetId), isLeftHand, localPos, handRot, NetPlayer.Get(grabbedByPlayer));
			}
		}
	}

	private void GrabEntityLocal(GameEntityId gameEntityId, bool isLeftHand, Vector3 localPosition, Quaternion localRotation, NetPlayer grabbedByPlayer)
	{
		if (!VRRigCache.Instance.TryGetVrrig(NetworkSystem.Instance.GetPlayer(grabbedByPlayer.ActorNumber), out var _))
		{
			return;
		}
		GameEntity gameEntity = entities[gameEntityId.index];
		if (gameEntityId.index < 0 || gameEntityId.index >= entities.Count || gameEntity == null || grabbedByPlayer == null)
		{
			return;
		}
		int handIndex = GamePlayer.GetHandIndex(isLeftHand);
		if (grabbedByPlayer.IsLocal && gameEntity.heldByActorNumber == grabbedByPlayer.ActorNumber && gameEntity.heldByHandIndex == handIndex)
		{
			return;
		}
		TryDetachCompletely(gameEntity);
		if (!GamePlayer.TryGetGamePlayer(grabbedByPlayer.ActorNumber, out var out_gamePlayer))
		{
			return;
		}
		if (GamePlayer.TryGetGamePlayer(gameEntity.heldByActorNumber, out var out_gamePlayer2))
		{
			int num = out_gamePlayer2.FindHandIndex(gameEntityId);
			bool flag = gameEntity.heldByActorNumber == PhotonNetwork.LocalPlayer.ActorNumber;
			out_gamePlayer2.ClearGrabbedIfHeld(gameEntityId, this);
			if (num != -1 && flag)
			{
				GamePlayerLocal.instance.ClearGrabbed(num);
			}
		}
		Transform handTransform = out_gamePlayer.GetHandTransform(handIndex);
		Rigidbody component = gameEntity.GetComponent<Rigidbody>();
		if (component != null)
		{
			if (grabbedByPlayer.IsLocal)
			{
				component.constraints = RigidbodyConstraints.FreezeAll;
				component.isKinematic = false;
			}
			else
			{
				component.constraints = RigidbodyConstraints.None;
				component.isKinematic = true;
			}
		}
		gameEntity.transform.SetParent(handTransform);
		gameEntity.transform.SetLocalPositionAndRotation(localPosition, localRotation);
		gameEntity.transform.localScale = Vector3.one;
		gameEntity.heldByActorNumber = grabbedByPlayer.ActorNumber;
		gameEntity.heldByHandIndex = handIndex;
		gameEntity.lastHeldByActorNumber = gameEntity.heldByActorNumber;
		out_gamePlayer.SetGrabbed(gameEntityId, handIndex, this);
		if (grabbedByPlayer.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
		{
			GamePlayerLocal.instance.SetGrabbed(gameEntityId, GamePlayer.GetHandIndex(isLeftHand));
			GamePlayerLocal.instance.PlayCatchFx(isLeftHand);
		}
		TryUnsnapLocal(gameEntity);
		gameEntity.PlayCatchFx();
		gameEntity.OnGrabbed?.Invoke();
		CustomGameMode.OnEntityGrabbed(gameEntity, isGrabbed: true);
	}

	public void GrabEntityOnCreate(GameEntityId gameEntityId, bool isLeftHand, Vector3 localPosition, Quaternion localRotation, NetPlayer grabbedByPlayer)
	{
		if (grabbedByPlayer.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
		{
			GamePlayerLocal.instance.gamePlayer.DeleteGrabbedEntityLocal(GamePlayer.GetHandIndex(isLeftHand));
		}
		GrabEntityLocal(gameEntityId, isLeftHand, localPosition, localRotation, grabbedByPlayer);
	}

	public GameEntityId TryGrabLocal(Vector3 handPosition, Vector3 fingerPosition, bool isLeftHand, out Vector3 closestPointOnBoundingBox, out bool fingerPositionUsed)
	{
		float a = 0.03f;
		float num = 0f;
		float num2 = 0.1f;
		float max = 0.25f;
		fingerPositionUsed = false;
		int actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
		Vector3 rigidbodyVelocity = GTPlayer.Instance.RigidbodyVelocity;
		GameEntity bestEntity = null;
		float bestDist = float.MaxValue;
		Vector3 closestPoint = handPosition;
		for (int i = 0; i < entities.Count; i++)
		{
			GameEntity gameEntity = entities[i];
			if (!ValidateGrab(gameEntity, actorNumber, isLeftHand))
			{
				continue;
			}
			float num3 = 0.75f;
			float magnitude = (handPosition - gameEntity.transform.position).magnitude;
			if (magnitude > num3 || (gameEntity.snappedByActorNumber != -1 && gameEntity.snappedByActorNumber == actorNumber && magnitude > 0.1f))
			{
				continue;
			}
			Vector3 vector = gameEntity.GetVelocity() - rigidbodyVelocity;
			float magnitude2 = vector.magnitude;
			float num4 = Mathf.Clamp(magnitude2 * num2, 0f, max);
			Vector3 slopProjection = ((magnitude2 > 0.2f) ? (vector.normalized * num4) : Vector3.zero);
			num = Mathf.Max(a, gameEntity.pickupRangeFromSurface);
			GameEntity.RendererSet grabbableRenderers = gameEntity.GetGrabbableRenderers();
			foreach (var (meshFilter, meshRenderer) in grabbableRenderers.renderers)
			{
				if (meshRenderer.gameObject.activeInHierarchy && meshRenderer.enabled)
				{
					_TryGrabLocal_TestBounds(handPosition, meshRenderer.transform, slopProjection, meshFilter.sharedMesh.bounds, num4, num, gameEntity, isTestingAltPosition: false, ref bestDist, ref bestEntity, ref closestPoint, ref fingerPositionUsed);
					if (IsThinAlongDirection(meshRenderer.transform, meshFilter.sharedMesh.bounds, fingerPosition - handPosition))
					{
						_TryGrabLocal_TestBounds(fingerPosition, meshRenderer.transform, slopProjection, meshFilter.sharedMesh.bounds, num4, num, gameEntity, isTestingAltPosition: true, ref bestDist, ref bestEntity, ref closestPoint, ref fingerPositionUsed);
					}
				}
			}
			foreach (SkinnedMeshRenderer skinnedRenderer in grabbableRenderers.skinnedRenderers)
			{
				if (skinnedRenderer.gameObject.activeInHierarchy && skinnedRenderer.enabled)
				{
					_TryGrabLocal_TestBounds(handPosition, skinnedRenderer.rootBone, slopProjection, skinnedRenderer.localBounds, num4, num, gameEntity, isTestingAltPosition: false, ref bestDist, ref bestEntity, ref closestPoint, ref fingerPositionUsed);
					if (IsThinAlongDirection(skinnedRenderer.rootBone, skinnedRenderer.bounds, fingerPosition - handPosition))
					{
						_TryGrabLocal_TestBounds(fingerPosition, skinnedRenderer.rootBone, slopProjection, skinnedRenderer.localBounds, num4, num, gameEntity, isTestingAltPosition: true, ref bestDist, ref bestEntity, ref closestPoint, ref fingerPositionUsed);
					}
				}
			}
			if (grabbableRenderers.renderers.Count == 0 && grabbableRenderers.skinnedRenderers.Count == 0)
			{
				float num5 = magnitude;
				if (num5 < bestDist)
				{
					bestDist = num5;
					bestEntity = gameEntity;
					closestPoint = gameEntity.transform.position;
				}
			}
		}
		closestPointOnBoundingBox = closestPoint;
		if (bestEntity != null)
		{
			if (!(bestDist <= Mathf.Max(a, bestEntity.pickupRangeFromSurface)))
			{
				return GameEntityId.Invalid;
			}
			return bestEntity.id;
		}
		return GameEntityId.Invalid;
	}

	private static bool IsThinAlongDirection(Transform transform, Bounds bounds, Vector3 direction, float thinThreshold = 0.1f)
	{
		return GetBoundsThicknessAlongDirection(bounds, transform.InverseTransformDirection(direction)) <= thinThreshold;
	}

	private static float GetBoundsThicknessAlongDirection(Bounds bounds, Vector3 localDirection)
	{
		localDirection.Normalize();
		return Vector3.Dot(rhs: new Vector3(Mathf.Abs(localDirection.x), Mathf.Abs(localDirection.y), Mathf.Abs(localDirection.z)), lhs: bounds.extents);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void _TryGrabLocal_TestBounds(Vector3 handPosition, Transform t, Vector3 slopProjection, Bounds bounds, float slopForSpeed, float maxAdjustedGrabDistance, GameEntity entity, bool isTestingAltPosition, ref float bestDist, ref GameEntity bestEntity, ref Vector3 closestPoint, ref bool usedAltPosition)
	{
		Vector3 vector = t.InverseTransformPoint(handPosition);
		Vector3 b = t.InverseTransformPoint(handPosition + slopProjection);
		_ = bounds.extents != bounds.extents;
		float num;
		Vector3 vector2;
		if (SegmentHitsBounds(bounds, vector, b, out var hitPoint, out var distance))
		{
			vector2 = ((distance <= 0f) ? Vector3.zero : t.TransformVector(vector - hitPoint));
			num = vector2.magnitude - slopForSpeed;
		}
		else
		{
			vector2 = t.TransformVector(vector - bounds.ClosestPoint(vector));
			num = vector2.magnitude;
		}
		num = Mathf.Max(0f, num - maxAdjustedGrabDistance);
		vector2 = vector2.normalized * num;
		if (num < bestDist)
		{
			bestDist = num;
			bestEntity = entity;
			closestPoint = handPosition - vector2;
			usedAltPosition = isTestingAltPosition;
		}
	}

	private void DrawDebugStar(Vector3 position, float radius)
	{
		for (int i = 0; i < 20; i++)
		{
			Debug.DrawLine(position, position + UnityEngine.Random.onUnitSphere * radius, Color.red, 10f);
		}
	}

	private static bool SegmentHitsBounds(Bounds bounds, Vector3 a, Vector3 b, out Vector3 hitPoint, out float distance)
	{
		hitPoint = default(Vector3);
		distance = float.MaxValue;
		Vector3 vector = b - a;
		float magnitude = vector.magnitude;
		if (magnitude <= Mathf.Epsilon)
		{
			if (bounds.Contains(a))
			{
				distance = 0f;
				hitPoint = a;
				return true;
			}
			return false;
		}
		Ray ray = new Ray(a, vector / magnitude);
		if (bounds.IntersectRay(ray, out distance) && distance <= magnitude)
		{
			hitPoint = a + ray.direction * distance;
			return true;
		}
		return false;
	}

	public bool GetEntitiesWithComponentInRadius<T>(Vector3 center, float radius, bool checkRootOnly, List<T> nearbyEntities)
	{
		float num = radius * radius;
		for (int i = 0; i < entities.Count; i++)
		{
			GameEntity gameEntity = entities[i];
			if (!(gameEntity == null))
			{
				T val = ((!checkRootOnly) ? gameEntity.GetComponentInChildren<T>() : gameEntity.GetComponent<T>());
				if (val != null && (entities[i].transform.position - center).sqrMagnitude < num)
				{
					nearbyEntities.Add(val);
				}
			}
		}
		return nearbyEntities.Count > 0;
	}

	public void LogGrabDiagnostics(Vector3 handPosition, bool isLeftHand, int handIndex)
	{
		int actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
		int num = 0;
		for (int i = 0; i < entities.Count; i++)
		{
			GameEntity gameEntity = entities[i];
			if (!(gameEntity == null) && !((handPosition - gameEntity.transform.position).magnitude > 0.75f))
			{
				num++;
				WhyGrabRejected(gameEntity, actorNumber, isLeftHand);
			}
		}
	}

	private string WhyGrabRejected(GameEntity gameEntity, int playerActorNumber, bool isLeftHand)
	{
		if (gameEntity == null)
		{
			return "null";
		}
		if (!gameEntity.pickupable)
		{
			return "not pickupable";
		}
		if (gameEntity.onlyGrabActorNumber != -1 && gameEntity.onlyGrabActorNumber != playerActorNumber)
		{
			return $"onlyGrabActor={gameEntity.onlyGrabActorNumber} (you={playerActorNumber})";
		}
		if (gameEntity.heldByActorNumber != -1 && gameEntity.heldByActorNumber != playerActorNumber && GamePlayer.TryGetGamePlayer(gameEntity.heldByActorNumber, out var out_gamePlayer))
		{
			return $"heldByActor={gameEntity.heldByActorNumber}";
		}
		if (gameEntity.snappedByActorNumber != -1 && gameEntity.snappedByActorNumber != playerActorNumber && GamePlayer.TryGetGamePlayer(gameEntity.snappedByActorNumber, out out_gamePlayer))
		{
			return $"snappedByActor={gameEntity.snappedByActorNumber}";
		}
		GameSnappable component = gameEntity.GetComponent<GameSnappable>();
		if (component != null && !component.CanGrabWithHand(isLeftHand))
		{
			return "GameSnappable disallows " + (isLeftHand ? "left" : "right") + " hand";
		}
		if (IsValidEntity(gameEntity.attachedToEntityId))
		{
			GameEntity gameEntity2 = GetGameEntity(gameEntity.attachedToEntityId);
			if (gameEntity2 != null)
			{
				if (gameEntity2.snappedByActorNumber != -1 && gameEntity2.snappedByActorNumber != playerActorNumber && GamePlayer.TryGetGamePlayer(gameEntity2.snappedByActorNumber, out out_gamePlayer))
				{
					return $"attachedTo '{gameEntity2.name}' snappedByActor={gameEntity2.snappedByActorNumber}";
				}
				GameSnappable component2 = gameEntity2.GetComponent<GameSnappable>();
				if (component2 != null && !component2.CanGrabWithHand(isLeftHand))
				{
					return "attachedTo '" + gameEntity2.name + "' GameSnappable disallows " + (isLeftHand ? "left" : "right") + " hand";
				}
			}
		}
		return null;
	}

	private bool ValidateGrab(GameEntity gameEntity, int playerActorNumber, bool isLeftHand)
	{
		if (gameEntity == null || !gameEntity.pickupable)
		{
			return false;
		}
		if (gameEntity.onlyGrabActorNumber != -1 && gameEntity.onlyGrabActorNumber != playerActorNumber)
		{
			return false;
		}
		if (gameEntity.heldByActorNumber != -1 && gameEntity.heldByActorNumber != playerActorNumber && GamePlayer.TryGetGamePlayer(gameEntity.heldByActorNumber, out var out_gamePlayer))
		{
			return false;
		}
		if (gameEntity.snappedByActorNumber != -1 && gameEntity.snappedByActorNumber != playerActorNumber && GamePlayer.TryGetGamePlayer(gameEntity.snappedByActorNumber, out out_gamePlayer))
		{
			return false;
		}
		GameSnappable component = gameEntity.GetComponent<GameSnappable>();
		if (component != null && !component.CanGrabWithHand(isLeftHand))
		{
			return false;
		}
		if (IsValidEntity(gameEntity.attachedToEntityId))
		{
			GameEntity gameEntity2 = GetGameEntity(gameEntity.attachedToEntityId);
			if (gameEntity2 != null)
			{
				if (gameEntity2.snappedByActorNumber != -1 && gameEntity2.snappedByActorNumber != playerActorNumber && GamePlayer.TryGetGamePlayer(gameEntity2.snappedByActorNumber, out out_gamePlayer))
				{
					return false;
				}
				GameSnappable component2 = gameEntity2.GetComponent<GameSnappable>();
				if (component2 != null && !component2.CanGrabWithHand(isLeftHand))
				{
					return false;
				}
			}
		}
		return true;
	}

	public T GetParentEntity<T>(Transform transform) where T : MonoBehaviour
	{
		while (transform != null)
		{
			T component = transform.GetComponent<T>();
			if (component != null)
			{
				return component;
			}
			transform = transform.parent;
		}
		return null;
	}

	public void RequestThrowEntity(GameEntityId entityId, bool isLeftHand, Vector3 headPosition, Vector3 velocity, Vector3 angVelocity)
	{
		GameEntity gameEntity = GetGameEntity(entityId);
		if (gameEntity == null)
		{
			return;
		}
		Vector3 position = gameEntity.transform.position;
		Quaternion rotation = gameEntity.transform.rotation;
		Rigidbody component = gameEntity.GetComponent<Rigidbody>();
		if (component != null)
		{
			Vector3 vector = gameEntity.transform.TransformPoint(component.centerOfMass);
			Vector3 vector2 = vector - headPosition;
			float magnitude = vector2.magnitude;
			if (magnitude > 0f)
			{
				vector2 /= magnitude;
				if (Physics.SphereCast(headPosition, 0.05f, vector2, out var hitInfo, magnitude + 0.1f, 513, QueryTriggerInteraction.Ignore))
				{
					component.GetComponentsInChildren(_collidersList);
					Vector3 position2 = component.position + -hitInfo.normal * 1000f;
					float num = float.MaxValue;
					bool flag = false;
					Plane plane = new Plane(hitInfo.normal, hitInfo.point);
					foreach (Collider colliders in _collidersList)
					{
						if (colliders.enabled && !colliders.isTrigger)
						{
							Vector3 point = colliders.ClosestPoint(position2);
							float num2 = Mathf.Abs(plane.GetDistanceToPoint(point));
							if (num2 < num)
							{
								num = num2;
								flag = true;
							}
						}
					}
					if (flag)
					{
						position += hitInfo.normal * num;
					}
					else
					{
						float num3 = Mathf.Max(hitInfo.distance - 0.2f, 0f);
						Vector3 vector3 = headPosition + vector2 * num3;
						position += vector3 - vector;
					}
				}
			}
		}
		bool inRoom = PhotonNetwork.InRoom;
		if (!IsAuthority() || !inRoom)
		{
			ThrowEntityLocal(entityId, isLeftHand, position, rotation, velocity, angVelocity, NetPlayer.Get(PhotonNetwork.LocalPlayer));
		}
		if (inRoom)
		{
			photonView.RPC("RequestThrowEntityRPC", GetAuthorityPlayer(), GetNetIdFromEntityId(entityId), isLeftHand, position, rotation, velocity, angVelocity);
		}
	}

	[PunRPC]
	public void RequestThrowEntityRPC(int entityNetId, bool isLeftHand, Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angVelocity, PhotonMessageInfo info)
	{
		if (IsValidAuthorityRPC(info.Sender, entityNetId) && position.IsValid(10000f) && rotation.IsValid() && velocity.IsValid(10000f) && angVelocity.IsValid(10000f) && !(velocity.sqrMagnitude > 1600f) && IsPositionInManagerBounds(position) && GamePlayer.TryGetGamePlayer(info.Sender, out var gamePlayer) && IsPlayerHandNearPosition(gamePlayer, position, isLeftHand, checkBothHands: false) && gamePlayer.IsHoldingEntity(GetEntityIdFromNetId(entityNetId), isLeftHand) && gamePlayer.netThrowLimiter.CheckCallTime(Time.time))
		{
			photonView.RPC("ThrowEntityRPC", RpcTarget.All, entityNetId, isLeftHand, position, rotation, velocity, angVelocity, info.Sender, info.SentServerTime);
			PhotonNetwork.SendAllOutgoingCommands();
		}
	}

	[PunRPC]
	public void ThrowEntityRPC(int entityNetId, bool isLeftHand, Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angVelocity, Player thrownByPlayer, double throwTime, PhotonMessageInfo info)
	{
		if (IsValidClientRPC(info.Sender, entityNetId, position) && !m_RpcSpamChecks.IsSpamming(RPC.ThrowEntity) && position.IsValid(10000f) && rotation.IsValid() && velocity.IsValid(10000f) && angVelocity.IsValid(10000f) && !(velocity.sqrMagnitude > 1600f))
		{
			NetPlayer netPlayer = NetPlayer.Get(thrownByPlayer);
			if (!netPlayer.IsLocal || IsAuthority())
			{
				ThrowEntityLocal(GetEntityIdFromNetId(entityNetId), isLeftHand, position, rotation, velocity, angVelocity, netPlayer);
			}
		}
	}

	private void ThrowEntityLocal(GameEntityId entityId, bool isLeftHand, Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angVelocity, NetPlayer thrownByPlayer)
	{
		if (entityId.index < 0 || entityId.index >= entities.Count)
		{
			return;
		}
		GameEntity gameEntity = entities[entityId.index];
		if (gameEntity == null || thrownByPlayer == null)
		{
			return;
		}
		gameEntity.transform.SetParent(null);
		if (gameEntity.IsScenePlaced)
		{
			MoveScenePlacedToHomeScene(gameEntity);
		}
		gameEntity.transform.SetLocalPositionAndRotation(position, rotation);
		Rigidbody component = gameEntity.GetComponent<Rigidbody>();
		if (component != null)
		{
			component.isKinematic = false;
			component.constraints = RigidbodyConstraints.None;
			component.position = position;
			component.rotation = rotation;
			component.linearVelocity = velocity;
			component.angularVelocity = angVelocity;
		}
		gameEntity.heldByActorNumber = -1;
		gameEntity.heldByHandIndex = -1;
		gameEntity.attachedToEntityId = GameEntityId.Invalid;
		VRRig vRRig = GorillaGameManager.StaticFindRigForPlayer(thrownByPlayer);
		if (vRRig != null && gameEntity.gravityController != null)
		{
			gameEntity.gravityController.SetPersonalGravityDirection(vRRig.transform.up);
		}
		bool num = thrownByPlayer.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber;
		int handIndex = GamePlayer.GetHandIndex(isLeftHand);
		RigContainer playerRig;
		if (num)
		{
			GamePlayerLocal.instance.gamePlayer.ClearGrabbed(handIndex);
			GamePlayerLocal.instance.ClearGrabbed(handIndex);
			GamePlayerLocal.instance.PlayThrowFx(isLeftHand);
		}
		else if (VRRigCache.Instance.TryGetVrrig(NetworkSystem.Instance.GetPlayer(thrownByPlayer.ActorNumber), out playerRig))
		{
			GamePlayer gamePlayerRef = playerRig.Rig.GamePlayerRef;
			if (gamePlayerRef != null)
			{
				gamePlayerRef.ClearGrabbedIfHeld(entityId, this);
				gamePlayerRef.ClearSnappedIfSnapped(entityId, this);
			}
		}
		gameEntity.PlayThrowFx();
		gameEntity.OnReleased?.Invoke();
		CustomGameMode.OnEntityGrabbed(gameEntity, isGrabbed: false);
		GRBadge component2 = gameEntity.GetComponent<GRBadge>();
		if (component2 != null)
		{
			GRPlayer gRPlayer = GRPlayer.Get(thrownByPlayer.ActorNumber);
			if (gRPlayer != null)
			{
				gRPlayer.AttachBadge(component2);
			}
		}
	}

	public void RequestSnapEntity(GameEntityId entityId, bool isLeftHand, SnapJointType jointType)
	{
		GameEntity gameEntity = GetGameEntity(entityId);
		if (!(gameEntity == null))
		{
			Vector3 position = gameEntity.transform.position;
			Quaternion rotation = gameEntity.transform.rotation;
			if (!IsAuthority())
			{
				SnapEntityLocal(entityId, isLeftHand, position, rotation, (int)jointType, NetPlayer.Get(PhotonNetwork.LocalPlayer));
			}
			photonView.RPC("RequestSnapEntityRPC", GetAuthorityPlayer(), GetNetIdFromEntityId(entityId), isLeftHand, position, rotation, (int)jointType);
		}
	}

	[PunRPC]
	public void RequestSnapEntityRPC(int entityNetId, bool isLeftHand, Vector3 position, Quaternion rotation, int jointType, PhotonMessageInfo info)
	{
		if (IsValidAuthorityRPC(info.Sender, entityNetId) && position.IsValid(10000f) && rotation.IsValid() && IsPositionInManagerBounds(position))
		{
			GamePlayer gamePlayer = GamePlayer.GetGamePlayer(info.Sender);
			if (!(gamePlayer == null) && IsPlayerHandNearPosition(gamePlayer, position, isLeftHand, checkBothHands: false) && gamePlayer.IsHoldingEntity(GetEntityIdFromNetId(entityNetId), isLeftHand) && gamePlayer.netSnapLimiter.CheckCallTime(Time.time))
			{
				photonView.RPC("SnapEntityRPC", RpcTarget.All, entityNetId, isLeftHand, position, rotation, jointType, info.Sender, info.SentServerTime);
				PhotonNetwork.SendAllOutgoingCommands();
			}
		}
	}

	[PunRPC]
	public void SnapEntityRPC(int entityNetId, bool isLeftHand, Vector3 position, Quaternion rotation, int jointType, Player thrownByPlayer, double snapTime, PhotonMessageInfo info)
	{
		if (IsValidClientRPC(info.Sender, entityNetId, position) && !m_RpcSpamChecks.IsSpamming(RPC.ThrowEntity) && position.IsValid(10000f) && rotation.IsValid() && (IsAuthority() || thrownByPlayer.ActorNumber != PhotonNetwork.LocalPlayer.ActorNumber))
		{
			SnapEntityLocal(GetEntityIdFromNetId(entityNetId), isLeftHand, position, rotation, jointType, NetPlayer.Get(thrownByPlayer));
		}
	}

	private void SnapEntityLocal(GameEntityId gameEntityId, bool isLeftHand, Vector3 position, Quaternion rotation, int jointType, NetPlayer snappedByPlayer)
	{
		if (gameEntityId.index < 0 || gameEntityId.index >= entities.Count)
		{
			return;
		}
		GameEntity gameEntity = entities[gameEntityId.index];
		if (gameEntity == null || snappedByPlayer == null || (snappedByPlayer.IsLocal && gameEntity.heldByActorNumber != snappedByPlayer.ActorNumber && gameEntity.lastHeldByActorNumber == snappedByPlayer.ActorNumber))
		{
			return;
		}
		SuperInfectionSnapPoint superInfectionSnapPoint = null;
		if (!GamePlayer.TryGetGamePlayer(snappedByPlayer.ActorNumber, out var out_gamePlayer))
		{
			return;
		}
		TryDetachCompletely(gameEntity);
		if (jointType == 64)
		{
			gameEntity.GetComponent<GameSnappable>();
			superInfectionSnapPoint = SuperInfectionSnapPointManager.FindSnapPoint(out_gamePlayer, (SnapJointType)jointType);
		}
		else
		{
			superInfectionSnapPoint = SuperInfectionSnapPointManager.FindSnapPoint(out_gamePlayer, (SnapJointType)jointType);
			int num = -1;
			if (jointType == 1)
			{
				num = 2;
			}
			if (jointType == 4)
			{
				num = 3;
			}
			if (num != -1)
			{
				out_gamePlayer.SetSnapped(gameEntityId, num, this);
			}
		}
		if (superInfectionSnapPoint == null)
		{
			return;
		}
		if (superInfectionSnapPoint.HasSnapped())
		{
			GameEntity snappedEntity = superInfectionSnapPoint.GetSnappedEntity();
			snappedEntity.transform.SetParent(null);
			snappedEntity.transform.SetLocalPositionAndRotation(position, rotation);
			Rigidbody component = snappedEntity.GetComponent<Rigidbody>();
			if (component != null)
			{
				component.isKinematic = false;
				component.constraints = RigidbodyConstraints.None;
				component.position = position;
				component.rotation = rotation;
				component.linearVelocity = Vector3.up * 5f;
			}
			snappedEntity.heldByActorNumber = -1;
			snappedEntity.heldByHandIndex = -1;
			snappedEntity.snappedByActorNumber = -1;
			snappedEntity.snappedJoint = SnapJointType.None;
			snappedEntity.PlayThrowFx();
			snappedEntity.OnReleased?.Invoke();
		}
		superInfectionSnapPoint.Snapped(gameEntity);
		gameEntity.transform.SetParent(superInfectionSnapPoint.transform);
		gameEntity.transform.SetLocalPositionAndRotation(position, rotation);
		gameEntity.transform.localScale = Vector3.one;
		Rigidbody component2 = gameEntity.GetComponent<Rigidbody>();
		if (component2 != null)
		{
			component2.isKinematic = true;
		}
		Vector3 positionOffset = Vector3.zero;
		Quaternion rotationOffset = Quaternion.identity;
		GameSnappable component3 = gameEntity.GetComponent<GameSnappable>();
		if (component3 != null)
		{
			component3.GetSnapOffset((SnapJointType)jointType, out positionOffset, out rotationOffset);
		}
		gameEntity.transform.localPosition = positionOffset;
		gameEntity.transform.localRotation = rotationOffset;
		gameEntity.snappedByActorNumber = snappedByPlayer.ActorNumber;
		gameEntity.snappedJoint = (SnapJointType)jointType;
		if (component3 != null)
		{
			component3.OnSnap();
		}
		gameEntity.OnSnapped?.Invoke();
		gameEntity.PlaySnapFx();
	}

	public void SnapEntityOnCreate(GameEntityId gameEntityId, bool isLeftHand, Vector3 localPosition, Quaternion localRotation, int jointType, NetPlayer grabbedByPlayer)
	{
		SnapEntityLocal(gameEntityId, isLeftHand, localPosition, localRotation, jointType, grabbedByPlayer);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void TryUnsnapLocal(GameEntity gameEntity)
	{
		if (GamePlayer.TryGetGamePlayer(gameEntity.snappedByActorNumber, out var out_gamePlayer))
		{
			out_gamePlayer.ClearSnappedIfSnapped(gameEntity.id, gameEntity.manager);
		}
		GameSnappable component = gameEntity.GetComponent<GameSnappable>();
		if (component != null && component.snappedToJoint != null && component.snappedToJoint.jointType != SnapJointType.None)
		{
			SuperInfectionSnapPoint superInfectionSnapPoint = SuperInfectionSnapPointManager.FindSnapPoint(out_gamePlayer, component.snappedToJoint.jointType);
			if (superInfectionSnapPoint == null)
			{
				superInfectionSnapPoint = component.snappedToJoint;
			}
			component.OnUnsnap();
			superInfectionSnapPoint.Unsnapped();
			gameEntity.OnUnsnapped?.Invoke();
		}
		gameEntity.snappedByActorNumber = -1;
		gameEntity.snappedJoint = SnapJointType.None;
	}

	public void RequestAttachEntity(GameEntityId entityId, GameEntityId attachToEntityId, int slotId, Vector3 localPosition, Quaternion localRotation)
	{
		if (!(GetGameEntity(entityId) == null))
		{
			if (!IsAuthority())
			{
				AttachEntityLocal(entityId, attachToEntityId, slotId, localPosition, localRotation);
			}
			photonView.RPC("RequestAttachEntityRPC", GetAuthorityPlayer(), GetNetIdFromEntityId(entityId), GetNetIdFromEntityId(attachToEntityId), slotId, localPosition, localRotation);
		}
	}

	public void RequestAttachEntityAuthority(GameEntityId entityId, GameEntityId attachToEntityId, int slotId, Vector3 localPosition, Quaternion localRotation)
	{
		if (!(GetGameEntity(entityId) == null) && IsAuthority())
		{
			photonView.RPC("AttachEntityRPC", RpcTarget.All, GetNetIdFromEntityId(entityId), GetNetIdFromEntityId(attachToEntityId), slotId, localPosition, localRotation, null, PhotonNetwork.Time);
		}
	}

	[PunRPC]
	public void RequestAttachEntityRPC(int entityNetId, int attachToEntityNetId, int slotId, Vector3 localPosition, Quaternion localRotation, PhotonMessageInfo info)
	{
		bool flag = !IsValidNetId(attachToEntityNetId);
		if (!IsValidAuthorityRPC(info.Sender, entityNetId) || !localPosition.IsValid(10000f) || !localRotation.IsValid())
		{
			return;
		}
		if (!flag)
		{
			if (localPosition.sqrMagnitude > 4f || !IsEntityNearEntity(entityNetId, attachToEntityNetId))
			{
				return;
			}
		}
		else if (!IsPositionInManagerBounds(localPosition))
		{
			return;
		}
		GameEntity gameEntityFromNetId = GetGameEntityFromNetId(entityNetId);
		if (gameEntityFromNetId == null)
		{
			return;
		}
		GameDockable component = gameEntityFromNetId.GetComponent<GameDockable>();
		if (component == null)
		{
			return;
		}
		GameEntity gameEntityFromNetId2 = GetGameEntityFromNetId(attachToEntityNetId);
		if (gameEntityFromNetId2 != null)
		{
			GameDock component2 = gameEntityFromNetId2.GetComponent<GameDock>();
			if (component2 == null || !component2.CanDock(component))
			{
				return;
			}
		}
		GamePlayer gamePlayer = GamePlayer.GetGamePlayer(info.Sender);
		if (!(gamePlayer == null) && gamePlayer.IsHoldingEntity(GetEntityIdFromNetId(entityNetId)) && gamePlayer.netSnapLimiter.CheckCallTime(Time.time))
		{
			photonView.RPC("AttachEntityRPC", RpcTarget.All, entityNetId, attachToEntityNetId, slotId, localPosition, localRotation, info.Sender, info.SentServerTime);
			PhotonNetwork.SendAllOutgoingCommands();
		}
	}

	[PunRPC]
	public void AttachEntityRPC(int entityNetId, int attachToEntityNetId, int slotId, Vector3 localPosition, Quaternion localRotation, Player attachedByPlayer, double snapTime, PhotonMessageInfo info)
	{
		if (IsValidClientRPC(info.Sender, entityNetId) && IsValidNetId(attachToEntityNetId) && !m_RpcSpamChecks.IsSpamming(RPC.ThrowEntity) && localPosition.IsValid(10000f) && localRotation.IsValid() && (IsAuthority() || attachedByPlayer == null || attachedByPlayer.ActorNumber != PhotonNetwork.LocalPlayer.ActorNumber))
		{
			AttachEntityLocal(GetEntityIdFromNetId(entityNetId), GetEntityIdFromNetId(attachToEntityNetId), slotId, localPosition, localRotation);
		}
	}

	private void AttachEntityLocal(GameEntityId gameEntityId, GameEntityId attachToEntityId, int slotId, Vector3 localPosition, Quaternion localRotation)
	{
		if (gameEntityId.index < 0 || gameEntityId.index >= entities.Count)
		{
			return;
		}
		GameEntity gameEntity = entities[gameEntityId.index];
		if (gameEntity == null)
		{
			return;
		}
		GameEntity gameEntity2 = entities[attachToEntityId.index];
		TryDetachCompletely(gameEntity);
		bool flag = gameEntity2 == null;
		Transform parent = ((gameEntity2 == null) ? null : gameEntity2.transform);
		gameEntity.transform.SetParent(parent);
		gameEntity.transform.SetLocalPositionAndRotation(localPosition, localRotation);
		gameEntity.attachedToEntityId = (flag ? GameEntityId.Invalid : gameEntity2.id);
		Rigidbody component = gameEntity.GetComponent<Rigidbody>();
		if (component != null)
		{
			component.isKinematic = !flag;
			component.constraints = RigidbodyConstraints.None;
		}
		GameDockable component2 = gameEntity.GetComponent<GameDockable>();
		if (!(gameEntity2 != null))
		{
			return;
		}
		gameEntity.OnAttached?.Invoke();
		GameDock component3 = gameEntity2.GetComponent<GameDock>();
		if (component3 != null)
		{
			component3.OnDock(gameEntity, gameEntity2);
			if (component2 != null)
			{
				component2.OnDock(gameEntity, gameEntity2);
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void TryDetachLocal(GameEntity gameEntity)
	{
		if (gameEntity.attachedToEntityId != GameEntityId.Invalid)
		{
			GameEntity gameEntity2 = gameEntity.manager.entities[gameEntity.attachedToEntityId.index];
			if (gameEntity2 != null)
			{
				GameDock component = gameEntity2.GetComponent<GameDock>();
				if (component != null)
				{
					component.OnUndock(gameEntity, gameEntity2);
					GameDockable component2 = gameEntity.GetComponent<GameDockable>();
					if (component2 != null)
					{
						component2.OnUndock(gameEntity, gameEntity2);
					}
				}
			}
		}
		if (gameEntity.attachedToEntityId != GameEntityId.Invalid)
		{
			gameEntity.OnDetached?.Invoke();
		}
		gameEntity.attachedToEntityId = GameEntityId.Invalid;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void TryDetachCompletely(GameEntity gameEntity)
	{
		if (!(gameEntity == null))
		{
			TryRemoveFromHandLocal(gameEntity);
			TryUnsnapLocal(gameEntity);
			TryDetachLocal(gameEntity);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void TryRemoveFromHandLocal(GameEntity gameEntity)
	{
		GameEntityId id = gameEntity.id;
		int heldByActorNumber = gameEntity.heldByActorNumber;
		if (GamePlayer.TryGetGamePlayer(heldByActorNumber, out var out_gamePlayer))
		{
			if (heldByActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
			{
				GamePlayerLocal.instance.ClearGrabbedIfHeld(id, gameEntity.manager);
			}
			out_gamePlayer.ClearGrabbedIfHeld(id, gameEntity.manager);
			gameEntity.OnReleased?.Invoke();
		}
		gameEntity.heldByActorNumber = -1;
		gameEntity.heldByHandIndex = -1;
	}

	public void AttachEntityOnCreate(GameEntityId gameEntityId, bool isLeftHand, Vector3 localPosition, Quaternion localRotation, int jointType, NetPlayer grabbedByPlayer)
	{
		SnapEntityLocal(gameEntityId, isLeftHand, localPosition, localRotation, jointType, grabbedByPlayer);
	}

	public void RequestHit(GameHitData hit)
	{
		GameHittable gameComponent = GetGameComponent<GameHittable>(hit.hitEntityId);
		if (!(gameComponent == null))
		{
			gameComponent.ApplyHit(hit);
			photonView.RPC("RequestHitRPC", GetAuthorityPlayer(), GetNetIdFromEntityId(hit.hitEntityId), GetNetIdFromEntityId(hit.hitByEntityId), hit.hitTypeId, hit.hitEntityPosition, hit.hitPosition, hit.hitImpulse, hit.hittablePoint);
		}
	}

	[PunRPC]
	public void RequestHitRPC(int hittableNetId, int hitByNetId, int hitTypeId, Vector3 entityPosition, Vector3 hitPosition, Vector3 hitImpulse, int hittablePoint, PhotonMessageInfo info)
	{
		if (!entityPosition.IsValid(10000f) || !hitPosition.IsValid(10000f) || !hitImpulse.IsValid(10000f) || !IsValidAuthorityRPC(info.Sender, hittableNetId, entityPosition) || !IsPositionInManagerBounds(hitPosition) || !GamePlayer.TryGetGamePlayer(info.Sender, out var gamePlayer) || !gamePlayer.netImpulseLimiter.CheckCallTime(Time.time))
		{
			return;
		}
		GameEntityId entityIdFromNetId = GetEntityIdFromNetId(hittableNetId);
		GameHittable gameComponent = GetGameComponent<GameHittable>(entityIdFromNetId);
		if (!(gameComponent == null))
		{
			GameHitData hitData = new GameHitData
			{
				hitTypeId = hitTypeId,
				hitEntityId = entityIdFromNetId,
				hitByEntityId = GetEntityIdFromNetId(hitByNetId),
				hitEntityPosition = entityPosition,
				hitPosition = hitPosition,
				hitImpulse = hitImpulse,
				hittablePoint = hittablePoint
			};
			if (gameComponent.IsHitValid(hitData))
			{
				SendRPC("ApplyHitRPC", RpcTarget.All, hittableNetId, hitByNetId, hitTypeId, entityPosition, hitPosition, hitImpulse, hittablePoint, info.Sender);
			}
		}
	}

	[PunRPC]
	public void ApplyHitRPC(int hittableNetId, int hitByNetId, int hitTypeId, Vector3 entityPosition, Vector3 hitPosition, Vector3 hitImpulse, int hittablePoint, Player player, PhotonMessageInfo info)
	{
		if (!hitPosition.IsValid(10000f) || !hitImpulse.IsValid(10000f) || !IsValidClientRPC(info.Sender, hittableNetId, entityPosition) || m_RpcSpamChecks.IsSpamming(RPC.HitEntity) || player == null || player.IsLocal || GetGameEntity(GetEntityIdFromNetId(hittableNetId)) == null)
		{
			return;
		}
		hitImpulse = Vector3.ClampMagnitude(hitImpulse, 100f);
		GameEntityId entityIdFromNetId = GetEntityIdFromNetId(hittableNetId);
		GameHitData hitData = new GameHitData
		{
			hitTypeId = hitTypeId,
			hitEntityId = entityIdFromNetId,
			hitByEntityId = GetEntityIdFromNetId(hitByNetId),
			hitEntityPosition = entityPosition,
			hitPosition = hitPosition,
			hitImpulse = hitImpulse,
			hitAmount = 0,
			hittablePoint = hittablePoint
		};
		GameEntity gameEntity = GetGameEntity(GetEntityIdFromNetId(hitByNetId));
		GameHittable gameComponent = GetGameComponent<GameHittable>(entityIdFromNetId);
		if (gameEntity != null)
		{
			GameHitter component = gameEntity.GetComponent<GameHitter>();
			if (component != null)
			{
				hitData.hitAmount = component.CalcHitAmount((GameHitType)hitTypeId, gameComponent, gameEntity);
			}
		}
		if (gameComponent != null)
		{
			gameComponent.ApplyHit(hitData);
		}
	}

	public bool IsPlayerHandNearEntity(GamePlayer player, int entityNetId, bool isLeftHand, bool checkBothHands, float acceptableRadius = 16f)
	{
		GameEntityId entityIdFromNetId = GetEntityIdFromNetId(entityNetId);
		GameEntity gameEntity = GetGameEntity(entityIdFromNetId);
		if (gameEntity == null)
		{
			return false;
		}
		return IsPlayerHandNearPosition(player, gameEntity.transform.position, isLeftHand, checkBothHands, acceptableRadius);
	}

	public static bool IsPlayerHandNearPosition(GamePlayer player, Vector3 worldPosition, bool isLeftHand, bool checkBothHands, float acceptableRadius = 16f)
	{
		bool flag = true;
		if (player != null && player.rig != null)
		{
			if (isLeftHand || checkBothHands)
			{
				flag = (worldPosition - player.rig.leftHandTransform.position).sqrMagnitude < acceptableRadius * acceptableRadius;
			}
			if (!isLeftHand || checkBothHands)
			{
				float sqrMagnitude = (worldPosition - player.rig.rightHandTransform.position).sqrMagnitude;
				flag = flag && sqrMagnitude < acceptableRadius * acceptableRadius;
			}
		}
		return flag;
	}

	public bool IsEntityNearEntity(int entityNetId, int otherEntityNetId, float acceptableRadius = 16f)
	{
		GameEntityId entityIdFromNetId = GetEntityIdFromNetId(otherEntityNetId);
		GameEntity gameEntity = GetGameEntity(entityIdFromNetId);
		if (gameEntity == null)
		{
			return false;
		}
		return IsEntityNearPosition(entityNetId, gameEntity.transform.position, acceptableRadius);
	}

	public bool IsEntityNearPosition(int entityNetId, Vector3 position, float acceptableRadius = 16f)
	{
		GameEntityId entityIdFromNetId = GetEntityIdFromNetId(entityNetId);
		GameEntity gameEntity = GetGameEntity(entityIdFromNetId);
		if (gameEntity == null)
		{
			return false;
		}
		return Vector3.SqrMagnitude(gameEntity.transform.position - position) < acceptableRadius * acceptableRadius;
	}

	public static bool ValidateDataType<T>(object obj, out T dataAsType)
	{
		if (obj is T)
		{
			dataAsType = (T)obj;
			return true;
		}
		dataAsType = default(T);
		return false;
	}

	private void ClearZone(bool ignoreHeldGadgets = false)
	{
		GamePlayerLocal.instance.DebugSlotsReport($"Pre ClearZone zone={zone}");
		ClearPendingRPCBatches();
		if (ignoreHeldGadgets)
		{
			List<GameEntity> list = GamePlayerLocal.instance.gamePlayer.HeldAndSnappedEntities();
			for (int num = list.Count - 1; num >= 0; num--)
			{
				if (list[num] == null || list[num].manager != this)
				{
					list.RemoveAt(num);
				}
				else if (list[num].shouldDestroyOnZoneExit)
				{
					list.RemoveAt(num);
				}
			}
			for (int num2 = entities.Count - 1; num2 >= 0; num2--)
			{
				if (!(entities[num2] == null) && !entities[num2].IsScenePlaced && !list.Contains(entities[num2]))
				{
					DestroyItemLocal(entities[num2].id);
				}
			}
			GamePlayerLocal.instance.joinWithItemsSentForCurrentMigration = false;
			GamePlayerLocal.instance.gamePlayer.DidJoinWithItems = false;
			GamePlayerLocal.instance.DebugSlotsReport($"ClearZone post-preserve zone={zone}");
		}
		else
		{
			for (int i = 0; i < entities.Count; i++)
			{
				if (!(entities[i] == null) && !(entities[i].manager != this) && !entities[i].IsScenePlaced)
				{
					DestroyItemLocal(entities[i].id);
				}
			}
			GamePlayerLocal.instance.DebugSlotsReport($"ClearZone post-destroy zone={zone}");
			GamePlayer gamePlayerRef = VRRig.LocalRig.GamePlayerRef;
			if (gamePlayerRef != null)
			{
				gamePlayerRef.ClearZone(this);
			}
			GamePlayerLocal.instance.DebugSlotsReport($"ClearZone post-ClearZone(player) zone={zone}");
		}
		for (int j = 0; j < entities.Count; j++)
		{
			if (entities[j] != null && entities[j].manager != this)
			{
				int key = netIds[j];
				if (netIdToIndex.TryGetValue(key, out var value) && value == j)
				{
					netIdToIndex.Remove(key);
				}
				entities[j] = null;
			}
		}
		foreach (VRRig activeRig in VRRigCache.ActiveRigs)
		{
			GamePlayer component = activeRig.GetComponent<GamePlayer>();
			if (!component.IsLocal())
			{
				component.ClearZone(this);
			}
		}
		gameEntityData.Clear();
		entitiesActiveCount = 0;
		scenePlacedEntitiesRegistered = false;
		scenePlacedEntities.Clear();
		for (int k = 0; k < zoneComponents.Count; k++)
		{
			zoneComponents[k].OnZoneClear(zoneClearReason);
		}
		GamePlayerLocal.instance.DebugSlotsReport($"ClearZone END zone={zone}");
	}

	public int SerializeGameState(int zoneId, byte[] bytes, int maxBytes)
	{
		MemoryStream memoryStream = new MemoryStream(bytes);
		BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
		for (int i = 0; i < zoneComponents.Count; i++)
		{
			zoneComponents[i].SerializeZoneData(binaryWriter);
		}
		tempEntitiesToSerialize.Clear();
		for (int j = 0; j < entities.Count; j++)
		{
			GameEntity gameEntity = entities[j];
			if (gameEntity == null)
			{
				continue;
			}
			int attachedPlayerActorNr = gameEntity.AttachedPlayerActorNr;
			if (attachedPlayerActorNr != -1)
			{
				bool flag = false;
				for (int k = 0; k < tempRigs.Count; k++)
				{
					if (tempRigs[k].Creator.ActorNumber == attachedPlayerActorNr)
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					continue;
				}
			}
			tempEntitiesToSerialize.Add(gameEntity);
		}
		binaryWriter.Write(tempEntitiesToSerialize.Count);
		for (int l = 0; l < tempEntitiesToSerialize.Count; l++)
		{
			GameEntity gameEntity2 = tempEntitiesToSerialize[l];
			if (!(gameEntity2 == null))
			{
				int netIdFromEntityId = GetNetIdFromEntityId(gameEntity2.id);
				binaryWriter.Write(netIdFromEntityId);
				binaryWriter.Write(gameEntity2.typeId);
				long value = BitPackUtils.PackWorldPosForNetwork(gameEntity2.transform.position);
				int value2 = BitPackUtils.PackQuaternionForNetwork(gameEntity2.transform.rotation);
				binaryWriter.Write(value);
				binaryWriter.Write(value2);
			}
		}
		for (int m = 0; m < tempEntitiesToSerialize.Count; m++)
		{
			GameEntity gameEntity3 = tempEntitiesToSerialize[m];
			if (gameEntity3 == null)
			{
				continue;
			}
			int netIdFromEntityId2 = GetNetIdFromEntityId(gameEntity3.id);
			binaryWriter.Write(netIdFromEntityId2);
			binaryWriter.Write(gameEntity3.createData);
			binaryWriter.Write(GetNetIdFromEntityId(gameEntity3.createdByEntityId));
			binaryWriter.Write(gameEntity3.GetState());
			int num = -1;
			GameEntity gameEntity4 = GetGameEntity(gameEntity3.attachedToEntityId);
			if (gameEntity4 != null)
			{
				num = GetNetIdFromEntityId(gameEntity4.id);
			}
			binaryWriter.Write(num);
			if (num != -1)
			{
				long value3 = BitPackUtils.PackHandPosRotForNetwork(gameEntity3.transform.localPosition, gameEntity3.transform.localRotation);
				binaryWriter.Write(value3);
			}
			GameAgent component = gameEntity3.GetComponent<GameAgent>();
			bool flag2 = component != null;
			binaryWriter.Write(flag2);
			if (flag2)
			{
				Vector3 worldPos = Vector3.zero;
				if (component.navAgent != null)
				{
					worldPos = component.navAgent.destination;
				}
				long value4 = BitPackUtils.PackWorldPosForNetwork(worldPos);
				binaryWriter.Write(value4);
				int value5 = component.targetPlayer?.ActorNumber ?? (-1);
				binaryWriter.Write(value5);
			}
			byte b = (byte)gameEntity3.entitySerialize.Count;
			binaryWriter.Write(b);
			for (int n = 0; n < b; n++)
			{
				gameEntity3.entitySerialize[n].OnGameEntitySerialize(binaryWriter);
			}
			for (int num2 = 0; num2 < zoneComponents.Count; num2++)
			{
				zoneComponents[num2].SerializeZoneEntityData(binaryWriter, gameEntity3);
			}
		}
		int count = tempRigs.Count;
		binaryWriter.Write(count);
		for (int num3 = 0; num3 < tempRigs.Count; num3++)
		{
			VRRig vRRig = tempRigs[num3];
			NetPlayer owningNetPlayer = vRRig.OwningNetPlayer;
			binaryWriter.Write(owningNetPlayer.ActorNumber);
			GamePlayer gamePlayerRef = vRRig.GamePlayerRef;
			bool flag3 = gamePlayerRef != null;
			binaryWriter.Write(flag3);
			if (flag3)
			{
				gamePlayerRef.SerializeNetworkState(binaryWriter, owningNetPlayer, this);
				for (int num4 = 0; num4 < zoneComponents.Count; num4++)
				{
					zoneComponents[num4].SerializeZonePlayerData(binaryWriter, owningNetPlayer.ActorNumber);
				}
			}
		}
		return (int)memoryStream.Position;
	}

	public void DeserializeTableState(byte[] bytes, int numBytes)
	{
		if (numBytes <= 0)
		{
			return;
		}
		tempAttachments.Clear();
		using MemoryStream input = new MemoryStream(bytes);
		using BinaryReader binaryReader = new BinaryReader(input);
		for (int i = 0; i < zoneComponents.Count; i++)
		{
			zoneComponents[i].DeserializeZoneData(binaryReader);
		}
		int num = binaryReader.ReadInt32();
		Span<bool> span = stackalloc bool[num];
		for (int j = 0; j < num; j++)
		{
			int netId = binaryReader.ReadInt32();
			int entityTypeId = binaryReader.ReadInt32();
			long data = binaryReader.ReadInt64();
			int data2 = binaryReader.ReadInt32();
			Vector3 position = BitPackUtils.UnpackWorldPosFromNetwork(data);
			Quaternion rotation = BitPackUtils.UnpackQuaternionFromNetwork(data2);
			GameEntity gameEntityFromNetId = GetGameEntityFromNetId(netId);
			if (gameEntityFromNetId != null)
			{
				span[j] = true;
				if (gameEntityFromNetId.IsScenePlaced)
				{
					gameEntityFromNetId.transform.SetPositionAndRotation(position, rotation);
				}
			}
			else if (IsScenePlacedNetId(netId))
			{
				span[j] = true;
			}
			else
			{
				CreateItemLocal(netId, entityTypeId, position, rotation);
			}
		}
		for (int k = 0; k < num; k++)
		{
			int num2 = binaryReader.ReadInt32();
			long createData = binaryReader.ReadInt64();
			int createdByEntityNetId = binaryReader.ReadInt32();
			long state = binaryReader.ReadInt64();
			GameEntity gameEntityFromNetId2 = GetGameEntityFromNetId(num2);
			if (gameEntityFromNetId2 != null)
			{
				if (!span[k])
				{
					InitItemLocal(gameEntityFromNetId2, createData, createdByEntityNetId);
					gameEntityFromNetId2.SetState(state);
				}
				else if (gameEntityFromNetId2.IsScenePlaced)
				{
					gameEntityFromNetId2.SetState(state);
				}
			}
			int num3 = binaryReader.ReadInt32();
			if (num3 != -1)
			{
				long data3 = binaryReader.ReadInt64();
				if (gameEntityFromNetId2 == null)
				{
					continue;
				}
				BitPackUtils.UnpackHandPosRotFromNetwork(data3, out var localPos, out var handRot);
				tempAttachments.Add(new AttachmentData
				{
					entityNetId = num2,
					attachToEntityNetId = num3,
					localPosition = localPos,
					localRotation = handRot
				});
			}
			if (binaryReader.ReadBoolean())
			{
				long data4 = binaryReader.ReadInt64();
				int playerID = binaryReader.ReadInt32();
				Vector3 destination = BitPackUtils.UnpackWorldPosFromNetwork(data4);
				GameAgent component = gameEntityFromNetId2.GetComponent<GameAgent>();
				if (component != null)
				{
					if (component.IsOnNavMesh())
					{
						component.navAgent.destination = destination;
					}
					component.targetPlayer = NetworkSystem.Instance.GetPlayer(playerID);
				}
			}
			byte b = binaryReader.ReadByte();
			for (int l = 0; l < b; l++)
			{
				gameEntityFromNetId2.entitySerialize[l].OnGameEntityDeserialize(binaryReader);
			}
			for (int m = 0; m < zoneComponents.Count; m++)
			{
				zoneComponents[m].DeserializeZoneEntityData(binaryReader, gameEntityFromNetId2);
			}
		}
		int num4 = binaryReader.ReadInt32();
		for (int n = 0; n < num4; n++)
		{
			int actorNumber = binaryReader.ReadInt32();
			if (binaryReader.ReadBoolean())
			{
				GamePlayer.TryGetGamePlayer(actorNumber, out var out_gamePlayer);
				GamePlayer.DeserializeNetworkState(binaryReader, out_gamePlayer, this);
				for (int num5 = 0; num5 < zoneComponents.Count; num5++)
				{
					zoneComponents[num5].DeserializeZonePlayerData(binaryReader, actorNumber);
				}
			}
		}
		for (int num6 = 0; num6 < tempAttachments.Count; num6++)
		{
			AttachmentData attachmentData = tempAttachments[num6];
			GameEntityId entityIdFromNetId = GetEntityIdFromNetId(attachmentData.entityNetId);
			GameEntityId entityIdFromNetId2 = GetEntityIdFromNetId(attachmentData.attachToEntityNetId);
			if (!(entityIdFromNetId == entityIdFromNetId2))
			{
				AttachEntityLocal(entityIdFromNetId, entityIdFromNetId2, 0, attachmentData.localPosition, attachmentData.localRotation);
			}
		}
	}

	private void UpdateZoneState()
	{
		UpdateAuthority(tempRigs);
		if (IsAuthority())
		{
			UpdateClientsFromAuthority(tempRigs);
			UpdateZoneStateAuthority();
		}
		else
		{
			UpdateZoneStateClient();
		}
		for (int num = zoneStateData.zonePlayers.Count - 1; num >= 0; num--)
		{
			if (zoneStateData.zonePlayers[num] == null)
			{
				zoneStateData.zonePlayers.RemoveAt(num);
			}
		}
	}

	private void UpdateAuthority(List<VRRig> allRigs)
	{
		if (!PhotonNetwork.InRoom && base.IsMine)
		{
			if (!IsAuthority())
			{
				guard.SetOwnership(NetworkSystem.Instance.LocalPlayer);
			}
		}
		else
		{
			if (!IsAuthority() || IsInZone())
			{
				return;
			}
			Player player = null;
			GTZone currentZone = VRRig.LocalRig.zoneEntity.currentZone;
			if (useRandomCheckForAuthority)
			{
				int num = 0;
				while (player == null && num < 10)
				{
					num++;
					int index = UnityEngine.Random.Range(0, allRigs.Count);
					VRRig vRRig = allRigs[index];
					if (GamePlayer.TryGetGamePlayer(vRRig, out var out_gamePlayer) && !(out_gamePlayer.rig == null) && out_gamePlayer.rig.Creator != null && !out_gamePlayer.rig.isLocal)
					{
						GTZone currentZone2 = vRRig.zoneEntity.currentZone;
						if (currentZone2 == zone && currentZone2 != currentZone)
						{
							player = out_gamePlayer.rig.Creator.GetPlayerRef();
						}
					}
				}
			}
			else
			{
				for (int i = 0; i < allRigs.Count; i++)
				{
					VRRig vRRig2 = allRigs[i];
					if (GamePlayer.TryGetGamePlayer(vRRig2, out var out_gamePlayer2) && !(out_gamePlayer2.rig == null) && out_gamePlayer2.rig.Creator != null && !out_gamePlayer2.rig.isLocal)
					{
						GTZone currentZone3 = vRRig2.zoneEntity.currentZone;
						if (currentZone3 == zone && currentZone3 != currentZone)
						{
							player = out_gamePlayer2.rig.Creator.GetPlayerRef();
						}
					}
				}
			}
			if (player != null)
			{
				guard.TransferOwnership(player);
			}
		}
	}

	private void UpdateClientsFromAuthority(List<VRRig> allRigs)
	{
		if (!IsInZone())
		{
			return;
		}
		int num = 0;
		while (num < zoneStateData.zoneStateRequests.Count)
		{
			ZoneStateRequest value = zoneStateData.zoneStateRequests[num];
			if (value.player == null || value.zone != zone)
			{
				zoneStateData.zoneStateRequests.RemoveAt(num);
				num--;
				num++;
				continue;
			}
			SendZoneStateToPlayerOrTarget(value.zone, value.player, RpcTarget.MasterClient);
			value.completed = true;
			zoneStateData.zoneStateRequests[num] = value;
			zoneStateData.zoneStateRequests.RemoveAt(num);
			break;
		}
	}

	public void TestSerializeTableState()
	{
		ClearByteBuffer(tempSerializeGameState);
		int num = SerializeGameState((int)zone, tempSerializeGameState, 15360);
		byte[] array = GZipStream.CompressBuffer(tempSerializeGameState);
		Debug.LogFormat("Test Serialize Game State Buffer Size Uncompressed {0}", num);
		Debug.LogFormat("Test Serialize Game State Buffer Size Compressed {0}", array.Length);
	}

	public static void ClearByteBuffer(byte[] buffer)
	{
		int num = buffer.Length;
		for (int i = 0; i < num; i++)
		{
			buffer[i] = 0;
		}
	}

	private void SendZoneStateToPlayerOrTarget(GTZone zone, Player player, RpcTarget target)
	{
		ClearByteBuffer(tempSerializeGameState);
		SerializeGameState((int)zone, tempSerializeGameState, 15360);
		byte[] array = GZipStream.CompressBuffer(tempSerializeGameState);
		byte[] array2 = new byte[512];
		int num = 0;
		int num2 = 0;
		int num3 = array.Length;
		while (num < num3)
		{
			int num4 = Mathf.Min(512, num3 - num);
			Array.Copy(array, num, array2, 0, num4);
			if (player != null)
			{
				photonView.RPC("SendTableDataRPC", player, num2, num3, array2);
			}
			else
			{
				photonView.RPC("SendTableDataRPC", target, num2, num3, array2);
			}
			num += num4;
			num2++;
		}
	}

	[PunRPC]
	public void SendTableDataRPC(int packetNum, int totalBytes, byte[] bytes, PhotonMessageInfo info)
	{
		if (!IsAuthorityPlayer(info.Sender) || m_RpcSpamChecks.IsSpamming(RPC.SendTableData) || bytes == null || bytes.Length >= 15360 || zoneStateData.state != ZoneState.WaitingForState)
		{
			return;
		}
		if (packetNum == 0)
		{
			zoneStateData.numRecievedStateBytes = 0;
			for (int i = 0; i < zoneStateData.recievedStateBytes.Length; i++)
			{
				zoneStateData.recievedStateBytes[i] = 0;
			}
		}
		Array.Copy(bytes, 0, zoneStateData.recievedStateBytes, zoneStateData.numRecievedStateBytes, bytes.Length);
		zoneStateData.numRecievedStateBytes += bytes.Length;
		if (zoneStateData.numRecievedStateBytes >= totalBytes)
		{
			if (superInfectionManager != null && superInfectionManager.zoneSuperInfection == null && !scenePlacedEntitiesRegistered)
			{
				PendingTableData = true;
				pendingTableDataSetFrame = Time.frameCount;
			}
			else
			{
				ResolveTableData();
			}
		}
	}

	public void ResolveTableData()
	{
		PendingTableData = false;
		if (activeManager.IsNotNull() && activeManager != this)
		{
			activeManager.zoneClearReason = ZoneClearReason.MigrateGameEntityZone;
			activeManager.ClearZone(ignoreHeldGadgets: true);
		}
		ClearZone(ignoreHeldGadgets: true);
		RegisterScenePlacedEntities();
		try
		{
			byte[] array = GZipStream.UncompressBuffer(zoneStateData.recievedStateBytes);
			int numBytes = array.Length;
			DeserializeTableState(array, numBytes);
			RecalculateNextNetId();
			SetZoneState(ZoneState.Active);
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
			Debug.LogError("[GT/GameEntityManager]  ERROR!!!  ResolveTableData: See exception in previous message.");
		}
	}

	private void UpdateZoneStateAuthority()
	{
		GamePlayer gamePlayer = GamePlayerLocal.instance.gamePlayer;
		if (gamePlayer == null || gamePlayer.rig == null || gamePlayer.rig.OwningNetPlayer == null)
		{
			return;
		}
		if (!IsInZone())
		{
			if (zoneStateData.state != ZoneState.WaitingToEnterZone)
			{
				zoneClearReason = ZoneClearReason.LeaveZone;
				SetZoneState(ZoneState.WaitingToEnterZone);
				return;
			}
			if (entitiesActiveCount > 0 && ShouldClearZone())
			{
				zoneClearReason = ZoneClearReason.LeaveZone;
				ClearZone();
				return;
			}
		}
		ZoneState state = zoneStateData.state;
		if ((uint)state > 2u)
		{
			_ = 3;
			return;
		}
		bool flag = IsInZone();
		bool inRoom = PhotonNetwork.InRoom;
		bool flag2 = HasAnyScenePlacedInScene(GetZoneSceneName());
		bool flag3 = scenePlacedEntitiesRegistered;
		int num = (int)((uint)zoneStateData.state | (uint)((flag ? 1 : 0) << 4) | (uint)((inRoom ? 1 : 0) << 5) | (uint)((flag2 ? 1 : 0) << 6) | (uint)((flag3 ? 1 : 0) << 7) | (uint)(entities.Count << 8)) | (zoneComponents.Count << 20);
		if (num != _lastUpdateZoneStateAuthLogSig)
		{
			_lastUpdateZoneStateAuthLogSig = num;
		}
		if (flag && inRoom)
		{
			SetZoneState(ZoneState.Active);
			for (int i = 0; i < zoneComponents.Count; i++)
			{
				zoneComponents[i].OnZoneCreate();
			}
		}
	}

	private void UpdateZoneStateClient()
	{
		GamePlayer gamePlayer = GamePlayerLocal.instance.gamePlayer;
		if (gamePlayer == null || gamePlayer.rig == null || gamePlayer.rig.OwningNetPlayer == null)
		{
			return;
		}
		if (!IsInZone())
		{
			if (zoneStateData.state != ZoneState.WaitingToEnterZone)
			{
				zoneClearReason = ZoneClearReason.LeaveZone;
				SetZoneState(ZoneState.WaitingToEnterZone);
				return;
			}
			if (entities.Count > 0 && ShouldClearZone())
			{
				zoneClearReason = ZoneClearReason.LeaveZone;
				ClearZone(ignoreHeldGadgets: true);
				return;
			}
		}
		switch (zoneStateData.state)
		{
		case ZoneState.WaitingToEnterZone:
			if (HasAuthority() && IsInZone() && !IsAuthority())
			{
				SetZoneState(ZoneState.WaitingToRequestState);
			}
			break;
		case ZoneState.WaitingToRequestState:
			if (Time.timeAsDouble - zoneStateData.stateStartTime > 1.0)
			{
				RecalculateNextNetId();
				List<GameEntity> list = GamePlayerLocal.instance.gamePlayer.HeldAndSnappedEntities();
				SetZoneState(ZoneState.WaitingForState);
				photonView.RPC("RequestZoneStateRPC", GetAuthorityPlayer(), (int)zone);
				JoinWithItems(list);
				GamePlayerLocal.instance.joinWithItemsSentForCurrentMigration = true;
			}
			break;
		}
	}

	protected virtual bool IsInZone()
	{
		if (GorillaComputer.instance.IsPlayerInVirtualStump() && IsSuppressZonesInVStumpEnabled())
		{
			if (CustomMapLoader.CanLoadEntities)
			{
				return zone == GTZone.customMaps;
			}
			return false;
		}
		bool flag = VRRig.LocalRig.zoneEntity.currentZone == zone;
		for (int i = 0; i < zoneComponents.Count; i++)
		{
			flag &= zoneComponents[i].IsZoneReady();
		}
		return flag;
	}

	private bool ShouldClearZone()
	{
		bool flag = false;
		for (int i = 0; i < zoneComponents.Count; i++)
		{
			flag |= zoneComponents[i].ShouldClearZone();
		}
		return flag;
	}

	private void SetZoneState(ZoneState newState)
	{
		if (newState == zoneStateData.state)
		{
			return;
		}
		zoneStateData.state = newState;
		zoneStateData.stateStartTime = Time.timeAsDouble;
		switch (zoneStateData.state)
		{
		case ZoneState.WaitingToEnterZone:
		{
			bool num = ShouldClearZone();
			bool flag = zoneClearReason == ZoneClearReason.MigrateGameEntityZone;
			bool flag2 = zoneClearReason == ZoneClearReason.Disconnect;
			bool ignoreHeldGadgets = !num && !flag && !flag2;
			if (flag2 && activeManager == this)
			{
				activeManager = null;
				GamePlayerLocal.instance.currGameEntityManager = null;
			}
			if (!IsAuthority())
			{
				photonView.RPC("PlayerLeftZoneRPC", GetAuthorityPlayer());
			}
			ClearZone(ignoreHeldGadgets);
			break;
		}
		case ZoneState.WaitingForState:
		{
			zoneStateData.numRecievedStateBytes = 0;
			for (int k = 0; k < zoneStateData.recievedStateBytes.Length; k++)
			{
				zoneStateData.recievedStateBytes[k] = 0;
			}
			RegisterScenePlacedEntities();
			if (scenePlacedEntities.Count > 0 && GamePlayerLocal.instance != null && GamePlayerLocal.instance.currGameEntityManager != this)
			{
				GamePlayerLocal.instance.currGameEntityManager = this;
				GamePlayerLocal.instance.pendingFullMigration = true;
			}
			break;
		}
		case ZoneState.Active:
		{
			if (activeManager == this)
			{
				for (int i = 0; i < zoneComponents.Count; i++)
				{
					try
					{
						zoneComponents[i].OnZoneInit();
					}
					catch (Exception exception)
					{
						Debug.LogException(exception);
					}
				}
				RegisterScenePlacedEntities();
				break;
			}
			GameEntityManager gameEntityManager = activeManager;
			activeManager = this;
			for (int j = 0; j < zoneComponents.Count; j++)
			{
				try
				{
					zoneComponents[j].OnZoneInit();
				}
				catch (Exception exception2)
				{
					Debug.LogException(exception2);
				}
			}
			RegisterScenePlacedEntities();
			GamePlayerLocal.instance.MigrateToEntityManager(this);
			if (gameEntityManager.IsNotNull())
			{
				gameEntityManager.zoneClearReason = ZoneClearReason.MigrateGameEntityZone;
				gameEntityManager.SetZoneState(ZoneState.WaitingToEnterZone);
			}
			break;
		}
		case ZoneState.WaitingToRequestState:
			break;
		}
	}

	public void DebugSendState()
	{
		SetZoneState(ZoneState.WaitingToRequestState);
	}

	[PunRPC]
	public void RequestZoneStateRPC(int zoneId, PhotonMessageInfo info)
	{
		int actorNumber = info.Sender.ActorNumber;
		if (!IsAuthority() || zoneId != (int)zone || zoneStateData.zoneStateRequests == null || !GamePlayer.TryGetGamePlayer(info.Sender, out var gamePlayer) || !gamePlayer.newJoinZoneLimiter.CheckCallTime(Time.time))
		{
			return;
		}
		playerZoneJoinTimes[actorNumber] = Time.unscaledTime;
		for (int i = 0; i < zoneStateData.zoneStateRequests.Count; i++)
		{
			Player player = zoneStateData.zoneStateRequests[i].player;
			if (player != null && player.ActorNumber == actorNumber)
			{
				return;
			}
		}
		zoneStateData.zoneStateRequests.Add(new ZoneStateRequest
		{
			player = info.Sender,
			zone = zone,
			completed = false
		});
	}

	public override void WriteDataFusion()
	{
	}

	public override void ReadDataFusion()
	{
	}

	protected override void WriteDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		if (superInfectionManager != null)
		{
			superInfectionManager.WriteDataPUN(stream, info);
		}
	}

	protected override void ReadDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		if (superInfectionManager != null)
		{
			superInfectionManager.ReadDataPUN(stream, info);
		}
	}

	private void OnNetworkJoinedRoom()
	{
		HasAnyScenePlacedInScene(GetZoneSceneName());
		zoneClearReason = ZoneClearReason.JoinZone;
		SetZoneState(ZoneState.WaitingToEnterZone);
	}

	private void OnNetworkLeftRoom()
	{
		for (int i = 0; i < entities.Count; i++)
		{
		}
		zoneClearReason = ZoneClearReason.Disconnect;
		if (zoneStateData.state != ZoneState.WaitingToEnterZone)
		{
			SetZoneState(ZoneState.WaitingToEnterZone);
		}
		else
		{
			if (activeManager == this)
			{
				activeManager = null;
				GamePlayerLocal.instance.currGameEntityManager = null;
			}
			ClearZone();
		}
		playerZoneJoinTimes.Clear();
	}

	private void OnNetworkPlayerLeft(NetPlayer leavingPlayer)
	{
		int num = 0;
		foreach (GameEntity entity in entities)
		{
			if (entity != null && entity.IsAttachedToPlayer(leavingPlayer))
			{
				num++;
			}
		}
		playerZoneJoinTimes.Remove(leavingPlayer.ActorNumber);
	}

	public void OnRigDeactivated(RigContainer container)
	{
		GamePlayer component = container.GetComponent<GamePlayer>();
		int? num = component?.rig?.OwningNetPlayer?.ActorNumber;
		if (num.HasValue)
		{
			num.GetValueOrDefault();
		}
		if (this != activeManager)
		{
			int num2 = 0;
			{
				foreach (GameEntity entity in entities)
				{
					if (entity != null && entity.IsAttachedToPlayer(component?.rig?.OwningNetPlayer))
					{
						num2++;
					}
				}
				return;
			}
		}
		if (component != null)
		{
			List<GameEntityId> list = component.HeldAndSnappedItems(this);
			_leavingItemScratch.Clear();
			for (int i = 0; i < list.Count; i++)
			{
				GameEntity gameEntity = GetGameEntity(list[i]);
				if (gameEntity != null && gameEntity.IsScenePlaced)
				{
					ReleaseScenePlacedHold(gameEntity);
					if (IsAuthority() && TryGetScenePlacedRecord(gameEntity, out var record))
					{
						ResetScenePlacedTransform(gameEntity, in record);
					}
				}
				else if (IsAuthority())
				{
					_leavingItemScratch.Add(list[i]);
				}
			}
			if (IsAuthority() && _leavingItemScratch.Count > 0)
			{
				RequestDestroyItems(_leavingItemScratch);
			}
			_leavingItemScratch.Clear();
		}
		component.ResetData();
	}

	public void OnOwnershipTransferred(NetPlayer toPlayer, NetPlayer fromPlayer)
	{
		if (toPlayer == null || !toPlayer.IsLocal || fromPlayer == null || fromPlayer.InRoom || !GamePlayer.TryGetGamePlayer(fromPlayer.ActorNumber, out var out_gamePlayer))
		{
			return;
		}
		_leavingItemScratch.Clear();
		foreach (GameEntityId item in out_gamePlayer.IterateHeldAndSnappedItems(this))
		{
			_leavingItemScratch.Add(item);
		}
		for (int i = 0; i < _leavingItemScratch.Count; i++)
		{
			GameEntityId gameEntityId = _leavingItemScratch[i];
			GameEntity gameEntity = GetGameEntity(gameEntityId);
			if (gameEntity != null && gameEntity.IsScenePlaced)
			{
				ReleaseScenePlacedHold(gameEntity);
				if (IsAuthority() && TryGetScenePlacedRecord(gameEntity, out var record))
				{
					ResetScenePlacedTransform(gameEntity, in record);
				}
			}
			else
			{
				if (!netIdsForDelete.Contains(GetNetIdFromEntityId(gameEntityId)))
				{
					netIdsForDelete.Add(GetNetIdFromEntityId(gameEntityId));
				}
				DestroyItemLocal(gameEntityId);
			}
		}
		_leavingItemScratch.Clear();
		out_gamePlayer.OnPlayerLeftZone?.Invoke();
	}

	public bool OnOwnershipRequest(NetPlayer fromPlayer)
	{
		return false;
	}

	public void OnMyOwnerLeft()
	{
	}

	public bool OnMasterClientAssistedTakeoverRequest(NetPlayer fromPlayer, NetPlayer toPlayer)
	{
		return false;
	}

	public void OnMyCreatorLeft()
	{
	}

	public void RefreshRigList()
	{
		tempRigs.Clear();
		tempRigs.Add(VRRig.LocalRig);
		VRRigCache.Instance.GetAllUsedRigs(tempRigs);
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void InitSceneUnloadHandler()
	{
		SceneManager.sceneUnloaded += OnZoneSceneUnloaded;
	}

	private static void OnZoneSceneUnloaded(Scene scene)
	{
		if (!s_scenePlacedEntities.TryGetValue(scene.name, out var value))
		{
			return;
		}
		for (int num = value.Count - 1; num >= 0; num--)
		{
			GameEntity gameEntity = value[num];
			if (!(gameEntity == null))
			{
				UnityEngine.Object.Destroy(gameEntity.gameObject);
			}
		}
		s_scenePlacedEntities.Remove(scene.name);
	}

	private string GetZoneSceneName()
	{
		string text = ((ZoneManagement.instance != null) ? ZoneManagement.instance.GetSceneNameForZone(zone) : null);
		if (!string.IsNullOrEmpty(text))
		{
			return text;
		}
		return base.gameObject.scene.name;
	}

	internal static bool HasAnyScenePlacedInScene(string sceneName)
	{
		if (s_scenePlacedEntities.TryGetValue(sceneName, out var value))
		{
			return value.Count > 0;
		}
		return false;
	}

	internal static void RegisterScenePlacedEntity(GameEntity entity)
	{
		string text = entity.gameObject.scene.name;
		if (!s_scenePlacedEntities.TryGetValue(text, out var value))
		{
			value = new List<GameEntity>(8);
			s_scenePlacedEntities[text] = value;
		}
		if (!value.Contains(entity))
		{
			value.Add(entity);
			NotifyManagersOfLateScenePlacedEntity(entity, text);
		}
	}

	private static void NotifyManagersOfLateScenePlacedEntity(GameEntity entity, string sceneName)
	{
		foreach (KeyValuePair<int, GameEntityManager> item in managersByZone)
		{
			GameEntityManager value = item.Value;
			if (!(value == null) && value.scenePlacedEntitiesRegistered && !(value.GetZoneSceneName() != sceneName))
			{
				value.RegisterSingleScenePlacedEntity(entity);
			}
		}
	}

	internal static void UnregisterScenePlacedEntity(GameEntity entity)
	{
		int num = ((entity.manager != null) ? entity.GetNetId() : 0);
		string key;
		if (num != 0 && s_scenePlacedHomeScenes.TryGetValue(num, out var value))
		{
			key = value;
			s_scenePlacedHomeScenes.Remove(num);
		}
		else
		{
			key = entity.gameObject.scene.name;
		}
		if (s_scenePlacedEntities.TryGetValue(key, out var value2))
		{
			value2.Remove(entity);
			if (value2.Count == 0)
			{
				s_scenePlacedEntities.Remove(key);
			}
		}
	}

	internal static bool IsScenePlacedNetId(int netId)
	{
		if (netId < -1)
		{
			return netId != int.MinValue;
		}
		return false;
	}

	public static int NetIdFromXSceneRefId(int uniqueId)
	{
		int num = -uniqueId;
		if (num == -1)
		{
			num = -2;
		}
		return num;
	}

	internal static int ComputeNetIdFromHierarchyForCustomMaps(Transform t)
	{
		int num = t.gameObject.scene.name.GetStaticHash();
		Transform transform = t;
		while (transform != null)
		{
			num = StaticHash.Compute(num, transform.name.GetStaticHash());
			transform = transform.parent;
		}
		if (num > 0)
		{
			num = -num;
		}
		if (num == 0 || num == -1 || num == int.MinValue)
		{
			num = -2;
		}
		return num;
	}

	[WeaverGenerated]
	public override void CopyBackingFieldsToState(bool P_0)
	{
		base.CopyBackingFieldsToState(P_0);
	}

	[WeaverGenerated]
	public override void CopyStateToBackingFields()
	{
		base.CopyStateToBackingFields();
	}
}
