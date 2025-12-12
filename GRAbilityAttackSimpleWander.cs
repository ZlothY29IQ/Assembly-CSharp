using System;
using UnityEngine;

[Serializable]
public class GRAbilityAttackSimpleWander : GRAbilityBase
{
	public GRAbilityWander wander;

	public GRAbilityAttackSimple attack;

	public override void Setup(GameAgent agent, Animation anim, AudioSource audioSource, Transform root, Transform head, GRSenseLineOfSight lineOfSight)
	{
		base.Setup(agent, anim, audioSource, root, head, lineOfSight);
		wander.Setup(agent, anim, audioSource, root, head, lineOfSight);
		attack.Setup(agent, anim, audioSource, root, head, lineOfSight);
	}

	protected override void OnStart()
	{
		wander.Start();
		attack.Start();
	}

	protected override void OnStop()
	{
		wander.Stop();
		attack.Stop();
	}

	protected override void OnThink(float dt)
	{
		wander.Think(dt);
		attack.Think(dt);
	}

	protected override void OnUpdateAuthority(float dt)
	{
		wander.UpdateAuthority(dt);
		attack.UpdateAuthority(dt);
	}

	protected override void OnUpdateRemote(float dt)
	{
		wander.UpdateRemote(dt);
		attack.UpdateRemote(dt);
	}

	public override bool IsDone()
	{
		return attack.IsDone();
	}

	public override bool IsCoolDownOver()
	{
		return attack.IsCoolDownOver();
	}

	public override float GetRange()
	{
		return attack.GetRange();
	}
}
