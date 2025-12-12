using System;
using UnityEngine;

[Serializable]
public class GRAbilityAttackLatchOn : GRAbilityBase
{
	public float duration;

	public float attackMoveSpeed;

	public float tellDuration;

	public float tellMoveSpeed;

	public string animName;

	public float animSpeed;

	public float maxTurnSpeed;

	public Transform target;

	public GameObject damageTrigger;

	public override void Setup(GameAgent agent, Animation anim, AudioSource audioSource, Transform root, Transform head, GRSenseLineOfSight lineOfSight)
	{
		base.Setup(agent, anim, audioSource, root, head, lineOfSight);
		target = null;
		if (damageTrigger != null)
		{
			damageTrigger.SetActive(value: false);
		}
	}

	protected override void OnStart()
	{
		PlayAnim(animName, 0.1f, animSpeed);
		agent.SetSpeed(tellMoveSpeed);
		startTime = Time.timeAsDouble;
		if (damageTrigger != null)
		{
			damageTrigger.SetActive(value: false);
		}
	}

	protected override void OnStop()
	{
		agent.transform.SetParent(null);
		agent.SetIsPathing(isPathing: true, ignoreRigiBody: true);
		if (damageTrigger != null)
		{
			damageTrigger.SetActive(value: false);
		}
	}

	public override bool IsDone()
	{
		return Time.timeAsDouble - startTime >= (double)duration;
	}

	protected override void OnUpdateAuthority(float dt)
	{
		UpdateNavSpeed();
		GameAgent.UpdateFacingTarget(root, agent.navAgent, target, maxTurnSpeed);
	}

	protected override void OnUpdateRemote(float dt)
	{
		UpdateNavSpeed();
	}

	private void UpdateNavSpeed()
	{
		if (Time.timeAsDouble - startTime > (double)tellDuration)
		{
			agent.SetSpeed(attackMoveSpeed);
			agent.SetVelocity(agent.navAgent.velocity.normalized * attackMoveSpeed);
			if (damageTrigger != null)
			{
				damageTrigger.SetActive(value: true);
			}
		}
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
				agent.transform.SetParent(gRPlayer.attachEnemy);
				agent.transform.localPosition = Vector3.zero;
				agent.transform.localRotation = Quaternion.identity;
				agent.SetIsPathing(isPathing: false, ignoreRigiBody: true);
			}
		}
	}
}
