using GorillaExtensions;
using GT_CustomMapSupportRuntime;
using UnityEngine;
using UnityEngine.AI;

public class CustomMapsChaseBehaviour : CustomMapsBehaviourBase
{
	private NavMeshAgent navMeshAgent;

	private CustomMapsAIBehaviourController controller;

	private float loseSightDist;

	private float loseSightDistSq;

	private Vector3 sightOffset;

	private bool rememberLoseSightPos;

	private float stopDistSq;

	private bool isChasing;

	public CustomMapsChaseBehaviour(CustomMapsAIBehaviourController AIController, AIAgent agentSettings)
	{
		sightOffset = agentSettings.sightOffset;
		rememberLoseSightPos = agentSettings.rememberLoseSightPosition;
		loseSightDist = agentSettings.loseSightDist;
		loseSightDistSq = loseSightDist * loseSightDist;
		stopDistSq = agentSettings.stopDist * agentSettings.stopDist;
		controller = AIController;
	}

	public override bool CanExecute()
	{
		if (controller.IsNull())
		{
			return false;
		}
		if (controller.TargetPlayer.IsNull())
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
		if (IsTargetInChaseRange(out var withinStopDist))
		{
			if (withinStopDist)
			{
				return false;
			}
			return true;
		}
		if (!controller.IsTargetable(controller.TargetPlayer))
		{
			controller.StopMoving();
		}
		controller.ClearTarget();
		return false;
	}

	public override void Execute()
	{
		if (!IsTargetInChaseRange(out var withinStopDist))
		{
			controller.ClearTarget();
			isChasing = false;
			if (!rememberLoseSightPos)
			{
				controller.StopMoving();
			}
		}
		else if (!IsTargetVisible())
		{
			controller.ClearTarget();
			isChasing = false;
			if (!rememberLoseSightPos)
			{
				controller.StopMoving();
			}
		}
		else if (withinStopDist && isChasing)
		{
			isChasing = false;
			controller.StopMoving();
		}
		else
		{
			isChasing = true;
			controller.RequestDestination(controller.TargetPlayer.transform.position);
		}
	}

	private bool IsTargetVisible()
	{
		Vector3 startPos = controller.transform.position + controller.transform.TransformVector(sightOffset);
		return controller.IsTargetVisible(startPos, controller.TargetPlayer, loseSightDist);
	}

	private bool IsTargetInChaseRange(out bool withinStopDist)
	{
		withinStopDist = false;
		if (!controller.IsTargetInRange(controller.transform.position, controller.TargetPlayer, loseSightDistSq, out var toTarget))
		{
			return false;
		}
		if (toTarget.sqrMagnitude < stopDistSq)
		{
			withinStopDist = true;
		}
		return true;
	}

	public override void NetExecute()
	{
	}

	public override void ResetBehavior()
	{
		isChasing = false;
	}

	public override void OnTriggerEnter(Collider otherCollider)
	{
	}
}
