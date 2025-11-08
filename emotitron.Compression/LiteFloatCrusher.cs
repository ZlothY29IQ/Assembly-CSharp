using System;
using emotitron.Compression.HalfFloat;
using emotitron.Compression.Utilities;
using UnityEngine;

namespace emotitron.Compression;

[Serializable]
public class LiteFloatCrusher : LiteCrusher<float>
{
	[SerializeField]
	protected float min;

	[SerializeField]
	protected float max;

	[SerializeField]
	public LiteFloatCompressType compressType = LiteFloatCompressType.Half16;

	[SerializeField]
	private bool accurateCenter = true;

	[SerializeField]
	private float encoder;

	[SerializeField]
	private float decoder;

	[SerializeField]
	private ulong maxCVal;

	public LiteFloatCrusher()
	{
		compressType = LiteFloatCompressType.Half16;
		min = 0f;
		max = 1f;
		accurateCenter = true;
		Recalculate(compressType, min, max, accurateCenter, ref bits, ref encoder, ref decoder, ref maxCVal);
	}

	public LiteFloatCrusher(LiteFloatCompressType compressType, float min, float max, bool accurateCenter)
	{
		this.compressType = compressType;
		this.min = min;
		this.max = max;
		this.accurateCenter = accurateCenter;
		Recalculate(compressType, min, max, accurateCenter, ref bits, ref encoder, ref decoder, ref maxCVal);
	}

	public static void Recalculate(LiteFloatCompressType compressType, float min, float max, bool accurateCenter, ref int bits, ref float encoder, ref float decoder, ref ulong maxCVal)
	{
		bits = (int)compressType;
		float num = max - min;
		ulong num2 = (ulong)((bits == 64) ? (-1) : ((1L << bits) - 1));
		if (accurateCenter && num2 != 0L)
		{
			num2--;
		}
		encoder = ((num == 0f) ? 0f : ((float)num2 / num));
		decoder = ((num2 == 0L) ? 0f : (num / (float)num2));
		maxCVal = num2;
	}

	public override ulong Encode(float val)
	{
		if (compressType == LiteFloatCompressType.Half16)
		{
			return HalfUtilities.Pack(val);
		}
		if (compressType == LiteFloatCompressType.Full32)
		{
			return ((ByteConverter)val).uint32;
		}
		float num = (val - min) * encoder + 0.5f;
		if (num < 0f)
		{
			return 0uL;
		}
		ulong num2 = (ulong)num;
		if (num2 <= maxCVal)
		{
			return num2;
		}
		return maxCVal;
	}

	public override float Decode(uint cval)
	{
		if (compressType == LiteFloatCompressType.Half16)
		{
			return HalfUtilities.Unpack((ushort)cval);
		}
		if (compressType == LiteFloatCompressType.Full32)
		{
			return ((ByteConverter)cval).float32;
		}
		if (cval == 0)
		{
			return min;
		}
		if (cval == maxCVal)
		{
			return max;
		}
		return (float)cval * decoder + min;
	}

	public override ulong WriteValue(float val, byte[] buffer, ref int bitposition)
	{
		if (compressType == LiteFloatCompressType.Half16)
		{
			ulong num = HalfUtilities.Pack(val);
			buffer.Write(num, ref bitposition, 16);
			return num;
		}
		if (compressType == LiteFloatCompressType.Full32)
		{
			ulong num2 = ((ByteConverter)val).uint32;
			buffer.Write(num2, ref bitposition, 32);
			return num2;
		}
		ulong num3 = Encode(val);
		buffer.Write(num3, ref bitposition, (int)compressType);
		return num3;
	}

	public override void WriteCValue(uint cval, byte[] buffer, ref int bitposition)
	{
		if (compressType == LiteFloatCompressType.Half16)
		{
			buffer.Write(cval, ref bitposition, 16);
		}
		else if (compressType == LiteFloatCompressType.Full32)
		{
			buffer.Write(cval, ref bitposition, 32);
		}
		else
		{
			buffer.Write(cval, ref bitposition, (int)compressType);
		}
	}

	public override float ReadValue(byte[] buffer, ref int bitposition)
	{
		if (compressType == LiteFloatCompressType.Half16)
		{
			return HalfUtilities.Unpack((ushort)buffer.Read(ref bitposition, 16));
		}
		if (compressType == LiteFloatCompressType.Full32)
		{
			return ((ByteConverter)(uint)buffer.Read(ref bitposition, 32)).float32;
		}
		uint val = (uint)buffer.Read(ref bitposition, (int)compressType);
		return Decode(val);
	}

	public override string ToString()
	{
		return GetType().Name + " " + compressType.ToString() + " mn: " + min + " mx: " + max + " e: " + encoder + " d: " + decoder;
	}
}
