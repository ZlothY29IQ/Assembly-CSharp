using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.AI;

public class GameAgent : MonoBehaviour, IGameEntityComponent
{
	public delegate void StateChangedEvent(byte newState);

	public delegate void NavigationLinkReachedEvent(OffMeshLinkData linkData);

	public delegate void JumpRequestedEvent(Vector3 start, Vector3 end, float heightScale, float speedScale);

	public delegate void NavigationFailedEvent(NavMeshPathStatus status, Vector3 destination, float remainingDistance);

	public GameEntity entity;

	public NavMeshAgent navAgent;

	public Rigidbody rigidBody;

	public float networkPositionCorrectionDist = 2.5f;

	[ReadOnly]
	public NetPlayer targetPlayer;

	private bool disableNetworkSync;

	private Vector3 lastPosOnNavMesh;

	private Vector3 lastRequestedDest;

	private Vector3 lastReceivedDest;

	private bool hasNotifiedNavigationFailure;

	private List<IGameAgentComponent> agentComponents;

	private bool wasOnOffMeshNavLink;

	[ReadOnly]
	public bool pauseEntityThink;

	public event StateChangedEvent onBodyStateChanged;

	public event StateChangedEvent onBehaviorStateChanged;

	public event NavigationLinkReachedEvent onReachedNavigationLink;

	public event JumpRequestedEvent onJumpRequested;

	public event NavigationFailedEvent onNavigationFailed;

	public GameAgentManager GetGameAgentManager()
	{
		return entity.manager.gameAgentManager;
	}

	private void Awake()
	{
		agentComponents = new List<IGameAgentComponent>(1);
		GetComponentsInChildren(agentComponents);
	}

	public void OnEntityInit()
	{
		GetGameAgentManager().AddGameAgent(this);
	}

	public void OnEntityDestroy()
	{
		GetGameAgentManager().RemoveGameAgent(this);
	}

	public void OnEntityStateChange(long prevState, long nextState)
	{
	}

	public void OnBehaviorStateChanged(byte newState)
	{
		this.onBehaviorStateChanged?.Invoke(newState);
	}

	public void OnBodyStateChanged(byte newState)
	{
		this.onBodyStateChanged?.Invoke(newState);
	}

	public void OnThink(float deltaTime)
	{
		if (!pauseEntityThink)
		{
			for (int i = 0; i < agentComponents.Count; i++)
			{
				agentComponents[i].OnEntityThink(deltaTime);
			}
		}
	}

	public void OnUpdate()
	{
		if (navAgent.isOnNavMesh)
		{
			lastPosOnNavMesh = navAgent.transform.position;
		}
		if (!navAgent.autoTraverseOffMeshLink && !wasOnOffMeshNavLink && navAgent.isOnOffMeshLink)
		{
			if (entity.IsAuthority())
			{
				if ((navAgent.transform.position - navAgent.currentOffMeshLinkData.startPos).sqrMagnitude < (navAgent.transform.position - navAgent.currentOffMeshLinkData.endPos).sqrMagnitude)
				{
					GetGameAgentManager().RequestJump(this, navAgent.transform.position, navAgent.currentOffMeshLinkData.endPos, 1f, 1f);
				}
				else
				{
					GetGameAgentManager().RequestJump(this, navAgent.transform.position, navAgent.currentOffMeshLinkData.startPos, 1f, 1f);
				}
			}
			this.onReachedNavigationLink?.Invoke(navAgent.currentOffMeshLinkData);
		}
		wasOnOffMeshNavLink = navAgent.isOnOffMeshLink;
		if (!hasNotifiedNavigationFailure && !navAgent.pathPending && (navAgent.pathStatus == NavMeshPathStatus.PathPartial || navAgent.pathStatus == NavMeshPathStatus.PathInvalid))
		{
			this.onNavigationFailed?.Invoke(navAgent.pathStatus, navAgent.destination, navAgent.remainingDistance);
			hasNotifiedNavigationFailure = true;
		}
	}

	public void OnJumpRequested(Vector3 start, Vector3 end, float heightScale, float speedScale)
	{
		this.onJumpRequested?.Invoke(start, end, heightScale, speedScale);
	}

	public bool IsOnNavMesh()
	{
		if (navAgent != null)
		{
			return navAgent.isOnNavMesh;
		}
		return false;
	}

	public Vector3 GetLastPosOnNavMesh()
	{
		return lastPosOnNavMesh;
	}

	public void RequestDestination(Vector3 dest)
	{
		if (!entity.IsAuthority())
		{
			return;
		}
		if (!IsOnNavMesh())
		{
			dest = lastPosOnNavMesh;
		}
		if (!(Vector3.Distance(lastRequestedDest, dest) < 0.5f))
		{
			lastRequestedDest = dest;
			if (entity.IsAuthority())
			{
				GetGameAgentManager().RequestDestination(this, dest);
			}
		}
	}

