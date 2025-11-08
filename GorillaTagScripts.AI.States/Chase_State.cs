using UnityEngine;
using UnityEngine.AI;

namespace GorillaTagScripts.AI.States;

public class Chase_State : IState
{
	private AIEntity entity;

	private NavMeshAgent agent;

	public bool chaseOver;

	public Transform FollowTarget { get; set; }

	public Chase_State(AIEntity entity)
	{
		this.entity = entity;
		agent = this.entity.navMeshAgent;
	}

	public void Tick()
	{
		agent.SetDestination(FollowTarget.position);
		if (agent.remainingDistance < entity.attackDistance)
		{
			chaseOver = true;
		}
	}

	public void OnEnter()
	{
		chaseOver = false;
		Debug.Log("Current State: " + typeof(Chase_State));
	}

	public void OnExit()
	{
		chaseOver = true;
	}
}
