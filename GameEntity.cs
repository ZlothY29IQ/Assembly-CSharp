using System;
using System.Collections.Generic;
using GorillaTag;
using GorillaTag.Gravity;
using Photon.Pun;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR;

public class GameEntity : MonoBehaviour
{
	public class RendererSet
	{
		public List<(MeshFilter filter, MeshRenderer renderer)> renderers = new List<(MeshFilter, MeshRenderer)>();

		public List<SkinnedMeshRenderer> skinnedRenderers = new List<SkinnedMeshRenderer>();
	}

	public delegate void StateChangedEvent(long prevState, long nextState);

	public delegate void EntityDestroyedEvent(GameEntity entity);

	public const int Invalid = -1;

	public const int ScenePlacedTypeId = -2147483647;

	public List<GameEntity> builtInEntities;

	[NonSerialized]
	public bool isBuiltIn;

	public bool pickupable = true;

	public float pickupRangeFromSurface;

	[Tooltip("Renderers on these objects are ignored when determining grab bounds")]
	public GameObject[] ignoreObjectGrabRenderers;

	public bool canHoldingPlayerUpdateState;

	public bool canLastHoldingPlayerUpdateState;

	public bool canSnapPlayerUpdateState;

	public AudioSource audioSource;

	public AudioClip catchSound;

	public float catchSoundVolume = 0.5f;

	public AudioClip throwSound;

	public float throwSoundVolume = 0.5f;

	public AudioClip snapSound;

	public float snapSoundVolume = 0.5f;

	private Rigidbody rigidBody;

	[SerializeField]
	public MonkeGravityController gravityController;

	[NonSerialized]
	public GameEntityManager manager;

	internal bool shouldDestroyOnZoneExit;

	[NonSerialized]
	internal bool scenePlacedInitialized;

	[NonSerialized]
	internal Vector3 scenePlacedHomePosition;

	[NonSerialized]
	internal Quaternion scenePlacedHomeRotation;

	[NonSerialized]
	internal float scenePlacedHomeScale;

	public Action OnGrabbed;

	public Action OnReleased;

	public Action OnSnapped;

	public Action OnUnsnapped;

	public Action OnAttached;

	public Action OnDetached;

	public Action OnTick;

	public float MinTimeBetweenTicks;

	[NonSerialized]
	public float LastTickTime;

	private long state;

	private List<IGameEntityComponent> entityComponents;

	public List<IGameEntitySerialize> entitySerialize;

	private RendererSet _grabbableRenderers;

	private List<MeshFilter> _meshFilters;

	[DebugReadout]
	public GameEntityId id { get; internal set; }

	[DebugReadout]
	public int typeId { get; internal set; }

	[DebugReadout]
	public long createData { get; set; }

	[DebugReadout]
	public GameEntityId createdByEntityId { get; set; }

	[DebugReadout]
	public int heldByActorNumber { get; internal set; }

	[DebugReadout]
	public int snappedByActorNumber { get; internal set; }

	[DebugReadout]
	public int slotIndex
	{
		get
		{
			if (heldByHandIndex == -1)
			{
				return GameSnappable.GetJointToSnapIndex(snappedJoint);
			}
			return heldByHandIndex;
		}
	}

	[DebugReadout]
	public SnapJointType snappedJoint { get; internal set; }

	[DebugReadout]
	public int heldByHandIndex { get; internal set; }

	[DebugReadout]
	public int lastHeldByActorNumber { get; internal set; }

	[DebugReadout]
	public int onlyGrabActorNumber { get; internal set; }

	[DebugReadout]
	public GameEntityId attachedToEntityId { get; internal set; }

	public bool IsScenePlaced { get; internal set; }

	public bool IsHeldOrSnappedByLocalPlayer => AttachedPlayerActorNr == NetworkSystem.Instance.LocalPlayer.ActorNumber;

	public bool IsSnappedToHand => (snappedJoint & (SnapJointType.HandL | SnapJointType.HandR)) != 0;

	public int AttachedPlayerActorNr
	{
		get
		{
			if (heldByActorNumber == -1)
			{
				return snappedByActorNumber;
			}
			return heldByActorNumber;
		}
	}

	public int EquippedSlotIndex
	{
		get
		{
			if (heldByHandIndex == -1)
			{
				if ((snappedJoint & SnapJointType.HandL) == 0)
				{
					if ((snappedJoint & SnapJointType.HandR) == 0)
					{
						return -1;
					}
					return 3;
				}
				return 2;
			}
			return heldByHandIndex;
		}
	}