	public void RequestBehaviorChange(byte behavior)
	{
		GetGameAgentManager().RequestBehavior(this, behavior);
	}

	public void RequestStateChange(byte state)
	{
		GetGameAgentManager().RequestState(this, state);
	}

	public void RequestTarget(NetPlayer targetPlayer)
	{
		GetGameAgentManager().RequestTarget(this, targetPlayer);
	}

	public void ApplyDestination(Vector3 dest)
	{
		if (NavMesh.SamplePosition(dest, out var hit, 1.5f, -1))
		{
			dest = hit.position;
			lastReceivedDest = dest;
			hasNotifiedNavigationFailure = false;
			if (navAgent.isOnNavMesh)
			{
				navAgent.destination = dest;
			}
		}
	}

	public void SetDisableNetworkSync(bool disable)
	{
		disableNetworkSync = disable;
	}

	public void SetIsPathing(bool isPathing, bool ignoreRigiBody = false)
	{
		navAgent.enabled = isPathing;
		if (!ignoreRigiBody && rigidBody != null)
		{
			rigidBody.isKinematic = isPathing;
		}
	}

	public void SetSpeed(float speed)
	{
		navAgent.speed = speed;
	}

	public void ApplyNetworkUpdate(Vector3 position, Quaternion rotation)
	{
		if (!disableNetworkSync)
		{
			if ((base.transform.position - position).sqrMagnitude > networkPositionCorrectionDist * networkPositionCorrectionDist)
			{
				navAgent.Warp(position);
				navAgent.destination = lastReceivedDest;
			}
			base.transform.rotation = rotation;
			if (rigidBody != null)
			{
				rigidBody.rotation = rotation;
			}
		}
	}

	public static void UpdateFacing(Transform transform, NavMeshAgent navAgent, NetPlayer targetPlayer, float turnspeed = 3600f)
	{
		Transform target = null;
		_ = transform.forward;
		if (targetPlayer != null)
		{
			GRPlayer gRPlayer = GRPlayer.Get(targetPlayer.ActorNumber);
			if (gRPlayer != null && gRPlayer.State == GRPlayer.GRPlayerState.Alive)
			{
				target = gRPlayer.transform;
			}
		}
		UpdateFacingTarget(transform, navAgent, target, turnspeed);
	}

	public static void UpdateFacingTarget(Transform transform, NavMeshAgent navAgent, Transform target, float turnspeed = 3600f)
	{
		Vector3 forward = transform.forward;
		if (target != null)
		{
			Vector3 position = target.position;
			Vector3 position2 = transform.position;
			Vector3 vector = position - position2;
			vector.y = 0f;
			float magnitude = vector.magnitude;
			if (magnitude > 0f)
			{
				forward = vector / magnitude;
			}
		}
		else
		{
			Vector3 desiredVelocity = navAgent.desiredVelocity;
			desiredVelocity.y = 0f;
			float magnitude2 = desiredVelocity.magnitude;
			if (magnitude2 > 0f)
			{
				forward = desiredVelocity / magnitude2;
			}
		}
		Quaternion b = Quaternion.LookRotation(forward);
		if (navAgent.speed > 0f)
		{
			transform.rotation = Quaternion.Lerp(transform.rotation, b, Mathf.Clamp(turnspeed * navAgent.speed / Quaternion.Angle(transform.rotation, b) * Time.deltaTime, 0f, 1f));
		}
		else
		{
			transform.rotation = Quaternion.Lerp(transform.rotation, b, Mathf.Clamp(turnspeed / Quaternion.Angle(transform.rotation, b) * Time.deltaTime, 0f, 1f));
		}
	}

	public static void UpdateFacingForward(Transform transform, NavMeshAgent navAgent, float turnspeed = 3600f)
	{
		Vector3 desiredVelocity = navAgent.desiredVelocity;
		desiredVelocity.y = 0f;
		float magnitude = desiredVelocity.magnitude;
		if (!(magnitude <= 0f))
		{
			Vector3 facingDir = desiredVelocity / magnitude;
			UpdateFacingDir(transform, navAgent, facingDir, turnspeed);
		}
	}

	public static void UpdateFacingPos(Transform transform, NavMeshAgent navAgent, Vector3 facingPos, float turnspeed = 3600f)
	{
		Vector3 facingDir = facingPos - transform.position;
		facingDir.y = 0f;
		facingDir.Normalize();
		UpdateFacingDir(transform, navAgent, facingDir, turnspeed);
	}

	public static void UpdateFacingDir(Transform transform, NavMeshAgent navAgent, Vector3 facingDir, float turnspeed = 3600f)
	{
		Quaternion b = Quaternion.LookRotation(facingDir);
		transform.rotation = Quaternion.Lerp(transform.rotation, b, Mathf.Clamp(turnspeed * navAgent.speed / Quaternion.Angle(transform.rotation, b) * Time.deltaTime, 0f, 1f));
	}
}
