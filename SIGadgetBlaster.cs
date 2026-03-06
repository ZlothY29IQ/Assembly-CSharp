using System;
using System.Collections.Generic;
using GorillaLocomotion;
using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(GameGrabbable))]
[RequireComponent(typeof(GameSnappable))]
[RequireComponent(typeof(GameButtonActivatable))]
[RequireComponent(typeof(SIGadgetBlasterType))]
public class SIGadgetBlaster : SIGadget, ITickSystemTick
{
	public enum RPCCalls
	{
		FireProjectile,
		ProjectileHitPlayer
	}

	[OnEnterPlay_SetNull]
	public static Dictionary<int, List<GameObject>> blasterProjectilePools;

	[NonSerialized]
	public const float PROJECTILE_MAX_LATENCY = 1f;

	private SIGadgetBlasterType blasterType;

	[NonSerialized]
	public SIGadgetBlasterState currentState;

	[SerializeField]
	private GameButtonActivatable buttonActivatable;

	[SerializeField]
	private float inputActivateThreshold = 0.35f;

	[SerializeField]
	private float inputDeactivateThreshold = 0.25f;

	public int maxProjectileCount = 10;

	public float maxLagDistance = 5f;

	private bool wasActivated;

	[NonSerialized]
	public float lastFired;

	[NonSerialized]
	public int projectileCount;

	private int projectileId;

	[NonSerialized]
	public List<SIGadgetBlasterProjectile> activeProjectiles = new List<SIGadgetBlasterProjectile>();

	[NonSerialized]
	public Queue<SIGadgetBlasterProjectile> projectilesToDespawn = new Queue<SIGadgetBlasterProjectile>();

	[NonSerialized]
	public Queue<float> projectilesToDespawnTimes = new Queue<float>();

	public Transform firingPosition;

	public AudioSource firingSource;

	public AudioSource blasterSource;

	[NonSerialized]
	public LayerMask environmentLayerMask;

	public bool LocalEquippedOrActivated
	{
		get
		{
			if (!IsEquippedLocal())
			{
				return activatedLocally;
			}
			return true;
		}
	}

	public bool TickRunning { get; set; }

	protected override void OnEnable()
	{
		base.OnEnable();
		blasterType = GetComponent<SIGadgetBlasterType>();
		lastFired = 0f;
		environmentLayerMask = GTPlayer.Instance.locomotionEnabledLayers;
		GameEntity obj = gameEntity;
		obj.OnGrabbed = (Action)Delegate.Combine(obj.OnGrabbed, new Action(StartGrabbing));
		GameEntity obj2 = gameEntity;
		obj2.OnSnapped = (Action)Delegate.Combine(obj2.OnSnapped, new Action(StartGrabbing));
		GameEntity obj3 = gameEntity;
		obj3.OnReleased = (Action)Delegate.Combine(obj3.OnReleased, new Action(StopGrabbing));
		GameEntity obj4 = gameEntity;
		obj4.OnUnsnapped = (Action)Delegate.Combine(obj4.OnUnsnapped, new Action(StopGrabbing));
		TickSystem<object>.AddTickCallback(this);
	}

	private new void OnDisable()
	{
		base.OnDisable();
		TickSystem<object>.RemoveTickCallback(this);
	}

	public void Tick()
	{
		if (projectilesToDespawn.Count > 0 && !(Time.time < projectilesToDespawnTimes.Peek() + 1f))
		{
			SIGadgetBlasterProjectile sIGadgetBlasterProjectile = projectilesToDespawn.Dequeue();
			activeProjectiles.RemoveIfContains(sIGadgetBlasterProjectile);
			if (!(sIGadgetBlasterProjectile == null) && !(sIGadgetBlasterProjectile.gameObject == null))
			{
				blasterProjectilePools[sIGadgetBlasterProjectile.poolId].Add(sIGadgetBlasterProjectile.gameObject);
			}
		}
	}

	protected override void OnUpdateAuthority(float dt)
	{
		base.OnUpdateAuthority(dt);
		blasterType.OnUpdateAuthority(dt);
	}

	protected override void OnUpdateRemote(float dt)
	{
		base.OnUpdateRemote(dt);
		SIGadgetBlasterState sIGadgetBlasterState = (SIGadgetBlasterState)gameEntity.GetState();
		if (sIGadgetBlasterState != currentState)
		{
			SetStateShared(sIGadgetBlasterState);
		}
		blasterType.OnUpdateRemote(dt);
	}

	public void SetStateAuthority(SIGadgetBlasterState newState)
	{
		SetStateShared(newState);
		gameEntity.RequestState(gameEntity.id, (long)newState);
	}

