using UnityEngine;

public class GRTransformLook : MonoBehaviour
{
	public bool followPlayer;

	public Transform lookTarget;

	public Vector3 offsetRotation;

	private void Awake()
	{
		if (followPlayer)
		{
			lookTarget = Camera.main.transform;
		}
	}

	private void LateUpdate()
	{
		base.transform.LookAt(lookTarget);
		base.transform.rotation *= Quaternion.Euler(offsetRotation);
	}
}
