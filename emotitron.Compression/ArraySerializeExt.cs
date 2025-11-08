using System;
using emotitron.Compression.Utilities;

namespace emotitron.Compression;

public static class ArraySerializeExt
{
	private const string bufferOverrunMsg = "Byte buffer length exceeded by write or read. Dataloss will occur. Likely due to a Read/Write mismatch.";

	public static void Zero(this byte[] buffer, int startByte, int endByte)
	{
		for (int i = startByte; i <= endByte; i++)
		{
			buffer[i] = 0;
		}
	}

	public static void Zero(this byte[] buffer, int startByte)
	{
		int num = buffer.Length;
		for (int i = startByte; i < num; i++)
		{
			buffer[i] = 0;
		}
	}

	public static void Zero(this byte[] buffer)
	{
		int num = buffer.Length;
		for (int i = 0; i < num; i++)
		{
			buffer[i] = 0;
		}
	}

	public static void Zero(this ushort[] buffer, int startByte, int endByte)
	{
		for (int i = startByte; i <= endByte; i++)
		{
			buffer[i] = 0;
		}
	}

	public static void Zero(this ushort[] buffer, int startByte)
	{
		int num = buffer.Length;
		for (int i = startByte; i < num; i++)
		{
			buffer[i] = 0;
		}
	}

	public static void Zero(this ushort[] buffer)
	{
		int num = buffer.Length;
		for (int i = 0; i < num; i++)
		{
			buffer[i] = 0;
		}
	}

	public static void Zero(this uint[] buffer, int startByte, int endByte)
	{
		for (int i = startByte; i <= endByte; i++)
		{
			buffer[i] = 0u;
		}
	}

	public static void Zero(this uint[] buffer, int startByte)
	{
		int num = buffer.Length;
		for (int i = startByte; i < num; i++)
		{
			buffer[i] = 0u;
		}
	}

	public static void Zero(this uint[] buffer)
	{
		int num = buffer.Length;
		for (int i = 0; i < num; i++)
		{
			buffer[i] = 0u;
		}
	}

	public static void Zero(this ulong[] buffer, int startByte, int endByte)
	{
		for (int i = startByte; i <= endByte; i++)
		{
			buffer[i] = 0uL;
		}
	}

	public static void Zero(this ulong[] buffer, int startByte)
	{
		int num = buffer.Length;
		for (int i = startByte; i < num; i++)
		{
			buffer[i] = 0uL;
		}
	}

	public static void Zero(this ulong[] buffer)
	{
		int num = buffer.Length;
		for (int i = 0; i < num; i++)
		{
			buffer[i] = 0uL;
		}
	}

	public static void WriteSigned(this byte[] buffer, int value, ref int bitposition, int bits)
	{
		uint num = (uint)((value << 1) ^ (value >> 31));
		buffer.Write(num, ref bitposition, bits);
	}

	public static void WriteSigned(this uint[] buffer, int value, ref int bitposition, int bits)
	{
		uint num = (uint)((value << 1) ^ (value >> 31));
		buffer.Write(num, ref bitposition, bits);
	}

	public static void WriteSigned(this ulong[] buffer, int value, ref int bitposition, int bits)
	{
		uint num = (uint)((value << 1) ^ (value >> 31));
		buffer.Write(num, ref bitposition, bits);
	}

	public static void WriteSigned(this byte[] buffer, long value, ref int bitposition, int bits)
	{
		ulong value2 = (ulong)((value << 1) ^ (value >> 63));
		buffer.Write(value2, ref bitposition, bits);
	}

	public static void WriteSigned(this uint[] buffer, long value, ref int bitposition, int bits)
	{
		ulong value2 = (ulong)((value << 1) ^ (value >> 63));
		buffer.Write(value2, ref bitposition, bits);
	}

	public static void WriteSigned(this ulong[] buffer, long value, ref int bitposition, int bits)
	{
		ulong value2 = (ulong)((value << 1) ^ (value >> 63));
		buffer.Write(value2, ref bitposition, bits);
	}

	public static int ReadSigned(this byte[] buffer, ref int bitposition, int bits)
	{
		uint num = (uint)buffer.Read(ref bitposition, bits);
		return (int)((num >> 1) ^ (int)(0 - (num & 1)));
	}

	public static int ReadSigned(this uint[] buffer, ref int bitposition, int bits)
	{
		uint num = (uint)buffer.Read(ref bitposition, bits);
		return (int)((num >> 1) ^ (int)(0 - (num & 1)));
	}

	public static int ReadSigned(this ulong[] buffer, ref int bitposition, int bits)
	{
		uint num = (uint)buffer.Read(ref bitposition, bits);
		return (int)((num >> 1) ^ (int)(0 - (num & 1)));
	}