	public EHandedness EquippedHandedness
	{
		get
		{
			if (heldByHandIndex != 0 && (snappedJoint & SnapJointType.HandL) == 0)
			{
				if (heldByHandIndex != 1 && (snappedJoint & SnapJointType.HandR) == 0)
				{
					return EHandedness.None;
				}
				return EHandedness.Right;
			}
			return EHandedness.Left;
		}
	}

	public XRNode EquippedHandXRNode
	{
		get
		{
			if (heldByHandIndex != 0 && (snappedJoint & SnapJointType.HandL) == 0)
			{
				if (heldByHandIndex != 1 && (snappedJoint & SnapJointType.HandR) == 0)
				{
					return (XRNode)(-1);
				}
				return XRNode.RightHand;
			}
			return XRNode.LeftHand;
		}
	}

	public event StateChangedEvent OnStateChanged;

	public event EntityDestroyedEvent onEntityDestroyed;

	private void Awake()
	{
		id = GameEntityId.Invalid;
		rigidBody = GetComponent<Rigidbody>();
		if (gravityController == null)
		{
			gravityController = GetComponent<MonkeGravityController>();
			if (gravityController == null)
			{
				gravityController = base.gameObject.AddComponent<MonkeGravityController>();
			}
		}
		heldByActorNumber = -1;
		heldByHandIndex = -1;
		onlyGrabActorNumber = -1;
		snappedByActorNumber = -1;
		attachedToEntityId = GameEntityId.Invalid;
		entityComponents = new List<IGameEntityComponent>(1);
		GetComponentsInChildren(entityComponents);
		entitySerialize = new List<IGameEntitySerialize>(1);
		GetComponentsInChildren(entitySerialize);
		if (builtInEntities != null)
		{
			for (int i = 0; i < builtInEntities.Count; i++)
			{
				builtInEntities[i].isBuiltIn = true;
			}
		}
		if (TryGetComponent<XSceneRefTarget>(out var component) && component.UniqueID > 0)
		{
			IsScenePlaced = true;
			GameEntityManager.RegisterScenePlacedEntity(this);
		}
	}

	private void Start()
	{
		if (IsScenePlaced && !PhotonNetwork.InRoom)
		{
			base.gameObject.SetActive(value: false);
		}
	}

	public void Create(GameEntityManager manager, int netId, int typeId)
	{
		this.manager = manager;
		this.typeId = typeId;
		if (builtInEntities != null)
		{
			bool flag = netId < -1 && netId != int.MinValue;
			for (int i = 0; i < builtInEntities.Count; i++)
			{
				int netId2 = (flag ? (netId - 1 - i) : (netId + 1 + i));
				manager.AddGameEntity(netId2, builtInEntities[i]);
				builtInEntities[i].Create(manager, netId2, -1);
			}
		}
	}

	public void Init(long createData, int createdByEntityNetId)
	{
		this.createData = createData;
		createdByEntityId = manager.GetEntityIdFromNetId(createdByEntityNetId);
		for (int i = 0; i < entityComponents.Count; i++)
		{
			entityComponents[i].OnEntityInit();
		}
		for (int j = 0; j < builtInEntities.Count; j++)
		{
			builtInEntities[j].Init(0L, -1);
		}
	}

	public void OnDestroy()
	{
		if (!GTAppState.isQuitting)
		{
			for (int i = 0; i < entityComponents.Count; i++)
			{
				entityComponents[i].OnEntityDestroy();
			}
			this.onEntityDestroyed?.Invoke(this);
			if (IsScenePlaced)
			{
				GameEntityManager.UnregisterScenePlacedEntity(this);
			}
		}
	}

	public RendererSet GetGrabbableRenderers()
	{
		if (_grabbableRenderers == null)
		{
			_grabbableRenderers = new RendererSet();
			_meshFilters = new List<MeshFilter>();
			GetComponentsInChildren(includeInactive: true, _meshFilters);
			GetComponentsInChildren(includeInactive: true, _grabbableRenderers.skinnedRenderers);
			List<SkinnedMeshRenderer> skinnedRenderers = _grabbableRenderers.skinnedRenderers;
			RemoveNotOwnedComponents<MeshFilter>(_meshFilters);
			RemoveNotOwnedComponents<SkinnedMeshRenderer>(skinnedRenderers);
			GameObject[] array = ignoreObjectGrabRenderers;
			foreach (GameObject gameObject in array)
			{
				for (int j = 0; j < _meshFilters.Count; j++)
				{
					if (_meshFilters[j].gameObject == gameObject)
					{
						_meshFilters.RemoveAtSwapBack(j--);
					}
				}
				for (int k = 0; k < skinnedRenderers.Count; k++)
				{
					if (skinnedRenderers[k].gameObject == gameObject)
					{
						skinnedRenderers.RemoveAtSwapBack(k--);
					}
				}
			}
			foreach (MeshFilter meshFilter in _meshFilters)
			{
				MeshRenderer component = meshFilter.GetComponent<MeshRenderer>();
				if ((object)component != null)
				{
					_grabbableRenderers.renderers.Add((meshFilter, component));
				}
			}
		}
		return _grabbableRenderers;
		void RemoveNotOwnedComponents<T>(List<T> components) where T : Component
		{
			for (int l = 0; l < components.Count; l++)
			{
				if (manager.GetParentEntity<GameEntity>(components[l].transform) != this)
				{
					components.RemoveAtSwapBack(l--);
				}
			}
		}
	}

