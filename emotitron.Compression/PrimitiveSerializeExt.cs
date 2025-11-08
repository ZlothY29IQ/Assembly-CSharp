using System;
using emotitron.Compression.HalfFloat;
using emotitron.Compression.Utilities;

namespace emotitron.Compression;

public static class PrimitiveSerializeExt
{
	private const string overrunerror = "Write buffer overrun. writepos + bits exceeds target length. Data loss will occur.";

	public static void Inject(this ByteConverter value, ref ulong buffer, ref int bitposition, int bits)
	{
		((ulong)value).Inject(ref buffer, ref bitposition, bits);
	}

	public static void Inject(this ByteConverter value, ref uint buffer, ref int bitposition, int bits)
	{
		((ulong)value).Inject(ref buffer, ref bitposition, bits);
	}

	public static void Inject(this ByteConverter value, ref ushort buffer, ref int bitposition, int bits)
	{
		((ulong)value).Inject(ref buffer, ref bitposition, bits);
	}

	public static void Inject(this ByteConverter value, ref byte buffer, ref int bitposition, int bits)
	{
		((ulong)value).Inject(ref buffer, ref bitposition, bits);
	}

	public static ulong WriteSigned(this ulong buffer, int value, ref int bitposition, int bits)
	{
		uint num = (uint)((value << 1) ^ (value >> 31));
		return buffer.Write(num, ref bitposition, bits);
	}

	public static void InjectSigned(this long value, ref ulong buffer, ref int bitposition, int bits)
	{
		((uint)((value << 1) ^ (value >> 31))).Inject(ref buffer, ref bitposition, bits);
	}

	public static void InjectSigned(this int value, ref ulong buffer, ref int bitposition, int bits)
	{
		((uint)((value << 1) ^ (value >> 31))).Inject(ref buffer, ref bitposition, bits);
	}

	public static void InjectSigned(this short value, ref ulong buffer, ref int bitposition, int bits)
	{
		((uint)((value << 1) ^ (value >> 31))).Inject(ref buffer, ref bitposition, bits);
	}

	public static void InjectSigned(this sbyte value, ref ulong buffer, ref int bitposition, int bits)
	{
		((uint)((value << 1) ^ (value >> 31))).Inject(ref buffer, ref bitposition, bits);
	}

	public static int ReadSigned(this ulong buffer, ref int bitposition, int bits)
	{
		uint num = (uint)buffer.Read(ref bitposition, bits);
		return (int)((num >> 1) ^ (int)(0 - (num & 1)));
	}

	public static uint WriteSigned(this uint buffer, int value, ref int bitposition, int bits)
	{
		uint num = (uint)((value << 1) ^ (value >> 31));
		return buffer.Write(num, ref bitposition, bits);
	}

	public static void InjectSigned(this long value, ref uint buffer, ref int bitposition, int bits)
	{
		((uint)((value << 1) ^ (value >> 31))).Inject(ref buffer, ref bitposition, bits);
	}

	public static void InjectSigned(this int value, ref uint buffer, ref int bitposition, int bits)
	{
		((uint)((value << 1) ^ (value >> 31))).Inject(ref buffer, ref bitposition, bits);
	}

	public static void InjectSigned(this short value, ref uint buffer, ref int bitposition, int bits)
	{
		((uint)((value << 1) ^ (value >> 31))).Inject(ref buffer, ref bitposition, bits);
	}

	public static void InjectSigned(this sbyte value, ref uint buffer, ref int bitposition, int bits)
	{
		((uint)((value << 1) ^ (value >> 31))).Inject(ref buffer, ref bitposition, bits);
	}

	public static int ReadSigned(this uint buffer, ref int bitposition, int bits)
	{
		uint num = buffer.Read(ref bitposition, bits);
		return (int)((num >> 1) ^ (int)(0 - (num & 1)));
	}

	public static ushort WriteSigned(this ushort buffer, int value, ref int bitposition, int bits)
	{
		uint num = (uint)((value << 1) ^ (value >> 31));
		return buffer.Write(num, ref bitposition, bits);
	}

	public static void InjectSigned(this long value, ref ushort buffer, ref int bitposition, int bits)
	{
		((uint)((value << 1) ^ (value >> 31))).Inject(ref buffer, ref bitposition, bits);
	}

	public static void InjectSigned(this int value, ref ushort buffer, ref int bitposition, int bits)
	{
		((uint)((value << 1) ^ (value >> 31))).Inject(ref buffer, ref bitposition, bits);
	}

