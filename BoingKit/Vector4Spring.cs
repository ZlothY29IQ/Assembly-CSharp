using UnityEngine;

namespace BoingKit;

public struct Vector4Spring
{
	public static readonly int Stride = 32;

	public Vector4 Value;

	public Vector4 Velocity;

	public void Reset()
	{
		Value = Vector4.zero;
		Velocity = Vector4.zero;
	}

	public void Reset(Vector4 initValue)
	{
		Value = initValue;
		Velocity = Vector4.zero;
	}

	public void Reset(Vector4 initValue, Vector4 initVelocity)
	{
		Value = initValue;
		Velocity = initVelocity;
	}

	public Vector4 TrackDampingRatio(Vector4 targetValue, float angularFrequency, float dampingRatio, float deltaTime)
	{
		if (angularFrequency < MathUtil.Epsilon)
		{
			Velocity = Vector4.zero;
			return Value;
		}
		Vector4 vector = targetValue - Value;
		float num = 1f + 2f * deltaTime * dampingRatio * angularFrequency;
		float num2 = angularFrequency * angularFrequency;
		float num3 = deltaTime * num2;
		float num4 = deltaTime * num3;
		float num5 = 1f / (num + num4);
		Vector4 vector2 = num * Value + deltaTime * Velocity + num4 * targetValue;
		Vector4 vector3 = Velocity + num3 * vector;
		Velocity = vector3 * num5;
		Value = vector2 * num5;
		if (Velocity.magnitude < MathUtil.Epsilon && vector.magnitude < MathUtil.Epsilon)
		{
			Velocity = Vector4.zero;
			Value = targetValue;
		}
		return Value;
	}

	public Vector4 TrackHalfLife(Vector4 targetValue, float frequencyHz, float halfLife, float deltaTime)
	{
		if (halfLife < MathUtil.Epsilon)
		{
			Velocity = Vector4.zero;
			Value = targetValue;
			return Value;
		}
		float num = frequencyHz * MathUtil.TwoPi;
		float dampingRatio = 0.6931472f / (num * halfLife);
		return TrackDampingRatio(targetValue, num, dampingRatio, deltaTime);
	}

	public Vector4 TrackExponential(Vector4 targetValue, float halfLife, float deltaTime)
	{
		if (halfLife < MathUtil.Epsilon)
		{
			Velocity = Vector4.zero;
			Value = targetValue;
			return Value;
		}
		float angularFrequency = 0.6931472f / halfLife;
		float dampingRatio = 1f;
		return TrackDampingRatio(targetValue, angularFrequency, dampingRatio, deltaTime);
	}
}
