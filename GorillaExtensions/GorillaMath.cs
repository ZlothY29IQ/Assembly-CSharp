using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace GorillaExtensions;

public static class GorillaMath
{
	[Serializable]
	public struct RemapFloatInfo(float fromMin = 0f, float toMin = 0f, float fromMax = 1f, float toMax = 1f)
	{
		public float fromMin = fromMin;

		public float toMin = toMin;

		public float fromMax = fromMax;

		public float toMax = toMax;

		public void OnValidate()
		{
			if (fromMin < fromMax)
			{
				fromMin = fromMax + float.Epsilon;
			}
			if (toMin < toMax)
			{
				toMin = toMax + float.Epsilon;
			}
		}

		public bool IsValid()
		{
			if (fromMin < fromMax)
			{
				return toMin < toMax;
			}
			return false;
		}

		public float Remap(float value)
		{
			return toMin + (value - fromMin) / (fromMax - fromMin) * (toMax - toMin);
		}
	}

	[StructLayout(LayoutKind.Explicit)]
	private struct FloatIntUnion
	{
		[FieldOffset(0)]
		public float f;

		[FieldOffset(0)]
		public int tmp;
	}

	public static Vector3 GetAngularVelocity(Quaternion oldRotation, Quaternion newRotation)
	{
		Quaternion quaternion = newRotation * Quaternion.Inverse(oldRotation);
		if (Mathf.Abs(quaternion.w) > 0.9995117f)
		{
			return Vector3.zero;
		}
		float num2;
		if (quaternion.w < 0f)
		{
			float num = Mathf.Acos(0f - quaternion.w);
			num2 = -2f * num / (Mathf.Sin(num) * Time.deltaTime);
		}
		else
		{
			float num3 = Mathf.Acos(quaternion.w);
			num2 = 2f * num3 / (Mathf.Sin(num3) * Time.deltaTime);
		}
		Vector3 result = new Vector3(quaternion.x * num2, quaternion.y * num2, quaternion.z * num2);
		if (float.IsNaN(result.z))
		{
			return Vector3.zero;
		}
		return result;
	}

	public static float FastInvSqrt(float z)
	{
		if (z == 0f)
		{
			return 0f;
		}
		FloatIntUnion floatIntUnion = default(FloatIntUnion);
		floatIntUnion.tmp = 0;
		float num = 0.5f * z;
		floatIntUnion.f = z;
		floatIntUnion.tmp = 1597463174 - (floatIntUnion.tmp >> 1);
		floatIntUnion.f *= 1.5f - num * floatIntUnion.f * floatIntUnion.f;
		return floatIntUnion.f * z;
	}

	public static float Dot2(in Vector3 v)
	{
		return Vector3.Dot(v, v);
	}

	public static Vector4 RaycastToCappedCone(in Vector3 rayOrigin, in Vector3 rayDirection, in Vector3 coneTip, in Vector3 coneBase, in float coneTipRadius, in float coneBaseRadius)
	{
		Vector3 vector = coneBase - coneTip;
		Vector3 vector2 = rayOrigin - coneTip;
		Vector3 vector3 = rayOrigin - coneBase;
		float num = Vector3.Dot(vector, vector);
		float num2 = Vector3.Dot(vector2, vector);
		float num3 = Vector3.Dot(vector3, vector);
		float num4 = Vector3.Dot(rayDirection, vector);
		if ((double)num2 < 0.0)
		{
			if (Dot2(vector2 * num4 - rayDirection * num2) < coneTipRadius * coneTipRadius * num4 * num4)
			{
				Vector3 vector4 = -vector * FastInvSqrt(num);
				return new Vector4((0f - num2) / num4, vector4.x, vector4.y, vector4.z);
			}
		}
		else if ((double)num3 > 0.0 && Dot2(vector3 * num4 - rayDirection * num3) < coneBaseRadius * coneBaseRadius * num4 * num4)
		{
			Vector3 vector5 = vector * FastInvSqrt(num);
			return new Vector4((0f - num3) / num4, vector5.x, vector5.y, vector5.z);
		}
		float num5 = Vector3.Dot(rayDirection, vector2);
		float num6 = Vector3.Dot(vector2, vector2);
		float num7 = coneTipRadius - coneBaseRadius;
		float num8 = num + num7 * num7;
		float num9 = num * num - num4 * num4 * num8;
		float num10 = num * num * num5 - num2 * num4 * num8 + num * coneTipRadius * (num7 * num4 * 1f);
		float num11 = num * num * num6 - num2 * num2 * num8 + num * coneTipRadius * (num7 * num2 * 2f - num * coneTipRadius);
		float num12 = num10 * num10 - num9 * num11;
		if ((double)num12 < 0.0)
		{
			return -Vector4.one;
		}
		float num13 = (0f - num10 - Mathf.Sqrt(num12)) / num9;
		float num14 = num2 + num13 * num4;
		if ((double)num14 > 0.0 && num14 < num)
		{
			Vector3 normalized = (num * (num * (vector2 + num13 * rayDirection) + num7 * vector * coneTipRadius) - vector * num8 * num14).normalized;
			return new Vector4(num13, normalized.x, normalized.y, normalized.z);
		}
		return -Vector4.one;
	}

	public static void LineSegClosestPoints(Vector3 a, Vector3 u, Vector3 b, Vector3 v, out Vector3 lineAPoint, out Vector3 lineBPoint)
	{
		lineAPoint = a;
		lineBPoint = b;
		Vector3 lhs = b - a;
		float num = Vector3.Dot(lhs, u);
		float num2 = Vector3.Dot(lhs, v);
		float num3 = Vector3.Dot(u, u);
		float num4 = Vector3.Dot(u, v);
		float num5 = Vector3.Dot(v, v);
		float num6 = num3 * num5 - num4 * num4;
		if (!((double)Mathf.Abs(num6) < 0.001))
		{
			float value = (num * num5 - num2 * num4) / num6;
			float value2 = (num * num4 - num2 * num3) / num6;
			value = Mathf.Clamp(value, 0f, 1f);
			float value3 = (Mathf.Clamp(value2, 0f, 1f) * num4 + num) / num3;
			float value4 = (value * num4 - num2) / num5;
			value3 = Mathf.Clamp(value3, 0f, 1f);
			value4 = Mathf.Clamp(value4, 0f, 1f);
			lineAPoint = a + value3 * u;
			lineBPoint = b + value4 * v;
		}
	}
}
