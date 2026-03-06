using System;
using GorillaExtensions;
using GorillaGameModes;
using GT_CustomMapSupportRuntime;
using UnityEngine;

public class CustomMapsAttackBehaviour : CustomMapsBehaviourBase
{
	private enum State
	{
		Idle,
		Attacking
	}

	private CustomMapsAIBehaviourController controller;

	private State state;

	private AttackType attackType;

	private float attackDist;

	private float attackDistSq;

	private bool stopMovingToAttack;

	private bool useColliders;

	private float damageAmount;

	private Vector3 sightOffset;

	private float sightFOV;

	private float sightMinDot;

	private string attackAnimName;

	private float timeBetweenAttacks;

	private float damageDelayAfterPlayingAnimation;

	private float animBlendTime;

	private float startTime;

	private float turnSpeed;

	private float lastAttackTime;

	public CustomMapsAttackBehaviour(CustomMapsAIBehaviourController AIController, AIAgent agentSettings)
	{
		attackType = agentSettings.attackType;
		attackDist = agentSettings.attackDist;
		attackDistSq = attackDist * attackDist;
		stopMovingToAttack = agentSettings.stopMovingToAttack;
		useColliders = agentSettings.useColliders;
		damageDelayAfterPlayingAnimation = agentSettings.damageDelayAfterPlayingAnim;
		damageAmount = agentSettings.damageAmount;
		attackAnimName = agentSettings.attackAnimName;
		sightOffset = agentSettings.sightOffset;
		sightFOV = agentSettings.sightFOV;
		sightMinDot = Mathf.Cos(sightFOV / 2f * (MathF.PI / 180f));
		controller = AIController;
		animBlendTime = agentSettings.animBlendTime;
		turnSpeed = agentSettings.turnSpeed * 10f;
		timeBetweenAttacks = agentSettings.timeBetweenAttacks;
		controller.attributes.AddAttribute(GRAttributeType.PlayerDamage, damageAmount);
		state = State.Idle;
	}

	public override bool CanExecute()
	{
		if (controller.IsNull() || controller.TargetPlayer.IsNull())
		{
			return false;
		}
		if (!IsTargetInAttackRange())
		{
			return false;
		}
		if (!IsTargetVisible())
		{
			return false;
		}
		return true;
	}

	private bool IsTargetVisible()
	{
		Vector3 startPos = controller.transform.position + controller.transform.TransformVector(sightOffset);
		return controller.IsTargetVisible(startPos, controller.TargetPlayer, attackDist);
	}

	private bool IsTargetInAttackRange(GRPlayer target = null)
	{
		if (target.IsNull() && controller.TargetPlayer.IsNull())
		{
			return false;
		}
		Vector3 toTarget;
		if (target.IsNotNull())
		{
			return controller.IsTargetInRange(controller.transform.position, target, attackDistSq, out toTarget);
		}
		Vector3 toTarget2;
		return controller.IsTargetInRange(controller.transform.position, controller.TargetPlayer, attackDistSq, out toTarget2);
	}

	public override bool CanContinueExecuting()
	{
		if (state != State.Idle && controller.IsAnimationPlaying(attackAnimName))
		{
			return true;
		}
		if (controller.IsNull() || controller.TargetPlayer.IsNull())
		{
			return false;
		}
		if (!controller.IsTargetable(controller.TargetPlayer))
		{
			controller.ClearTarget();
			return false;
		}
		return CanExecute();
	}

	public override void Execute()
	{
		if (!controller.IsNull())
		{
			if (stopMovingToAttack)
			{
				controller.StopMoving();
			}
			FaceTarget();
			controller.agent.RequestBehaviorChange(2);
		}
	}

	public override void NetExecute()
	{
		if (controller.IsNull())
		{
			return;
		}
		if (state == State.Attacking && !useColliders && startTime > lastAttackTime && Time.time > startTime + damageDelayAfterPlayingAnimation)
		{
			TriggerAttack();
		}
		if (controller.IsAnimationPlaying(attackAnimName))
		{
			return;
		}
		switch (state)
		{
		case State.Attacking:
			if (Time.time < startTime + timeBetweenAttacks)
			{
				state = State.Idle;
				break;
			}
			startTime = Time.time;
			controller.PlayAnimation(attackAnimName, animBlendTime);
			break;
		case State.Idle:
			if (!(Time.time < startTime + timeBetweenAttacks))
			{
				startTime = Time.time;
				state = State.Attacking;
				controller.PlayAnimation(attackAnimName, animBlendTime);
			}
			break;
		}
	}

	public override void ResetBehavior()
	{
		state = State.Idle;
	}

	private void FaceTarget()
	{
		if (!controller.TargetPlayer.IsNull())
		{
			GameAgent.UpdateFacingTarget(controller.transform, controller.agent.navAgent, controller.TargetPlayer.transform, turnSpeed);
		}
	}

	public override void OnTriggerEnter(Collider otherCollider)
	{
		if (useColliders && !(Time.time < lastAttackTime + timeBetweenAttacks) && state == State.Attacking)
		{
			GRPlayer componentInParent = otherCollider.GetComponentInParent<GRPlayer>();
			if (!componentInParent.IsNull() && (!componentInParent.MyRig.IsNotNull() || componentInParent.MyRig.isLocal) && componentInParent.State != GRPlayer.GRPlayerState.Ghost)
			{
				TriggerAttack(componentInParent);
			}
		}
	}

	private void TriggerAttack(GRPlayer targetPlayer = null)
	{
		lastAttackTime = Time.time;
		GRPlayer gRPlayer = ((targetPlayer != null) ? targetPlayer : (controller.entity.IsAuthority() ? controller.TargetPlayer : null));
		if (!controller.entity.IsAuthority() && gRPlayer == null)
		{
			Vector3 sourcePos = controller.transform.position + controller.transform.TransformVector(sightOffset);
			gRPlayer = controller.FindBestTarget(sourcePos, attackDist, attackDistSq, sightMinDot);
		}
		if (gRPlayer == null || !gRPlayer.MyRig.isLocal || (controller.entity.IsAuthority() && !IsTargetInAttackRange(gRPlayer)))
		{
			return;
		}
		switch (attackType)
		{
		case AttackType.Tag:
			if (GameMode.ActiveGameMode.GameType() != GameModeType.Custom)
			{
				GameMode.ReportHit();
			}
			else
			{
				CustomGameMode.TaggedByAI(controller.entity, gRPlayer.MyRig.OwningNetPlayer.ActorNumber);
			}
			break;
		case AttackType.UseGT:
			CustomMapsGameManager.instance.OnPlayerHit(controller.entity.id, gRPlayer, controller.transform.position);
			break;
		case AttackType.UseLuau:
			CustomGameMode.OnPlayerHit(controller.entity, gRPlayer.MyRig.OwningNetPlayer.ActorNumber, damageAmount);
			break;
		}
	}
}
