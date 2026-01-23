using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[Serializable]
public class GRAbilityAttackLaser : GRAbilityBase
{
	private enum State
	{
		Tell,
		Attack,
		FollowThrough,
		Done
	}

	public float duration;

	public float tellDuration;

	public float attackDuration;

	public float coolDown;

	public float range;

	public float attackMoveSpeed;

	public bool doNotFaceTarget;

	public List<AnimationData> animData;

	public AbilitySound soundAttack;

	public float maxLaserRange;

	public Transform[] laserOrigins;

	public Monkeye_LazerFX tellLaserFx;

	public Monkeye_LazerFX laserFx;

	private State state;

	public float maxTurnSpeed;

	public GameObject damageTrigger;

	public CapsuleCollider damageCollider;

	private Transform target;

	private string animNameString;

	private int lastAnimIndex = -1;

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
		if (animData.Count > 0)
		{
			lastAnimIndex = AbilityHelperFunctions.RandomRangeUnique(0, animData.Count, lastAnimIndex);
			duration = animData[lastAnimIndex].duration;
			PlayAnim(animData[lastAnimIndex].animName, 0.1f, animData[lastAnimIndex].speed);
			animNameString = animData[lastAnimIndex].animName;
		}
		else
		{
			duration = 0.5f;
		}
		soundAttack.soundSelectMode = AbilitySound.SoundSelectMode.Random;
		soundAttack.Play(null);
		agent.SetIsPathing(isPathing: false, ignoreRigiBody: true);
		agent.SetDisableNetworkSync(disable: true);
		if (damageTrigger != null)
		{
			damageTrigger.SetActive(value: false);
		}
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
		if (laserFx != null)
		{
			laserFx.DisableLazer();
		}
		if (tellLaserFx != null)
		{
			tellLaserFx.DisableLazer();
		}
	}

	public override bool IsDone()
	{
		return state == State.Done;
	}

	protected override void OnUpdateShared(float dt)
	{
		float num = (float)(Time.timeAsDouble - startTime);
		switch (state)
		{
		case State.Tell:
		{
			targetPos = root.position + root.transform.forward;
			if (target != null)
			{
				targetPos = target.position;
			}
			Vector3 position = head.position;
			Vector3 vector = targetPos - position;
			float num2 = vector.magnitude;
			if (num2 > 0f)
			{
				Vector3 vector2 = vector / num2;
				num2 = Mathf.Min(maxLaserRange, num2);
				targetPos = position + vector2 * num2;
			}
			if (!doNotFaceTarget)
			{
				GameAgent.UpdateFacingTarget(root, agent.navAgent, target, maxTurnSpeed);
			}
			if (num > tellDuration)
			{
				state = State.Attack;
				if (damageCollider != null && laserOrigins.Length != 0)
				{
					damageCollider.transform.position = (position + targetPos) / 2f;
					damageCollider.height = num2;
					damageCollider.direction = 2;
					if (num2 > 0f)
					{
						damageCollider.transform.rotation = Quaternion.LookRotation(vector / num2);
					}
				}
				if (damageTrigger != null)
				{
					damageTrigger.SetActive(value: true);
				}
				if (tellLaserFx != null)
				{
					tellLaserFx.DisableLazer();
				}
				if (laserFx != null && target != null)
				{
					GamePlayer component = target.GetComponent<GamePlayer>();
					if (component != null && component.rig != null)
					{
						laserFx.EnableLazer(laserOrigins, targetPos);
					}
				}
				initialPos = root.position;
				initialVel = (targetPos - initialPos).normalized * attackMoveSpeed;
			}
			else if (tellLaserFx != null)
			{
				tellLaserFx.EnableLazer(laserOrigins, targetPos);
			}
			break;
		}
		case State.Attack:
		{
			float num3 = num - tellDuration;
			Vector3 sourcePosition = initialPos + initialVel * num3;
			if (NavMesh.SamplePosition(sourcePosition, out var hit, 0.5f, walkableArea))
			{
				sourcePosition = hit.position;
				if (NavMesh.Raycast(initialPos, sourcePosition, out hit, walkableArea))
				{
					sourcePosition = hit.position;
				}
				root.position = sourcePosition;
			}
			if (num > tellDuration + attackDuration)
			{
				if (damageTrigger != null)
				{
					damageTrigger.SetActive(value: false);
				}
				if (laserFx != null)
				{
					laserFx.DisableLazer();
				}
				state = State.FollowThrough;
			}
			break;
		}
		case State.FollowThrough:
			if (num >= duration)
			{
				state = State.Done;
			}
			break;
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
			}
		}
	}

	public string GetAnimName()
	{
		return animNameString;
	}

	public override bool IsCoolDownOver()
	{
		return IsCoolDownOver(coolDown);
	}

	public override float GetRange()
	{
		return range;
	}
}
