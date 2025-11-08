namespace emotitron.Compression;

public static class ArrayPackBytesExt
{
	public unsafe static void WritePackedBytes(ulong* uPtr, ulong value, ref int bitposition, int bits)
	{
		if (bits != 0)
		{
			int bits2 = (bits + 7 >> 3).UsedBitCount();
			int num = value.UsedByteCount();
			ArraySerializeUnsafe.Write(uPtr, (uint)num, ref bitposition, bits2);
			ArraySerializeUnsafe.Write(uPtr, value, ref bitposition, num << 3);
		}
	}

	public static void WritePackedBytes(this ulong[] buffer, ulong value, ref int bitposition, int bits)
	{
		if (bits != 0)
		{
			int bits2 = (bits + 7 >> 3).UsedBitCount();
			int num = value.UsedByteCount();
			buffer.Write((uint)num, ref bitposition, bits2);
			buffer.Write(value, ref bitposition, num << 3);
		}
	}

	public static void WritePackedBytes(this uint[] buffer, ulong value, ref int bitposition, int bits)
	{
		if (bits != 0)
		{
			int bits2 = (bits + 7 >> 3).UsedBitCount();
			int num = value.UsedByteCount();
			buffer.Write((uint)num, ref bitposition, bits2);
			buffer.Write(value, ref bitposition, num << 3);
		}
	}

	public static void WritePackedBytes(this byte[] buffer, ulong value, ref int bitposition, int bits)
	{
		if (bits != 0)
		{
			int bits2 = (bits + 7 >> 3).UsedBitCount();
			int num = value.UsedByteCount();
			buffer.Write((uint)num, ref bitposition, bits2);
			buffer.Write(value, ref bitposition, num << 3);
		}
	}

	public unsafe static ulong ReadPackedBytes(ulong* uPtr, ref int bitposition, int bits)
	{
		if (bits == 0)
		{
			return 0uL;
		}
		int bits2 = (bits + 7 >> 3).UsedBitCount();
		int bits3 = (int)ArraySerializeUnsafe.Read(uPtr, ref bitposition, bits2) << 3;
		return ArraySerializeUnsafe.Read(uPtr, ref bitposition, bits3);
	}

	public static ulong ReadPackedBytes(this ulong[] buffer, ref int bitposition, int bits)
	{
		if (bits == 0)
		{
			return 0uL;
		}
		int bits2 = (bits + 7 >> 3).UsedBitCount();
		int bits3 = (int)buffer.Read(ref bitposition, bits2) << 3;
		return buffer.Read(ref bitposition, bits3);
	}

	public static ulong ReadPackedBytes(this uint[] buffer, ref int bitposition, int bits)
	{
		if (bits == 0)
		{
			return 0uL;
		}
		int bits2 = (bits + 7 >> 3).UsedBitCount();
		int bits3 = (int)buffer.Read(ref bitposition, bits2) << 3;
		return buffer.Read(ref bitposition, bits3);
	}

	public static ulong ReadPackedBytes(this byte[] buffer, ref int bitposition, int bits)
	{
		if (bits == 0)
		{
			return 0uL;
		}
		int bits2 = (bits + 7 >> 3).UsedBitCount();
		int bits3 = (int)buffer.Read(ref bitposition, bits2) << 3;
		return buffer.Read(ref bitposition, bits3);
	}

	public unsafe static void WriteSignedPackedBytes(ulong* uPtr, int value, ref int bitposition, int bits)
	{
		uint num = (uint)((value << 1) ^ (value >> 31));
		WritePackedBytes(uPtr, num, ref bitposition, bits);
	}

	public unsafe static int ReadSignedPackedBytes(ulong* uPtr, ref int bitposition, int bits)
	{
		uint num = (uint)ReadPackedBytes(uPtr, ref bitposition, bits);
		return (int)((num >> 1) ^ (int)(0 - (num & 1)));
	}

	public static void WriteSignedPackedBytes(this ulong[] buffer, int value, ref int bitposition, int bits)
	{
		uint num = (uint)((value << 1) ^ (value >> 31));
		buffer.WritePackedBytes(num, ref bitposition, bits);
	}

	public static int ReadSignedPackedBytes(this ulong[] buffer, ref int bitposition, int bits)
	{
		uint num = (uint)buffer.ReadPackedBytes(ref bitposition, bits);
		return (int)((num >> 1) ^ (int)(0 - (num & 1)));
	}

	public static void WriteSignedPackedBytes(this uint[] buffer, int value, ref int bitposition, int bits)
	{
		uint num = (uint)((value << 1) ^ (value >> 31));
		buffer.WritePackedBytes(num, ref bitposition, bits);
	}

	public static int ReadSignedPackedBytes(this uint[] buffer, ref int bitposition, int bits)
	{
		uint num = (uint)buffer.ReadPackedBytes(ref bitposition, bits);
		return (int)((num >> 1) ^ (int)(0 - (num & 1)));
	}

	public static void WriteSignedPackedBytes(this byte[] buffer, int value, ref int bitposition, int bits)
	{
		uint num = (uint)((value << 1) ^ (value >> 31));
		buffer.WritePackedBytes(num, ref bitposition, bits);
	}

	public static int ReadSignedPackedBytes(this byte[] buffer, ref int bitposition, int bits)
	{
		uint num = (uint)buffer.ReadPackedBytes(ref bitposition, bits);
		return (int)((num >> 1) ^ (int)(0 - (num & 1)));
	}

	public static void WriteSignedPackedBytes64(this byte[] buffer, long value, ref int bitposition, int bits)
	{
		ulong value2 = (ulong)((value << 1) ^ (value >> 63));
		buffer.WritePackedBytes(value2, ref bitposition, bits);
	}

	public static long ReadSignedPackedBytes64(this byte[] buffer, ref int bitposition, int bits)
	{
		ulong num = buffer.ReadPackedBytes(ref bitposition, bits);
		return (long)((num >> 1) ^ (0L - (num & 1)));
	}
}
