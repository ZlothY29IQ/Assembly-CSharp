using System.Runtime.CompilerServices;
using GorillaExtensions;
using UnityEngine;

namespace GorillaTag.Gravity;

public class CubicPlanetZone : PlanetZone
{
	[Header("box constraint for where gravity center can be")]
	[SerializeField]
	protected Vector3 constraints;

	[SerializeField]
	protected Vector3 minConstraints;

	[SerializeField]
	protected Vector3 maxConstraints;

	protected Quaternion inverseRotation;

	private void UpdateConstraint()
	{
		float num = Mathf.Abs(constraints.x * 0.5f);
		float num2 = Mathf.Abs(constraints.y * 0.5f);
		float num3 = Mathf.Abs(constraints.z * 0.5f);
		minConstraints.x = num * -1f;
		minConstraints.y = num2 * -1f;
		minConstraints.z = num3 * -1f;
		maxConstraints.x = num;
		maxConstraints.y = num2;
		maxConstraints.z = num3;
	}

	protected override void Awake()
	{
		base.Awake();
		inverseRotation = Quaternion.Inverse(base.transform.rotation);
		UpdateConstraint();
	}

	protected override Vector3 GetGravityVectorAtPoint(in Vector3 worldPosition, in MonkeGravityController controller)
	{
		return worldPosition - GetPointOnBounds(in worldPosition);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vector3 GetPointOnBounds(in Vector3 point)
	{
		Transform transform = base.transform;
		Vector3 position = transform.position;
		Vector3 vec = inverseRotation * (point - position);
		float x = vec.x;
		float y = vec.y;
		float z = vec.z;
		if (x <= maxConstraints.x && x >= minConstraints.x && y <= maxConstraints.y && y >= minConstraints.y && z <= maxConstraints.z && z >= minConstraints.z)
		{
			Vector3 vec2 = new Vector3((x > 0f) ? maxConstraints.x : minConstraints.x, (y > 0f) ? maxConstraints.y : minConstraints.y, (z > 0f) ? maxConstraints.z : minConstraints.z);
			float num = Mathf.Abs(vec2.x - x);
			float num2 = Mathf.Abs(vec2.y - y);
			float num3 = Mathf.Abs(vec2.z - z);
			Vector3 mulitplier = new Vector3((num <= num2 && num <= num3) ? 1f : 0f, (num2 <= num && num2 <= num3) ? 1f : 0f, (num3 <= num && num3 <= num2) ? 1f : 0f);
			Vector3 vector = vec.MultiplyBy(in mulitplier);
			Vector3 vector2 = vec2.MultiplyBy(in mulitplier);
			float magnitude = vector.magnitude;
			float magnitude2 = vector2.magnitude;
			float num4 = 1f / magnitude2;
			float num5 = magnitude * num4;
			num5 *= 0.99f;
			return position + transform.rotation * vec.Clamp(minConstraints * num5, maxConstraints * num5);
		}
		return position + transform.rotation * vec.Clamp(minConstraints, maxConstraints);
	}
}