	public Vector3 GetVelocity()
	{
		if (rigidBody == null)
		{
			return Vector3.zero;
		}
		return rigidBody.linearVelocity;
	}

	public void PlayCatchFx()
	{
		if (audioSource != null && audioSource.isActiveAndEnabled)
		{
			audioSource.volume = catchSoundVolume;
			audioSource.GTPlayOneShot(catchSound);
		}
	}

	public void PlayThrowFx()
	{
		if (audioSource != null && audioSource.isActiveAndEnabled)
		{
			audioSource.volume = throwSoundVolume;
			audioSource.GTPlayOneShot(throwSound);
		}
	}

	public void PlaySnapFx()
	{
		if (audioSource != null && audioSource.isActiveAndEnabled)
		{
			audioSource.volume = snapSoundVolume;
			audioSource.GTPlayOneShot(snapSound);
		}
	}

	private bool IsGamePlayer(Collider collider)
	{
		return GamePlayer.GetGamePlayer(collider) != null;
	}

	public long GetState()
	{
		return state;
	}

	public void RequestState(GameEntityId id, long newState)
	{
		manager.RequestState(id, newState);
	}

	public bool IsAuthority()
	{
		return manager.IsAuthority();
	}

	public bool IsValidToMigrate()
	{
		return manager.IsEntityValidToMigrate(this);
	}

	public void SetState(long newState)
	{
		if (state != newState)
		{
			long prevState = state;
			state = newState;
			this.OnStateChanged?.Invoke(prevState, newState);
			for (int i = 0; i < entityComponents.Count; i++)
			{
				entityComponents[i].OnEntityStateChange(prevState, newState);
			}
		}
	}

	public GameEntityId MigrateToEntityManager(GameEntityManager newManager)
	{
		if (IsScenePlaced)
		{
			if (manager != null)
			{
				manager.ReleaseScenePlacedHold(this);
			}
			return id;
		}
		manager.RemoveGameEntity(this);
		manager = newManager;
		GameEntityId result = (id = newManager.AddGameEntity(this));
		manager.InitItemLocal(this, createData, -1);
		return result;
	}

	public void MigrateHeldBy(int actorNumber)
	{
		if (heldByActorNumber >= 0)
		{
			heldByActorNumber = actorNumber;
		}
	}

	public void MigrateSnappedBy(int actorNumber)
	{
		if (snappedByActorNumber >= 0)
		{
			snappedByActorNumber = actorNumber;
		}
	}

	public int GetNetId(GameEntityId gameEntityId)
	{
		return manager.GetNetIdFromEntityId(gameEntityId);
	}

	public int GetNetId()
	{
		return manager.GetNetIdFromEntityId(id);
	}

	public static GameEntity Get(Collider collider)
	{
		if (collider == null)
		{
			return null;
		}
		Transform parent = collider.transform;
		while (parent != null)
		{
			GameEntity component = parent.GetComponent<GameEntity>();
			if (component != null)
			{
				return component;
			}
			parent = parent.parent;
		}
		return null;
	}

	public bool IsHeldByLocalPlayer()
	{
		return heldByActorNumber == NetworkSystem.Instance.LocalPlayer.ActorNumber;
	}

	public bool IsSnappedByLocalPlayer()
	{
		return snappedByActorNumber == NetworkSystem.Instance.LocalPlayer.ActorNumber;
	}

	public bool IsHeld()
	{
		return heldByActorNumber != -1;
	}

	public int GetLastHeldByPlayerForEntityID(GameEntityId gameEntityId)
	{
		GameEntity gameEntity = manager.GetGameEntity(gameEntityId);
		if (gameEntity != null)
		{
			return gameEntity.lastHeldByActorNumber;
		}
		return 0;
	}

	public bool WasLastHeldByLocalPlayer()
	{
		return lastHeldByActorNumber == NetworkSystem.Instance.LocalPlayer.ActorNumber;
	}

	public bool IsAttachedToPlayer(NetPlayer player)
	{
		if (player != null)
		{
			if (heldByActorNumber != player.ActorNumber)
			{
				return snappedByActorNumber == player.ActorNumber;
			}
			return true;
		}
		return false;
	}
}
