using GorillaExtensions;
using UnityEngine;

public class BeeAvoiderTest : MonoBehaviour
{
	public GameObject[] patrolPoints;

	public GameObject[] avoidancePoints;

	public float speed;

	public float acceleration;

	public float instability;

	public float instabilityOffRadius;

	public float drag;

	public float avoidRadius;

	public float patrolArrivedRadius;

	private int nextPatrolPoint;

	private Vector3 velocity;

	public void Update()
	{
		Vector3 position = patrolPoints[nextPatrolPoint].transform.position;
		Vector3 position2 = base.transform.position;
		Vector3 target = (position - position2).normalized * speed;
		velocity = Vector3.MoveTowards(velocity * drag, target, acceleration);
		if ((position2 - position).IsLongerThan(instabilityOffRadius))
		{
			velocity += Random.insideUnitSphere * instability * Time.deltaTime;
		}
		Vector3 vector = position2 + velocity * Time.deltaTime;
		GameObject[] array = avoidancePoints;
		for (int i = 0; i < array.Length; i++)
		{
			Vector3 position3 = array[i].transform.position;
			if ((vector - position3).IsShorterThan(avoidRadius))
			{
				Vector3 normalized = Vector3.Cross(position3 - vector, position - vector).normalized;
				_ = (position - position3).normalized;
				float num = Vector3.Dot(vector - position3, normalized);
				Vector3 vector2 = (avoidRadius - num) * normalized;
				vector += vector2;
				velocity += vector2;
			}
		}
		base.transform.position = vector;
		base.transform.rotation = Quaternion.LookRotation(position - vector);
		if ((vector - position).IsShorterThan(patrolArrivedRadius))
		{
			nextPatrolPoint = (nextPatrolPoint + 1) % patrolPoints.Length;
		}
	}
}
