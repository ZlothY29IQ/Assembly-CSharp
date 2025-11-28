using System;
using System.Collections.Generic;
using GorillaTag;
using UnityEngine;

public class GameEntity : MonoBehaviour
{
	public delegate void StateChangedEvent(long prevState, long nextState);

	public delegate void EntityDestroyedEvent(GameEntity entity);

	public const int Invalid = -1;

	public bool pickupable = true;

	public float pickupRangeFromSurface;

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

	[NonSerialized]
	public GameEntityManager manager;

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

	[DebugReadout]
	public GameEntityId id { get; internal set; }

	[DebugReadout]
	public int typeId { get; private set; }

	[DebugReadout]
	public long createData { get; set; }

	[DebugReadout]
	public int heldByActorNumber { get; internal set; }

	[DebugReadout]
	public int snappedByActorNumber { get; internal set; }

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

	public event StateChangedEvent OnStateChanged;

	public event EntityDestroyedEvent onEntityDestroyed;

	private void Awake()
	{
		id = GameEntityId.Invalid;
		rigidBody = GetComponent<Rigidbody>();
		heldByActorNumber = -1;
		heldByHandIndex = -1;
		onlyGrabActorNumber = -1;
		snappedByActorNumber = -1;
		attachedToEntityId = GameEntityId.Invalid;
		entityComponents = new List<IGameEntityComponent>(1);
		GetComponentsInChildren(entityComponents);
		entitySerialize = new List<IGameEntitySerialize>(1);
		GetComponentsInChildren(entitySerialize);
	}

	public void Create(GameEntityManager manager, int typeId)
	{
		this.manager = manager;
		this.typeId = typeId;
	}

	public void Init(long createData)
	{
		this.createData = createData;
		for (int i = 0; i < entityComponents.Count; i++)
		{
			entityComponents[i].OnEntityInit();
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
		if (audioSource != null)
		{
			audioSource.volume = catchSoundVolume;
			audioSource.GTPlayOneShot(catchSound);
		}
	}

	public void PlayThrowFx()
	{
		if (audioSource != null)
		{
			audioSource.volume = throwSoundVolume;
			audioSource.GTPlayOneShot(throwSound);
		}
	}

	public void PlaySnapFx()
	{
		if (audioSource != null)
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
		manager.RemoveGameEntity(this);
		manager = newManager;
		GameEntityId result = (id = newManager.AddGameEntity(this));
		manager.InitItemLocal(this, createData);
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