	public static long ReadSigned64(this byte[] buffer, ref int bitposition, int bits)
	{
		ulong num = buffer.Read(ref bitposition, bits);
		return (long)((num >> 1) ^ (0L - (num & 1)));
	}

	public static long ReadSigned64(this uint[] buffer, ref int bitposition, int bits)
	{
		ulong num = buffer.Read(ref bitposition, bits);
		return (long)((num >> 1) ^ (0L - (num & 1)));
	}

	public static long ReadSigned64(this ulong[] buffer, ref int bitposition, int bits)
	{
		ulong num = buffer.Read(ref bitposition, bits);
		return (long)((num >> 1) ^ (0L - (num & 1)));
	}

	public static void WriteFloat(this byte[] buffer, float value, ref int bitposition)
	{
		buffer.Write(((ByteConverter)value).uint32, ref bitposition, 32);
	}

	public static float ReadFloat(this byte[] buffer, ref int bitposition)
	{
		return (ByteConverter)buffer.Read(ref bitposition, 32);
	}

	public static void Append(this byte[] buffer, ulong value, ref int bitposition, int bits)
	{
		if (bits != 0)
		{
			int num = bitposition & 7;
			int num2 = bitposition >> 3;
			ulong num3 = (ulong)((1L << num) - 1);
			ulong num4 = (buffer[num2] & num3) | (value << num);
			buffer[num2] = (byte)num4;
			for (num = 8 - num; num < bits; num += 8)
			{
				num2++;
				buffer[num2] = (byte)(value >> num);
			}
			bitposition += bits;
		}
	}

	public static void Append(this uint[] buffer, ulong value, ref int bitposition, int bits)
	{
		if (bits != 0)
		{
			int num = bitposition & 0x1F;
			int num2 = bitposition >> 5;
			ulong num3 = (ulong)((1L << num) - 1);
			ulong num4 = (buffer[num2] & num3) | (value << num);
			buffer[num2] = (uint)num4;
			for (num = 32 - num; num < bits; num += 32)
			{
				num2++;
				buffer[num2] = (uint)(value >> num);
			}
			bitposition += bits;
		}
	}

	public static void Append(this uint[] buffer, uint value, ref int bitposition, int bits)
	{
		if (bits != 0)
		{
			int num = bitposition & 0x1F;
			int num2 = bitposition >> 5;
			ulong num3 = (ulong)((1L << num) - 1);
			ulong num4 = (buffer[num2] & num3) | ((ulong)value << num);
			buffer[num2] = (uint)num4;
			buffer[num2 + 1] = (uint)(num4 >> 32);
			bitposition += bits;
		}
	}

	public static void Append(this ulong[] buffer, ulong value, ref int bitposition, int bits)
	{
		if (bits != 0)
		{
			int num = bitposition & 0x3F;
			int num2 = bitposition >> 6;
			ulong num3 = (ulong)((1L << num) - 1);
			ulong num4 = (buffer[num2] & num3) | (value << num);
			buffer[num2] = num4;
			buffer[num2 + 1] = value >> 64 - num;
			bitposition += bits;
		}
	}

	public static void Write(this byte[] buffer, ulong value, ref int bitposition, int bits)
	{
		if (bits != 0)
		{
			int num = bitposition & 7;
			int num2 = bitposition >> 3;
			int num3 = num + bits;
			ulong num4 = ulong.MaxValue >> 64 - bits;
			ulong num5 = num4 << num;
			ulong num6 = value << num;
			buffer[num2] = (byte)((buffer[num2] & ~num5) | (num6 & num5));
			num = 8 - num;
			for (num3 -= 8; num3 > 8; num3 -= 8)
			{
				num2++;
				num6 = value >> num;
				buffer[num2] = (byte)num6;
				num += 8;
			}
			if (num3 > 0)
			{
				num2++;
				num5 = num4 >> num;
				num6 = value >> num;
				buffer[num2] = (byte)((buffer[num2] & ~num5) | (num6 & num5));
			}
			bitposition += bits;
		}
	}

	public static void Write(this uint[] buffer, ulong value, ref int bitposition, int bits)
	{
		if (bits != 0)
		{
			int num = bitposition & 0x1F;
			int num2 = bitposition >> 5;
			int num3 = num + bits;
			ulong num4 = ulong.MaxValue >> 64 - bits;
			ulong num5 = num4 << num;
			ulong num6 = value << num;
			buffer[num2] = (uint)((buffer[num2] & ~num5) | (num6 & num5));
			num = 32 - num;
			for (num3 -= 32; num3 > 32; num3 -= 32)
			{
				num2++;
				num5 = num4 >> num;
				num6 = value >> num;
				buffer[num2] = (uint)((buffer[num2] & ~num5) | (num6 & num5));
				num += 32;
			}
			bitposition += bits;
		}
	}

