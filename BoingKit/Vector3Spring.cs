using UnityEngine;

namespace BoingKit;

public struct Vector3Spring
{
	public static readonly int Stride = 32;

	public Vector3 Value;

	private float m_padding0;

	public Vector3 Velocity;

	private float m_padding1;

	public void Reset()
	{
		Value = Vector3.zero;
		Velocity = Vector3.zero;
	}

	public void Reset(Vector3 initValue)
	{
		Value = initValue;
		Velocity = Vector3.zero;
	}

	public void Reset(Vector3 initValue, Vector3 initVelocity)
	{
		Value = initValue;
		Velocity = initVelocity;
	}

	public Vector3 TrackDampingRatio(Vector3 targetValue, float angularFrequency, float dampingRatio, float deltaTime)
	{
		if (angularFrequency < MathUtil.Epsilon)
		{
			Velocity = Vector3.zero;
			return Value;
		}
		Vector3 vector = targetValue - Value;
		float num = 1f + 2f * deltaTime * dampingRatio * angularFrequency;
		float num2 = angularFrequency * angularFrequency;
		float num3 = deltaTime * num2;
		float num4 = deltaTime * num3;
		float num5 = 1f / (num + num4);
		Vector3 vector2 = num * Value + deltaTime * Velocity + num4 * targetValue;
		Vector3 vector3 = Velocity + num3 * vector;
		Velocity = vector3 * num5;
		Value = vector2 * num5;
		if (Velocity.magnitude < MathUtil.Epsilon && vector.magnitude < MathUtil.Epsilon)
		{
			Velocity = Vector3.zero;
			Value = targetValue;
		}
		return Value;
	}

	public Vector3 TrackHalfLife(Vector3 targetValue, float frequencyHz, float halfLife, float deltaTime)
	{
		if (halfLife < MathUtil.Epsilon)
		{
			Velocity = Vector3.zero;
			Value = targetValue;
			return Value;
		}
		float num = frequencyHz * MathUtil.TwoPi;
		float dampingRatio = 0.6931472f / (num * halfLife);
		return TrackDampingRatio(targetValue, num, dampingRatio, deltaTime);
	}

	public Vector3 TrackExponential(Vector3 targetValue, float halfLife, float deltaTime)
	{
		if (halfLife < MathUtil.Epsilon)
		{
			Velocity = Vector3.zero;
			Value = targetValue;
			return Value;
		}
		float angularFrequency = 0.6931472f / halfLife;
		float dampingRatio = 1f;
		return TrackDampingRatio(targetValue, angularFrequency, dampingRatio, deltaTime);
	}
}
