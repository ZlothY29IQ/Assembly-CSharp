using System;
using System.Runtime.CompilerServices;

public static class GTBitOps
{
	public readonly struct BitWriteInfo
	{
		public readonly int index;

		public readonly int valueMask;

		public readonly int clearMask;

		public BitWriteInfo(int index, int count)
		{
			this.index = index;
			valueMask = GetValueMask(count);
			clearMask = GetClearMask(index, valueMask);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int GetValueMask(int count)
	{
		return (1 << count) - 1;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int GetClearMask(int index, int valueMask)
	{
		return ~(valueMask << index);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int GetClearMaskByCount(int index, int count)
	{
		return ~((1 << count) - 1 << index);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int ReadBits(int bits, int index, int valueMask)
	{
		return (bits >> index) & valueMask;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int ReadBits(int bits, BitWriteInfo info)
	{
		return (bits >> info.index) & info.valueMask;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int ReadBitsByCount(int bits, int index, int count)
	{
		return (bits >> index) & ((1 << count) - 1);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool ReadBit(int bits, int index)
	{
		return ((bits >> index) & 1) == 1;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void WriteBits(ref int bits, BitWriteInfo info, int value)
	{
		bits = (bits & info.clearMask) | ((value & info.valueMask) << info.index);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int WriteBits(int bits, BitWriteInfo info, int value)
	{
		WriteBits(ref bits, info, value);
		return bits;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void WriteBits(ref int bits, int index, int valueMask, int clearMask, int value)
	{
		bits = (bits & clearMask) | ((value & valueMask) << index);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int WriteBits(int bits, int index, int valueMask, int clearMask, int value)
	{
		WriteBits(ref bits, index, valueMask, clearMask, value);
		return bits;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void WriteBitsByCount(ref int bits, int index, int count, int value)
	{
		bits = (bits & ~((1 << count) - 1 << index)) | ((value & ((1 << count) - 1)) << index);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int WriteBitsByCount(int bits, int index, int count, int value)
	{
		WriteBitsByCount(ref bits, index, count, value);
		return bits;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void WriteBit(ref int bits, int index, bool value)
	{
		bits = (bits & ~(1 << index)) | ((value ? 1 : 0) << index);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int WriteBit(int bits, int index, bool value)
	{
		WriteBit(ref bits, index, value);
		return bits;
	}

	public static string ToBinaryString(int number)
	{
		return Convert.ToString(number, 2).PadLeft(32, '0');
	}
}
