using UnityEngine;

namespace GorillaTag.Gravity;

public class PlanetZone : BasicGravityZone
{
	[Tooltip("how close to the center of the zone to enable rotating the player")]
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
		return worldPosition - base.transform.position;
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
