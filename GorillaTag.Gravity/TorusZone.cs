using UnityEngine;

namespace GorillaTag.Gravity;

public class TorusZone : BasicGravityZone
{
	[Tooltip("Major radius of the torus (distance from torus center to the centerline of the tube). Torus axis is transform.up.")]
	[SerializeField]
	protected float majorRadius = 5f;

	[Tooltip("how close to the central ring of the torus to enable rotating the player")]
	[SerializeField]
	protected float rotationDistance;

	[Tooltip("if enabled, always rotates the player")]
	[SerializeField]
	protected bool alwaysRotate = true;

	private float sqrDistance;

	protected override void Awake()
	{
		base.Awake();
		sqrDistance = rotationDistance * rotationDistance;
	}

	protected override Vector3 GetGravityVectorAtPoint(in Vector3 worldPosition, in MonkeGravityController controller)
	{
		Vector3 up = base.transform.up;
		Vector3 vector = worldPosition - base.transform.position;
		Vector3 vector2 = vector - up * Vector3.Dot(vector, up);
		float sqrMagnitude = vector2.sqrMagnitude;
		Vector3 vector4;
		if (sqrMagnitude < 1E-10f)
		{
			Vector3 vector3 = Vector3.Cross(up, Vector3.right);
			if (vector3.sqrMagnitude < 1E-10f)
			{
				vector3 = Vector3.Cross(up, Vector3.forward);
			}
			vector4 = base.transform.position + vector3.normalized * majorRadius;
		}
		else
		{
			vector4 = base.transform.position + vector2 * (majorRadius / Mathf.Sqrt(sqrMagnitude));
		}
		return worldPosition - vector4;
	}

	protected override bool GetRotationIntent(in Vector3 offsetFromGravity)
	{
		if (!alwaysRotate)
		{
			if (rotateTarget)
			{
				return offsetFromGravity.sqrMagnitude < sqrDistance;
			}
			return false;
		}
		return true;
	}
}