	public static void Write(this ulong[] buffer, ulong value, ref int bitposition, int bits)
	{
		if (bits != 0)
		{
			int num = bitposition & 0x3F;
			int num2 = bitposition >> 6;
			int num3 = num + bits;
			ulong num4 = ulong.MaxValue >> 64 - bits;
			ulong num5 = num4 << num;
			ulong num6 = value << num;
			buffer[num2] = (buffer[num2] & ~num5) | (num6 & num5);
			num = 64 - num;
			for (num3 -= 64; num3 > 64; num3 -= 64)
			{
				num2++;
				num5 = num4 >> num;
				num6 = value >> num;
				buffer[num2] = (buffer[num2] & ~num5) | (num6 & num5);
				num += 64;
			}
			bitposition += bits;
		}
	}

	public static void WriteBool(this ulong[] buffer, bool b, ref int bitposition)
	{
		buffer.Write((ulong)(int)(b ? 1u : 0u), ref bitposition, 1);
	}

	public static void WriteBool(this uint[] buffer, bool b, ref int bitposition)
	{
		buffer.Write((ulong)(int)(b ? 1u : 0u), ref bitposition, 1);
	}

	public static void WriteBool(this byte[] buffer, bool b, ref int bitposition)
	{
		buffer.Write((ulong)(int)(b ? 1u : 0u), ref bitposition, 1);
	}

	public static ulong Read(this byte[] buffer, ref int bitposition, int bits)
	{
		if (bits == 0)
		{
			return 0uL;
		}
		int num = bitposition & 7;
		int num2 = bitposition >> 3;
		ulong num3 = ulong.MaxValue >> 64 - bits;
		ulong num4 = (ulong)buffer[num2] >> num;
		for (num = 8 - num; num < bits; num += 8)
		{
			num2++;
			num4 |= (ulong)buffer[num2] << num;
		}
		bitposition += bits;
		return num4 & num3;
	}

	public static ulong Read(this uint[] buffer, ref int bitposition, int bits)
	{
		if (bits == 0)
		{
			return 0uL;
		}
		int num = bitposition & 0x1F;
		int num2 = bitposition >> 5;
		ulong num3 = ulong.MaxValue >> 64 - bits;
		ulong num4 = (ulong)buffer[num2] >> num;
		for (num = 32 - num; num < bits; num += 32)
		{
			num2++;
			num4 |= (ulong)buffer[num2] << num;
		}
		bitposition += bits;
		return num4 & num3;
	}

	public static ulong Read(this ulong[] buffer, ref int bitposition, int bits)
	{
		if (bits == 0)
		{
			return 0uL;
		}
		int num = bitposition & 0x3F;
		int num2 = bitposition >> 6;
		ulong num3 = ulong.MaxValue >> 64 - bits;
		ulong num4 = buffer[num2] >> num;
		for (num = 64 - num; num < bits; num += 64)
		{
			num2++;
			num4 |= buffer[num2] << num;
		}
		bitposition += bits;
		return num4 & num3;
	}

	[Obsolete("Just use Read(), it return a ulong already.")]
	public static ulong ReadUInt64(this byte[] buffer, ref int bitposition, int bits = 64)
	{
		return buffer.Read(ref bitposition, bits);
	}

	[Obsolete("Just use Read(), it return a ulong already.")]
	public static ulong ReadUInt64(this uint[] buffer, ref int bitposition, int bits = 64)
	{
		return buffer.Read(ref bitposition, bits);
	}

	[Obsolete("Just use Read(), it return a ulong already.")]
	public static ulong ReadUInt64(this ulong[] buffer, ref int bitposition, int bits = 64)
	{
		return buffer.Read(ref bitposition, bits);
	}

	public static uint ReadUInt32(this byte[] buffer, ref int bitposition, int bits = 32)
	{
		return (uint)buffer.Read(ref bitposition, bits);
	}

	public static uint ReadUInt32(this uint[] buffer, ref int bitposition, int bits = 32)
	{
		return (uint)buffer.Read(ref bitposition, bits);
	}

	public static uint ReadUInt32(this ulong[] buffer, ref int bitposition, int bits = 32)
	{
		return (uint)buffer.Read(ref bitposition, bits);
	}

	public static ushort ReadUInt16(this byte[] buffer, ref int bitposition, int bits = 16)
	{
		return (ushort)buffer.Read(ref bitposition, bits);
	}

	public static ushort ReadUInt16(this uint[] buffer, ref int bitposition, int bits = 16)
	{
		return (ushort)buffer.Read(ref bitposition, bits);
	}

	public static ushort ReadUInt16(this ulong[] buffer, ref int bitposition, int bits = 16)
	{
		return (ushort)buffer.Read(ref bitposition, bits);
	}

	public static byte ReadByte(this byte[] buffer, ref int bitposition, int bits = 8)
	{
		return (byte)buffer.Read(ref bitposition, bits);
	}

