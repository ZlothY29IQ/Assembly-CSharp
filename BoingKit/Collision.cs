using UnityEngine;

namespace BoingKit;

public class Collision
{
	public static bool SphereSphere(Vector3 centerA, float radiusA, Vector3 centerB, float radiusB, out Vector3 push)
	{
		push = Vector3.zero;
		Vector3 vector = centerA - centerB;
		float sqrMagnitude = vector.sqrMagnitude;
		float num = radiusA + radiusB;
		if (sqrMagnitude >= num * num)
		{
			return false;
		}
		float num2 = Mathf.Sqrt(sqrMagnitude);
		push = VectorUtil.NormalizeSafe(vector, Vector3.zero) * (num - num2);
		return true;
	}

	public static bool SphereSphereInverse(Vector3 centerA, float radiusA, Vector3 centerB, float radiusB, out Vector3 push)
	{
		push = Vector3.zero;
		Vector3 vector = centerB - centerA;
		float sqrMagnitude = vector.sqrMagnitude;
		float num = radiusB - radiusA;
		if (sqrMagnitude <= num * num)
		{
			return false;
		}
		float num2 = Mathf.Sqrt(sqrMagnitude);
		push = VectorUtil.NormalizeSafe(vector, Vector3.zero) * (num2 - num);
		return true;
	}

	public static bool SphereCapsule(Vector3 centerA, float radiusA, Vector3 headB, Vector3 tailB, float radiusB, out Vector3 push)
	{
		push = Vector3.zero;
		Vector3 vector = tailB - headB;
		float sqrMagnitude = vector.sqrMagnitude;
		if (sqrMagnitude < MathUtil.Epsilon)
		{
			return SphereSphereInverse(centerA, radiusA, 0.5f * (headB + tailB), radiusB, out push);
		}
		float num = 1f / Mathf.Sqrt(sqrMagnitude);
		Vector3 rhs = vector * num;
		float t = Mathf.Clamp01(Vector3.Dot(centerA - headB, rhs) * num);
		Vector3 centerB = Vector3.Lerp(headB, tailB, t);
		return SphereSphere(centerA, radiusA, centerB, radiusB, out push);
	}

	public static bool SphereCapsuleInverse(Vector3 centerA, float radiusA, Vector3 headB, Vector3 tailB, float radiusB, out Vector3 push)
	{
		push = Vector3.zero;
		Vector3 vector = tailB - headB;
		float sqrMagnitude = vector.sqrMagnitude;
		if (sqrMagnitude < MathUtil.Epsilon)
		{
			return SphereSphereInverse(centerA, radiusA, 0.5f * (headB + tailB), radiusB, out push);
		}
		float num = 1f / Mathf.Sqrt(sqrMagnitude);
		Vector3 rhs = vector * num;
		float t = Mathf.Clamp01(Vector3.Dot(centerA - headB, rhs) * num);
		Vector3 centerB = Vector3.Lerp(headB, tailB, t);
		return SphereSphereInverse(centerA, radiusA, centerB, radiusB, out push);
	}

	public static bool SphereBox(Vector3 centerOffsetA, float radiusA, Vector3 halfExtentB, out Vector3 push)
	{
		push = Vector3.zero;
		Vector3 vector = new Vector3(Mathf.Clamp(centerOffsetA.x, 0f - halfExtentB.x, halfExtentB.x), Mathf.Clamp(centerOffsetA.y, 0f - halfExtentB.y, halfExtentB.y), Mathf.Clamp(centerOffsetA.z, 0f - halfExtentB.z, halfExtentB.z));
		Vector3 vector2 = centerOffsetA - vector;
		float sqrMagnitude = vector2.sqrMagnitude;
		if (sqrMagnitude > radiusA * radiusA)
		{
			return false;
		}
		switch (((!(centerOffsetA.x < 0f - halfExtentB.x) && !(centerOffsetA.x > halfExtentB.x)) ? 1 : 0) + ((!(centerOffsetA.y < 0f - halfExtentB.y) && !(centerOffsetA.y > halfExtentB.y)) ? 1 : 0) + ((!(centerOffsetA.z < 0f - halfExtentB.z) && !(centerOffsetA.z > halfExtentB.z)) ? 1 : 0))
		{
		case 0:
		case 1:
		case 2:
			push = VectorUtil.NormalizeSafe(vector2, Vector3.right) * (radiusA - Mathf.Sqrt(sqrMagnitude));
			break;
		case 3:
		{
			Vector3 vector3 = new Vector3(halfExtentB.x - Mathf.Abs(centerOffsetA.x) + radiusA, halfExtentB.y - Mathf.Abs(centerOffsetA.y) + radiusA, halfExtentB.z - Mathf.Abs(centerOffsetA.z) + radiusA);
			if (vector3.x < vector3.y)
			{
				if (vector3.x < vector3.z)
				{
					push = new Vector3(Mathf.Sign(centerOffsetA.x) * vector3.x, 0f, 0f);
				}
				else
				{
					push = new Vector3(0f, 0f, Mathf.Sign(centerOffsetA.z) * vector3.z);
				}
			}
			else if (vector3.y < vector3.z)
			{
				push = new Vector3(0f, Mathf.Sign(centerOffsetA.y) * vector3.y, 0f);
			}
			else
			{
				push = new Vector3(0f, 0f, Mathf.Sign(centerOffsetA.z) * vector3.z);
			}
			break;
		}
		}
		return true;
	}

	public static bool SphereBoxInverse(Vector3 centerOffsetA, float radiusA, Vector3 halfExtentB, out Vector3 push)
	{
		push = Vector3.zero;
		return false;
	}
}
