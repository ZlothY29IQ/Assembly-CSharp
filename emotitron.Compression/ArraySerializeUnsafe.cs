namespace emotitron.Compression;

public static class ArraySerializeUnsafe
{
	private const string bufferOverrunMsg = "Byte buffer overrun. Dataloss will occur.";

	public unsafe static void WriteSigned(ulong* buffer, int value, ref int bitposition, int bits)
	{
		uint num = (uint)((value << 1) ^ (value >> 31));
		Write(buffer, num, ref bitposition, bits);
	}

	public unsafe static void AppendSigned(ulong* buffer, int value, ref int bitposition, int bits)
	{
		uint num = (uint)((value << 1) ^ (value >> 31));
		Append(buffer, num, ref bitposition, bits);
	}

	public unsafe static void AddSigned(this int value, ulong* uPtr, ref int bitposition, int bits)
	{
		uint num = (uint)((value << 1) ^ (value >> 31));
		Append(uPtr, num, ref bitposition, bits);
	}

	public unsafe static void AddSigned(this short value, ulong* uPtr, ref int bitposition, int bits)
	{
		uint num = (uint)((value << 1) ^ (value >> 31));
		Append(uPtr, num, ref bitposition, bits);
	}

	public unsafe static void AddSigned(this sbyte value, ulong* uPtr, ref int bitposition, int bits)
	{
		uint num = (uint)((value << 1) ^ (value >> 31));
		Append(uPtr, num, ref bitposition, bits);
	}

	public unsafe static void InjectSigned(this int value, ulong* uPtr, ref int bitposition, int bits)
	{
		uint num = (uint)((value << 1) ^ (value >> 31));
		Write(uPtr, num, ref bitposition, bits);
	}

	public unsafe static void InjectSigned(this short value, ulong* uPtr, ref int bitposition, int bits)
	{
		uint num = (uint)((value << 1) ^ (value >> 31));
		Write(uPtr, num, ref bitposition, bits);
	}

	public unsafe static void InjectSigned(this sbyte value, ulong* uPtr, ref int bitposition, int bits)
	{
		uint num = (uint)((value << 1) ^ (value >> 31));
		Write(uPtr, num, ref bitposition, bits);
	}

	public unsafe static void PokeSigned(this int value, ulong* uPtr, int bitposition, int bits)
	{
		uint num = (uint)((value << 1) ^ (value >> 31));
		Write(uPtr, num, ref bitposition, bits);
	}

	public unsafe static void PokeSigned(this short value, ulong* uPtr, int bitposition, int bits)
	{
		uint num = (uint)((value << 1) ^ (value >> 31));
		Write(uPtr, num, ref bitposition, bits);
	}

	public unsafe static void PokeSigned(this sbyte value, ulong* uPtr, int bitposition, int bits)
	{
		uint num = (uint)((value << 1) ^ (value >> 31));
		Write(uPtr, num, ref bitposition, bits);
	}

	public unsafe static int ReadSigned(ulong* uPtr, ref int bitposition, int bits)
	{
		uint num = (uint)Read(uPtr, ref bitposition, bits);
		return (int)((num >> 1) ^ (int)(0 - (num & 1)));
	}

	public unsafe static int PeekSigned(ulong* uPtr, int bitposition, int bits)
	{
		uint num = (uint)Read(uPtr, ref bitposition, bits);
		return (int)((num >> 1) ^ (int)(0 - (num & 1)));
	}

	public unsafe static void Append(ulong* uPtr, ulong value, ref int bitposition, int bits)
	{
		if (bits != 0)
		{
			int num = bitposition & 0x3F;
			int num2 = bitposition >> 6;
			ulong num3 = (ulong)((1L << num) - 1);
			uPtr[num2 + 1] = (uPtr[num2] = (uPtr[num2] & num3) | (value << num)) >> 64 - num;
			bitposition += bits;
		}
	}

