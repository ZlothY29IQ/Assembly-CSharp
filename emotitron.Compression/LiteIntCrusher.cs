using System;
using UnityEngine;

namespace emotitron.Compression;

[Serializable]
public class LiteIntCrusher : LiteCrusher<int>
{
	[SerializeField]
	public LiteIntCompressType compressType;

	[SerializeField]
	protected int min;

	[SerializeField]
	protected int max;

	[SerializeField]
	private int smallest;

	[SerializeField]
	private int biggest;

	public LiteIntCrusher()
	{
		compressType = LiteIntCompressType.PackSigned;
		min = -128;
		max = 127;
		if (compressType == LiteIntCompressType.Range)
		{
			Recalculate(min, max, ref smallest, ref biggest, ref bits);
		}
	}

	public LiteIntCrusher(LiteIntCompressType comType = LiteIntCompressType.PackSigned, int min = -128, int max = 127)
	{
		compressType = comType;
		this.min = min;
		this.max = max;
		if (compressType == LiteIntCompressType.Range)
		{
			Recalculate(min, max, ref smallest, ref biggest, ref bits);
		}
	}

	public override ulong WriteValue(int val, byte[] buffer, ref int bitposition)
	{
		switch (compressType)
		{
		case LiteIntCompressType.PackUnsigned:
			buffer.WritePackedBytes((uint)val, ref bitposition, 32);
			return (uint)val;
		case LiteIntCompressType.PackSigned:
		{
			uint num2 = (uint)((val << 1) ^ (val >> 31));
			buffer.WritePackedBytes(num2, ref bitposition, 32);
			return num2;
		}
		case LiteIntCompressType.Range:
		{
			ulong num = Encode(val);
			buffer.Write(num, ref bitposition, bits);
			return num;
		}
		default:
			return 0uL;
		}
	}

	public override void WriteCValue(uint cval, byte[] buffer, ref int bitposition)
	{
		switch (compressType)
		{
		case LiteIntCompressType.PackUnsigned:
			buffer.WritePackedBytes(cval, ref bitposition, 32);
			break;
		case LiteIntCompressType.PackSigned:
			buffer.WritePackedBytes(cval, ref bitposition, 32);
			break;
		case LiteIntCompressType.Range:
			buffer.Write(cval, ref bitposition, bits);
			break;
		}
	}

	public override int ReadValue(byte[] buffer, ref int bitposition)
	{
		switch (compressType)
		{
		case LiteIntCompressType.PackUnsigned:
			return (int)buffer.ReadPackedBytes(ref bitposition, 32);
		case LiteIntCompressType.PackSigned:
			return buffer.ReadSignedPackedBytes(ref bitposition, 32);
		case LiteIntCompressType.Range:
		{
			uint val = (uint)buffer.Read(ref bitposition, bits);
			return Decode(val);
		}
		default:
			return 0;
		}
	}

	public override ulong Encode(int value)
	{
		value = ((value > biggest) ? biggest : ((value < smallest) ? smallest : value));
		return (ulong)(value - smallest);
	}

	public override int Decode(uint cvalue)
	{
		return (int)(cvalue + smallest);
	}

	public static void Recalculate(int min, int max, ref int smallest, ref int biggest, ref int bits)
	{
		if (min < max)
		{
			smallest = min;
			biggest = max;
		}
		else
		{
			smallest = max;
			biggest = min;
		}
		int maxvalue = biggest - smallest;
		bits = LiteCrusher.GetBitsForMaxValue((uint)maxvalue);
	}

	public override string ToString()
	{
		return GetType().Name + " " + compressType.ToString() + " mn: " + min + " mx: " + max + " sm: " + smallest;
	}
}
