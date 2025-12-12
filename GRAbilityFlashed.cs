using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GRAbilityFlashed : GRAbilityBase
{
	public List<AnimationData> flashAnimations;

	private int flashAnimationIndex;

	private double behaviorEndTime;

	private float stunTime;

	public override void Setup(GameAgent agent, Animation anim, AudioSource audioSource, Transform root, Transform head, GRSenseLineOfSight lineOfSight)
	{
		base.Setup(agent, anim, audioSource, root, head, lineOfSight);
	}

	public void SetStunTime(float time)
	{
		stunTime = time;
	}

	protected override void OnStart()
	{
		if (flashAnimations.Count > 0)
		{
			flashAnimationIndex = AbilityHelperFunctions.RandomRangeUnique(0, flashAnimations.Count, flashAnimationIndex);
			PlayAnim(flashAnimations[flashAnimationIndex].animName, 0.1f, flashAnimations[flashAnimationIndex].speed);
			behaviorEndTime = Time.timeAsDouble + (double)flashAnimations[flashAnimationIndex].duration + (double)stunTime;
		}
		else
		{
			PlayAnim("GREnemyFlashReaction01", 0.1f, 1f);
			behaviorEndTime = Time.timeAsDouble + 0.5 + (double)stunTime;
		}
		agent.SetIsPathing(isPathing: false, ignoreRigiBody: true);
		agent.SetDisableNetworkSync(disable: true);
	}

	protected override void OnStop()
	{
		agent.SetIsPathing(isPathing: true, ignoreRigiBody: true);
		agent.SetDisableNetworkSync(disable: false);
	}

	public override bool IsDone()
	{
		return Time.timeAsDouble >= behaviorEndTime;
	}
}
