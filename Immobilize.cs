using UnityEngine;

public class Immobilize : MonoBehaviour
{
	private Vector3 lockedPosition;

	private Quaternion lockedRotation;

	private void Start()
	{
		lockedPosition = base.transform.position;
		lockedRotation = base.transform.rotation;
	}

	private void LateUpdate()
	{
		base.transform.position = lockedPosition;
		base.transform.rotation = lockedRotation;
	}
}
