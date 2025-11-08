using System;
using UnityEngine;

[Serializable]
public class GRAbilityGrabbed : GRAbilityBase
{
	public GRAbilityIdle idleAbility;

	public override void Setup(GameAgent agent, Animation anim, AudioSource audioSource, Transform root, Transform head, GRSenseLineOfSight lineOfSight)
	{
		base.Setup(agent, anim, audioSource, root, head, lineOfSight);
		idleAbility.Setup(agent, anim, audioSource, root, head, lineOfSight);
	}

	public override void Start()
	{
		base.Start();
		agent.SetIsPathing(isPathing: false, ignoreRigiBody: true);
		idleAbility.Start();
	}

	public override void Stop()
	{
		idleAbility.Stop();
		agent.SetIsPathing(isPathing: true, ignoreRigiBody: true);
	}

	public override bool IsDone()
	{
		return idleAbility.IsDone();
	}

	public override void Update(float dt)
	{
		idleAbility.Update(dt);
	}
}
