using GorillaExtensions;
using UnityEngine;

public class LongScarfSim : MonoBehaviour
{
	[SerializeField]
	private GameObject[] gameObjects;

	[SerializeField]
	private float speedThreshold = 1f;

	[SerializeField]
	private float blendAmountPerSecond = 1f;

	private GorillaVelocityEstimator velocityEstimator;

	private Quaternion[] baseLocalRotations;

	private float currentBlend;

	[SerializeField]
	private float centerOfMassLength;

	[SerializeField]
	private float gravityStrength;

	[SerializeField]
	private float drag;

	[SerializeField]
	private Vector3 clampToPlane;

	private Vector3 lastCenterPos;

	private Vector3 velocity;

	private void Start()
	{
		clampToPlane.Normalize();
		velocityEstimator = GetComponent<GorillaVelocityEstimator>();
		baseLocalRotations = new Quaternion[gameObjects.Length];
		for (int i = 0; i < gameObjects.Length; i++)
		{
			baseLocalRotations[i] = gameObjects[i].transform.localRotation;
		}
	}

	private void LateUpdate()
	{
		velocity *= drag;
		velocity.y -= gravityStrength * Time.deltaTime;
		Vector3 position = base.transform.position;
		Vector3 vector = lastCenterPos + velocity * Time.deltaTime;
		Vector3 vector2 = position + (vector - position).normalized * centerOfMassLength;
		Vector3 vector3 = base.transform.InverseTransformPoint(vector2);
		float num = Vector3.Dot(vector3, clampToPlane);
		if (num < 0f)
		{
			vector3 -= clampToPlane * num;
			vector2 = base.transform.TransformPoint(vector3);
		}
		Vector3 vector4 = vector2;
		velocity = (vector4 - lastCenterPos) / Time.deltaTime;
		lastCenterPos = vector4;
		float target = (velocityEstimator.linearVelocity.IsLongerThan(speedThreshold) ? 1 : 0);
		currentBlend = Mathf.MoveTowards(currentBlend, target, blendAmountPerSecond * Time.deltaTime);
		Quaternion b = Quaternion.LookRotation(vector4 - position);
		for (int i = 0; i < gameObjects.Length; i++)
		{
			Quaternion a = gameObjects[i].transform.parent.rotation * baseLocalRotations[i];
			gameObjects[i].transform.rotation = Quaternion.Lerp(a, b, currentBlend);
		}
	}
}
