using UnityEngine;

namespace BoingKit;

public struct Aabb
{
	public Vector3 Min;

	public Vector3 Max;

	public float MinX
	{
		get
		{
			return Min.x;
		}
		set
		{
			Min.x = value;
		}
	}

	public float MinY
	{
		get
		{
			return Min.y;
		}
		set
		{
			Min.y = value;
		}
	}

	public float MinZ
	{
		get
		{
			return Min.z;
		}
		set
		{
			Min.z = value;
		}
	}

	public float MaxX
	{
		get
		{
			return Max.x;
		}
		set
		{
			Max.x = value;
		}
	}

	public float MaxY
	{
		get
		{
			return Max.y;
		}
		set
		{
			Max.y = value;
		}
	}

	public float MaxZ
	{
		get
		{
			return Max.z;
		}
		set
		{
			Max.z = value;
		}
	}

	public Vector3 Center => 0.5f * (Min + Max);

	public Vector3 Size
	{
		get
		{
			Vector3 result = Max - Min;
			result.x = Mathf.Max(0f, result.x);
			result.y = Mathf.Max(0f, result.y);
			result.z = Mathf.Max(0f, result.z);
			return result;
		}
	}

	public static Aabb Empty => new Aabb(new Vector3(float.MaxValue, float.MaxValue, float.MaxValue), new Vector3(float.MinValue, float.MinValue, float.MinValue));

	public static Aabb FromPoint(Vector3 p)
	{
		Aabb empty = Empty;
		empty.Include(p);
		return empty;
	}

	public static Aabb FromPoints(Vector3 a, Vector3 b)
	{
		Aabb empty = Empty;
		empty.Include(a);
		empty.Include(b);
		return empty;
	}

	public Aabb(Vector3 min, Vector3 max)
	{
		Min = min;
		Max = max;
	}

	public void Include(Vector3 p)
	{
		MinX = Mathf.Min(MinX, p.x);
		MinY = Mathf.Min(MinY, p.y);
		MinZ = Mathf.Min(MinZ, p.z);
		MaxX = Mathf.Max(MaxX, p.x);
		MaxY = Mathf.Max(MaxY, p.y);
		MaxZ = Mathf.Max(MaxZ, p.z);
	}

	public bool Contains(Vector3 p)
	{
		if (MinX <= p.x && MinY <= p.y && MinZ <= p.z && MaxX >= p.x && MaxY >= p.y)
		{
			return MaxZ >= p.z;
		}
		return false;
	}

	public bool ContainsX(Vector3 p)
	{
		if (MinX <= p.x)
		{
			return MaxX >= p.x;
		}
		return false;
	}

	public bool ContainsY(Vector3 p)
	{
		if (MinY <= p.y)
		{
			return MaxY >= p.y;
		}
		return false;
	}

	public bool ContainsZ(Vector3 p)
	{
		if (MinZ <= p.z)
		{
			return MaxZ >= p.z;
		}
		return false;
	}

	public bool Intersects(Aabb rhs)
	{
		if (MinX <= rhs.MaxX && MinY <= rhs.MaxY && MinZ <= rhs.MaxZ && MaxX >= rhs.MinX && MaxY >= rhs.MinY)
		{
			return MaxZ >= rhs.MinZ;
		}
		return false;
	}

	public bool Intersects(ref BoingEffector.Params effector)
	{
		if (!effector.Bits.IsBitSet(0))
		{
			return Intersects(FromPoint(effector.CurrPosition).Expand(effector.Radius));
		}
		return Intersects(FromPoints(effector.PrevPosition, effector.CurrPosition).Expand(effector.Radius));
	}

	public Aabb Expand(float amount)
	{
		MinX -= amount;
		MinY -= amount;
		MinZ -= amount;
		MaxX += amount;
		MaxY += amount;
		MaxZ += amount;
		return this;
	}
}
