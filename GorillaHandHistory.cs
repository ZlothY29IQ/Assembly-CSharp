using UnityEngine;

public class GorillaHandHistory : MonoBehaviour
{
	public Vector3 direction;

	private Vector3 lastPosition;

	private Vector3 lastLastPosition;

	private void Start()
	{
		direction = default(Vector3);
		lastPosition = default(Vector3);
	}

	private void FixedUpdate()
	{
		direction = lastPosition - base.transform.position;
		lastLastPosition = lastPosition;
		lastPosition = base.transform.position;
	}
}
