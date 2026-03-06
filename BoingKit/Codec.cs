using System.Runtime.InteropServices;
using UnityEngine;

namespace BoingKit;

public class Codec
{
	[StructLayout(LayoutKind.Explicit)]
	private struct IntFloat
	{
		[FieldOffset(0)]
		public int IntValue;

		[FieldOffset(0)]
		public float FloatValue;
	}

	public static readonly int FnvDefaultBasis = -2128831035;

	public static readonly int FnvPrime = 16777619;

	public static float PackSaturated(float a, float b)
	{
		a = Mathf.Floor(a * 4095f);
		b = Mathf.Floor(b * 4095f);
		return a * 4096f + b;
	}

	public static float PackSaturated(Vector2 v)
	{
		return PackSaturated(v.x, v.y);
	}

	public static Vector2 UnpackSaturated(float f)
	{
		return new Vector2(Mathf.Floor(f / 4096f), Mathf.Repeat(f, 4096f)) / 4095f;
	}

	public static Vector2 OctWrap(Vector2 v)
	{
		return (Vector2.one - new Vector2(Mathf.Abs(v.y), Mathf.Abs(v.x))) * new Vector2(Mathf.Sign(v.x), Mathf.Sign(v.y));
	}

	public static float PackNormal(Vector3 n)
	{
		n /= Mathf.Abs(n.x) + Mathf.Abs(n.y) + Mathf.Abs(n.z);
		return PackSaturated(((n.z >= 0f) ? new Vector2(n.x, n.y) : OctWrap(new Vector2(n.x, n.y))) * 0.5f + 0.5f * Vector2.one);
	}

	public static Vector3 UnpackNormal(float f)
	{
		Vector2 vector = UnpackSaturated(f);
		vector = vector * 2f - Vector2.one;
		Vector3 vector2 = new Vector3(vector.x, vector.y, 1f - Mathf.Abs(vector.x) - Mathf.Abs(vector.y));
		float num = Mathf.Clamp01(0f - vector2.z);
		vector2.x += ((vector2.x >= 0f) ? (0f - num) : num);
		vector2.y += ((vector2.y >= 0f) ? (0f - num) : num);
		return vector2.normalized;
	}

	public static uint PackRgb(Color color)
	{
		return ((uint)(color.b * 255f) << 16) | ((uint)(color.g * 255f) << 8) | (uint)(color.r * 255f);
	}

	public static Color UnpackRgb(uint i)
	{
		return new Color((float)(i & 0xFF) / 255f, (float)((i & 0xFF00) >> 8) / 255f, (float)((i & 0xFF0000) >> 16) / 255f);
	}

	public static uint PackRgba(Color color)
	{
		return ((uint)(color.a * 255f) << 24) | ((uint)(color.b * 255f) << 16) | ((uint)(color.g * 255f) << 8) | (uint)(color.r * 255f);
	}

	public static Color UnpackRgba(uint i)
	{
		return new Color((float)(i & 0xFF) / 255f, (float)((i & 0xFF00) >> 8) / 255f, (float)((i & 0xFF0000) >> 16) / 255f, (float)((i & 0xFF000000u) >> 24) / 255f);
	}

	public static uint Pack8888(uint x, uint y, uint z, uint w)
	{
		return ((x & 0xFF) << 24) | ((y & 0xFF) << 16) | ((z & 0xFF) << 8) | (w & 0xFF);
	}

	public static void Unpack8888(uint i, out uint x, out uint y, out uint z, out uint w)
	{
		x = (i >> 24) & 0xFF;
		y = (i >> 16) & 0xFF;
		z = (i >> 8) & 0xFF;
		w = i & 0xFF;
	}

	private static int IntReinterpret(float f)
	{
		IntFloat intFloat = new IntFloat
		{
			FloatValue = f
		};
		return intFloat.IntValue;
	}

	public static int HashConcat(int hash, int i)
	{
		return (hash ^ i) * FnvPrime;
	}

	public static int HashConcat(int hash, long i)
	{
		hash = HashConcat(hash, (int)(i & 0xFFFFFFFFu));
		hash = HashConcat(hash, (int)(i >> 32));
		return hash;
	}

	public static int HashConcat(int hash, float f)
	{
		return HashConcat(hash, IntReinterpret(f));
	}

	public static int HashConcat(int hash, bool b)
	{
		return HashConcat(hash, b ? 1 : 0);
	}

	public static int HashConcat(int hash, params int[] ints)
	{
		foreach (int i2 in ints)
		{
			hash = HashConcat(hash, i2);
		}
		return hash;
	}

	public static int HashConcat(int hash, params float[] floats)
	{
		foreach (float f in floats)
		{
			hash = HashConcat(hash, f);
		}
		return hash;
	}

	public static int HashConcat(int hash, Vector2 v)
	{
		return HashConcat(hash, v.x, v.y);
	}

	public static int HashConcat(int hash, Vector3 v)
	{
		return HashConcat(hash, v.x, v.y, v.z);
	}

	public static int HashConcat(int hash, Vector4 v)
	{
		return HashConcat(hash, v.x, v.y, v.z, v.w);
	}

	public static int HashConcat(int hash, Quaternion q)
	{
		return HashConcat(hash, q.x, q.y, q.z, q.w);
	}

	public static int HashConcat(int hash, Color c)
	{
		return HashConcat(hash, c.r, c.g, c.b, c.a);
	}

	public static int HashConcat(int hash, Transform t)
	{
		return HashConcat(hash, t.GetHashCode());
	}

	public static int Hash(int i)
	{
		return HashConcat(FnvDefaultBasis, i);
	}

	public static int Hash(long i)
	{
		return HashConcat(FnvDefaultBasis, i);
	}

	public static int Hash(float f)
	{
		return HashConcat(FnvDefaultBasis, f);
	}

	public static int Hash(bool b)
	{
		return HashConcat(FnvDefaultBasis, b);
	}

	public static int Hash(params int[] ints)
	{
		return HashConcat(FnvDefaultBasis, ints);
	}

	public static int Hash(params float[] floats)
	{
		return HashConcat(FnvDefaultBasis, floats);
	}

	public static int Hash(Vector2 v)
	{
		return HashConcat(FnvDefaultBasis, v);
	}

	public static int Hash(Vector3 v)
	{
		return HashConcat(FnvDefaultBasis, v);
	}

	public static int Hash(Vector4 v)
	{
		return HashConcat(FnvDefaultBasis, v);
	}

	public static int Hash(Quaternion q)
	{
		return HashConcat(FnvDefaultBasis, q);
	}

	public static int Hash(Color c)
	{
		return HashConcat(FnvDefaultBasis, c);
	}

	private static int HashTransformHierarchyRecurvsive(int hash, Transform t)
	{
		hash = HashConcat(hash, t);
		hash = HashConcat(hash, t.childCount);
		for (int i = 0; i < t.childCount; i++)
		{
			hash = HashTransformHierarchyRecurvsive(hash, t.GetChild(i));
		}
		return hash;
	}

	public static int HashTransformHierarchy(Transform t)
	{
		return HashTransformHierarchyRecurvsive(FnvDefaultBasis, t);
	}
}
