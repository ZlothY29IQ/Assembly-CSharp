using System;
using Unity.XR.CoreUtils;
using UnityEngine;

[Serializable]
public class GRAbilityIdle : GRAbilityBase
{
	public float duration;

	public string animName;

	public float animSpeed;

	public GameAbilityEvents events;

	[ReadOnly]
	public int animLoops;

	public override void Setup(GameAgent agent, Animation anim, AudioSource audioSource, Transform root, Transform head, GRSenseLineOfSight lineOfSight)
	{
		base.Setup(agent, anim, audioSource, root, head, lineOfSight);
		animLoops = 0;
	}

	public override void Start()
	{
		base.Start();
		agent.navAgent.isStopped = true;
		PlayAnim(animName, 0.3f, animSpeed);
		animLoops = 0;
		events.Reset();
	}

	public override void Stop()
	{
		base.Stop();
		agent.navAgent.isStopped = false;
	}

	public override bool IsDone()
	{
		if ((double)duration > 0.0)
		{
			return Time.timeAsDouble >= startTime + (double)duration;
		}
		return false;
	}

	protected override void UpdateShared(float dt)
	{
		float abilityTime = (float)(Time.timeAsDouble - startTime);
		if (anim != null && anim[animName] != null)
		{
			if ((int)anim[animName].normalizedTime > animLoops)
			{
				events.Reset();
				animLoops = (int)anim[animName].normalizedTime;
			}
			abilityTime = anim[animName].time - anim[animName].length * (float)animLoops;
		}
		events.TryPlay(abilityTime, audioSource);
	}
}