	public static void InjectSigned(this short value, ref ushort buffer, ref int bitposition, int bits)
	{
		((uint)((value << 1) ^ (value >> 31))).Inject(ref buffer, ref bitposition, bits);
	}

	public static void InjectSigned(this sbyte value, ref ushort buffer, ref int bitposition, int bits)
	{
		((uint)((value << 1) ^ (value >> 31))).Inject(ref buffer, ref bitposition, bits);
	}

	public static int ReadSigned(this ushort buffer, ref int bitposition, int bits)
	{
		uint num = buffer.Read(ref bitposition, bits);
		return (int)((num >> 1) ^ (int)(0 - (num & 1)));
	}

	public static byte WriteSigned(this byte buffer, int value, ref int bitposition, int bits)
	{
		uint num = (uint)((value << 1) ^ (value >> 31));
		return buffer.Write(num, ref bitposition, bits);
	}

	public static void InjectSigned(this long value, ref byte buffer, ref int bitposition, int bits)
	{
		((uint)((value << 1) ^ (value >> 31))).Inject(ref buffer, ref bitposition, bits);
	}

	public static void InjectSigned(this int value, ref byte buffer, ref int bitposition, int bits)
	{
		((uint)((value << 1) ^ (value >> 31))).Inject(ref buffer, ref bitposition, bits);
	}

	public static void InjectSigned(this short value, ref byte buffer, ref int bitposition, int bits)
	{
		((uint)((value << 1) ^ (value >> 31))).Inject(ref buffer, ref bitposition, bits);
	}

	public static void InjectSigned(this sbyte value, ref byte buffer, ref int bitposition, int bits)
	{
		((uint)((value << 1) ^ (value >> 31))).Inject(ref buffer, ref bitposition, bits);
	}

	public static int ReadSigned(this byte buffer, ref int bitposition, int bits)
	{
		uint num = buffer.Read(ref bitposition, bits);
		return (int)((num >> 1) ^ (int)(0 - (num & 1)));
	}

	public static ulong WritetBool(this ulong buffer, bool value, ref int bitposition)
	{
		return buffer.Write((ulong)(int)(value ? 1u : 0u), ref bitposition, 1);
	}

	public static uint WritetBool(this uint buffer, bool value, ref int bitposition)
	{
		return buffer.Write((ulong)(int)(value ? 1u : 0u), ref bitposition, 1);
	}

	public static ushort WritetBool(this ushort buffer, bool value, ref int bitposition)
	{
		return buffer.Write((ulong)(int)(value ? 1u : 0u), ref bitposition, 1);
	}

	public static byte WritetBool(this byte buffer, bool value, ref int bitposition)
	{
		return buffer.Write((ulong)(int)(value ? 1u : 0u), ref bitposition, 1);
	}

	public static void Inject(this bool value, ref ulong buffer, ref int bitposition)
	{
		((ulong)(int)(value ? 1u : 0u)).Inject(ref buffer, ref bitposition, 1);
	}

	public static void Inject(this bool value, ref uint buffer, ref int bitposition)
	{
		((ulong)(int)(value ? 1u : 0u)).Inject(ref buffer, ref bitposition, 1);
	}

	public static void Inject(this bool value, ref ushort buffer, ref int bitposition)
	{
		((ulong)(int)(value ? 1u : 0u)).Inject(ref buffer, ref bitposition, 1);
	}

	public static void Inject(this bool value, ref byte buffer, ref int bitposition)
	{
		((ulong)(int)(value ? 1u : 0u)).Inject(ref buffer, ref bitposition, 1);
	}

	public static bool ReadBool(this ulong buffer, ref int bitposition)
	{
		if (buffer.Read(ref bitposition, 1) != 0L)
		{
			return true;
		}
		return false;
	}

	public static bool ReadtBool(this uint buffer, ref int bitposition)
	{
		if (buffer.Read(ref bitposition, 1) != 0)
		{
			return true;
		}
		return false;
	}

	public static bool ReadBool(this ushort buffer, ref int bitposition)
	{
		if (buffer.Read(ref bitposition, 1) != 0)
		{
			return true;
		}
		return false;
	}

	public static bool ReadBool(this byte buffer, ref int bitposition)
	{
		if (buffer.Read(ref bitposition, 1) != 0)
		{
			return true;
		}
		return false;
	}

	public static ulong Write(this ulong buffer, ulong value, ref int bitposition, int bits = 64)
	{
		ulong num = value << bitposition;
		ulong num2 = ulong.MaxValue >> 64 - bits << bitposition;
		buffer &= ~num2;
		buffer |= num2 & num;
		bitposition += bits;
		return buffer;
	}

