namespace emotitron.Compression;

public static class PrimitivePackBytesExt
{
	public static ulong WritePackedBytes(this ulong buffer, ulong value, ref int bitposition, int bits)
	{
		int bits2 = (bits + 7 >> 3).UsedBitCount();
		int num = value.UsedByteCount();
		buffer = buffer.Write((uint)num, ref bitposition, bits2);
		buffer = buffer.Write(value, ref bitposition, num << 3);
		return buffer;
	}

	public static uint WritePackedBytes(this uint buffer, uint value, ref int bitposition, int bits)
	{
		int bits2 = (bits + 7 >> 3).UsedBitCount();
		int num = value.UsedByteCount();
		buffer = buffer.Write((uint)num, ref bitposition, bits2);
		buffer = buffer.Write(value, ref bitposition, num << 3);
		return buffer;
	}

	public static void InjectPackedBytes(this ulong value, ref ulong buffer, ref int bitposition, int bits)
	{
		int bits2 = (bits + 7 >> 3).UsedBitCount();
		int num = value.UsedByteCount();
		buffer = buffer.Write((uint)num, ref bitposition, bits2);
		buffer = buffer.Write(value, ref bitposition, num << 3);
	}

	public static void InjectPackedBytes(this uint value, ref uint buffer, ref int bitposition, int bits)
	{
		int bits2 = (bits + 7 >> 3).UsedBitCount();
		int num = value.UsedByteCount();
		buffer = buffer.Write((uint)num, ref bitposition, bits2);
		buffer = buffer.Write(value, ref bitposition, num << 3);
	}

	public static ulong ReadPackedBytes(this ulong buffer, ref int bitposition, int bits)
	{
		int bits2 = (bits + 7 >> 3).UsedBitCount();
		int num = (int)buffer.Read(ref bitposition, bits2);
		return buffer.Read(ref bitposition, num << 3);
	}

	public static uint ReadPackedBytes(this uint buffer, ref int bitposition, int bits)
	{
		int bits2 = (bits + 7 >> 3).UsedBitCount();
		int num = (int)buffer.Read(ref bitposition, bits2);
		return buffer.Read(ref bitposition, num << 3);
	}

	public static ulong WriteSignedPackedBytes(this ulong buffer, int value, ref int bitposition, int bits)
	{
		uint num = (uint)((value << 1) ^ (value >> 31));
		return buffer.WritePackedBytes(num, ref bitposition, bits);
	}

	public static int ReadSignedPackedBytes(this ulong buffer, ref int bitposition, int bits)
	{
		uint num = (uint)buffer.ReadPackedBytes(ref bitposition, bits);
		return (int)((num >> 1) ^ (int)(0 - (num & 1)));
	}
}
