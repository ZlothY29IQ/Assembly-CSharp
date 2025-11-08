using UnityEngine;

namespace BoingKit;

public class QuaternionUtil
{
	public enum SterpMode
	{
		Nlerp,
		Slerp
	}

	public static float Magnitude(Quaternion q)
	{
		return Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
	}

	public static float MagnitudeSqr(Quaternion q)
	{
		return q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w;
	}

	public static Quaternion Normalize(Quaternion q)
	{
		float num = 1f / Magnitude(q);
		return new Quaternion(num * q.x, num * q.y, num * q.z, num * q.w);
	}

	public static Quaternion AxisAngle(Vector3 axis, float angle)
	{
		float f = 0.5f * angle;
		float num = Mathf.Sin(f);
		float w = Mathf.Cos(f);
		return new Quaternion(num * axis.x, num * axis.y, num * axis.z, w);
	}

	public static Vector3 GetAxis(Quaternion q)
	{
		Vector3 vector = new Vector3(q.x, q.y, q.z);
		float magnitude = vector.magnitude;
		if (magnitude < MathUtil.Epsilon)
		{
			return Vector3.left;
		}
		return vector / magnitude;
	}

	public static float GetAngle(Quaternion q)
	{
		return 2f * Mathf.Acos(Mathf.Clamp(q.w, -1f, 1f));
	}

	public static Quaternion FromAngularVector(Vector3 v)
	{
		float magnitude = v.magnitude;
		if (magnitude < MathUtil.Epsilon)
		{
			return Quaternion.identity;
		}
		v /= magnitude;
		float f = 0.5f * magnitude;
		float num = Mathf.Sin(f);
		float w = Mathf.Cos(f);
		return new Quaternion(num * v.x, num * v.y, num * v.z, w);
	}

	public static Vector3 ToAngularVector(Quaternion q)
	{
		Vector3 axis = GetAxis(q);
		return GetAngle(q) * axis;
	}

	public static Quaternion Pow(Quaternion q, float exp)
	{
		Vector3 axis = GetAxis(q);
		float angle = GetAngle(q) * exp;
		return AxisAngle(axis, angle);
	}

	public static Quaternion Integrate(Quaternion q, Quaternion v, float dt)
	{
		return Pow(v, dt) * q;
	}

	public static Quaternion Integrate(Quaternion q, Vector3 omega, float dt)
	{
		omega *= 0.5f;
		Quaternion quaternion = new Quaternion(omega.x, omega.y, omega.z, 0f) * q;
		return Normalize(new Quaternion(q.x + quaternion.x * dt, q.y + quaternion.y * dt, q.z + quaternion.z * dt, q.w + quaternion.w * dt));
	}

	public static Vector4 ToVector4(Quaternion q)
	{
		return new Vector4(q.x, q.y, q.z, q.w);
	}

	public static Quaternion FromVector4(Vector4 v, bool normalize = true)
	{
		if (normalize)
		{
			float sqrMagnitude = v.sqrMagnitude;
			if (sqrMagnitude < MathUtil.Epsilon)
			{
				return Quaternion.identity;
			}
			v /= Mathf.Sqrt(sqrMagnitude);
		}
		return new Quaternion(v.x, v.y, v.z, v.w);
	}

	public static void DecomposeSwingTwist(Quaternion q, Vector3 twistAxis, out Quaternion swing, out Quaternion twist)
	{
		Vector3 vector = new Vector3(q.x, q.y, q.z);
		if (vector.sqrMagnitude < MathUtil.Epsilon)
		{
			Vector3 vector2 = q * twistAxis;
			Vector3 axis = Vector3.Cross(twistAxis, vector2);
			if (axis.sqrMagnitude > MathUtil.Epsilon)
			{
				float angle = Vector3.Angle(twistAxis, vector2);
				swing = Quaternion.AngleAxis(angle, axis);
			}
			else
			{
				swing = Quaternion.identity;
			}
			twist = Quaternion.AngleAxis(180f, twistAxis);
		}
		else
		{
			Vector3 vector3 = Vector3.Project(vector, twistAxis);
			twist = new Quaternion(vector3.x, vector3.y, vector3.z, q.w);
			twist = Normalize(twist);
			swing = q * Quaternion.Inverse(twist);
		}
	}

	public static Quaternion Sterp(Quaternion a, Quaternion b, Vector3 twistAxis, float t, SterpMode mode = SterpMode.Slerp)
	{
		Quaternion swing;
		Quaternion twist;
		return Sterp(a, b, twistAxis, t, out swing, out twist, mode);
	}

	public static Quaternion Sterp(Quaternion a, Quaternion b, Vector3 twistAxis, float t, out Quaternion swing, out Quaternion twist, SterpMode mode = SterpMode.Slerp)
	{
		return Sterp(a, b, twistAxis, t, t, out swing, out twist, mode);
	}

	public static Quaternion Sterp(Quaternion a, Quaternion b, Vector3 twistAxis, float tSwing, float tTwist, SterpMode mode = SterpMode.Slerp)
	{
		Quaternion swing;
		Quaternion twist;
		return Sterp(a, b, twistAxis, tSwing, tTwist, out swing, out twist, mode);
	}

	public static Quaternion Sterp(Quaternion a, Quaternion b, Vector3 twistAxis, float tSwing, float tTwist, out Quaternion swing, out Quaternion twist, SterpMode mode)
	{
		DecomposeSwingTwist(b * Quaternion.Inverse(a), twistAxis, out var swing2, out var twist2);
		if (mode == SterpMode.Nlerp || mode != SterpMode.Slerp)
		{
			swing = Quaternion.Lerp(Quaternion.identity, swing2, tSwing);
			twist = Quaternion.Lerp(Quaternion.identity, twist2, tTwist);
		}
		else
		{
			swing = Quaternion.Slerp(Quaternion.identity, swing2, tSwing);
			twist = Quaternion.Slerp(Quaternion.identity, twist2, tTwist);
		}
		return twist * swing;
	}
}
