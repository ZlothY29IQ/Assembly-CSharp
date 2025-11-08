using UnityEngine;

namespace GorillaTag.Cosmetics;

public class CompassNeedleRotator : MonoBehaviour
{
	private const float smoothTime = 0.005f;

	private float currentVelocity;

	protected void OnEnable()
	{
		currentVelocity = 0f;
		base.transform.localRotation = Quaternion.identity;
	}

	protected void LateUpdate()
	{
		Transform obj = base.transform;
		Vector3 forward = obj.forward;
		forward.y = 0f;
		forward.Normalize();
		obj.Rotate(angle: Mathf.SmoothDamp(Vector3.SignedAngle(forward, Vector3.forward, Vector3.up), 0f, ref currentVelocity, 0.005f), axis: obj.up, relativeTo: Space.World);
	}
}
