using Unity.Mathematics;
using UnityEngine;

public static class MathHelper
{
	public static float RoundTo(this float value, float increment)
	{
		return Mathf.Floor(value / increment + 0.5f) * increment;
	}

	public static Vector3 RoundTo(this Vector3 value, float increment)
	{
		value.x = Mathf.Floor(value.x / increment + 0.5f) * increment;
		value.y = Mathf.Floor(value.y / increment + 0.5f) * increment;
		value.z = Mathf.Floor(value.z / increment + 0.5f) * increment;
		return value;
	}

	public static Vector3 SnapToInt(this Vector3 value)
	{
		value.x = Mathf.Floor(value.x + 0.5f);
		value.y = Mathf.Floor(value.y + 0.5f);
		value.z = Mathf.Floor(value.z + 0.5f);
		return value;
	}

	public static Quaternion RoundTo(this Quaternion value, float increment)
	{
		Vector3 eulerAngles = value.eulerAngles;
		eulerAngles.x = Mathf.Floor(eulerAngles.x / increment + 0.5f) * increment;
		eulerAngles.y = Mathf.Floor(eulerAngles.y / increment + 0.5f) * increment;
		eulerAngles.z = Mathf.Floor(eulerAngles.z / increment + 0.5f) * increment;
		value.eulerAngles = eulerAngles;
		return value;
	}

	public static Quaternion SnapToCardinal(this Quaternion value)
	{
		Vector3 eulerAngles = value.eulerAngles;
		eulerAngles.x = (eulerAngles.z = 0f);
		eulerAngles.y = Mathf.Floor(eulerAngles.y / 90f + 0.5f) * 90f;
		return Quaternion.Euler(eulerAngles);
	}

	public static bool IsInBounds(this int3 a, int3 min, int3 max)
	{
		if (min.x <= a.x && max.x >= a.x && min.y <= a.y && max.y >= a.y && min.z <= a.z)
		{
			return max.z >= a.z;
		}
		return false;
	}
}
