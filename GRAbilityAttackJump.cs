using System;
using UnityEngine;

[Serializable]
public class GRAbilityAttackJump : GRAbilityBase
{
	private enum State
	{
		Tell,
		Jump,
		Return,
		Done
	}

	public float duration;

	public float jumpTime;

	public float attackLandTime;

	public float attackReturnTime;

	public bool doReturnPhase = true;

	public float jumpLengthScale = 1f;

	public string animName;

	public float animSpeed;

	public float maxTurnSpeed;

	public string jumpAnimName;

	public AbilitySound jumpSound;

	public GameObject damageTrigger;

	private Transform target;

	private State state;

	public Vector3 targetPos;

	public Vector3 initialPos;

	public Vector3 initialVel;

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
		startTime = Time.timeAsDouble;
		if (damageTrigger != null)
		{
			damageTrigger.SetActive(value: false);
		}
		agent.SetIsPathing(isPathing: false, ignoreRigiBody: true);
		agent.SetDisableNetworkSync(disable: true);
		state = State.Tell;
	}

	protected override void OnStop()
	{
		agent.SetIsPathing(isPathing: true, ignoreRigiBody: true);
		agent.SetDisableNetworkSync(disable: false);
		if (damageTrigger != null)
		{
			damageTrigger.SetActive(value: false);
		}
	}

	public override bool IsDone()
	{
		return Time.timeAsDouble - startTime >= (double)duration;
	}

	protected override void OnUpdateShared(float dt)
	{
		double num = (double)(float)Time.timeAsDouble - startTime;
		switch (state)
		{
		case State.Tell:
			if (num > (double)jumpTime)
			{
				targetPos = agent.transform.position + agent.transform.forward * 0.5f;
				if (target != null)
				{
					Vector3 vector = target.transform.position - agent.transform.position;
					targetPos = agent.transform.position + vector * jumpLengthScale;
					targetPos.y = target.transform.position.y;
				}
				float b = attackLandTime - jumpTime;
				b = Mathf.Max(0.1f, b);
				initialPos = agent.transform.position;
				Vector3 vector2 = targetPos - initialPos;
				float y = vector2.y;
				vector2.y = 0f;
				float num3 = b;
				float y2 = 0f;
				if (num3 > 0f)
				{
					y2 = (y - 0.5f * Physics.gravity.y * num3 * num3) / num3;
				}
				initialVel = vector2 / b;
				initialVel.y = y2;
				if (damageTrigger != null)
				{
					damageTrigger.SetActive(value: true);
				}
				PlayAnim(jumpAnimName, 0.1f, animSpeed);
				jumpSound.Play(null);
				state = State.Jump;
			}
			break;
		case State.Jump:
		{
			float num4 = (float)(num - (double)jumpTime);
			Vector3 position2 = initialPos + initialVel * num4 + 0.5f * Physics.gravity * num4 * num4;
			root.position = position2;
			if (num > (double)attackLandTime)
			{
				if (damageTrigger != null)
				{
					damageTrigger.SetActive(value: false);
				}
				if (doReturnPhase)
				{
					float b2 = attackReturnTime - attackLandTime;
					b2 = Mathf.Max(0.1f, b2);
					Vector3 vector3 = initialPos;
					initialPos = agent.transform.position;
					initialVel = (vector3 - initialPos) / b2;
					state = State.Return;
				}
				else
				{
					state = State.Done;
				}
			}
			break;
		}
		case State.Return:
		{
			float num2 = (float)(num - (double)attackLandTime);
			Vector3 position = initialPos + initialVel * num2;
			root.position = position;
			if (num > (double)attackReturnTime)
			{
				state = State.Done;
			}
			break;
		}
		}
		GameAgent.UpdateFacingTarget(root, agent.navAgent, target, maxTurnSpeed);
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
			}
		}
	}
}
