using System;
using UnityEngine;

namespace CjLib;

public class MathUtil
{
	public static readonly float Pi = MathF.PI;

	public static readonly float TwoPi = MathF.PI * 2f;

	public static readonly float HalfPi = MathF.PI / 2f;

	public static readonly float ThirdPi = MathF.PI / 3f;

	public static readonly float QuarterPi = MathF.PI / 4f;

	public static readonly float FifthPi = MathF.PI / 5f;

	public static readonly float SixthPi = MathF.PI / 6f;

	public static readonly float Sqrt2 = Mathf.Sqrt(2f);

	public static readonly float Sqrt2Inv = 1f / Mathf.Sqrt(2f);

	public static readonly float Sqrt3 = Mathf.Sqrt(3f);

	public static readonly float Sqrt3Inv = 1f / Mathf.Sqrt(3f);

	public static readonly float Epsilon = 1E-09f;

	public static readonly float EpsilonComp = 1f - Epsilon;

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

	public static float CatmullRom(float p0, float p1, float p2, float p3, float t)
	{
		float num = t * t;
		return 0.5f * (2f * p1 + (0f - p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * num + (0f - p0 + 3f * p1 - 3f * p2 + p3) * num * t);
	}
}
