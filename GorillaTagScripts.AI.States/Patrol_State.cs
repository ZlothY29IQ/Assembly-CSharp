using UnityEngine;
using UnityEngine.AI;

namespace GorillaTagScripts.AI.States;

public class Patrol_State : IState
{
	private AIEntity entity;

	private NavMeshAgent agent;

	public Patrol_State(AIEntity entity)
	{
		this.entity = entity;
		agent = this.entity.navMeshAgent;
	}

	public void Tick()
	{
		if (agent.remainingDistance <= agent.stoppingDistance)
		{
			Vector3 position = entity.waypoints[Random.Range(0, entity.waypoints.Count - 1)].transform.position;
			agent.SetDestination(position);
		}
	}

	public void OnEnter()
	{
		Debug.Log("Current State: " + typeof(Patrol_State));
		if (entity.waypoints.Count > 0)
		{
			agent.SetDestination(entity.waypoints[0].transform.position);
		}
	}

	public void OnExit()
	{
	}
}
