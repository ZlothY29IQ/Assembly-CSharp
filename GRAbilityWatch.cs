using System;
using Unity.XR.CoreUtils;
using UnityEngine;

[Serializable]
public class GRAbilityWatch : GRAbilityBase
{
	public float duration;

	public string animName;

	public float animSpeed;

	public float maxTurnSpeed;

	private Transform target;

	[ReadOnly]
	public double endTime;

	public override void Setup(GameAgent agent, Animation anim, AudioSource audioSource, Transform root, Transform head, GRSenseLineOfSight lineOfSight)
	{
		base.Setup(agent, anim, audioSource, root, head, lineOfSight);
		target = null;
	}

	protected override void OnStart()
	{
		PlayAnim(animName, 0.1f, animSpeed);
		endTime = -1.0;
		if (duration > 0f)
		{
			endTime = Time.timeAsDouble + (double)duration;
		}
		agent.SetStopped(stopMovement: true);
	}

	protected override void OnStop()
	{
		agent.SetStopped(stopMovement: false);
	}

	public override bool IsDone()
	{
		if (endTime > 0.0)
		{
			return Time.timeAsDouble >= endTime;
		}
		return false;
	}

	protected override void OnUpdateShared(float dt)
	{
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
