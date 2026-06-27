using GorillaTag.Gravity;
using UnityEngine;

public class CounterRotator : MonoBehaviour
{
	[SerializeField]
	private GameObject stabilizedObject;

	[SerializeField]
	private ChangingBasicGravityZone gravityCompensator;

	private Vector3 startingPosition;

	private Quaternion startingRotation;

	private void Start()
	{
		startingPosition = stabilizedObject.transform.position;
		startingRotation = stabilizedObject.transform.rotation;
	}

	private void LateUpdate()
	{
		Quaternion quaternion = startingRotation * Quaternion.Inverse(stabilizedObject.transform.rotation);
		base.transform.rotation = quaternion * base.transform.rotation;
		Vector3 vector = startingPosition - stabilizedObject.transform.position;
		base.transform.position += vector;
		if (gravityCompensator != null)
		{
			gravityCompensator.SetGravityDirection(-base.transform.up);
		}
	}
}
