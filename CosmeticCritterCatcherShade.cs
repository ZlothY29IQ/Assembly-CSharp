using Photon.Pun;
using UnityEngine;

public class CosmeticCritterCatcherShade : CosmeticCritterCatcher
{
	[SerializeField]
	private float secondsToReveal = 1f;

	[SerializeField]
	private float minSecondsLockedToCatch = 1f;

	[SerializeField]
	private Transform catchOrigin;

	[SerializeField]
	private float catchRadius = 1f;

	[SerializeField]
	private float vacuumSpeed = 3f;

	private ShadeRevealer shadeRevealer;

	private CosmeticCritter currentTarget;

	private float targetHoldTime;

	private float maxHoldTime;

	private const float HEARTBEAT_DELAY = 1f;

	private float heartbeatCooldown;

	public Vector3 LastTargetPosition { get; private set; }

	public float GetActionTimeFrac()
	{
		return targetHoldTime / maxHoldTime;
	}

	protected override CallLimiter CreateCallLimiter()
	{
		return new CallLimiter(10, 0.25f);
	}

	public override CosmeticCritterAction GetLocalCatchAction(CosmeticCritter critter)
	{
		if (heartbeatCooldown > 0.5f || (currentTarget != null && currentTarget != critter))
		{
			return CosmeticCritterAction.None;
		}
		if (critter is CosmeticCritterShadeFleeing && shadeRevealer.CritterWithinBeamThreshold(critter, ShadeRevealer.State.LOCKED, 0f))
		{
			if (targetHoldTime >= minSecondsLockedToCatch && (critter.transform.position - catchOrigin.position).sqrMagnitude <= catchRadius * catchRadius)
			{
				return CosmeticCritterAction.RPC | CosmeticCritterAction.Despawn;
			}
			return CosmeticCritterAction.RPC | CosmeticCritterAction.ShadeHeartbeat;
		}
		if (critter is CosmeticCritterShadeHidden && shadeRevealer.CritterWithinBeamThreshold(critter, ShadeRevealer.State.TRACKING, 0f))
		{
			if (targetHoldTime >= secondsToReveal)
			{
				return CosmeticCritterAction.RPC | CosmeticCritterAction.Despawn | CosmeticCritterAction.SpawnLinked;
			}
			return CosmeticCritterAction.RPC | CosmeticCritterAction.ShadeHeartbeat;
		}
		return CosmeticCritterAction.None;
	}

	public override bool ValidateRemoteCatchAction(CosmeticCritter critter, CosmeticCritterAction catchAction, double serverTime)
	{
		if (!base.ValidateRemoteCatchAction(critter, catchAction, serverTime))
		{
			return false;
		}
		if (critter is CosmeticCritterShadeFleeing)
		{
			if ((catchAction & CosmeticCritterAction.Despawn) != CosmeticCritterAction.None && (critter.transform.position - catchOrigin.position).sqrMagnitude <= catchRadius * catchRadius + 1f && targetHoldTime >= minSecondsLockedToCatch * 0.8f)
			{
				return true;
			}
			if ((catchAction & CosmeticCritterAction.ShadeHeartbeat) != CosmeticCritterAction.None && shadeRevealer.CritterWithinBeamThreshold(critter, ShadeRevealer.State.LOCKED, 2f))
			{
				return true;
			}
		}
		else if (critter is CosmeticCritterShadeHidden)
		{
			if ((catchAction & (CosmeticCritterAction.Despawn | CosmeticCritterAction.SpawnLinked)) != CosmeticCritterAction.None && targetHoldTime >= secondsToReveal * 0.8f)
			{
				return true;
			}
			if ((catchAction & CosmeticCritterAction.ShadeHeartbeat) != CosmeticCritterAction.None && shadeRevealer.CritterWithinBeamThreshold(critter, ShadeRevealer.State.TRACKING, 2f))
			{
				return true;
			}
		}
		return false;
	}

	public override void OnCatch(CosmeticCritter critter, CosmeticCritterAction catchAction, double serverTime)
	{
		currentTarget = critter;
		float num = (PhotonNetwork.InRoom ? ((float)(PhotonNetwork.Time - serverTime)) : 0f);
		heartbeatCooldown = 1f + num;
		targetHoldTime += num;
		if (critter is CosmeticCritterShadeFleeing)
		{
			maxHoldTime = minSecondsLockedToCatch;
			if ((catchAction & CosmeticCritterAction.Despawn) != CosmeticCritterAction.None)
			{
				shadeRevealer.ShadeCaught();
				currentTarget = null;
				targetHoldTime = 0f;
			}
			else
			{
				_ = catchAction & CosmeticCritterAction.ShadeHeartbeat;
			}
		}
		else if (critter is CosmeticCritterShadeHidden)
		{
			maxHoldTime = secondsToReveal;
			if ((catchAction & (CosmeticCritterAction.Despawn | CosmeticCritterAction.SpawnLinked)) != CosmeticCritterAction.None)
			{
				(optionalLinkedSpawner as CosmeticCritterSpawnerShadeFleeing).SetSpawnPosition(critter.transform.position);
				currentTarget = null;
				targetHoldTime = 0f;
			}
		}
	}

	protected override void Awake()
	{
		base.Awake();
		shadeRevealer = transferrableObject as ShadeRevealer;
		maxHoldTime = Mathf.Max(secondsToReveal, minSecondsLockedToCatch);
	}

	protected void LateUpdate()
	{
		if (heartbeatCooldown > 0f)
		{
			heartbeatCooldown -= Time.deltaTime;
			if (heartbeatCooldown < 0f)
			{
				heartbeatCooldown = 0f;
				currentTarget = null;
				return;
			}
			targetHoldTime = Mathf.Min(targetHoldTime + Time.deltaTime, maxHoldTime);
			if (currentTarget is CosmeticCritterShadeFleeing)
			{
				if (!base.IsLocal || heartbeatCooldown > 0.4f)
				{
					shadeRevealer.SetBestBeamState(ShadeRevealer.State.LOCKED);
				}
				Vector3 normalized = (catchOrigin.position - currentTarget.transform.position).normalized;
				(currentTarget as CosmeticCritterShadeFleeing).pullVector += vacuumSpeed * Time.deltaTime * normalized;
			}
			else if (currentTarget is CosmeticCritterShadeHidden && (!base.IsLocal || heartbeatCooldown > 0.4f))
			{
				shadeRevealer.SetBestBeamState(ShadeRevealer.State.TRACKING);
			}
		}
		else if (targetHoldTime > 0f)
		{
			targetHoldTime = Mathf.Max(targetHoldTime - Time.deltaTime, 0f);
		}
	}

	protected override void OnEnable()
	{
		base.OnEnable();
		currentTarget = null;
		targetHoldTime = 0f;
		heartbeatCooldown = 1f;
	}

	protected override void OnDisable()
	{
		base.OnDisable();
		currentTarget = null;
		targetHoldTime = 0f;
		heartbeatCooldown = 1f;
	}
}
