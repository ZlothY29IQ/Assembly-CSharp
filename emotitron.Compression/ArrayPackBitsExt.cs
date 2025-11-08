namespace emotitron.Compression;

public static class ArrayPackBitsExt
{
	public unsafe static void WritePackedBits(ulong* uPtr, ulong value, ref int bitposition, int bits)
	{
		if (bits != 0)
		{
			int num = value.UsedBitCount();
			int bits2 = bits.UsedBitCount();
			ArraySerializeUnsafe.Write(uPtr, (uint)num, ref bitposition, bits2);
			ArraySerializeUnsafe.Write(uPtr, value, ref bitposition, num);
		}
	}

	public static void WritePackedBits(this ulong[] buffer, ulong value, ref int bitposition, int bits)
	{
		if (bits != 0)
		{
			int num = value.UsedBitCount();
			int bits2 = bits.UsedBitCount();
			buffer.Write((uint)num, ref bitposition, bits2);
			buffer.Write(value, ref bitposition, num);
		}
	}

	public static void WritePackedBits(this uint[] buffer, ulong value, ref int bitposition, int bits)
	{
		if (bits != 0)
		{
			int num = value.UsedBitCount();
			int bits2 = bits.UsedBitCount();
			buffer.Write((ulong)num, ref bitposition, bits2);
			buffer.Write(value, ref bitposition, num);
		}
	}

	public static void WritePackedBits(this byte[] buffer, ulong value, ref int bitposition, int bits)
	{
		int num = value.UsedBitCount();
		int bits2 = bits.UsedBitCount();
		buffer.Write((uint)num, ref bitposition, bits2);
		buffer.Write(value, ref bitposition, num);
	}

	public unsafe static ulong ReadPackedBits(ulong* uPtr, ref int bitposition, int bits)
	{
		if (bits == 0)
		{
			return 0uL;
		}
		int bits2 = bits.UsedBitCount();
		int bits3 = (int)ArraySerializeUnsafe.Read(uPtr, ref bitposition, bits2);
		return ArraySerializeUnsafe.Read(uPtr, ref bitposition, bits3);
	}

	public static ulong ReadPackedBits(this ulong[] buffer, ref int bitposition, int bits)
	{
		if (bits == 0)
		{
			return 0uL;
		}
		int bits2 = bits.UsedBitCount();
		int bits3 = (int)buffer.Read(ref bitposition, bits2);
		return buffer.Read(ref bitposition, bits3);
	}

	public static ulong ReadPackedBits(this uint[] buffer, ref int bitposition, int bits)
	{
		if (bits == 0)
		{
			return 0uL;
		}
		int bits2 = bits.UsedBitCount();
		int bits3 = (int)buffer.Read(ref bitposition, bits2);
		return buffer.Read(ref bitposition, bits3);
	}

	public static ulong ReadPackedBits(this byte[] buffer, ref int bitposition, int bits)
	{
		if (bits == 0)
		{
			return 0uL;
		}
		int bits2 = bits.UsedBitCount();
		int bits3 = (int)buffer.Read(ref bitposition, bits2);
		return buffer.Read(ref bitposition, bits3);
	}

	public unsafe static void WriteSignedPackedBits(ulong* uPtr, int value, ref int bitposition, int bits)
	{
		uint num = (uint)((value << 1) ^ (value >> 31));
		WritePackedBits(uPtr, num, ref bitposition, bits);
	}

	public unsafe static int ReadSignedPackedBits(ulong* buffer, ref int bitposition, int bits)
	{
		uint num = (uint)ReadPackedBits(buffer, ref bitposition, bits);
		return (int)((num >> 1) ^ (int)(0 - (num & 1)));
	}

	public static void WriteSignedPackedBits(this ulong[] buffer, int value, ref int bitposition, int bits)
	{
		uint num = (uint)((value << 1) ^ (value >> 31));
		buffer.WritePackedBits(num, ref bitposition, bits);
	}

	public static int ReadSignedPackedBits(this ulong[] buffer, ref int bitposition, int bits)
	{
		uint num = (uint)buffer.ReadPackedBits(ref bitposition, bits);
		return (int)((num >> 1) ^ (int)(0 - (num & 1)));
	}

	public static void WriteSignedPackedBits(this uint[] buffer, int value, ref int bitposition, int bits)
	{
		uint num = (uint)((value << 1) ^ (value >> 31));
		buffer.WritePackedBits(num, ref bitposition, bits);
	}

	public static int ReadSignedPackedBits(this uint[] buffer, ref int bitposition, int bits)
	{
		uint num = (uint)buffer.ReadPackedBits(ref bitposition, bits);
		return (int)((num >> 1) ^ (int)(0 - (num & 1)));
	}

	public static void WriteSignedPackedBits(this byte[] buffer, int value, ref int bitposition, int bits)
	{
		uint num = (uint)((value << 1) ^ (value >> 31));
		buffer.WritePackedBits(num, ref bitposition, bits);
	}

	public static int ReadSignedPackedBits(this byte[] buffer, ref int bitposition, int bits)
	{
		uint num = (uint)buffer.ReadPackedBits(ref bitposition, bits);
		return (int)((num >> 1) ^ (int)(0 - (num & 1)));
	}

	public static void WriteSignedPackedBits64(this byte[] buffer, long value, ref int bitposition, int bits)
	{
		ulong value2 = (ulong)((value << 1) ^ (value >> 63));
		buffer.WritePackedBits(value2, ref bitposition, bits);
	}

	public static long ReadSignedPackedBits64(this byte[] buffer, ref int bitposition, int bits)
	{
		ulong num = buffer.ReadPackedBits(ref bitposition, bits);
		return (long)((num >> 1) ^ (0L - (num & 1)));
	}
}