	private void SetStateShared(SIGadgetBlasterState newState)
	{
		if (newState != currentState && CanChangeState((long)newState))
		{
			_ = currentState;
			currentState = newState;
			blasterType.SetStateShared();
		}
	}

	public override void ApplyUpgradeNodes(SIUpgradeSet withUpgrades)
	{
		blasterType.ApplyUpgradeNodes(withUpgrades);
	}

	public static bool CanChangeState(long newStateIndex)
	{
		if (newStateIndex >= 0)
		{
			return newStateIndex < 4;
		}
		return false;
	}

	public bool CheckInput()
	{
		float sensitivity = (wasActivated ? inputActivateThreshold : inputDeactivateThreshold);
		wasActivated = buttonActivatable.CheckInput(sensitivity);
		return wasActivated;
	}

	public int NextFireId()
	{
		return projectileId++;
	}

	public override void ProcessClientToClientRPC(PhotonMessageInfo info, int rpcID, object[] data)
	{
		switch ((RPCCalls)rpcID)
		{
		case RPCCalls.FireProjectile:
			if (data != null && data.Length != 0 && gameEntity.IsAttachedToPlayer(NetPlayer.Get(info.Sender)))
			{
				blasterType.NetworkFireProjectile(data);
			}
			break;
		case RPCCalls.ProjectileHitPlayer:
		{
			if (data == null || data.Length < 2 || !GameEntityManager.ValidateDataType<int>(data[0], out var dataAsType))
			{
				break;
			}
			SIGadgetBlasterProjectile sIGadgetBlasterProjectile = null;
			for (int i = 0; i < activeProjectiles.Count; i++)
			{
				if (activeProjectiles[i].projectileId == dataAsType)
				{
					sIGadgetBlasterProjectile = activeProjectiles[i];
					break;
				}
			}
			if (!(sIGadgetBlasterProjectile == null) && !(sIGadgetBlasterProjectile.firedByPlayer != SIPlayer.Get(info.Sender.ActorNumber)))
			{
				sIGadgetBlasterProjectile.GetComponent<SIGadgetProjectileType>().NetworkedProjectileHit(data);
			}
			break;
		}
		}
	}

	public void StartGrabbing()
	{
		if (IsEquippedLocal() || activatedLocally)
		{
			SetStateAuthority(SIGadgetBlasterState.Idle);
		}
	}

	public void StopGrabbing()
	{
		SetStateShared(SIGadgetBlasterState.Idle);
	}

	public void DespawnProjectile(SIGadgetBlasterProjectile projectile)
	{
		projectile.gameObject.SetActive(value: false);
		if (!projectilesToDespawn.Contains(projectile))
		{
			projectilesToDespawn.Enqueue(projectile);
			projectilesToDespawnTimes.Enqueue(Time.time);
		}
	}

	public GameObject InstantiateProjectile(SIGadgetBlasterProjectile projectilePrefab, Vector3 position, Quaternion rotation, int thisFireId)
	{
		if (blasterProjectilePools == null)
		{
			blasterProjectilePools = new Dictionary<int, List<GameObject>>();
		}
		int instanceID = projectilePrefab.GetInstanceID();
		if (!blasterProjectilePools.ContainsKey(instanceID))
		{
			blasterProjectilePools.Add(instanceID, new List<GameObject>());
		}
		List<GameObject> list = blasterProjectilePools[instanceID];
		GameObject gameObject;
		if (list.Count <= 0)
		{
			gameObject = UnityEngine.Object.Instantiate(projectilePrefab.gameObject, position, rotation);
		}
		else
		{
			gameObject = list[list.Count - 1];
			list.RemoveAt(list.Count - 1);
			gameObject.SetActive(value: true);
		}
		SIGadgetBlasterProjectile component = gameObject.GetComponent<SIGadgetBlasterProjectile>();
		component.transform.position = position;
		component.transform.rotation = rotation;
		component.parentBlaster = this;
		component.projectileId = thisFireId;
		component.firedByPlayer = (gameEntity.IsHeld() ? SIPlayer.Get(gameEntity.heldByActorNumber) : SIPlayer.Get(gameEntity.snappedByActorNumber));
		component.poolId = instanceID;
		activeProjectiles.Add(component);
		lastFired = Time.time;
		component.InitializeProjectile();
		return gameObject;
	}

	public void FireProjectileHaptics(float strength, float duration)
	{
		GorillaTagger.Instance.StartVibration(gameEntity.EquippedHandedness == EHandedness.Left, strength, duration);
	}

	public float CurrentFireRate()
	{
		int count = activeProjectiles.Count;
		if (count <= 1)
		{
			return 0f;
		}
		return (float)(count - 1) / (activeProjectiles[count - 1].timeSpawned - activeProjectiles[0].timeSpawned);
	}
}
