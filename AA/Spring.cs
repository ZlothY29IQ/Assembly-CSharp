using System;
using UnityEngine;

namespace AA;

public class Spring
{
	public static float Damper(float x, float g, float factor)
	{
		return Mathf.Lerp(x, g, factor);
	}

	public static float DamperExponential(float x, float g, float damping, float dt, float ft = 1f / 60f)
	{
		return Mathf.Lerp(x, g, 1f - Mathf.Pow(1f / (1f - ft * damping), (0f - dt) / ft));
	}

	public static float FastNegExp(float x)
	{
		return 1f / (1f + x + 0.48f * x * x + 0.235f * x * x * x);
	}

	public static float DamperExact(float x, float g, float halflife, float dt, float eps = 1E-05f)
	{
		return Mathf.Lerp(x, g, 1f - FastNegExp(0.6931472f * dt / (halflife + eps)));
	}

	public static float DamperDecayExact(float x, float halflife, float dt, float eps = 1E-05f)
	{
		return x * FastNegExp(0.6931472f * dt / (halflife + eps));
	}

	public static float CopySign(float a, float s)
	{
		return Mathf.Abs(a) * Mathf.Sign(s);
	}

	public static float FastAtan(float x)
	{
		float num = Mathf.Abs(x);
		float num2 = ((num > 1f) ? (1f / num) : num);
		float num3 = MathF.PI / 4f * num2 - num2 * (num2 - 1f) * (0.2447f + 0.0663f * num2);
		return CopySign((num > 1f) ? (MathF.PI / 2f - num3) : num3, x);
	}

	public static float Square(float x)
	{
		return x * x;
	}

	public static void SpringDamperExactStiffnessDamping(ref float x, ref float v, float x_goal, float v_goal, float stiffness, float damping, float dt, float eps = 1E-05f)
	{
		float num = x_goal + damping * v_goal / (stiffness + eps);
		float num2 = damping / 2f;
		if (Mathf.Abs(stiffness - damping * damping / 4f) < eps)
		{
			float num3 = x - num;
			float num4 = v + num3 * num2;
			float num5 = FastNegExp(num2 * dt);
			x = num3 * num5 + dt * num4 * num5 + num;
			v = (0f - num2) * num3 * num5 - num2 * dt * num4 * num5 + num4 * num5;
		}
		else if ((double)(stiffness - damping * damping / 4f) > 0.0)
		{
			float num6 = Mathf.Sqrt(stiffness - damping * damping / 4f);
			float num7 = Mathf.Sqrt(Square(v + num2 * (x - num)) / (num6 * num6 + eps) + Square(x - num));
			float num8 = FastAtan((v + (x - num) * num2) / ((0f - (x - num)) * num6 + eps));
			num7 = ((x - num > 0f) ? num7 : (0f - num7));
			float num9 = FastNegExp(num2 * dt);
			x = num7 * num9 * Mathf.Cos(num6 * dt + num8) + num;
			v = (0f - num2) * num7 * num9 * Mathf.Cos(num6 * dt + num8) - num6 * num7 * num9 * Mathf.Sin(num6 * dt + num8);
		}
		else if ((double)(stiffness - damping * damping / 4f) < 0.0)
		{
			float num10 = (damping + Mathf.Sqrt(damping * damping - 4f * stiffness)) / 2f;
			float num11 = (damping - Mathf.Sqrt(damping * damping - 4f * stiffness)) / 2f;
			float num12 = (num * num10 - x * num10 - v) / (num11 - num10);
			float num13 = x - num12 - num;
			float num14 = FastNegExp(num10 * dt);
			float num15 = FastNegExp(num11 * dt);
			x = num13 * num14 + num12 * num15 + num;
			v = (0f - num10) * num13 * num14 - num11 * num12 * num15;
		}
	}

	public static float HalflifeToDamping(float halflife, float eps = 1E-05f)
	{
		return 2.7725887f / (halflife + eps);
	}

	public static float DampingToHalflife(float damping, float eps = 1E-05f)
	{
		return 2.7725887f / (damping + eps);
	}

	public static float FrequencyToStiffness(float frequency)
	{
		return Square(MathF.PI * 2f * frequency);
	}

	public static float stiffness_to_frequency(float stiffness)
	{
		return Mathf.Sqrt(stiffness) / (MathF.PI * 2f);
	}

	public static float critical_halflife(float frequency)
	{
		return DampingToHalflife(Mathf.Sqrt(FrequencyToStiffness(frequency) * 4f));
	}

	public static float critical_frequency(float halflife)
	{
		return stiffness_to_frequency(Square(HalflifeToDamping(halflife)) / 4f);
	}

