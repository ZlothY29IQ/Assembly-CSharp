using System;
using UnityEngine;

[Serializable]
public struct BoundsInt
{
	private const int SCALE_FACTOR = 1000;

	public Vector3Int min;

	public Vector3Int max;

	public Vector3Int center => (min + max) / 2;

	public Vector3Int size => max - min;

	public Vector3 centerFloat => IntToFloat(center);

	public Vector3 sizeFloat => IntToFloat(size);

	public BoundsInt(Vector3Int min, Vector3Int max)
	{
		this.min = min;
		this.max = max;
	}

	public BoundsInt(Vector3 center, Vector3 size)
	{
		Vector3 vector = size * 0.5f;
		min = FloatToInt(center - vector);
		max = FloatToInt(center + vector);
	}

	public static Vector3Int FloatToInt(Vector3 v)
	{
		return new Vector3Int(Mathf.RoundToInt(v.x * 1000f), Mathf.RoundToInt(v.y * 1000f), Mathf.RoundToInt(v.z * 1000f));
	}

	public static Vector3 IntToFloat(Vector3Int v)
	{
		return new Vector3((float)v.x / 1000f, (float)v.y / 1000f, (float)v.z / 1000f);
	}

	public static BoundsInt FromBounds(Bounds bounds)
	{
		return new BoundsInt(bounds.center, bounds.size);
	}

	public Bounds ToBounds()
	{
		return new Bounds(centerFloat, sizeFloat);
	}

	public void SetMinMax(Vector3Int min, Vector3Int max)
	{
		this.min = min;
		this.max = max;
	}

	public void SetMinMax(Vector3 min, Vector3 max)
	{
		this.min = FloatToInt(min);
		this.max = FloatToInt(max);
	}

	public void Encapsulate(BoundsInt other)
	{
		min = new Vector3Int(Mathf.Min(min.x, other.min.x), Mathf.Min(min.y, other.min.y), Mathf.Min(min.z, other.min.z));
		max = new Vector3Int(Mathf.Max(max.x, other.max.x), Mathf.Max(max.y, other.max.y), Mathf.Max(max.z, other.max.z));
	}

	public void Expand(float amount)
	{
		int num = Mathf.RoundToInt(amount * 1000f);
		Vector3Int vector3Int = new Vector3Int(num, num, num);
		min -= vector3Int;
		max += vector3Int;
	}

	public bool Intersects(BoundsInt other)
	{
		if (min.x < other.max.x && max.x > other.min.x && min.y < other.max.y && max.y > other.min.y)
		{
			if (min.z < other.max.z)
			{
				return max.z > other.min.z;
			}
			return false;
		}
		return false;
	}

	public bool Contains(BoundsInt other)
	{
		if (min.x <= other.min.x && min.y <= other.min.y && min.z <= other.min.z && max.x >= other.max.x && max.y >= other.max.y)
		{
			return max.z >= other.max.z;
		}
		return false;
	}

	public bool Contains(Vector3 point)
	{
		Vector3Int vector3Int = FloatToInt(point);
		if (vector3Int.x >= min.x && vector3Int.x <= max.x && vector3Int.y >= min.y && vector3Int.y <= max.y && vector3Int.z >= min.z)
		{
			return vector3Int.z <= max.z;
		}
		return false;
	}

	public BoundsInt GetIntersection(BoundsInt other)
	{
		Vector3Int vector3Int = new Vector3Int(Mathf.Max(min.x, other.min.x), Mathf.Max(min.y, other.min.y), Mathf.Max(min.z, other.min.z));
		Vector3Int vector3Int2 = new Vector3Int(Mathf.Min(max.x, other.max.x), Mathf.Min(max.y, other.max.y), Mathf.Min(max.z, other.max.z));
		if (vector3Int.x > vector3Int2.x || vector3Int.y > vector3Int2.y || vector3Int.z > vector3Int2.z)
		{
			return new BoundsInt(Vector3Int.zero, Vector3Int.zero);
		}
		return new BoundsInt(vector3Int, vector3Int2);
	}

	public long Volume()
	{
		Vector3Int vector3Int = size;
		return (long)vector3Int.x * (long)vector3Int.y * vector3Int.z;
	}

	public float VolumeFloat()
	{
		return (float)Volume() / 1E+09f;
	}

	public static bool operator ==(BoundsInt a, BoundsInt b)
	{
		if (a.min == b.min)
		{
			return a.max == b.max;
		}
		return false;
	}

	public static bool operator !=(BoundsInt a, BoundsInt b)
	{
		return !(a == b);
	}

	public override bool Equals(object obj)
	{
		if (obj is BoundsInt boundsInt)
		{
			return this == boundsInt;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return min.GetHashCode() ^ (max.GetHashCode() << 2);
	}

	public override string ToString()
	{
		return $"BoundsInt(min: {min}, max: {max})";
	}
}
