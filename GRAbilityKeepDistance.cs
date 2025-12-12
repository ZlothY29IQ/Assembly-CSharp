using System;
using CjLib;
using UnityEngine;
using UnityEngine.AI;

[Serializable]
public class GRAbilityKeepDistance : GRAbilityBase
{
	private NavMeshAgent navMeshAgent;

	private Transform target;

	public GRAbilityMoveToTarget moveAbility;

	public string idleAnimName;

	public AbilitySound idleSound;

	public float minBackupSpaceRequired = 0.5f;

	public float maxDistanceFromTarget = -1f;

	private bool defaultUpdateRotation;

	private static Quaternion[] rotations = new Quaternion[10]
	{
		Quaternion.Euler(0f, 0f, 0f),
		Quaternion.Euler(0f, 30f, 0f),
		Quaternion.Euler(0f, -30f, 0f),
		Quaternion.Euler(0f, 60f, 0f),
		Quaternion.Euler(0f, -60f, 0f),
		Quaternion.Euler(0f, 90f, 0f),
		Quaternion.Euler(0f, -90f, 0f),
		Quaternion.Euler(0f, 135f, 0f),
		Quaternion.Euler(0f, -135f, 0f),
		Quaternion.Euler(0f, 180f, 0f)
	};

	public override void Setup(GameAgent agent, Animation anim, AudioSource audioSource, Transform root, Transform head, GRSenseLineOfSight lineOfSight)
	{
		base.Setup(agent, anim, audioSource, root, head, lineOfSight);
		navMeshAgent = agent.GetComponent<NavMeshAgent>();
		moveAbility.Setup(agent, anim, audioSource, root, head, lineOfSight);
		if ((bool)attributes && moveAbility.moveSpeed == 0f)
		{
			moveAbility.moveSpeed = attributes.CalculateFinalFloatValueForAttribute(GRAttributeType.BackupSpeed);
		}
	}

	protected override void OnStart()
	{
		if (target != null)
		{
			Vector3 vector = agent.transform.position - target.position;
			if (maxDistanceFromTarget > 0f && vector.magnitude > maxDistanceFromTarget)
			{
				agent.SetStopped(stopMovement: true);
				PlayAnim(idleAnimName, 0.5f, 1f);
				idleSound.Play(null);
			}
			else
			{
				moveAbility.Start();
			}
		}
		else
		{
			moveAbility.Start();
		}
		agent.SetIsPathing(isPathing: true, ignoreRigiBody: true);
		Vector3 targetPos = PickBackupDestination();
		moveAbility.SetTargetPos(targetPos);
		if (navMeshAgent != null)
		{
			defaultUpdateRotation = navMeshAgent.updateRotation;
			navMeshAgent.updateRotation = false;
		}
	}

	protected override void OnStop()
	{
		moveAbility.Stop();
		idleSound.Stop();
		if (navMeshAgent != null)
		{
			navMeshAgent.updateRotation = defaultUpdateRotation;
		}
		agent.SetStopped(stopMovement: false);
	}

	public override bool IsDone()
	{
		return false;
	}

	public void SetTargetPlayer(NetPlayer targetPlayer)
	{
		target = null;
		if (targetPlayer != null)
		{
			GRPlayer gRPlayer = GRPlayer.Get(targetPlayer.ActorNumber);
			if (gRPlayer != null && gRPlayer.State == GRPlayer.GRPlayerState.Alive)
			{
				target = gRPlayer.transform;
				moveAbility.SetLookAtTarget(target);
			}
		}
	}

	protected override void OnThink(float dt)
	{
		Vector3 vector = agent.transform.position - target.position;
		if (moveAbility.IsDone())
		{
			if (maxDistanceFromTarget < 0f || vector.magnitude < maxDistanceFromTarget)
			{
				if (navMeshAgent != null && navMeshAgent.isOnNavMesh && navMeshAgent.isStopped)
				{
					idleSound.Stop();
					moveAbility.Start();
				}
				Vector3 targetPos = PickBackupDestination();
				moveAbility.SetTargetPos(targetPos);
			}
		}
		else if (maxDistanceFromTarget > 0f && vector.magnitude > maxDistanceFromTarget)
		{
			moveAbility.SetTargetPos(root.position);
			moveAbility.Stop();
			agent.SetStopped(stopMovement: true);
			PlayAnim(idleAnimName, 0.5f, 1f);
			idleSound.Play(null);
		}
	}

	private Vector3 PickBackupDestination()
	{
		Vector3 position = agent.transform.position;
		if (target == null)
		{
			return position;
		}
		if (NavMesh.SamplePosition(position, out var hit, 1f, walkableArea))
		{
			Vector3 position2 = hit.position;
			Vector3 vector = agent.transform.position - target.position;
			vector.y = 0f;
			Vector3 normalized = vector.normalized;
			for (int i = 0; i < rotations.Length; i++)
			{
				Vector3 vector2 = rotations[i] * normalized;
				float num = 2f;
				Vector3 vector3 = position2 + vector2 * num;
				if (NavMesh.Raycast(position2, vector3, out var hit2, walkableArea))
				{
					if (hit2.distance < minBackupSpaceRequired)
					{
						continue;
					}
					vector3 = hit2.position;
				}
				if (NavMesh.SamplePosition(vector3, out var hit3, 1f, walkableArea))
				{
					Vector3 position3 = hit3.position;
					Vector3 vector4 = position3 - target.position;
					vector4.y = 0f;
					if (vector4.sqrMagnitude > vector.sqrMagnitude)
					{
						return position3;
					}
				}
			}
		}
		return position;
	}

	protected override void OnUpdateShared(float dt)
	{
		if (GhostReactorManager.entityDebugEnabled)
		{
			DebugUtil.DrawLine(root.position, moveAbility.GetTargetPos(), Color.magenta);
		}
	}

	protected override void OnUpdateAuthority(float dt)
	{
		moveAbility.UpdateAuthority(dt);
	}

	protected override void OnUpdateRemote(float dt)
	{
		moveAbility.UpdateRemote(dt);
	}
}
