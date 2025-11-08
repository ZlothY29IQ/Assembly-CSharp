using System;
using System.Runtime.CompilerServices;
using UnityEngine;

public static class UnityEngineUtils
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool EqualsColor(this Color32 c, Color32 other)
	{
		if (c.r == other.r && c.g == other.g && c.b == other.b)
		{
			return c.a == other.a;
		}
		return false;
	}

	public static Color32 IdToColor32(this UnityEngine.Object obj, int alpha = -1, bool distinct = true)
	{
		if (!(obj == null))
		{
			return obj.GetInstanceID().IdToColor32(alpha, distinct);
		}
		return default(Color32);
	}

	public static Color32 IdToColor32(this int id, int alpha = -1, bool distinct = true)
	{
		if (distinct)
		{
			id = StaticHash.ComputeTriple32(id);
		}
		Color32 result = Unsafe.As<int, Color32>(ref id);
		if (alpha > -1)
		{
			result.a = (byte)Math.Clamp(alpha, 0, 255);
		}
		return result;
	}

	public static Color32 ToHighViz(this Color32 c)
	{
		Color.RGBToHSV(c, out var H, out var _, out var _);
		return Color.HSVToRGB(H, 1f, 1f);
	}

	public static int Color32ToId(this Color32 c, bool distinct = true)
	{
		int num = Unsafe.As<Color32, int>(ref c);
		if (distinct)
		{
			num = StaticHash.ReverseTriple32(num);
		}
		return num;
	}

	public static Hash128 QuantizedHash128(this Matrix4x4 m)
	{
		Hash128 hash = default(Hash128);
		HashUtilities.QuantisedMatrixHash(ref m, ref hash);
		return hash;
	}

	public static Hash128 QuantizedHash128(this Vector3 v)
	{
		Hash128 hash = default(Hash128);
		HashUtilities.QuantisedVectorHash(ref v, ref hash);
		return hash;
	}

	public static Id128 QuantizedId128(this Vector3 v)
	{
		return v.QuantizedHash128();
	}

	public static Id128 QuantizedId128(this Matrix4x4 m)
	{
		return m.QuantizedHash128();
	}

	public static Id128 QuantizedId128(this Quaternion q)
	{
		int a = (int)((double)q.x * 1000.0 + 0.5);
		int b = (int)((double)q.y * 1000.0 + 0.5);
		int c = (int)((double)q.z * 1000.0 + 0.5);
		int d = (int)((double)q.w * 1000.0 + 0.5);
		return new Id128(a, b, c, d);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static long QuantizedHash64(this Vector4 v)
	{
		int a = (int)((double)v.x * 1000.0 + 0.5);
		int b = (int)((double)v.y * 1000.0 + 0.5);
		int a2 = (int)((double)v.z * 1000.0 + 0.5);
		int b2 = (int)((double)v.w * 1000.0 + 0.5);
		ulong a3 = MergeTo64(a, b);
		ulong b3 = MergeTo64(a2, b2);
		return StaticHash.Compute128To64(a3, b3);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static long QuantizedHash64(this Matrix4x4 m)
	{
		m4x4 obj = m4x4.From(ref m);
		long a = obj.r0.QuantizedHash64();
		long b = obj.r1.QuantizedHash64();
		long a2 = obj.r2.QuantizedHash64();
		long b2 = obj.r3.QuantizedHash64();
		long a3 = StaticHash.Compute128To64(a, b);
		long b3 = StaticHash.Compute128To64(a2, b2);
		return StaticHash.Compute128To64(a3, b3);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static ulong MergeTo64(int a, int b)
	{
		return ((ulong)(uint)b << 32) | (uint)a;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector4 ToVector(this Quaternion q)
	{
		return Unsafe.As<Quaternion, Vector4>(ref q);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void CopyTo(this ref Quaternion q, ref Vector4 v)
	{
		v.x = q.x;
		v.y = q.y;
		v.z = q.z;
		v.w = q.w;
	}
}
