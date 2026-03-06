using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

[Serializable]
public struct SRand
{
	[SerializeField]
	private uint _seed;

	[SerializeField]
	private uint _state;

	private const double MAX_AS_DOUBLE = 268435456.0;

	private const uint MAX_PLUS_ONE = 268435457u;

	private const double STEP_SIZE = 3.725290298461914E-09;

	private const float ONE_THIRD = 1f / 3f;

	public SRand(int seed)
	{
		_seed = (uint)seed;
		_state = _seed;
	}

	public SRand(uint seed)
	{
		_seed = seed;
		_state = _seed;
	}

	public SRand(long seed)
	{
		_seed = (uint)StaticHash.Compute(seed);
		_state = _seed;
	}

	public SRand(DateTime seed)
	{
		_seed = (uint)StaticHash.Compute(seed);
		_state = _seed;
	}

	public SRand(string seed)
	{
		if (string.IsNullOrEmpty(seed))
		{
			throw new ArgumentException("Seed cannot be null or empty", "seed");
		}
		_seed = (uint)StaticHash.Compute(seed);
		_state = _seed;
	}

	public SRand(byte[] seed)
	{
		if (seed == null || seed.Length == 0)
		{
			throw new ArgumentException("Seed cannot be null or empty", "seed");
		}
		_seed = (uint)StaticHash.Compute(seed);
		_state = _seed;
	}

	public double NextDouble()
	{
		return (double)(NextState() % 268435457) * 3.725290298461914E-09;
	}

	public double NextDouble(double max)
	{
		if (max < 0.0)
		{
			return 0.0;
		}
		return NextDouble() * max;
	}

	public double NextDouble(double min, double max)
	{
		double num = max - min;
		if (num <= 0.0)
		{
			return min;
		}
		double num2 = NextDouble() * num;
		return min + num2;
	}

	public float NextFloat()
	{
		return (float)NextDouble();
	}

	public float NextFloat(float max)
	{
		return (float)NextDouble(max);
	}

	public float NextFloat(float min, float max)
	{
		return (float)NextDouble(min, max);
	}

	public bool NextBool()
	{
		return NextState() % 2 == 1;
	}

	public uint NextUInt()
	{
		return NextState();
	}

	public int NextInt()
	{
		return (int)NextState();
	}

	public int NextInt(int max)
	{
		if (max <= 0)
		{
			return 0;
		}
		return (int)(NextState() % max);
	}

	public int NextInt(int min, int max)
	{
		int num = max - min;
		if (num <= 0)
		{
			return min;
		}
		return min + NextInt(num);
	}

	public int NextIntWithExclusion(int min, int max, int exclude)
	{
		int num = max - min - 1;
		if (num <= 0)
		{
			return min;
		}
		int num2 = min + 1 + NextInt(num);
		if (num2 > exclude)
		{
			return num2;
		}
		return num2 - 1;
	}

	public int NextIntWithExclusion2(int min, int max, int exclude, int exclude2)
	{
		if (exclude == exclude2)
		{
			return NextIntWithExclusion(min, max, exclude);
		}
		int num = max - min - 2;
		if (num <= 0)
		{
			return min;
		}
		int num2 = min + 2 + NextInt(num);
		int num5;
		int num6;
		if (exclude >= exclude2)
		{
			int num3 = exclude2 + 1;
			int num4 = exclude;
			num5 = num3;
			num6 = num4;
		}
		else
		{
			int num7 = exclude + 1;
			int num4 = exclude2;
			num5 = num7;
			num6 = num4;
		}
		if (num2 <= num5)
		{
			return num2 - 2;
		}
		if (num2 <= num6)
		{
			return num2 - 1;
		}
		return num2;
	}

	public byte NextByte()
	{
		return (byte)(NextState() & 0xFF);
	}

