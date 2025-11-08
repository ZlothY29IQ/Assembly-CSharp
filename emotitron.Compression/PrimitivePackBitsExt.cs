namespace emotitron.Compression;

public static class PrimitivePackBitsExt
{
	public static ulong WritePackedBits(this ulong buffer, uint value, ref int bitposition, int bits)
	{
		int bits2 = ((uint)bits).UsedBitCount();
		int num = value.UsedBitCount();
		buffer = buffer.Write((uint)num, ref bitposition, bits2);
		buffer = buffer.Write(value, ref bitposition, num);
		return buffer;
	}

	public static uint WritePackedBits(this uint buffer, ushort value, ref int bitposition, int bits)
	{
		int bits2 = ((uint)bits).UsedBitCount();
		int num = value.UsedBitCount();
		buffer = buffer.Write((uint)num, ref bitposition, bits2);
		buffer = buffer.Write(value, ref bitposition, num);
		return buffer;
	}

	public static ushort WritePackedBits(this ushort buffer, byte value, ref int bitposition, int bits)
	{
		int bits2 = ((uint)bits).UsedBitCount();
		int num = value.UsedBitCount();
		buffer = buffer.Write((uint)num, ref bitposition, bits2);
		buffer = buffer.Write(value, ref bitposition, num);
		return buffer;
	}

	public static ulong ReadPackedBits(this ulong buffer, ref int bitposition, int bits)
	{
		int bits2 = bits.UsedBitCount();
		int bits3 = (int)buffer.Read(ref bitposition, bits2);
		return buffer.Read(ref bitposition, bits3);
	}

	public static ulong ReadPackedBits(this uint buffer, ref int bitposition, int bits)
	{
		int bits2 = bits.UsedBitCount();
		int bits3 = (int)buffer.Read(ref bitposition, bits2);
		return buffer.Read(ref bitposition, bits3);
	}

	public static ulong ReadPackedBits(this ushort buffer, ref int bitposition, int bits)
	{
		int bits2 = bits.UsedBitCount();
		int bits3 = (int)buffer.Read(ref bitposition, bits2);
		return buffer.Read(ref bitposition, bits3);
	}

	public static ulong WriteSignedPackedBits(this ulong buffer, int value, ref int bitposition, int bits)
	{
		uint value2 = (uint)((value << 1) ^ (value >> 31));
		buffer = buffer.WritePackedBits(value2, ref bitposition, bits);
		return buffer;
	}

	public static uint WriteSignedPackedBits(this uint buffer, short value, ref int bitposition, int bits)
	{
		uint num = (uint)((value << 1) ^ (value >> 31));
		buffer = buffer.WritePackedBits((ushort)num, ref bitposition, bits);
		return buffer;
	}

	public static ushort WriteSignedPackedBits(this ushort buffer, sbyte value, ref int bitposition, int bits)
	{
		uint num = (uint)((value << 1) ^ (value >> 31));
		buffer = buffer.WritePackedBits((byte)num, ref bitposition, bits);
		return buffer;
	}

	public static int ReadSignedPackedBits(this ulong buffer, ref int bitposition, int bits)
	{
		uint num = (uint)buffer.ReadPackedBits(ref bitposition, bits);
		return (int)((num >> 1) ^ (int)(0 - (num & 1)));
	}

	public static short ReadSignedPackedBits(this uint buffer, ref int bitposition, int bits)
	{
		uint num = (uint)buffer.ReadPackedBits(ref bitposition, bits);
		return (short)((num >> 1) ^ (int)(0 - (num & 1)));
	}

	public static sbyte ReadSignedPackedBits(this ushort buffer, ref int bitposition, int bits)
	{
		uint num = (uint)buffer.ReadPackedBits(ref bitposition, bits);
		return (sbyte)((num >> 1) ^ (int)(0 - (num & 1)));
	}
}
