using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public static class GTVector3Extensions
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector3 X_Z(this Vector3 vector)
	{
		return new Vector3(vector.x, 0f, vector.z);
	}

	public static Vector3 Sum(this IList<Vector3> vecs)
	{
		Vector3 zero = Vector3.zero;
		for (int i = 0; i < vecs.Count; i++)
		{
			zero += vecs[i];
		}
		return zero;
	}

	public static Vector3 Average(this IList<Vector3> vecs)
	{
		int count = vecs.Count;
		if (count == 0)
		{
			return Vector3.zero;
		}
		Vector3 zero = Vector3.zero;
		for (int i = 0; i < count; i++)
		{
			zero += vecs[i];
		}
		return zero / count;
	}

	public static Vector3 Sum(this IEnumerable<Vector3> vecs)
	{
		Vector3 zero = Vector3.zero;
		foreach (Vector3 vec in vecs)
		{
			zero += vec;
		}
		return zero;
	}

	public static Vector3 Average(this IEnumerable<Vector3> vecs)
	{
		Vector3 zero = Vector3.zero;
		int num = 0;
		foreach (Vector3 vec in vecs)
		{
			zero += vec;
			num++;
		}
		if (num == 0)
		{
			return Vector3.zero;
		}
		return zero / num;
	}
}