	public static byte ReadByte(this uint[] buffer, ref int bitposition, int bits = 32)
	{
		return (byte)buffer.Read(ref bitposition, bits);
	}

	public static byte ReadByte(this ulong[] buffer, ref int bitposition, int bits)
	{
		return (byte)buffer.Read(ref bitposition, bits);
	}

	public static bool ReadBool(this ulong[] buffer, ref int bitposition)
	{
		if (buffer.Read(ref bitposition, 1) != 1)
		{
			return false;
		}
		return true;
	}

	public static bool ReadBool(this uint[] buffer, ref int bitposition)
	{
		if (buffer.Read(ref bitposition, 1) != 1)
		{
			return false;
		}
		return true;
	}

	public static bool ReadBool(this byte[] buffer, ref int bitposition)
	{
		if (buffer.Read(ref bitposition, 1) != 1)
		{
			return false;
		}
		return true;
	}

	public static char ReadChar(this ulong[] buffer, ref int bitposition)
	{
		return (char)buffer.Read(ref bitposition, 16);
	}

	public static char ReadChar(this uint[] buffer, ref int bitposition)
	{
		return (char)buffer.Read(ref bitposition, 16);
	}

	public static char ReadChar(this byte[] buffer, ref int bitposition)
	{
		return (char)buffer.Read(ref bitposition, 16);
	}

	public static void ReadOutSafe(this ulong[] source, int srcStartPos, byte[] target, ref int bitposition, int bits)
	{
		if (bits != 0)
		{
			int bitposition2 = srcStartPos;
			int num = bits;
			while (num > 0)
			{
				int num2 = ((num > 64) ? 64 : num);
				ulong value = source.Read(ref bitposition2, num2);
				target.Write(value, ref bitposition, num2);
				num -= num2;
			}
			bitposition += bits;
		}
	}

	public static void ReadOutSafe(this ulong[] source, int srcStartPos, ulong[] target, ref int bitposition, int bits)
	{
		if (bits != 0)
		{
			int bitposition2 = srcStartPos;
			int num = bits;
			while (num > 0)
			{
				int num2 = ((num > 64) ? 64 : num);
				ulong value = source.Read(ref bitposition2, num2);
				target.Write(value, ref bitposition, num2);
				num -= num2;
			}
		}
	}

	public static void ReadOutSafe(this byte[] source, int srcStartPos, ulong[] target, ref int bitposition, int bits)
	{
		if (bits != 0)
		{
			int bitposition2 = srcStartPos;
			int num = bits;
			while (num > 0)
			{
				int num2 = ((num > 8) ? 8 : num);
				ulong value = source.Read(ref bitposition2, num2);
				target.Write(value, ref bitposition, num2);
				num -= num2;
			}
		}
	}

	public static void ReadOutSafe(this byte[] source, int srcStartPos, byte[] target, ref int bitposition, int bits)
	{
		if (bits != 0)
		{
			int bitposition2 = srcStartPos;
			int num = bits;
			while (num > 0)
			{
				int num2 = ((num > 8) ? 8 : num);
				ulong value = source.Read(ref bitposition2, num2);
				target.Write(value, ref bitposition, num2);
				num -= num2;
			}
		}
	}

	public static ulong IndexAsUInt64(this byte[] buffer, int index)
	{
		int num = index << 3;
		return buffer[num] | ((ulong)buffer[num + 1] << 8) | ((ulong)buffer[num + 2] << 16) | ((ulong)buffer[num + 3] << 24) | ((ulong)buffer[num + 4] << 32) | ((ulong)buffer[num + 5] << 40) | ((ulong)buffer[num + 6] << 48) | ((ulong)buffer[num + 7] << 56);
	}

	public static ulong IndexAsUInt64(this uint[] buffer, int index)
	{
		int num = index << 1;
		return buffer[num] | ((ulong)buffer[num + 1] << 32);
	}

	public static uint IndexAsUInt32(this byte[] buffer, int index)
	{
		int num = index << 3;
		return (uint)(buffer[num] | (buffer[num + 1] << 8) | (buffer[num + 2] << 16) | (buffer[num + 3] << 24));
	}

	public static uint IndexAsUInt32(this ulong[] buffer, int index)
	{
		int num = index >> 1;
		int num2 = (index & 1) << 5;
		return (byte)(buffer[num] >> num2);
	}

	public static byte IndexAsUInt8(this ulong[] buffer, int index)
	{
		int num = index >> 3;
		int num2 = (index & 7) << 3;
		return (byte)(buffer[num] >> num2);
	}

	public static byte IndexAsUInt8(this uint[] buffer, int index)
	{
		int num = index >> 3;
		int num2 = (index & 3) << 3;
		return (byte)((ulong)buffer[num] >> num2);
	}
}
