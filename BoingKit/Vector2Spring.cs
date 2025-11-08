using UnityEngine;

namespace BoingKit;

public struct Vector2Spring
{
	public static readonly int Stride = 16;

	public Vector2 Value;

	public Vector2 Velocity;

	public void Reset()
	{
		Value = Vector2.zero;
		Velocity = Vector2.zero;
	}

	public void Reset(Vector2 initValue)
	{
		Value = initValue;
		Velocity = Vector2.zero;
	}

	public void Reset(Vector2 initValue, Vector2 initVelocity)
	{
		Value = initValue;
		Velocity = initVelocity;
	}

	public Vector2 TrackDampingRatio(Vector2 targetValue, float angularFrequency, float dampingRatio, float deltaTime)
	{
		if (angularFrequency < MathUtil.Epsilon)
		{
			Velocity = Vector2.zero;
			return Value;
		}
		Vector2 vector = targetValue - Value;
		float num = 1f + 2f * deltaTime * dampingRatio * angularFrequency;
		float num2 = angularFrequency * angularFrequency;
		float num3 = deltaTime * num2;
		float num4 = deltaTime * num3;
		float num5 = 1f / (num + num4);
		Vector2 vector2 = num * Value + deltaTime * Velocity + num4 * targetValue;
		Vector2 vector3 = Velocity + num3 * vector;
		Velocity = vector3 * num5;
		Value = vector2 * num5;
		if (Velocity.magnitude < MathUtil.Epsilon && vector.magnitude < MathUtil.Epsilon)
		{
			Velocity = Vector2.zero;
			Value = targetValue;
		}
		return Value;
	}

	public Vector2 TrackHalfLife(Vector2 targetValue, float frequencyHz, float halfLife, float deltaTime)
	{
		if (halfLife < MathUtil.Epsilon)
		{
			Velocity = Vector2.zero;
			Value = targetValue;
			return Value;
		}
		float num = frequencyHz * MathUtil.TwoPi;
		float dampingRatio = 0.6931472f / (num * halfLife);
		return TrackDampingRatio(targetValue, num, dampingRatio, deltaTime);
	}

	public Vector2 TrackExponential(Vector2 targetValue, float halfLife, float deltaTime)
	{
		if (halfLife < MathUtil.Epsilon)
		{
			Velocity = Vector2.zero;
			Value = targetValue;
			return Value;
		}
		float angularFrequency = 0.6931472f / halfLife;
		float dampingRatio = 1f;
		return TrackDampingRatio(targetValue, angularFrequency, dampingRatio, deltaTime);
	}
}