	public static uint Write(this uint buffer, ulong value, ref int bitposition, int bits = 64)
	{
		uint num = (uint)((int)value << bitposition);
		uint num2 = (uint)(-1 >>> 32 - bits << bitposition);
		buffer &= ~num2;
		buffer |= num2 & num;
		bitposition += bits;
		return buffer;
	}

	public static ushort Write(this ushort buffer, ulong value, ref int bitposition, int bits = 64)
	{
		uint num = (uint)((int)value << bitposition);
		uint num2 = (uint)(65535 >>> 16 - bits << bitposition);
		buffer = (ushort)((buffer & ~num2) | (num2 & num));
		bitposition += bits;
		return buffer;
	}

	public static byte Write(this byte buffer, ulong value, ref int bitposition, int bits = 64)
	{
		uint num = (uint)((int)value << bitposition);
		uint num2 = (uint)(255 >>> 8 - bits << bitposition);
		buffer = (byte)((buffer & ~num2) | (num2 & num));
		bitposition += bits;
		return buffer;
	}

	public static void Inject(this ulong value, ref ulong buffer, ref int bitposition, int bits = 64)
	{
		buffer = buffer.Write(value, ref bitposition, bits);
	}

	public static void Inject(this ulong value, ref ulong buffer, int bitposition, int bits = 64)
	{
		ulong num = value << bitposition;
		ulong num2 = ulong.MaxValue >> 64 - bits << bitposition;
		buffer &= ~num2;
		buffer |= num2 & num;
	}

	public static void Inject(this uint value, ref ulong buffer, ref int bitposition, int bits = 32)
	{
		buffer = buffer.Write(value, ref bitposition, bits);
	}

	public static void Inject(this uint value, ref ulong buffer, int bitposition, int bits = 32)
	{
		ulong num = (ulong)value << bitposition;
		ulong num2 = ulong.MaxValue >> 64 - bits << bitposition;
		buffer &= ~num2;
		buffer |= num2 & num;
	}

	public static void Inject(this ushort value, ref ulong buffer, ref int bitposition, int bits = 16)
	{
		buffer = buffer.Write(value, ref bitposition, bits);
	}

	public static void Inject(this ushort value, ref ulong buffer, int bitposition, int bits = 16)
	{
		buffer = buffer.Write(value, ref bitposition, bits);
	}

	public static void Inject(this byte value, ref ulong buffer, ref int bitposition, int bits = 8)
	{
		buffer = buffer.Write(value, ref bitposition, bits);
	}

	public static void Inject(this byte value, ref ulong buffer, int bitposition, int bits = 8)
	{
		buffer = buffer.Write(value, ref bitposition, bits);
	}

	public static void InjectUnsigned(this long value, ref ulong buffer, ref int bitposition, int bits = 32)
	{
		buffer = buffer.Write((ulong)value, ref bitposition, bits);
	}

	public static void InjectUnsigned(this int value, ref ulong buffer, ref int bitposition, int bits = 32)
	{
		buffer = buffer.Write((ulong)value, ref bitposition, bits);
	}

	public static void InjectUnsigned(this short value, ref ulong buffer, ref int bitposition, int bits = 32)
	{
		buffer = buffer.Write((ulong)value, ref bitposition, bits);
	}

	public static void InjectUnsigned(this sbyte value, ref ulong buffer, ref int bitposition, int bits = 32)
	{
		buffer = buffer.Write((ulong)value, ref bitposition, bits);
	}

	public static void Inject(this ulong value, ref uint buffer, ref int bitposition, int bits = 64)
	{
		buffer = buffer.Write(value, ref bitposition, bits);
	}

	public static void Inject(this ulong value, ref uint buffer, int bitposition, int bits = 64)
	{
		buffer = buffer.Write(value, ref bitposition, bits);
	}

	public static void Inject(this uint value, ref uint buffer, ref int bitposition, int bits = 32)
	{
		buffer = buffer.Write(value, ref bitposition, bits);
	}

	public static void Inject(this uint value, ref uint buffer, int bitposition, int bits = 32)
	{
		buffer = buffer.Write(value, ref bitposition, bits);
	}

	public static void Inject(this ushort value, ref uint buffer, ref int bitposition, int bits = 16)
	{
		buffer = buffer.Write(value, ref bitposition, bits);
	}

	public static void Inject(this ushort value, ref uint buffer, int bitposition, int bits = 16)
	{
		buffer = buffer.Write(value, ref bitposition, bits);
	}

