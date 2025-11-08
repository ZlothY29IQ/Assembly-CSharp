using UnityEngine;

namespace BoingKit;

public struct FloatSpring
{
	public static readonly int Stride = 8;

	public float Value;

	public float Velocity;

	public void Reset()
	{
		Value = 0f;
		Velocity = 0f;
	}

	public void Reset(float initValue)
	{
		Value = initValue;
		Velocity = 0f;
	}

	public void Reset(float initValue, float initVelocity)
	{
		Value = initValue;
		Velocity = initVelocity;
	}

	public float TrackDampingRatio(float targetValue, float angularFrequency, float dampingRatio, float deltaTime)
	{
		if (angularFrequency < MathUtil.Epsilon)
		{
			Velocity = 0f;
			return Value;
		}
		float num = targetValue - Value;
		float num2 = 1f + 2f * deltaTime * dampingRatio * angularFrequency;
		float num3 = angularFrequency * angularFrequency;
		float num4 = deltaTime * num3;
		float num5 = deltaTime * num4;
		float num6 = 1f / (num2 + num5);
		float num7 = num2 * Value + deltaTime * Velocity + num5 * targetValue;
		float num8 = Velocity + num4 * num;
		Velocity = num8 * num6;
		Value = num7 * num6;
		if (Mathf.Abs(Velocity) < MathUtil.Epsilon && Mathf.Abs(num) < MathUtil.Epsilon)
		{
			Velocity = 0f;
			Value = targetValue;
		}
		return Value;
	}

	public float TrackHalfLife(float targetValue, float frequencyHz, float halfLife, float deltaTime)
	{
		if (halfLife < MathUtil.Epsilon)
		{
			Velocity = 0f;
			Value = targetValue;
			return Value;
		}
		float num = frequencyHz * MathUtil.TwoPi;
		float dampingRatio = 0.6931472f / (num * halfLife);
		return TrackDampingRatio(targetValue, num, dampingRatio, deltaTime);
	}

	public float TrackExponential(float targetValue, float halfLife, float deltaTime)
	{
		if (halfLife < MathUtil.Epsilon)
		{
			Velocity = 0f;
			Value = targetValue;
			return Value;
		}
		float angularFrequency = 0.6931472f / halfLife;
		float dampingRatio = 1f;
		return TrackDampingRatio(targetValue, angularFrequency, dampingRatio, deltaTime);
	}
}
