using System;
using CjLib;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.AI;

[Serializable]
public class GRAbilityJump : GRAbilityBase
{
	private Vector3 startPos;

	private Vector3 endPos;

	private Vector3 controlPoint;

	[ReadOnly]
	public float jumpTime;

	[ReadOnly]
	public float elapsedTime;

	private bool isActive;

	public AnimationData animationData;

	public float jumpSpeed = 3f;

	public AbilitySound soundJump;

	public override void Setup(GameAgent agent, Animation anim, AudioSource audioSource, Transform root, Transform head, GRSenseLineOfSight lineOfSight)
	{
		base.Setup(agent, anim, audioSource, root, head, lineOfSight);
		isActive = false;
	}

	public void SetupJump(Vector3 start, Vector3 end, float heightScale = 1f, float speedScale = 1f)
	{
		elapsedTime = 0f;
		startPos = start;
		endPos = end;
		float magnitude = (endPos - startPos).magnitude;
		controlPoint = (startPos + endPos) / 2f + new Vector3(0f, magnitude * heightScale, 0f);
		jumpTime = magnitude / (jumpSpeed * speedScale);
	}

	public void SetupJumpFromLinkData(OffMeshLinkData linkData)
	{
		if ((root.position - linkData.startPos).sqrMagnitude < (root.position - linkData.endPos).sqrMagnitude)
		{
			SetupJump(linkData.startPos, linkData.endPos);
		}
		else
		{
			SetupJump(linkData.endPos, linkData.startPos);
		}
	}

	protected override void OnStart()
	{
		elapsedTime = 0f;
		isActive = true;
		PlayAnim(animationData.animName, 0.05f, animationData.speed);
		agent.SetStopped(stopMovement: true);
		agent.SetDisableNetworkSync(disable: true);
		agent.pauseEntityThink = true;
		soundJump.Play(audioSource);
	}

	protected override void OnStop()
	{
		agent.navAgent.Warp(endPos);
		agent.navAgent.CompleteOffMeshLink();
		agent.SetStopped(stopMovement: false);
		isActive = false;
		agent.SetDisableNetworkSync(disable: false);
		agent.pauseEntityThink = false;
	}

	public override bool IsDone()
	{
		return elapsedTime >= jumpTime;
	}

	public bool IsActive()
	{
		return isActive;
	}

	protected override void OnUpdateShared(float dt)
	{
		if (GhostReactorManager.entityDebugEnabled)
		{
			DebugUtil.DrawLine(startPos, controlPoint, Color.green);
			DebugUtil.DrawLine(endPos, controlPoint, Color.green);
		}
		float t = ((jumpTime > 0f) ? Math.Clamp(elapsedTime / jumpTime, 0f, 1f) : 1f);
		Vector3 position = EvaluateQuadratic(startPos, controlPoint, endPos, t);
		root.position = position;
		if (rb != null)
		{
			rb.position = position;
		}
		elapsedTime += dt;
	}

	public static Vector3 EvaluateQuadratic(Vector3 p0, Vector3 p1, Vector3 p2, float t)
	{
		Vector3 a = Vector3.Lerp(p0, p1, t);
		Vector3 b = Vector3.Lerp(p1, p2, t);
		return Vector3.Lerp(a, b, t);
	}
}