	public static void Inject(this byte value, ref uint buffer, ref int bitposition, int bits = 8)
	{
		buffer = buffer.Write(value, ref bitposition, bits);
	}

	public static void Inject(this byte value, ref uint buffer, int bitposition, int bits = 8)
	{
		buffer = buffer.Write(value, ref bitposition, bits);
	}

	public static void InjectUnsigned(this long value, ref uint buffer, ref int bitposition, int bits = 64)
	{
		buffer = buffer.Write((ulong)value, ref bitposition, bits);
	}

	public static void InjectUnsigned(this int value, ref uint buffer, ref int bitposition, int bits = 64)
	{
		buffer = buffer.Write((ulong)value, ref bitposition, bits);
	}

	public static void InjectUnsigned(this short value, ref uint buffer, ref int bitposition, int bits = 64)
	{
		buffer = buffer.Write((ulong)value, ref bitposition, bits);
	}

	public static void InjectUnsigned(this sbyte value, ref uint buffer, ref int bitposition, int bits = 64)
	{
		buffer = buffer.Write((ulong)value, ref bitposition, bits);
	}

	public static void Inject(this ulong value, ref ushort buffer, ref int bitposition, int bits = 16)
	{
		buffer = buffer.Write(value, ref bitposition, bits);
	}

	public static void Inject(this ulong value, ref ushort buffer, int bitposition, int bits = 16)
	{
		buffer = buffer.Write(value, ref bitposition, bits);
	}

	public static void Inject(this uint value, ref ushort buffer, ref int bitposition, int bits = 16)
	{
		buffer = buffer.Write(value, ref bitposition, bits);
	}

	public static void Inject(this uint value, ref ushort buffer, int bitposition, int bits = 16)
	{
		buffer = buffer.Write(value, ref bitposition, bits);
	}

	public static void Inject(this ushort value, ref ushort buffer, ref int bitposition, int bits = 16)
	{
		buffer = buffer.Write(value, ref bitposition, bits);
	}

	public static void Inject(this ushort value, ref ushort buffer, int bitposition, int bits = 16)
	{
		buffer = buffer.Write(value, ref bitposition, bits);
	}

	public static void Inject(this byte value, ref ushort buffer, ref int bitposition, int bits = 8)
	{
		buffer = buffer.Write(value, ref bitposition, bits);
	}

	public static void Inject(this byte value, ref ushort buffer, int bitposition, int bits = 8)
	{
		buffer = buffer.Write(value, ref bitposition, bits);
	}

	public static void Inject(this ulong value, ref byte buffer, ref int bitposition, int bits = 8)
	{
		buffer = buffer.Write(value, ref bitposition, bits);
	}

	public static void Inject(this ulong value, ref byte buffer, int bitposition, int bits = 8)
	{
		buffer = buffer.Write(value, ref bitposition, bits);
	}

	public static void Inject(this uint value, ref byte buffer, ref int bitposition, int bits = 8)
	{
		buffer = buffer.Write(value, ref bitposition, bits);
	}

	public static void Inject(this uint value, ref byte buffer, int bitposition, int bits = 8)
	{
		buffer = buffer.Write(value, ref bitposition, bits);
	}

	public static void Inject(this ushort value, ref byte buffer, ref int bitposition, int bits = 8)
	{
		buffer = buffer.Write(value, ref bitposition, bits);
	}

	public static void Inject(this ushort value, ref byte buffer, int bitposition, int bits = 8)
	{
		buffer = buffer.Write(value, ref bitposition, bits);
	}

	public static void Inject(this byte value, ref byte buffer, ref int bitposition, int bits = 8)
	{
		buffer = buffer.Write(value, ref bitposition, bits);
	}

	public static void Inject(this byte value, ref byte buffer, int bitposition, int bits = 8)
	{
		buffer = buffer.Write(value, ref bitposition, bits);
	}

	[Obsolete("Argument order changed")]
	public static ulong Extract(this ulong value, int bits, ref int bitposition)
	{
		return value.Extract(bits, ref bitposition);
	}

	public static ulong Read(this ulong value, ref int bitposition, int bits)
	{
		ulong num = ulong.MaxValue >> 64 - bits;
		ulong result = (value >> bitposition) & num;
		bitposition += bits;
		return result;
	}

	[Obsolete("Use Read instead.")]
	public static ulong Extract(this ulong value, ref int bitposition, int bits)
	{
		ulong num = ulong.MaxValue >> 64 - bits;
		ulong result = (value >> bitposition) & num;
		bitposition += bits;
		return result;
	}

