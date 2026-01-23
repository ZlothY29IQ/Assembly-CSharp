using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GRAbilityAttackSimple : GRAbilityBase
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

	public bool allowMovement;

	public AnimationData tellAnimData;

	public AnimationData attackAnimData;

	public AnimationData outroAnimData;

	public AbilitySound soundTell;

	public AbilitySound soundAttack;

	public AbilitySound soundOutro;

	private float timeMult = 1f;

	private State state;

	public float maxTurnSpeed;

	public List<GameObject> damageTrigger;

	private string animNameString;

	public GameAbilityEvents events;

	public bool adjustByAnimationSpeed;

	public override void Setup(GameAgent agent, Animation anim, AudioSource audioSource, Transform root, Transform head, GRSenseLineOfSight lineOfSight)
	{
		base.Setup(agent, anim, audioSource, root, head, lineOfSight);
		EnableList(damageTrigger, enable: false);
	}

	protected override void OnStart()
	{
		if ((double)(tellDuration * timeMult) > 0.0)
		{
			PlayState(State.Tell, tellAnimData, soundTell, damageEnabled: false);
		}
		else
		{
			PlayState(State.Attack, attackAnimData, soundAttack, damageEnabled: true);
		}
		if (!allowMovement)
		{
			agent.SetIsPathing(isPathing: false, ignoreRigiBody: true);
			agent.SetDisableNetworkSync(disable: true);
		}
		events.Reset();
		events.OnAbilityStart(GetAbilityTime(Time.timeAsDouble), audioSource);
	}

	protected override void OnStop()
	{
		if (!allowMovement)
		{
			agent.SetIsPathing(isPathing: true, ignoreRigiBody: true);
			agent.SetDisableNetworkSync(disable: false);
		}
		EnableList(damageTrigger, enable: false);
		events.OnAbilityStop(GetAbilityTime(Time.timeAsDouble), audioSource);
	}

	private void PlayState(State newState, AnimationData animData, AbilitySound sound, bool damageEnabled)
	{
		if (!string.IsNullOrEmpty(animData.animName))
		{
			PlayAnim(animData.animName, 0.1f, animData.speed);
			animNameString = animData.animName;
			timeMult = ((adjustByAnimationSpeed && !Mathf.Approximately(animData.speed, 0f)) ? (1f / animData.speed) : 1f);
		}
		sound.soundSelectMode = AbilitySound.SoundSelectMode.Random;
		sound.Play(null);
		EnableList(damageTrigger, damageEnabled);
		state = newState;
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
			if (num > tellDuration * timeMult)
			{
				PlayState(State.Attack, attackAnimData, soundAttack, damageEnabled: true);
			}
			break;
		case State.Attack:
			if (num > (tellDuration + attackDuration) * timeMult)
			{
				PlayState(State.FollowThrough, outroAnimData, soundOutro, damageEnabled: false);
			}
			break;
		case State.FollowThrough:
			if (num >= duration * timeMult)
			{
				state = State.Done;
			}
			break;
		}
		events.TryPlay(num / timeMult, audioSource);
	}

	public void SetTargetPlayer(NetPlayer targetPlayer)
	{
	}

	public string GetAnimName()
	{
		return animNameString;
	}

	public void EnableList(List<GameObject> objs, bool enable)
	{
		for (int i = 0; i < objs.Count; i++)
		{
			if (objs[i] != null)
			{
				objs[i].SetActive(enable);
			}
		}
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
