using System;
using System.Diagnostics;
using UnityEngine;

[Serializable]
public struct Arc
{
	public Vector3 start;

	public Vector3 end;

	public Vector3 control;

	public Vector3[] GetArcPoints(int count = 12)
	{
		return ComputeArcPoints(start, end, control, count);
	}

	[Conditional("UNITY_EDITOR")]
	public void DrawGizmo()
	{
	}

	public static Arc From(Vector3 start, Vector3 end)
	{
		Vector3 vector = DeriveArcControlPoint(start, end);
		return new Arc
		{
			start = start,
			end = end,
			control = vector
		};
	}

	public static Vector3[] ComputeArcPoints(Vector3 a, Vector3 b, Vector3? c = null, int count = 12)
	{
		Vector3[] array = new Vector3[count];
		float num = 1f / (float)count;
		Vector3 valueOrDefault = c.GetValueOrDefault();
		if (!c.HasValue)
		{
			valueOrDefault = DeriveArcControlPoint(a, b);
			c = valueOrDefault;
		}
		for (int i = 0; i < count; i++)
		{
			float t = ((i != 0) ? ((i != count - 1) ? (num * (float)i) : 1f) : 0f);
			array[i] = BezierLerp(a, b, c.Value, t);
		}
		return array;
	}

	public static Vector3 BezierLerp(Vector3 a, Vector3 b, Vector3 c, float t)
	{
		Vector3 a2 = Vector3.Lerp(a, c, t);
		Vector3 b2 = Vector3.Lerp(c, b, t);
		return Vector3.Lerp(a2, b2, t);
	}

	public static Vector3 DeriveArcControlPoint(Vector3 a, Vector3 b, Vector3? dir = null, float? height = null)
	{
		Vector3 vector = (b - a) * 0.5f;
		Vector3 normalized = vector.normalized;
		float valueOrDefault = height.GetValueOrDefault();
		if (!height.HasValue)
		{
			valueOrDefault = vector.magnitude;
			height = valueOrDefault;
		}
		if (!dir.HasValue)
		{
			Vector3 rhs = Vector3.Cross(normalized, Vector3.up);
			dir = Vector3.Cross(normalized, rhs);
		}
		Vector3 vector2 = dir.Value * (0f - height.Value);
		return a + vector + vector2;
	}
}