	[Obsolete("Always include the [ref int bitposition] argument. Extracting from position 0 would be better handled with a mask operation.")]
	public static ulong Extract(this ulong value, int bits)
	{
		ulong num = ulong.MaxValue >> 64 - bits;
		return value & num;
	}

	public static uint Read(this uint value, ref int bitposition, int bits)
	{
		uint num = uint.MaxValue >> 32 - bits;
		uint result = (value >> bitposition) & num;
		bitposition += bits;
		return result;
	}

	[Obsolete("Use Read instead.")]
	public static uint Extract(this uint value, ref int bitposition, int bits)
	{
		uint num = uint.MaxValue >> 32 - bits;
		uint result = (value >> bitposition) & num;
		bitposition += bits;
		return result;
	}

	[Obsolete("Always include the [ref int bitposition] argument. Extracting from position 0 would be better handled with a mask operation.")]
	public static uint Extract(this uint value, int bits)
	{
		uint num = uint.MaxValue >> 32 - bits;
		return value & num;
	}

	public static uint Read(this ushort value, ref int bitposition, int bits)
	{
		uint num = 65535u >> 16 - bits;
		int result = (value >>> bitposition) & (int)num;
		bitposition += bits;
		return (uint)result;
	}

	[Obsolete("Use Read instead.")]
	public static uint Extract(this ushort value, ref int bitposition, int bits)
	{
		uint num = 65535u >> 16 - bits;
		int result = (value >>> bitposition) & (int)num;
		bitposition += bits;
		return (uint)result;
	}

	public static uint Read(this byte value, ref int bitposition, int bits)
	{
		uint num = 255u >> 8 - bits;
		int result = (value >>> bitposition) & (int)num;
		bitposition += bits;
		return (uint)result;
	}

	[Obsolete("Use Read instead.")]
	public static uint Extract(this byte value, ref int bitposition, int bits)
	{
		uint num = 255u >> 8 - bits;
		int result = (value >>> bitposition) & (int)num;
		bitposition += bits;
		return (uint)result;
	}

	[Obsolete("Always include the [ref int bitposition] argument. Extracting from position 0 would be better handled with a mask operation.")]
	public static byte Extract(this byte value, int bits)
	{
		uint num = 255u >> 8 - bits;
		return (byte)(value & num);
	}

	public static void Inject(this float f, ref ulong buffer, ref int bitposition)
	{
		buffer = buffer.Write((ByteConverter)f, ref bitposition, 32);
	}

	public static float ReadFloat(this ulong buffer, ref int bitposition)
	{
		return (ByteConverter)buffer.Read(ref bitposition, 32);
	}

	[Obsolete("Use Read instead.")]
	public static float ExtractFloat(this ulong buffer, ref int bitposition)
	{
		return (ByteConverter)buffer.Extract(ref bitposition, 32);
	}

	public static ushort InjectAsHalfFloat(this float f, ref ulong buffer, ref int bitposition)
	{
		ushort num = HalfUtilities.Pack(f);
		buffer = buffer.Write(num, ref bitposition, 16);
		return num;
	}

	public static ushort InjectAsHalfFloat(this float f, ref uint buffer, ref int bitposition)
	{
		ushort num = HalfUtilities.Pack(f);
		buffer = buffer.Write(num, ref bitposition, 16);
		return num;
	}

	public static float ReadHalfFloat(this ulong buffer, ref int bitposition)
	{
		return HalfUtilities.Unpack((ushort)buffer.Read(ref bitposition, 16));
	}

	[Obsolete("Use Read instead.")]
	public static float ExtractHalfFloat(this ulong buffer, ref int bitposition)
	{
		return HalfUtilities.Unpack((ushort)buffer.Extract(ref bitposition, 16));
	}

	public static float ReadHalfFloat(this uint buffer, ref int bitposition)
	{
		return HalfUtilities.Unpack((ushort)buffer.Read(ref bitposition, 16));
	}

	[Obsolete("Use Read instead.")]
	public static float ExtractHalfFloat(this uint buffer, ref int bitposition)
	{
		return HalfUtilities.Unpack((ushort)buffer.Extract(ref bitposition, 16));
	}

	[Obsolete("Argument order changed")]
	public static void Inject(this ulong value, ref uint buffer, int bits, ref int bitposition)
	{
		value.Inject(ref buffer, ref bitposition, bits);
	}

	[Obsolete("Argument order changed")]
	public static void Inject(this ulong value, ref ulong buffer, int bits, ref int bitposition)
	{
		value.Inject(ref buffer, ref bitposition, bits);
	}
}