	public unsafe static void Write(ulong* uPtr, ulong value, ref int bitposition, int bits)
	{
		if (bits != 0)
		{
			int num = bitposition & 0x3F;
			int num2 = bitposition >> 6;
			ulong num3 = ulong.MaxValue >> 64 - bits;
			ulong num4 = num3 << num;
			ulong num5 = value << num;
			uPtr[num2] = (uPtr[num2] & ~num4) | (num5 & num4);
			num = 64 - num;
			if (num < bits)
			{
				num4 = num3 >> num;
				num5 = value >> num;
				num2++;
				uPtr[num2] = (uPtr[num2] & ~num4) | (num5 & num4);
			}
			bitposition += bits;
		}
	}

	public unsafe static ulong Read(ulong* uPtr, ref int bitposition, int bits)
	{
		if (bits == 0)
		{
			return 0uL;
		}
		int num = bitposition & 0x3F;
		int num2 = bitposition >> 6;
		ulong num3 = ulong.MaxValue >> 64 - bits;
		ulong num4 = uPtr[num2] >> num;
		for (num = 64 - num; num < bits; num += 64)
		{
			num2++;
			num4 |= uPtr[num2] << num;
		}
		bitposition += bits;
		return num4 & num3;
	}

	public unsafe static ulong Read(ulong* uPtr, int bitposition, int bits)
	{
		if (bits == 0)
		{
			return 0uL;
		}
		int num = bitposition & 0x3F;
		int num2 = bitposition >> 6;
		ulong num3 = ulong.MaxValue >> 64 - bits;
		ulong num4 = uPtr[num2] >> num;
		for (num = 64 - num; num < bits; num += 64)
		{
			num2++;
			num4 |= uPtr[num2] << num;
		}
		bitposition += bits;
		return num4 & num3;
	}

	public unsafe static void Add(this ulong value, ulong* uPtr, int bitposition, int bits)
	{
		Append(uPtr, value, ref bitposition, bits);
	}

	public unsafe static void Add(this uint value, ulong* uPtr, int bitposition, int bits)
	{
		Append(uPtr, value, ref bitposition, bits);
	}

	public unsafe static void Add(this ushort value, ulong* uPtr, int bitposition, int bits)
	{
		Append(uPtr, value, ref bitposition, bits);
	}

	public unsafe static void Add(this byte value, ulong* uPtr, int bitposition, int bits)
	{
		Append(uPtr, value, ref bitposition, bits);
	}

	public unsafe static void AddUnsigned(this long value, ulong* uPtr, int bitposition, int bits)
	{
		Append(uPtr, (ulong)value, ref bitposition, bits);
	}

	public unsafe static void AddUnsigned(this int value, ulong* uPtr, int bitposition, int bits)
	{
		Append(uPtr, (ulong)value, ref bitposition, bits);
	}

	public unsafe static void AddUnsigned(this short value, ulong* uPtr, int bitposition, int bits)
	{
		Append(uPtr, (ulong)value, ref bitposition, bits);
	}

	public unsafe static void AddUnsigned(this sbyte value, ulong* uPtr, int bitposition, int bits)
	{
		Append(uPtr, (ulong)value, ref bitposition, bits);
	}

	public unsafe static void Inject(this ulong value, ulong* uPtr, ref int bitposition, int bits)
	{
		Write(uPtr, value, ref bitposition, bits);
	}

	public unsafe static void Inject(this uint value, ulong* uPtr, ref int bitposition, int bits)
	{
		Write(uPtr, value, ref bitposition, bits);
	}

	public unsafe static void Inject(this ushort value, ulong* uPtr, ref int bitposition, int bits)
	{
		Write(uPtr, value, ref bitposition, bits);
	}

	public unsafe static void Inject(this byte value, ulong* uPtr, ref int bitposition, int bits)
	{
		Write(uPtr, value, ref bitposition, bits);
	}

	public unsafe static void InjectUnsigned(this long value, ulong* uPtr, ref int bitposition, int bits)
	{
		Write(uPtr, (ulong)value, ref bitposition, bits);
	}

	public unsafe static void InjectUnsigned(this int value, ulong* uPtr, ref int bitposition, int bits)
	{
		Write(uPtr, (ulong)value, ref bitposition, bits);
	}

	public unsafe static void InjectUnsigned(this short value, ulong* uPtr, int bitposition, int bits)
	{
		Write(uPtr, (ulong)value, ref bitposition, bits);
	}