	public Color32 NextColor32()
	{
		byte r = NextByte();
		byte g = NextByte();
		byte b = NextByte();
		return new Color32(r, g, b, byte.MaxValue);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vector3 NextPointInsideSphere(float radius)
	{
		float num = NextFloat() * 2f - 1f;
		float num2 = NextFloat() * 2f - 1f;
		float num3 = NextFloat() * 2f - 1f;
		float num4 = MathF.Pow(NextFloat(), 1f / 3f);
		float num5 = 1f / MathF.Sqrt(num * num + num2 * num2 + num3 * num3);
		return new Vector3(num * num5 * num4 * radius, num2 * num5 * num4 * radius, num3 * num5 * num4 * radius);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vector3 NextPointOnSphere(float radius)
	{
		float num = NextFloat() * 2f - 1f;
		float num2 = NextFloat() * 2f - 1f;
		float num3 = NextFloat() * 2f - 1f;
		float num4 = 1f / MathF.Sqrt(num * num + num2 * num2 + num3 * num3);
		return new Vector3(num * num4 * radius, num2 * num4 * radius, num3 * num4 * radius);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vector3 NextPointInsideBox(Vector3 extents)
	{
		float num = NextFloat() - 0.5f;
		float num2 = NextFloat() - 0.5f;
		return new Vector3(z: (NextFloat() - 0.5f) * extents.z, x: num * extents.x, y: num2 * extents.y);
	}

	public Color NextColor()
	{
		float r = NextFloat();
		float g = NextFloat();
		float b = NextFloat();
		return new Color(r, g, b, 1f);
	}

	public void Shuffle<T>(T[] array)
	{
		int num = array.Length;
		while (num > 1)
		{
			int num2 = NextInt(num--);
			int num3 = num;
			int num4 = num2;
			T val = array[num2];
			T val2 = array[num];
			array[num3] = val;
			array[num4] = val2;
		}
	}

	public void Shuffle<T>(List<T> list)
	{
		int count = list.Count;
		while (count > 1)
		{
			int num = NextInt(count--);
			int index = count;
			int index2 = num;
			T val = list[num];
			T val2 = list[count];
			T val3 = (list[index] = val);
			val3 = (list[index2] = val2);
		}
	}

	public void Reset()
	{
		_state = _seed;
	}

	public void Reset(int seed)
	{
		_seed = (uint)seed;
		_state = _seed;
	}

	public void Reset(uint seed)
	{
		_seed = seed;
		_state = _seed;
	}

	public void Reset(long seed)
	{
		_seed = (uint)StaticHash.Compute(seed);
		_state = _seed;
	}

	public void Reset(DateTime seed)
	{
		_seed = (uint)StaticHash.Compute(seed);
		_state = _seed;
	}

	public void Reset(string seed)
	{
		if (string.IsNullOrEmpty(seed))
		{
			throw new ArgumentException("Seed cannot be null or empty", "seed");
		}
		_seed = (uint)StaticHash.Compute(seed);
		_state = _seed;
	}

	public void Reset(byte[] seed)
	{
		if (seed == null || seed.Length == 0)
		{
			throw new ArgumentException("Seed cannot be null or empty", "seed");
		}
		_seed = (uint)StaticHash.Compute(seed);
		_state = _seed;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private uint NextState()
	{
		return _state = Mix(_state + 184402071);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private uint Mix(uint x)
	{
		x = ((x >> 17) ^ x) * 3982152891u;
		x = ((x >> 11) ^ x) * 2890668881u;
		x = ((x >> 15) ^ x) * 830770091;
		x = (x >> 14) ^ x;
		return x;
	}

	public override int GetHashCode()
	{
		return StaticHash.Compute((int)_seed, (int)_state);
	}

	public override string ToString()
	{
		return string.Format("{0} {{ {1}: {2:X8} {3}: {4:X8} }}", "SRand", "_seed", _seed, "_state", _state);
	}

	public static SRand New()
	{
		return new SRand(DateTime.UtcNow);
	}

	public static explicit operator SRand(int seed)
	{
		return new SRand(seed);
	}

	public static explicit operator SRand(uint seed)
	{
		return new SRand(seed);
	}

	public static explicit operator SRand(long seed)
	{
		return new SRand(seed);
	}

	public static explicit operator SRand(string seed)
	{
		return new SRand(seed);
	}

	public static explicit operator SRand(byte[] seed)
	{
		return new SRand(seed);
	}

	public static explicit operator SRand(DateTime seed)
	{
		return new SRand(seed);
	}
}
