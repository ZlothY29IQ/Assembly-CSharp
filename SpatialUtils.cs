using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public static class SpatialUtils
{
	private static readonly Vector3 kMinVector = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);

	private static readonly Vector3 kMaxVector = new Vector3(float.MinValue, float.MinValue, float.MinValue);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int XYZToFlatIndex(int x, int y, int z, int xMax, int yMax)
	{
		return z * xMax * yMax + y * xMax + x;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int XYZToFlatIndex(Vector3Int xyz, int xMax, int yMax)
	{
		return xyz.z * xMax * yMax + xyz.y * xMax + xyz.x;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void FlatIndexToXYZ(int idx, int xMax, int yMax, out int x, out int y, out int z)
	{
		z = idx / (xMax * yMax);
		idx -= z * xMax * yMax;
		y = idx / xMax;
		x = idx % xMax;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector3Int FlatIndexToXYZ(int idx, int xMax, int yMax)
	{
		int num = idx / (xMax * yMax);
		idx -= num * xMax * yMax;
		int y = idx / xMax;
		return new Vector3Int(idx % xMax, y, num);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int CompareByZOrder(Vector3Int a, Vector3Int b)
	{
		ZOrderEncode64((uint)a.x, (uint)a.y, (uint)a.z, out var code);
		ZOrderEncode64((uint)b.x, (uint)b.y, (uint)b.z, out var code2);
		return code.CompareTo(code2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ZOrderEncode64(uint x, uint y, uint z, out ulong code)
	{
		code = Encode64(x) | (Encode64(y) << 1) | (Encode64(z) << 2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ZOrderDecode64(ulong code, out uint x, out uint y, out uint z)
	{
		x = Decode64(code);
		y = Decode64(code >> 1);
		z = Decode64(code >> 2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static ulong Encode64(ulong w)
	{
		w &= 0x1FFFFF;
		w = (w | (w << 32)) & 0x1F00000000FFFFL;
		w = (w | (w << 16)) & 0x1F0000FF0000FFL;
		w = (w | (w << 8)) & 0x10F00F00F00F00FL;
		w = (w | (w << 4)) & 0x10C30C30C30C30C3L;
		w = (w | (w << 2)) & 0x1249249249249249L;
		return w;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static uint Decode64(ulong w)
	{
		w &= 0x1249249249249249L;
		w = (w ^ (w >> 2)) & 0x30C30C30C30C30C3L;
		w = (w ^ (w >> 4)) & 0xF00F00F00F00F00FuL;
		w = (w ^ (w >> 8)) & 0xFF0000FF0000FFL;
		w = (w ^ (w >> 16)) & 0xFF00000000FFFFL;
		w = (w ^ (w >> 32)) & 0x1FFFFF;
		return (uint)w;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static uint ZOrderEncode(uint x, uint y)
	{
		x = (x | (x << 16)) & 0xFFFF;
		x = (x | (x << 8)) & 0xFF00FF;
		x = (x | (x << 4)) & 0xF0F0F0F;
		x = (x | (x << 2)) & 0x33333333;
		x = (x | (x << 1)) & 0x55555555;
		y = (y | (y << 16)) & 0xFFFF;
		y = (y | (y << 8)) & 0xFF00FF;
		y = (y | (y << 4)) & 0xF0F0F0F;
		y = (y | (y << 2)) & 0x33333333;
		y = (y | (y << 1)) & 0x55555555;
		return x | (y << 1);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ZOrderDecode(uint code, out uint x, out uint y)
	{
		x = code & 0x55555555;
		x = (x ^ (x >> 1)) & 0x33333333;
		x = (x ^ (x >> 2)) & 0xF0F0F0F;
		x = (x ^ (x >> 4)) & 0xFF00FF;
		x = (x ^ (x >> 8)) & 0xFFFF;
		y = (code >> 1) & 0x55555555;
		y = (y ^ (y >> 1)) & 0x33333333;
		y = (y ^ (y >> 2)) & 0xF0F0F0F;
		y = (y ^ (y >> 4)) & 0xFF00FF;
		y = (y ^ (y >> 8)) & 0xFFFF;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static uint ZOrderEncode(uint x, uint y, uint z)
	{
		x = (x | (x << 16)) & 0x30000FF;
		x = (x | (x << 8)) & 0x300F00F;
		x = (x | (x << 4)) & 0x30C30C3;
		x = (x | (x << 2)) & 0x9249249;
		y = (y | (y << 16)) & 0x30000FF;
		y = (y | (y << 8)) & 0x300F00F;
		y = (y | (y << 4)) & 0x30C30C3;
		y = (y | (y << 2)) & 0x9249249;
		z = (z | (z << 16)) & 0x30000FF;
		z = (z | (z << 8)) & 0x300F00F;
		z = (z | (z << 4)) & 0x30C30C3;
		z = (z | (z << 2)) & 0x9249249;
		return x | (y << 1) | (z << 2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ZOrderDecode(uint code, out uint x, out uint y, out uint z)
	{
		x = code & 0x9249249;
		x = (x ^ (x >> 2)) & 0x30C30C3;
		x = (x ^ (x >> 4)) & 0x300F00F;
		x = (x ^ (x >> 8)) & 0x30000FF;
		x = (x ^ (x >> 16)) & 0x3FF;
		y = (code >> 1) & 0x9249249;
		y = (y ^ (y >> 2)) & 0x30C30C3;
		y = (y ^ (y >> 4)) & 0x300F00F;
		y = (y ^ (y >> 8)) & 0x30000FF;
		y = (y ^ (y >> 16)) & 0x3FF;
		z = (code >> 2) & 0x9249249;
		z = (z ^ (z >> 2)) & 0x30C30C3;
		z = (z ^ (z >> 4)) & 0x300F00F;
		z = (z ^ (z >> 8)) & 0x30000FF;
		z = (z ^ (z >> 16)) & 0x3FF;
	}

	public static bool TryGetBounds(IList<Renderer> renderers, out Bounds result)
	{
		result = default(Bounds);
		if (renderers == null)
		{
			return false;
		}
		int count = renderers.Count;
		if (count == 0)
		{
			return false;
		}
		Renderer renderer = null;
		int num = 0;
		for (int i = 0; i < count; i++)
		{
			Renderer renderer2 = renderers[i];
			if (renderer == null)
			{
				renderer = renderer2;
				if (renderer != null)
				{
					result = renderer.bounds;
					num++;
				}
			}
			else if (!(renderer2 == null))
			{
				Bounds bounds = renderer2.bounds;
				if (!(bounds.size == Vector3.zero))
				{
					result.Encapsulate(bounds);
					num++;
				}
			}
		}
		return num > 0;
	}

	public static bool TryGetBounds(IList<Collider> colliders, out Bounds result)
	{
		result = default(Bounds);
		if (colliders == null)
		{
			return false;
		}
		int count = colliders.Count;
		if (count == 0)
		{
			return false;
		}
		Collider collider = null;
		int num = 0;
		for (int i = 0; i < count; i++)
		{
			Collider collider2 = colliders[i];
			if (collider == null)
			{
				collider = collider2;
				if (collider != null)
				{
					result = collider.bounds;
					num++;
				}
			}
			else if (!(collider2 == null))
			{
				Bounds bounds = collider2.bounds;
				if (!(bounds.size == Vector3.zero))
				{
					result.Encapsulate(bounds);
					num++;
				}
			}
		}
		return num > 0;
	}

	public static bool TryGetBounds(Transform x, out Bounds result, bool includeRenderers = true, bool includeColliders = true, bool fallbackToXforms = false)
	{
		result = default(Bounds);
		if (x == null)
		{
			return false;
		}
		bool flag = false;
		bool flag2 = false;
		if (includeRenderers)
		{
			flag = TryGetBounds(x.GetComponentsInChildren<Renderer>(), out var result2);
			if (flag)
			{
				result = result2;
			}
		}
		if (includeColliders)
		{
			flag2 = TryGetBounds(x.GetComponentsInChildren<Collider>(), out var result3);
			if (flag2)
			{
				if (flag)
				{
					result.Encapsulate(result3);
				}
				else
				{
					result = result3;
				}
			}
		}
		bool flag3 = flag || flag2;
		if (flag3 || !fallbackToXforms)
		{
			return flag3;
		}
		Transform[] componentsInChildren = x.GetComponentsInChildren<Transform>();
		result.center = componentsInChildren[0].position;
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			result.Encapsulate(componentsInChildren[i].position);
		}
		return true;
	}

	public static BoundingSphere GetRadialBounds(ref Bounds bounds, ref Matrix4x4 xform)
	{
		Vector3 center = bounds.center;
		Vector3 extents = bounds.extents;
		Vector3 vector = new Vector3(extents.x, 0f, 0f);
		Vector3 vector2 = new Vector3(0f, extents.y, 0f);
		Vector3 vector3 = new Vector3(0f, 0f, extents.z);
		Vector3 vector4 = xform.MultiplyPoint(center + vector + vector2 + vector3);
		Vector3 vector5 = xform.MultiplyPoint(center + vector + vector2 - vector3);
		Vector3 vector6 = xform.MultiplyPoint(center - vector + vector2 - vector3);
		Vector3 vector7 = xform.MultiplyPoint(center - vector + vector2 + vector3);
		Vector3 vector8 = xform.MultiplyPoint(center + vector - vector2 + vector3);
		Vector3 vector9 = xform.MultiplyPoint(center + vector - vector2 - vector3);
		Vector3 vector10 = xform.MultiplyPoint(center - vector - vector2 - vector3);
		Vector3 vector11 = xform.MultiplyPoint(center - vector - vector2 + vector3);
		Vector3 vector12 = (vector4 + vector5 + vector6 + vector7 + vector8 + vector9 + vector10 + vector11) * 0.125f;
		float num = 0f;
		float num2 = 0f;
		num2 = DistSq(vector4, vector12);
		if (num2 > num)
		{
			num = num2;
		}
		num2 = DistSq(vector5, vector12);
		if (num2 > num)
		{
			num = num2;
		}
		num2 = DistSq(vector6, vector12);
		if (num2 > num)
		{
			num = num2;
		}
		num2 = DistSq(vector7, vector12);
		if (num2 > num)
		{
			num = num2;
		}
		num2 = DistSq(vector8, vector12);
		if (num2 > num)
		{
			num = num2;
		}
		num2 = DistSq(vector9, vector12);
		if (num2 > num)
		{
			num = num2;
		}
		num2 = DistSq(vector10, vector12);
		if (num2 > num)
		{
			num = num2;
		}
		num2 = DistSq(vector11, vector12);
		if (num2 > num)
		{
			num = num2;
		}
		return new BoundingSphere(vector12, Mathf.Sqrt(num));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static float DistSq(Vector3 a, Vector3 b)
	{
		float num = b.x - a.x;
		float num2 = b.y - a.y;
		float num3 = b.z - a.z;
		return num * num + num2 * num2 + num3 * num3;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector3[] GetCorners(this Bounds b)
	{
		return GetCorners(b.min, b.max);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector3[] GetCorners(Vector3 min, Vector3 max)
	{
		return new Vector3[8]
		{
			new Vector3(min.x, max.y, max.z),
			new Vector3(max.x, max.y, max.z),
			new Vector3(max.x, min.y, max.z),
			new Vector3(min.x, min.y, max.z),
			new Vector3(min.x, max.y, min.z),
			new Vector3(max.x, max.y, min.z),
			new Vector3(max.x, min.y, min.z),
			new Vector3(min.x, min.y, min.z)
		};
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector3[] GetCorners(this Bounds b, Matrix4x4 transform)
	{
		Vector3[] corners = b.GetCorners();
		for (int i = 0; i < corners.Length; i++)
		{
			corners[i] = transform.MultiplyPoint(corners[i]);
		}
		return corners;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Bounds TransformedBy(this Bounds b, Matrix4x4 transform)
	{
		Vector3 position = transform.GetPosition();
		Vector3 vector = transform.MultiplyVector(Vector3.right);
		Vector3 vector2 = transform.MultiplyVector(Vector3.up);
		Vector3 vector3 = transform.MultiplyVector(Vector3.forward);
		Vector3 min = b.min;
		Vector3 max = b.max;
		Vector3 lhs = vector * min.x;
		Vector3 rhs = vector * max.x;
		Vector3 lhs2 = vector2 * min.y;
		Vector3 rhs2 = vector2 * max.y;
		Vector3 lhs3 = vector3 * min.z;
		Vector3 rhs3 = vector3 * max.z;
		b.SetMinMax(Vector3.Min(lhs, rhs) + Vector3.Min(lhs2, rhs2) + Vector3.Min(lhs3, rhs3) + position, Vector3.Max(lhs, rhs) + Vector3.Max(lhs2, rhs2) + Vector3.Max(lhs3, rhs3) + position);
		return b;
	}

	public static bool BoxIntersectsBox(ref Bounds a, ref Bounds b)
	{
		Vector3 min = a.min;
		Vector3 max = a.max;
		Vector3 min2 = b.min;
		Vector3 max2 = b.max;
		if (min.x > max2.x || min2.x > max.x)
		{
			return false;
		}
		if (min.y > max2.y || min2.y > max.y)
		{
			return false;
		}
		if (min.z > max2.z || min2.z > max.z)
		{
			return false;
		}
		return true;
	}

	public static void ComputeBoundingSphere2Pass(Vector3[] points, out Vector3 center, out float radius)
	{
		center = default(Vector3);
		radius = 0f;
		if (!points.IsNullOrEmpty())
		{
			Bounds bounds = GeometryUtility.CalculateBounds(points, Matrix4x4.identity);
			Vector3 center2 = bounds.center;
			float num = (bounds.max - bounds.min).magnitude * 0.5f;
			if (num.Approx0())
			{
				num = 0f;
			}
			ComputeBoundingSphereRitter(points, out var center3, out var radius2);
			bool flag = num < radius2;
			center = (flag ? center2 : center3);
			radius = (flag ? num : radius2);
		}
	}

	public static void ComputeBoundingSphereRitter(Vector3[] points, out Vector3 center, out float radius)
	{
		center = default(Vector3);
		radius = 0f;
		if (points.IsNullOrEmpty())
		{
			return;
		}
		Vector3 vector = kMinVector;
		Vector3 vector2 = kMinVector;
		Vector3 vector3 = kMinVector;
		Vector3 vector4 = kMaxVector;
		Vector3 vector5 = kMaxVector;
		Vector3 vector6 = kMaxVector;
		for (int i = 0; i < points.Length; i++)
		{
			Vector3 vector7 = points[i];
			if (vector7.x < vector.x)
			{
				vector = vector7;
			}
			if (vector7.x > vector4.x)
			{
				vector4 = vector7;
			}
			if (vector7.y < vector2.y)
			{
				vector2 = vector7;
			}
			if (vector7.y > vector5.y)
			{
				vector5 = vector7;
			}
			if (vector7.z < vector3.z)
			{
				vector3 = vector7;
			}
			if (vector7.z > vector6.z)
			{
				vector6 = vector7;
			}
		}
		float num = vector4.x - vector.x;
		float num2 = vector4.y - vector.y;
		float num3 = vector4.z - vector.z;
		float num4 = num * num + num2 * num2 + num3 * num3;
		float num5 = vector5.x - vector2.x;
		num2 = vector5.y - vector2.y;
		num3 = vector5.z - vector2.z;
		float num6 = num5 * num5 + num2 * num2 + num3 * num3;
		float num7 = vector6.x - vector3.x;
		num2 = vector6.y - vector3.y;
		num3 = vector6.z - vector3.z;
		float num8 = num7 * num7 + num2 * num2 + num3 * num3;
		Vector3 vector8 = vector;
		Vector3 vector9 = vector4;
		float num9 = num4;
		if (num6 > num9)
		{
			num9 = num6;
			vector8 = vector2;
			vector9 = vector5;
		}
		if (num8 > num9)
		{
			vector8 = vector3;
			vector9 = vector6;
		}
		center = new Vector3((vector8.x + vector9.x) * 0.5f, (vector8.y + vector9.y) * 0.5f, (vector8.z + vector9.z) * 0.5f);
		float num10 = vector9.x - center.x;
		num2 = vector9.y - center.y;
		num3 = vector9.z - center.z;
		float num11 = num10 * num10 + num2 * num2 + num3 * num3;
		radius = Mathf.Sqrt(num11);
		for (int j = 0; j < points.Length; j++)
		{
			Vector3 vector10 = points[j];
			float num12 = vector10.x - center.x;
			num2 = vector10.y - center.y;
			num3 = vector10.z - center.z;
			float num13 = num12 * num12 + num2 * num2 + num3 * num3;
			if (!(num13 <= num11))
			{
				float num14 = Mathf.Sqrt(num13);
				radius = (radius + num14) * 0.5f;
				num11 = radius * radius;
				float num15 = num14 - radius;
				center.x = (radius * center.x + num15 * vector10.x) / num14;
				center.y = (radius * center.y + num15 * vector10.y) / num14;
				center.z = (radius * center.z + num15 * vector10.z) / num14;
			}
		}
	}
}
