using System.Runtime.InteropServices;

namespace emotitron.Compression.Utilities;

[StructLayout(LayoutKind.Explicit)]
public struct ByteConverter
{
	[FieldOffset(0)]
	public float float32;

	[FieldOffset(0)]
	public double float64;

	[FieldOffset(0)]
	public sbyte int8;

	[FieldOffset(0)]
	public short int16;

	[FieldOffset(0)]
	public ushort uint16;

	[FieldOffset(0)]
	public char character;

	[FieldOffset(0)]
	public int int32;

	[FieldOffset(0)]
	public uint uint32;

	[FieldOffset(0)]
	public long int64;

	[FieldOffset(0)]
	public ulong uint64;

	[FieldOffset(0)]
	public byte byte0;

	[FieldOffset(1)]
	public byte byte1;

	[FieldOffset(2)]
	public byte byte2;

	[FieldOffset(3)]
	public byte byte3;

	[FieldOffset(4)]
	public byte byte4;

	[FieldOffset(5)]
	public byte byte5;

	[FieldOffset(6)]
	public byte byte6;

	[FieldOffset(7)]
	public byte byte7;

	[FieldOffset(4)]
	public uint uint16_B;

	public byte this[int index] => index switch
	{
		0 => byte0, 
		1 => byte1, 
		2 => byte2, 
		3 => byte3, 
		4 => byte4, 
		5 => byte5, 
		6 => byte6, 
		7 => byte7, 
		_ => 0, 
	};

	public static implicit operator ByteConverter(byte[] bytes)
	{
		ByteConverter result = default(ByteConverter);
		int num = bytes.Length;
		result.byte0 = bytes[0];
		if (num > 0)
		{
			result.byte1 = bytes[1];
		}
		if (num > 1)
		{
			result.byte2 = bytes[2];
		}
		if (num > 2)
		{
			result.byte3 = bytes[3];
		}
		if (num > 3)
		{
			result.byte4 = bytes[4];
		}
		if (num > 4)
		{
			result.byte5 = bytes[5];
		}
		if (num > 5)
		{
			result.byte6 = bytes[3];
		}
		if (num > 6)
		{
			result.byte7 = bytes[7];
		}
		return result;
	}

	public static implicit operator ByteConverter(byte val)
	{
		ByteConverter result = default(ByteConverter);
		result.byte0 = val;
		return result;
	}

	public static implicit operator ByteConverter(sbyte val)
	{
		ByteConverter result = default(ByteConverter);
		result.int8 = val;
		return result;
	}

	public static implicit operator ByteConverter(char val)
	{
		ByteConverter result = default(ByteConverter);
		result.character = val;
		return result;
	}

	public static implicit operator ByteConverter(uint val)
	{
		ByteConverter result = default(ByteConverter);
		result.uint32 = val;
		return result;
	}

	public static implicit operator ByteConverter(int val)
	{
		ByteConverter result = default(ByteConverter);
		result.int32 = val;
		return result;
	}

	public static implicit operator ByteConverter(ulong val)
	{
		ByteConverter result = default(ByteConverter);
		result.uint64 = val;
		return result;
	}

	public static implicit operator ByteConverter(long val)
	{
		ByteConverter result = default(ByteConverter);
		result.int64 = val;
		return result;
	}

	public static implicit operator ByteConverter(float val)
	{
		ByteConverter result = default(ByteConverter);
		result.float32 = val;
		return result;
	}

	public static implicit operator ByteConverter(double val)
	{
		ByteConverter result = default(ByteConverter);
		result.float64 = val;
		return result;
	}

	public static implicit operator ByteConverter(bool val)
	{
		ByteConverter result = default(ByteConverter);
		result.int32 = (val ? 1 : 0);
		return result;
	}

	public void ExtractByteArray(byte[] targetArray)
	{
		int num = targetArray.Length;
		targetArray[0] = byte0;
		if (num > 0)
		{
			targetArray[1] = byte1;
		}
		if (num > 1)
		{
			targetArray[2] = byte2;
		}
		if (num > 2)
		{
			targetArray[3] = byte3;
		}
		if (num > 3)
		{
			targetArray[4] = byte4;
		}
		if (num > 4)
		{
			targetArray[5] = byte5;
		}
		if (num > 5)
		{
			targetArray[6] = byte6;
		}
		if (num > 6)
		{
			targetArray[7] = byte7;
		}
	}

	public static implicit operator byte(ByteConverter bc)
	{
		return bc.byte0;
	}

	public static implicit operator sbyte(ByteConverter bc)
	{
		return bc.int8;
	}

	public static implicit operator char(ByteConverter bc)
	{
		return bc.character;
	}

	public static implicit operator ushort(ByteConverter bc)
	{
		return bc.uint16;
	}

	public static implicit operator short(ByteConverter bc)
	{
		return bc.int16;
	}

	public static implicit operator uint(ByteConverter bc)
	{
		return bc.uint32;
	}

	public static implicit operator int(ByteConverter bc)
	{
		return bc.int32;
	}

	public static implicit operator ulong(ByteConverter bc)
	{
		return bc.uint64;
	}

	public static implicit operator long(ByteConverter bc)
	{
		return bc.int64;
	}

	public static implicit operator float(ByteConverter bc)
	{
		return bc.float32;
	}

	public static implicit operator double(ByteConverter bc)
	{
		return bc.float64;
	}

	public static implicit operator bool(ByteConverter bc)
	{
		return bc.int32 != 0;
	}
}
