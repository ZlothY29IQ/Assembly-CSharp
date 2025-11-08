using UnityEngine;

public class TasselPhysics : MonoBehaviour
{
	[SerializeField]
	private GameObject[] tasselInstances;

	[SerializeField]
	private Vector3 localCenterOfMass;

	[SerializeField]
	private float gravityStrength;

	[SerializeField]
	private float drag;

	[SerializeField]
	private bool LockXAxis;

	private Vector3 lastCenterPos;

	private Vector3 velocity;

	private float centerOfMassLength;

	private Quaternion rotCorrection;

	private void Awake()
	{
		centerOfMassLength = localCenterOfMass.magnitude;
		if (LockXAxis)
		{
			rotCorrection = Quaternion.Inverse(Quaternion.LookRotation(Vector3.right, localCenterOfMass));
		}
		else
		{
			rotCorrection = Quaternion.Inverse(Quaternion.LookRotation(localCenterOfMass));
		}
	}

	private void Update()
	{
		float y = base.transform.lossyScale.y;
		velocity *= drag;
		velocity.y -= gravityStrength * y * Time.deltaTime;
		Vector3 position = base.transform.position;
		Vector3 vector = lastCenterPos + velocity * Time.deltaTime;
		Vector3 vector2 = position + (vector - position).normalized * centerOfMassLength * y;
		velocity = (vector2 - lastCenterPos) / Time.deltaTime;
		lastCenterPos = vector2;
		if (LockXAxis)
		{
			GameObject[] array = tasselInstances;
			foreach (GameObject gameObject in array)
			{
				gameObject.transform.rotation = Quaternion.LookRotation(gameObject.transform.right, vector2 - position) * rotCorrection;
			}
		}
		else
		{
			GameObject[] array = tasselInstances;
			foreach (GameObject gameObject2 in array)
			{
				gameObject2.transform.rotation = Quaternion.LookRotation(vector2 - position, gameObject2.transform.position - position) * rotCorrection;
			}
		}
	}
}
