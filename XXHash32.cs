using System;
using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

public static class XXHash32
{
	private const uint Prime32_1 = 2654435761u;

	private const uint Prime32_2 = 2246822519u;

	private const uint Prime32_3 = 3266489917u;

	private const uint Prime32_4 = 668265263u;

	private const uint Prime32_5 = 374761393u;

	private const int StripeSize = 16;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Compute(string s, uint seed = 0u)
	{
		return (int)Compute(Encoding.Unicode.GetBytes(s), seed);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static uint Compute(ReadOnlySpan<byte> input, uint seed = 0u)
	{
		int length = input.Length;
		uint num9;
		if (length >= 16)
		{
			uint num = seed + 606290984;
			uint num2 = seed + 2246822519u;
			uint num3 = seed;
			uint num4 = seed - 2654435761u;
			do
			{
				uint num5 = BinaryPrimitives.ReadUInt32LittleEndian(input);
				uint num6 = BinaryPrimitives.ReadUInt32LittleEndian(input.Slice(4));
				uint num7 = BinaryPrimitives.ReadUInt32LittleEndian(input.Slice(8));
				uint num8 = BinaryPrimitives.ReadUInt32LittleEndian(input.Slice(12));
				num += (uint)((int)num5 * -2048144777);
				num = BitOperations.RotateLeft(num, 13);
				num *= 2654435761u;
				num2 += (uint)((int)num6 * -2048144777);
				num2 = BitOperations.RotateLeft(num2, 13);
				num2 *= 2654435761u;
				num3 += (uint)((int)num7 * -2048144777);
				num3 = BitOperations.RotateLeft(num3, 13);
				num3 *= 2654435761u;
				num4 += (uint)((int)num8 * -2048144777);
				num4 = BitOperations.RotateLeft(num4, 13);
				num4 *= 2654435761u;
				input = input.Slice(16);
			}
			while (input.Length >= 16);
			num9 = BitOperations.RotateLeft(num, 1) + BitOperations.RotateLeft(num2, 7) + BitOperations.RotateLeft(num3, 12) + BitOperations.RotateLeft(num4, 18);
		}
		else
		{
			num9 = seed + 374761393;
		}
		num9 += (uint)length;
		while (input.Length >= 4)
		{
			num9 += (uint)((int)BinaryPrimitives.ReadUInt32LittleEndian(input) * -1028477379);
			num9 = BitOperations.RotateLeft(num9, 17) * 668265263;
			input = input.Slice(4);
		}
		for (int i = 0; i < input.Length; i++)
		{
			num9 += (uint)(input[i] * 374761393);
			num9 = BitOperations.RotateLeft(num9, 11) * 2654435761u;
		}
		num9 ^= num9 >> 15;
		num9 *= 2246822519u;
		num9 ^= num9 >> 13;
		num9 *= 3266489917u;
		return num9 ^ (num9 >> 16);
	}
}
