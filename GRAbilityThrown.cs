using System;
using UnityEngine;

[Serializable]
public class GRAbilityThrown : GRAbilityBase
{
	public GRAbilityIdle idleAbility;

	public override void Setup(GameAgent agent, Animation anim, AudioSource audioSource, Transform root, Transform head, GRSenseLineOfSight lineOfSight)
	{
		base.Setup(agent, anim, audioSource, root, head, lineOfSight);
		idleAbility.Setup(agent, anim, audioSource, root, head, lineOfSight);
	}

	protected override void OnStart()
	{
		agent.SetIsPathing(isPathing: false);
		idleAbility.Start();
	}

	protected override void OnStop()
	{
		idleAbility.Stop();
		agent.SetIsPathing(isPathing: true);
	}

	public override bool IsDone()
	{
		return idleAbility.IsDone();
	}

	protected override void OnUpdateAuthority(float dt)
	{
		idleAbility.UpdateAuthority(dt);
	}

	protected override void OnUpdateRemote(float dt)
	{
		idleAbility.UpdateRemote(dt);
	}
}