	public unsafe static void InjectUnsigned(this sbyte value, ulong* uPtr, ref int bitposition, int bits)
	{
		Write(uPtr, (ulong)value, ref bitposition, bits);
	}

	public unsafe static void Poke(this ulong value, ulong* uPtr, int bitposition, int bits)
	{
		Write(uPtr, value, ref bitposition, bits);
	}

	public unsafe static void Poke(this uint value, ulong* uPtr, int bitposition, int bits)
	{
		Write(uPtr, value, ref bitposition, bits);
	}

	public unsafe static void Poke(this ushort value, ulong* uPtr, int bitposition, int bits)
	{
		Write(uPtr, value, ref bitposition, bits);
	}

	public unsafe static void Poke(this byte value, ulong* uPtr, int bitposition, int bits)
	{
		Write(uPtr, value, ref bitposition, bits);
	}

	public unsafe static void InjectUnsigned(this long value, ulong* uPtr, int bitposition, int bits)
	{
		Write(uPtr, (ulong)value, ref bitposition, bits);
	}

	public unsafe static void InjectUnsigned(this int value, ulong* uPtr, int bitposition, int bits)
	{
		Write(uPtr, (ulong)value, ref bitposition, bits);
	}

	public unsafe static void PokeUnsigned(this short value, ulong* uPtr, int bitposition, int bits)
	{
		Write(uPtr, (ulong)value, ref bitposition, bits);
	}

	public unsafe static void PokeUnsigned(this sbyte value, ulong* uPtr, int bitposition, int bits)
	{
		Write(uPtr, (ulong)value, ref bitposition, bits);
	}

	public unsafe static void ReadOutUnsafe(ulong* sourcePtr, int sourcePos, ulong* targetPtr, ref int targetPos, int bits)
	{
		if (bits != 0)
		{
			int bitposition = sourcePos;
			int num = bits;
			while (num > 0)
			{
				int num2 = ((num > 64) ? 64 : num);
				ulong value = Read(sourcePtr, ref bitposition, num2);
				Write(targetPtr, value, ref targetPos, num2);
				num -= num2;
			}
			targetPos += bits;
		}
	}

	public unsafe static void ReadOutUnsafe(this ulong[] source, int sourcePos, byte[] target, ref int targetPos, int bits)
	{
		if (bits == 0)
		{
			return;
		}
		int bitposition = sourcePos;
		int num = bits;
		fixed (ulong* uPtr2 = source)
		{
			fixed (byte* ptr = target)
			{
				ulong* uPtr = (ulong*)ptr;
				while (num > 0)
				{
					int num2 = ((num > 64) ? 64 : num);
					ulong value = Read(uPtr2, ref bitposition, num2);
					Write(uPtr, value, ref targetPos, num2);
					num -= num2;
				}
			}
		}
		targetPos += bits;
	}

	public unsafe static void ReadOutUnsafe(this ulong[] source, int sourcePos, uint[] target, ref int targetPos, int bits)
	{
		if (bits == 0)
		{
			return;
		}
		int bitposition = sourcePos;
		int num = bits;
		fixed (ulong* uPtr2 = source)
		{
			fixed (uint* ptr = target)
			{
				ulong* uPtr = (ulong*)ptr;
				while (num > 0)
				{
					int num2 = ((num > 64) ? 64 : num);
					ulong value = Read(uPtr2, ref bitposition, num2);
					Write(uPtr, value, ref targetPos, num2);
					num -= num2;
				}
			}
		}
		targetPos += bits;
	}

	public unsafe static void ReadOutUnsafe(this ulong[] source, int sourcePos, ulong[] target, ref int targetPos, int bits)
	{
		if (bits == 0)
		{
			return;
		}
		int bitposition = sourcePos;
		int num = bits;
		fixed (ulong* uPtr = source)
		{
			fixed (ulong* uPtr2 = target)
			{
				while (num > 0)
				{
					int num2 = ((num > 64) ? 64 : num);
					ulong value = Read(uPtr, ref bitposition, num2);
					Write(uPtr2, value, ref targetPos, num2);
					num -= num2;
				}
			}
		}
		targetPos += bits;
	}

