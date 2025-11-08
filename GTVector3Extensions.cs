using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

public static class GTVector3Extensions
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector3 X_Z(this Vector3 vector)
	{
		return new Vector3(vector.x, 0f, vector.z);
	}

	public static Vector3 Sum(this IEnumerable<Vector3> vecs)
	{
		Vector3 zero = Vector3.zero;
		for (int i = 0; i < vecs.Count(); i++)
		{
			zero += vecs.ElementAt(i);
		}
		return zero;
	}

	public static Vector3 Average(this IEnumerable<Vector3> vecs)
	{
		return vecs.Sum() / vecs.Count();
	}
}
