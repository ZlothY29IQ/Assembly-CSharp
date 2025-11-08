using System;
using GorillaExtensions;
using GT_CustomMapSupportRuntime;
using UnityEngine;

public class CustomMapsSearchBehaviour : CustomMapsBehaviourBase
{
	private const float SEARCH_COOLDOWN = 0.1f;

	private CustomMapsAIBehaviourController controller;

	private float sightDist;

	private float sightDistSq;

	private Vector3 sightOffset;

	private float sightFOV;

	private float sightMinDot;

	private float lastSearchTime;

	public CustomMapsSearchBehaviour(CustomMapsAIBehaviourController AIcontroller, AIAgent agentSettings)
	{
		sightOffset = agentSettings.sightOffset;
		sightDist = agentSettings.sightDist;
		sightDistSq = sightDist * sightDist;
		sightFOV = agentSettings.sightFOV;
		sightMinDot = Mathf.Cos(sightFOV / 2f * (MathF.PI / 180f));
		controller = AIcontroller;
	}

	public override bool CanExecute()
	{
		if (controller.IsNull())
		{
			return false;
		}
		return true;
	}

	public override bool CanContinueExecuting()
	{
		if (!CanExecute())
		{
			return false;
		}
		return controller.TargetPlayer == null;
	}

	public override void Execute()
	{
		if (!(Time.time < lastSearchTime + 0.1f))
		{
			lastSearchTime = Time.time;
			Vector3 sourcePos = controller.transform.position + controller.transform.TransformVector(sightOffset);
			controller.SetTarget(controller.FindBestTarget(sourcePos, sightDist, sightDistSq, sightMinDot));
		}
	}

	public override void NetExecute()
	{
	}

	public override void ResetBehavior()
	{
	}

	public override void OnTriggerEnter(Collider otherCollider)
	{
	}
}
