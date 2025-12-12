using System;
using UnityEngine;

[Serializable]
public class GRAbilityMoveToTarget : GRAbilityBase
{
	public float moveSpeed;

	public string animName;

	public float animSpeed = 1f;

	public float maxTurnSpeed = 360f;

	public AbilitySound movementSound;

	private Vector3 targetPos;

	private Transform target;

	private Transform lookAtTarget;

	public override void Setup(GameAgent agent, Animation anim, AudioSource audioSource, Transform root, Transform head, GRSenseLineOfSight lineOfSight)
	{
		base.Setup(agent, anim, audioSource, root, head, lineOfSight);
		target = null;
		targetPos = agent.transform.position;
	}

	protected override void OnStart()
	{
		PlayAnim(animName, 0.3f, animSpeed);
		if ((bool)attributes && moveSpeed == 0f)
		{
			moveSpeed = attributes.CalculateFinalFloatValueForAttribute(GRAttributeType.PatrolSpeed);
		}
		agent.navAgent.speed = moveSpeed;
		targetPos = agent.transform.position;
		movementSound.Play(null);
	}

	protected override void OnStop()
	{
		movementSound.Stop();
	}

	public override bool IsDone()
	{
		return (targetPos - root.position).sqrMagnitude < 0.25f;
	}

	protected override void OnUpdateShared(float dt)
	{
		if (target != null)
		{
			targetPos = target.position;
			agent.RequestDestination(targetPos);
		}
		Transform transform = ((lookAtTarget != null) ? lookAtTarget : target);
		GameAgent.UpdateFacingTarget(root, agent.navAgent, transform, maxTurnSpeed);
	}

	public void SetTarget(Transform transform)
	{
		target = transform;
	}

	public void SetTargetPos(Vector3 targetPos)
	{
		this.targetPos = targetPos;
		agent.RequestDestination(targetPos);
	}

	public Vector3 GetTargetPos()
	{
		return targetPos;
	}

	public void SetLookAtTarget(Transform transform)
	{
		lookAtTarget = transform;
	}
}
