using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public static class StaticHash
{
	[StructLayout(LayoutKind.Explicit)]
	private struct SingleInt32
	{
		[FieldOffset(0)]
		public float single;

		[FieldOffset(0)]
		public int int32;
	}

	[StructLayout(LayoutKind.Explicit)]
	private struct DoubleInt64
	{
		[FieldOffset(0)]
		public double @double;

		[FieldOffset(0)]
		public long int64;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static uint ComputeU(uint u)
	{
		uint num = u;
		num = num + 2127912214 + (num << 12);
		num = num ^ 0xC761C23Cu ^ (num >> 19);
		num = num + 374761393 + (num << 5);
		num = (uint)((int)num + -744332180) ^ (num << 9);
		num = (uint)((int)num + -42973499) + (num << 3);
		return num ^ 0xB55A4F09u ^ (num >> 16);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static uint ComputeU(int i)
	{
		return ComputeU((uint)i);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Compute(int i)
	{
		return (int)ComputeU(i);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Compute(uint u)
	{
		return (int)ComputeU(u);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Compute(float f)
	{
		return Compute(Unsafe.As<float, uint>(ref f));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Compute(float f1, float f2)
	{
		int i = Compute(f1);
		int i2 = Compute(f2);
		return Compute(i, i2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Compute(float f1, float f2, float f3)
	{
		int i = Compute(f1);
		int i2 = Compute(f2);
		int i3 = Compute(f3);
		return Compute(i, i2, i3);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Compute(float f1, float f2, float f3, float f4)
	{
		int i = Compute(f1);
		int i2 = Compute(f2);
		int i3 = Compute(f3);
		int i4 = Compute(f4);
		return Compute(i, i2, i3, i4);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static ulong ComputeUL(ulong h)
	{
		h = ~h + (h << 18);
		h ^= h >> 31;
		h *= 21;
		h ^= h >> 11;
		h += h << 6;
		h ^= h >> 22;
		return h;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Compute(long l)
	{
		return (int)ComputeUL((ulong)l);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Compute(long l1, long l2)
	{
		int i = Compute(l1);
		int i2 = Compute(l2);
		return Compute(i, i2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Compute(long l1, long l2, long l3)
	{
		int i = Compute(l1);
		int i2 = Compute(l2);
		int i3 = Compute(l3);
		return Compute(i, i2, i3);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Compute(long l1, long l2, long l3, long l4)
	{
		int i = Compute(l1);
		int i2 = Compute(l2);
		int i3 = Compute(l3);
		int i4 = Compute(l4);
		return Compute(i, i2, i3, i4);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Compute(double d)
	{
		return Compute(Unsafe.As<double, long>(ref d));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Compute(double d1, double d2)
	{
		int i = Compute(d1);
		int i2 = Compute(d2);
		return Compute(i, i2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Compute(double d1, double d2, double d3)
	{
		int i = Compute(d1);
		int i2 = Compute(d2);
		int i3 = Compute(d3);
		return Compute(i, i2, i3);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Compute(double d1, double d2, double d3, double d4)
	{
		int i = Compute(d1);
		int i2 = Compute(d2);
		int i3 = Compute(d3);
		int i4 = Compute(d4);
		return Compute(i, i2, i3, i4);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Compute(bool b)
	{
		if (!b)
		{
			return 1800329511;
		}
		return -1266253386;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Compute(bool b1, bool b2)
	{
		int i = Compute(b1);
		int i2 = Compute(b2);
		return Compute(i, i2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Compute(bool b1, bool b2, bool b3)
	{
		int i = Compute(b1);
		int i2 = Compute(b2);
		int i3 = Compute(b3);
		return Compute(i, i2, i3);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Compute(bool b1, bool b2, bool b3, bool b4)
	{
		int i = Compute(b1);
		int i2 = Compute(b2);
		int i3 = Compute(b3);
		int i4 = Compute(b4);
		return Compute(i, i2, i3, i4);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Compute(DateTime dt)
	{
		return Compute(dt.ToBinary());
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Compute(string s)
	{
		if (s == null || s.Length == 0)
		{
			return 0;
		}
		int length = s.Length;
		uint num = (uint)length;
		int num2 = length & 1;
		length >>= 1;
		int num3 = 0;
		while (length > 0)
		{
			num += s[num3];
			uint num4 = ((uint)s[num3 + 1] << 11) ^ num;
			num = (num << 16) ^ num4;
			num3 += 2;
			num += num >> 11;
			length--;
		}
		if (num2 == 1)
		{
			num += s[num3];
			num ^= num << 11;
			num += num >> 17;
		}
		num ^= num << 3;
		num += num >> 5;
		num ^= num << 4;
		num += num >> 17;
		num ^= num << 25;
		return (int)(num + (num >> 6));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Compute(string s1, string s2)
	{
		int i = Compute(s1);
		int i2 = Compute(s2);
		return Compute(i, i2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Compute(string s1, string s2, string s3)
	{
		int i = Compute(s1);
		int i2 = Compute(s2);
		int i3 = Compute(s3);
		return Compute(i, i2, i3);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Compute(string s1, string s2, string s3, string s4)
	{
		int i = Compute(s1);
		int i2 = Compute(s2);
		int i3 = Compute(s3);
		int i4 = Compute(s4);
		return Compute(i, i2, i3, i4);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Compute(byte[] bytes)
	{
		if (bytes == null || bytes.Length == 0)
		{
			return 0;
		}
		int num = bytes.Length;
		uint num2 = (uint)num;
		int num3 = num & 1;
		num >>= 1;
		int num4 = 0;
		while (num > 0)
		{
			num2 += bytes[num4];
			uint num5 = (uint)(bytes[num4 + 1] << 11) ^ num2;
			num2 = (num2 << 16) ^ num5;
			num4 += 2;
			num2 += num2 >> 11;
			num--;
		}
		if (num3 == 1)
		{
			num2 += bytes[num4];
			num2 ^= num2 << 11;
			num2 += num2 >> 17;
		}
		num2 ^= num2 << 3;
		num2 += num2 >> 5;
		num2 ^= num2 << 4;
		num2 += num2 >> 17;
		num2 ^= num2 << 25;
		return (int)(num2 + (num2 >> 6));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Compute(int i1, int i2)
	{
		uint num = 3735928567u;
		uint num2 = num;
		uint c = num;
		num += (uint)i1;
		num2 += (uint)i2;
		Finalize(ref num, ref num2, ref c);
		return (int)c;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Compute(int i1, int i2, int i3)
	{
		uint num = 3735928571u;
		uint num2 = num;
		uint num3 = num;
		num += (uint)i1;
		num2 += (uint)i2;
		num3 += (uint)i3;
		Finalize(ref num, ref num2, ref num3);
		return (int)num3;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Compute(int i1, int i2, int i3, int i4)
	{
		uint num = 3735928575u;
		uint num2 = num;
		uint num3 = num;
		num += (uint)i1;
		num2 += (uint)i2;
		num3 += (uint)i3;
		Mix(ref num, ref num2, ref num3);
		num += (uint)i4;
		Finalize(ref num, ref num2, ref num3);
		return (int)num3;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Compute(int[] values)
	{
		if (values == null || values.Length == 0)
		{
			return 224428569;
		}
		int num = values.Length;
		uint a = (uint)(-559038737 + (num << 2));
		uint b = a;
		uint c = a;
		int i;
		for (i = 0; num - i > 3; i += 3)
		{
			a += (uint)values[i];
			b += (uint)values[i + 1];
			c += (uint)values[i + 2];
			Mix(ref a, ref b, ref c);
		}
		if (num - i > 2)
		{
			c += (uint)values[i + 2];
		}
		if (num - i > 1)
		{
			b += (uint)values[i + 1];
		}
		if (num - i > 0)
		{
			a += (uint)values[i];
			Finalize(ref a, ref b, ref c);
		}
		return (int)c;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Compute(uint[] values)
	{
		if (values == null || values.Length == 0)
		{
			return 224428569;
		}
		int num = values.Length;
		uint a = (uint)(-559038737 + (num << 2));
		uint b = a;
		uint c = a;
		int i;
		for (i = 0; num - i > 3; i += 3)
		{
			a += values[i];
			b += values[i + 1];
			c += values[i + 2];
			Mix(ref a, ref b, ref c);
		}
		if (num - i > 2)
		{
			c += values[i + 2];
		}
		if (num - i > 1)
		{
			b += values[i + 1];
		}
		if (num - i > 0)
		{
			a += values[i];
			Finalize(ref a, ref b, ref c);
		}
		return (int)c;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Compute(uint u1, uint u2)
	{
		uint num = 3735928567u;
		uint num2 = num;
		uint c = num;
		num += u1;
		num2 += u2;
		Finalize(ref num, ref num2, ref c);
		return (int)c;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Compute(uint u1, uint u2, uint u3)
	{
		uint num = 3735928571u;
		uint num2 = num;
		uint num3 = num;
		num += u1;
		num2 += u2;
		num3 += u3;
		Finalize(ref num, ref num2, ref num3);
		return (int)num3;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Compute(uint u1, uint u2, uint u3, uint u4)
	{
		uint num = 3735928575u;
		uint num2 = num;
		uint num3 = num;
		num += u1;
		num2 += u2;
		num3 += u3;
		Mix(ref num, ref num2, ref num3);
		num += u4;
		Finalize(ref num, ref num2, ref num3);
		return (int)num3;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int ComputeOrderAgnostic(int[] values)
	{
		if (values == null || values.Length == 0)
		{
			return 0;
		}
		uint num = (uint)Compute(values[0]);
		if (values.Length == 1)
		{
			return (int)num;
		}
		for (int i = 1; i < values.Length; i++)
		{
			num += (uint)Compute(values[i]);
		}
		return (int)num;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static long Compute128To64(long a, long b)
	{
		ulong num = (ulong)((b ^ a) * -7070675565921424023L);
		num ^= num >> 47;
		long num2 = (long)((ulong)a ^ num) * -7070675565921424023L;
		return (num2 ^ (num2 >>> 47)) * -7070675565921424023L;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static long Compute128To64(ulong a, ulong b)
	{
		ulong num = (b ^ a) * 11376068507788127593uL;
		num ^= num >> 47;
		long num2 = (long)(a ^ num) * -7070675565921424023L;
		return (num2 ^ (num2 >>> 47)) * -7070675565921424023L;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int ComputeTriple32(int i)
	{
		int num = i + 1;
		int num2 = (num ^ (num >>> 17)) * -312814405;
		int num3 = (num2 ^ (num2 >>> 11)) * -1404298415;
		int num4 = (num3 ^ (num3 >>> 15)) * 830770091;
		return num4 ^ (num4 >>> 14);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int ReverseTriple32(int i)
	{
		uint num = (uint)i;
		num ^= (num >> 14) ^ (num >> 28);
		num *= 850532099;
		num ^= (num >> 15) ^ (num >> 30);
		num *= 1184763313;
		num ^= (num >> 11) ^ (num >> 22);
		num *= 2041073779;
		num ^= num >> 17;
		return (int)(num - 1);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void Mix(ref uint a, ref uint b, ref uint c)
	{
		a -= c;
		a ^= Rotate(c, 4);
		c += b;
		b -= a;
		b ^= Rotate(a, 6);
		a += c;
		c -= b;
		c ^= Rotate(b, 8);
		b += a;
		a -= c;
		a ^= Rotate(c, 16);
		c += b;
		b -= a;
		b ^= Rotate(a, 19);
		a += c;
		c -= b;
		c ^= Rotate(b, 4);
		b += a;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void Finalize(ref uint a, ref uint b, ref uint c)
	{
		c ^= b;
		c -= Rotate(b, 14);
		a ^= c;
		a -= Rotate(c, 11);
		b ^= a;
		b -= Rotate(a, 25);
		c ^= b;
		c -= Rotate(b, 16);
		a ^= c;
		a -= Rotate(c, 4);
		b ^= a;
		b -= Rotate(a, 14);
		c ^= b;
		c -= Rotate(b, 24);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static uint Rotate(uint x, int k)
	{
		return (x << k) | (x >> 32 - k);
	}
}
