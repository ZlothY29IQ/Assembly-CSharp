using System;
using UnityEngine;

namespace BoingKit;

public class MathUtil
{
	public static readonly float Pi = MathF.PI;

	public static readonly float TwoPi = MathF.PI * 2f;

	public static readonly float HalfPi = MathF.PI / 2f;

	public static readonly float QuaterPi = MathF.PI / 4f;

	public static readonly float SixthPi = MathF.PI / 6f;

	public static readonly float Sqrt2 = Mathf.Sqrt(2f);

	public static readonly float Sqrt2Inv = 1f / Mathf.Sqrt(2f);

	public static readonly float Sqrt3 = Mathf.Sqrt(3f);

	public static readonly float Sqrt3Inv = 1f / Mathf.Sqrt(3f);

	public static readonly float Epsilon = 1E-06f;

	public static readonly float Rad2Deg = 180f / MathF.PI;

	public static readonly float Deg2Rad = MathF.PI / 180f;

	public static float AsinSafe(float x)
	{
		return Mathf.Asin(Mathf.Clamp(x, -1f, 1f));
	}

	public static float AcosSafe(float x)
	{
		return Mathf.Acos(Mathf.Clamp(x, -1f, 1f));
	}

	public static float InvSafe(float x)
	{
		return 1f / Mathf.Max(Epsilon, x);
	}

	public static float PointLineDist(Vector2 point, Vector2 linePos, Vector2 lineDir)
	{
		Vector2 vector = point - linePos;
		return (vector - Vector2.Dot(vector, lineDir) * lineDir).magnitude;
	}

	public static float PointSegmentDist(Vector2 point, Vector2 segmentPosA, Vector2 segmentPosB)
	{
		Vector2 vector = segmentPosB - segmentPosA;
		float num = 1f / vector.magnitude;
		Vector2 rhs = vector * num;
		float value = Vector2.Dot(point - segmentPosA, rhs) * num;
		return (segmentPosA + Mathf.Clamp(value, 0f, 1f) * vector - point).magnitude;
	}

	public static float Seek(float current, float target, float maxDelta)
	{
		float f = target - current;
		f = Mathf.Sign(f) * Mathf.Min(maxDelta, Mathf.Abs(f));
		return current + f;
	}

	public static Vector2 Seek(Vector2 current, Vector2 target, float maxDelta)
	{
		Vector2 vector = target - current;
		float magnitude = vector.magnitude;
		if (magnitude < Epsilon)
		{
			return target;
		}
		vector = Mathf.Min(maxDelta, magnitude) * vector.normalized;
		return current + vector;
	}

	public static float Remainder(float a, float b)
	{
		return a - a / b * b;
	}

	public static int Remainder(int a, int b)
	{
		return a - a / b * b;
	}

	public static float Modulo(float a, float b)
	{
		return Mathf.Repeat(a, b);
	}

	public static int Modulo(int a, int b)
	{
		int num = a % b;
		if (num < 0)
		{
			return num + b;
		}
		return num;
	}
}
