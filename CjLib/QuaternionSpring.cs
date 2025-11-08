using UnityEngine;

namespace CjLib;

public struct QuaternionSpring
{
	public static readonly int Stride = 32;

	public Vector4 ValueVec;

	public Vector4 VelocityVec;

	public Quaternion ValueQuat
	{
		get
		{
			return QuaternionUtil.FromVector4(ValueVec);
		}
		set
		{
			ValueVec = QuaternionUtil.ToVector4(value);
		}
	}

	public Quaternion VelocityQuat
	{
		get
		{
			return QuaternionUtil.FromVector4(VelocityVec, normalize: false);
		}
		set
		{
			VelocityVec = QuaternionUtil.ToVector4(value);
		}
	}

	public void Reset()
	{
		ValueVec = QuaternionUtil.ToVector4(Quaternion.identity);
		VelocityVec = Vector4.zero;
	}

	public void Reset(Vector4 initValue)
	{
		ValueVec = initValue;
		VelocityVec = Vector4.zero;
	}

	public void Reset(Vector4 initValue, Vector4 initVelocity)
	{
		ValueVec = initValue;
		VelocityVec = initVelocity;
	}

	public void Reset(Quaternion initValue)
	{
		ValueVec = QuaternionUtil.ToVector4(initValue);
		VelocityVec = Vector4.zero;
	}

	public void Reset(Quaternion initValue, Quaternion initVelocity)
	{
		ValueVec = QuaternionUtil.ToVector4(initValue);
		VelocityVec = QuaternionUtil.ToVector4(initVelocity);
	}

	public Quaternion TrackDampingRatio(Quaternion targetValue, float angularFrequency, float dampingRatio, float deltaTime)
	{
		if (angularFrequency < MathUtil.Epsilon)
		{
			VelocityVec = QuaternionUtil.ToVector4(Quaternion.identity);
			return QuaternionUtil.FromVector4(ValueVec);
		}
		Vector4 vector = QuaternionUtil.ToVector4(targetValue);
		if (Vector4.Dot(ValueVec, vector) < 0f)
		{
			vector = -vector;
		}
		Vector4 vector2 = vector - ValueVec;
		float num = 1f + 2f * deltaTime * dampingRatio * angularFrequency;
		float num2 = angularFrequency * angularFrequency;
		float num3 = deltaTime * num2;
		float num4 = deltaTime * num3;
		float num5 = 1f / (num + num4);
		Vector4 vector3 = num * ValueVec + deltaTime * VelocityVec + num4 * vector;
		Vector4 vector4 = VelocityVec + num3 * vector2;
		VelocityVec = vector4 * num5;
		ValueVec = vector3 * num5;
		if (VelocityVec.magnitude < MathUtil.Epsilon && vector2.magnitude < MathUtil.Epsilon)
		{
			VelocityVec = QuaternionUtil.ToVector4(Quaternion.identity);
			ValueVec = vector;
		}
		return QuaternionUtil.FromVector4(ValueVec);
	}

	public Quaternion TrackHalfLife(Quaternion targetValue, float frequencyHz, float halfLife, float deltaTime)
	{
		if (halfLife < MathUtil.Epsilon)
		{
			VelocityVec = QuaternionUtil.ToVector4(Quaternion.identity);
			ValueVec = QuaternionUtil.ToVector4(targetValue);
			return targetValue;
		}
		float num = frequencyHz * MathUtil.TwoPi;
		float dampingRatio = 0.6931472f / (num * halfLife);
		return TrackDampingRatio(targetValue, num, dampingRatio, deltaTime);
	}

	public Quaternion TrackExponential(Quaternion targetValue, float halfLife, float deltaTime)
	{
		if (halfLife < MathUtil.Epsilon)
		{
			VelocityVec = QuaternionUtil.ToVector4(Quaternion.identity);
			ValueVec = QuaternionUtil.ToVector4(targetValue);
			return targetValue;
		}
		float angularFrequency = 0.6931472f / halfLife;
		float dampingRatio = 1f;
		return TrackDampingRatio(targetValue, angularFrequency, dampingRatio, deltaTime);
	}
}