	public static void SpringDamperExact(ref float x, ref float v, float x_goal, float v_goal, float frequency, float halflife, float dt, float eps = 1E-05f)
	{
		float num = FrequencyToStiffness(frequency);
		float num2 = HalflifeToDamping(halflife);
		float num3 = x_goal + num2 * v_goal / (num + eps);
		float num4 = num2 / 2f;
		if (Mathf.Abs(num - num2 * num2 / 4f) < eps)
		{
			float num5 = x - num3;
			float num6 = v + num5 * num4;
			float num7 = FastNegExp(num4 * dt);
			x = num5 * num7 + dt * num6 * num7 + num3;
			v = (0f - num4) * num5 * num7 - num4 * dt * num6 * num7 + num6 * num7;
		}
		else if ((double)(num - num2 * num2 / 4f) > 0.0)
		{
			float num8 = Mathf.Sqrt(num - num2 * num2 / 4f);
			float num9 = Mathf.Sqrt(Square(v + num4 * (x - num3)) / (num8 * num8 + eps) + Square(x - num3));
			float num10 = FastAtan((v + (x - num3) * num4) / ((0f - (x - num3)) * num8 + eps));
			num9 = ((x - num3 > 0f) ? num9 : (0f - num9));
			float num11 = FastNegExp(num4 * dt);
			x = num9 * num11 * Mathf.Cos(num8 * dt + num10) + num3;
			v = (0f - num4) * num9 * num11 * Mathf.Cos(num8 * dt + num10) - num8 * num9 * num11 * Mathf.Sin(num8 * dt + num10);
		}
		else if ((double)(num - num2 * num2 / 4f) < 0.0)
		{
			float num12 = (num2 + Mathf.Sqrt(num2 * num2 - 4f * num)) / 2f;
			float num13 = (num2 - Mathf.Sqrt(num2 * num2 - 4f * num)) / 2f;
			float num14 = (num3 * num12 - x * num12 - v) / (num13 - num12);
			float num15 = x - num14 - num3;
			float num16 = FastNegExp(num12 * dt);
			float num17 = FastNegExp(num13 * dt);
			x = num15 * num16 + num14 * num17 + num3;
			v = (0f - num12) * num15 * num16 - num13 * num14 * num17;
		}
	}

	public static float DampingRatioToStiffness(float ratio, float damping)
	{
		return Square(damping / (ratio * 2f));
	}

	public static float DampingRatioToDamping(float ratio, float stiffness)
	{
		return ratio * 2f * Mathf.Sqrt(stiffness);
	}

	public static void SpringDamperExactRatio(ref float x, ref float v, float x_goal, float v_goal, float damping_ratio, float halflife, float dt, float eps = 1E-05f)
	{
		float num = HalflifeToDamping(halflife);
		float num2 = DampingRatioToStiffness(damping_ratio, num);
		float num3 = x_goal + num * v_goal / (num2 + eps);
		float num4 = num / 2f;
		if (Mathf.Abs(num2 - num * num / 4f) < eps)
		{
			float num5 = x - num3;
			float num6 = v + num5 * num4;
			float num7 = FastNegExp(num4 * dt);
			x = num5 * num7 + dt * num6 * num7 + num3;
			v = (0f - num4) * num5 * num7 - num4 * dt * num6 * num7 + num6 * num7;
		}
		else if ((double)(num2 - num * num / 4f) > 0.0)
		{
			float num8 = Mathf.Sqrt(num2 - num * num / 4f);
			float num9 = Mathf.Sqrt(Square(v + num4 * (x - num3)) / (num8 * num8 + eps) + Square(x - num3));
			float num10 = FastAtan((v + (x - num3) * num4) / ((0f - (x - num3)) * num8 + eps));
			num9 = ((x - num3 > 0f) ? num9 : (0f - num9));
			float num11 = FastNegExp(num4 * dt);
			x = num9 * num11 * Mathf.Cos(num8 * dt + num10) + num3;
			v = (0f - num4) * num9 * num11 * Mathf.Cos(num8 * dt + num10) - num8 * num9 * num11 * Mathf.Sin(num8 * dt + num10);
		}
		else if ((double)(num2 - num * num / 4f) < 0.0)
		{
			float num12 = (num + Mathf.Sqrt(num * num - 4f * num2)) / 2f;
			float num13 = (num - Mathf.Sqrt(num * num - 4f * num2)) / 2f;
			float num14 = (num3 * num12 - x * num12 - v) / (num13 - num12);
			float num15 = x - num14 - num3;
			float num16 = FastNegExp(num12 * dt);
			float num17 = FastNegExp(num13 * dt);
			x = num15 * num16 + num14 * num17 + num3;
			v = (0f - num12) * num15 * num16 - num13 * num14 * num17;
		}
	}

	public static void CriticalSpringDamperExact(ref float x, ref float v, float x_goal, float v_goal, float halflife, float dt)
	{
		float num = HalflifeToDamping(halflife);
		float num2 = x_goal + num * v_goal / (num * num / 4f);
		float num3 = num / 2f;
		float num4 = x - num2;
		float num5 = v + num4 * num3;
		float num6 = FastNegExp(num3 * dt);
		x = num6 * (num4 + num5 * dt) + num2;
		v = num6 * (v - num5 * num3 * dt);
	}

	public static void SimpleSpringDamperExact(ref float x, ref float v, float x_goal, float halflife, float dt)
	{
		float num = HalflifeToDamping(halflife) / 2f;
		float num2 = x - x_goal;
		float num3 = v + num2 * num;
		float num4 = FastNegExp(num * dt);
		x = num4 * (num2 + num3 * dt) + x_goal;
		v = num4 * (v - num3 * num * dt);
	}

	public static void DecaySringDamperExact(ref float x, ref float v, float halflife, float dt)
	{
		float num = HalflifeToDamping(halflife) / 2f;
		float num2 = v + x * num;
		float num3 = FastNegExp(num * dt);
		x = num3 * (x + num2 * dt);
		v = num3 * (v - num2 * num * dt);
	}
}