	public unsafe static void ReadOutUnsafe(this uint[] source, int sourcePos, byte[] target, ref int targetPos, int bits)
	{
		if (bits == 0)
		{
			return;
		}
		int bitposition = sourcePos;
		int num = bits;
		fixed (uint* ptr = source)
		{
			fixed (byte* ptr2 = target)
			{
				ulong* uPtr = (ulong*)ptr;
				ulong* uPtr2 = (ulong*)ptr2;
				while (num > 0)
				{
					int num2 = ((num > 64) ? 64 : num);
					ulong value = Read(uPtr, ref bitposition, num2);
					Write(uPtr2, value, ref targetPos, num2);
					num -= num2;
				}
			}
		}
		targetPos += bits;
	}

	public unsafe static void ReadOutUnsafe(this uint[] source, int sourcePos, uint[] target, ref int targetPos, int bits)
	{
		if (bits == 0)
		{
			return;
		}
		int bitposition = sourcePos;
		int num = bits;
		fixed (uint* ptr = source)
		{
			fixed (uint* ptr2 = target)
			{
				ulong* uPtr = (ulong*)ptr;
				ulong* uPtr2 = (ulong*)ptr2;
				while (num > 0)
				{
					int num2 = ((num > 64) ? 64 : num);
					ulong value = Read(uPtr, ref bitposition, num2);
					Write(uPtr2, value, ref targetPos, num2);
					num -= num2;
				}
			}
		}
		targetPos += bits;
	}

	public unsafe static void ReadOutUnsafe(this uint[] source, int sourcePos, ulong[] target, ref int targetPos, int bits)
	{
		if (bits == 0)
		{
			return;
		}
		int bitposition = sourcePos;
		int num = bits;
		fixed (uint* ptr = source)
		{
			fixed (ulong* uPtr2 = target)
			{
				ulong* uPtr = (ulong*)ptr;
				while (num > 0)
				{
					int num2 = ((num > 64) ? 64 : num);
					ulong value = Read(uPtr, ref bitposition, num2);
					Write(uPtr2, value, ref targetPos, num2);
					num -= num2;
				}
			}
		}
		targetPos += bits;
	}

	public unsafe static void ReadOutUnsafe(this byte[] source, int sourcePos, ulong[] target, ref int targetPos, int bits)
	{
		if (bits == 0)
		{
			return;
		}
		int bitposition = sourcePos;
		int num = bits;
		fixed (byte* ptr = source)
		{
			fixed (ulong* uPtr2 = target)
			{
				ulong* uPtr = (ulong*)ptr;
				while (num > 0)
				{
					int num2 = ((num > 64) ? 64 : num);
					ulong value = Read(uPtr, ref bitposition, num2);
					Write(uPtr2, value, ref targetPos, num2);
					num -= num2;
				}
			}
		}
		targetPos += bits;
	}

	public unsafe static void ReadOutUnsafe(this byte[] source, int sourcePos, uint[] target, ref int targetPos, int bits)
	{
		if (bits == 0)
		{
			return;
		}
		int bitposition = sourcePos;
		int num = bits;
		fixed (byte* ptr = source)
		{
			fixed (uint* ptr2 = target)
			{
				ulong* uPtr = (ulong*)ptr;
				ulong* uPtr2 = (ulong*)ptr2;
				while (num > 0)
				{
					int num2 = ((num > 64) ? 64 : num);
					ulong value = Read(uPtr, ref bitposition, num2);
					Write(uPtr2, value, ref targetPos, num2);
					num -= num2;
				}
			}
		}
		targetPos += bits;
	}

	public unsafe static void ReadOutUnsafe(this byte[] source, int sourcePos, byte[] target, ref int targetPos, int bits)
	{
		if (bits == 0)
		{
			return;
		}
		int bitposition = sourcePos;
		int num = bits;
		fixed (byte* ptr = source)
		{
			fixed (byte* ptr2 = target)
			{
				ulong* uPtr = (ulong*)ptr;
				ulong* uPtr2 = (ulong*)ptr2;
				while (num > 0)
				{
					int num2 = ((num > 64) ? 64 : num);
					ulong value = Read(uPtr, ref bitposition, num2);
					Write(uPtr2, value, ref targetPos, num2);
					num -= num2;
				}
			}
		}
		targetPos += bits;
	}
}
