using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GRAbilityStagger : GRAbilityBase
{
	private float duration;

	public List<AnimationData> animData;

	private int lastAnimIndex = -1;

	private string animNameString;

	public GRAbilityInterpolatedMovement staggerMovement;

	private float stunTime;

	public void SetStunTime(float time)
	{
		stunTime = time;
	}

	public void SetStaggerVelocity(Vector3 vel)
	{
		float magnitude = vel.magnitude;
		if (magnitude > 0f)
		{
			Vector3 vector = vel / magnitude;
			vector.y = 0f;
			vel = vector * magnitude;
		}
		staggerMovement.InitFromVelocityAndDuration(vel, duration);
	}

	public override void Setup(GameAgent agent, Animation anim, AudioSource audioSource, Transform root, Transform head, GRSenseLineOfSight lineOfSight)
	{
		base.Setup(agent, anim, audioSource, root, head, lineOfSight);
		staggerMovement.Setup(root);
		staggerMovement.interpolationType = GRAbilityInterpolatedMovement.InterpType.EaseOut;
	}

	protected override void OnStart()
	{
		if (animData.Count > 0)
		{
			lastAnimIndex = AbilityHelperFunctions.RandomRangeUnique(0, animData.Count, lastAnimIndex);
			duration = animData[lastAnimIndex].duration + stunTime;
			PlayAnim(animData[lastAnimIndex].animName, 0.1f, animData[lastAnimIndex].speed);
			animNameString = animData[lastAnimIndex].animName;
		}
		else
		{
			duration = 0.5f + stunTime;
		}
		agent.SetIsPathing(isPathing: false, ignoreRigiBody: true);
		agent.SetDisableNetworkSync(disable: true);
		staggerMovement.InitFromVelocityAndDuration(staggerMovement.velocity, duration);
		staggerMovement.Start();
	}

	protected override void OnStop()
	{
		agent.SetIsPathing(isPathing: true, ignoreRigiBody: true);
		agent.SetDisableNetworkSync(disable: false);
	}

	public override bool IsDone()
	{
		return staggerMovement.IsDone();
	}

	protected override void OnUpdateShared(float dt)
	{
		staggerMovement.Update(dt);
	}

	public string GetAnimName()
	{
		return animNameString;
	}
}
