using UnityEngine;

namespace GorillaTagScripts.AI.States;

public class CircularPatrol_State : IState
{
	private AIEntity entity;

	private float angle;

	public CircularPatrol_State(AIEntity entity)
	{
		this.entity = entity;
	}

	public void Tick()
	{
		Vector3 position = entity.circleCenter.position;
		float x = position.x + Mathf.Cos(angle) * entity.angularSpeed;
		float y = position.y;
		float z = position.z + Mathf.Sin(angle) * entity.angularSpeed;
		entity.transform.position = new Vector3(x, y, z);
		angle += entity.angularSpeed * Time.deltaTime;
	}

	public void OnEnter()
	{
	}

	public void OnExit()
	{
	}
}
