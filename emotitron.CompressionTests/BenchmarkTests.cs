using System.Diagnostics;
using emotitron.Compression;
using UnityEngine;

namespace emotitron.CompressionTests;

public class BenchmarkTests : MonoBehaviour
{
	public const int BYTE_CNT = 128;

	public const int LOOP = 1000000;

	public static byte[] buffer = new byte[4800];

	public static uint[] ibuffer = new uint[128];

	public static ulong[] ubuffer = new ulong[128];

	public static ulong[] ubuffer2 = new ulong[128];

	private void Start()
	{
		TestWriterIntegrity();
		ArrayCopy();
		ArrayCopySafe();
	}

	public static void TestWriterIntegrity()
	{
		int bitposition = 1;
		int bitposition2 = 1;
		ubuffer.Write(ulong.MaxValue, ref bitposition, 64);
		if (ubuffer.Read(ref bitposition2, 64) != ulong.MaxValue)
		{
			UnityEngine.Debug.Log("Error writing with maxulong");
		}
		for (int i = 0; i < 3000; i++)
		{
			bitposition = Random.Range(0, 200);
			bitposition2 = bitposition;
			int num = bitposition;
			int num2 = Random.Range(1, 64);
			int num3 = Random.Range(-(1 << num2 - 1), (1 << num2 - 1) - 1);
			ubuffer.WriteSigned(num3, ref bitposition, num2);
			ubuffer.WriteSigned(num3, ref bitposition, num2);
			ubuffer.WriteSigned(num3, ref bitposition, num2);
			if (ubuffer.ReadSigned(ref bitposition2, num2) != num3)
			{
				UnityEngine.Debug.Log("Error writing " + num3 + " to pos " + num + " with size " + num2);
			}
			if (ubuffer.ReadSigned(ref bitposition2, num2) != num3)
			{
				UnityEngine.Debug.Log("Error writing " + num3 + " to pos " + num + " with size " + num2);
			}
			if (ubuffer.ReadSigned(ref bitposition2, num2) != num3)
			{
				UnityEngine.Debug.Log("Error writing " + num3 + " to pos " + num + " with size " + num2);
			}
			ulong num4 = (ulong)Random.Range(0f, (ulong)((1L << num2) - 1));
			ubuffer.Write(num4, ref bitposition, num2);
			ubuffer.Write(num4, ref bitposition, num2);
			ubuffer.Write(num4, ref bitposition, num2);
			if (ubuffer.Read(ref bitposition2, num2) != num4)
			{
				UnityEngine.Debug.Log("Error writing " + num4 + " to pos " + num + " with size " + num2);
			}
			if (ubuffer.Read(ref bitposition2, num2) != num4)
			{
				UnityEngine.Debug.Log("Error writing " + num4 + " to pos " + num + " with size " + num2);
			}
			if (ubuffer.Read(ref bitposition2, num2) != num4)
			{
				UnityEngine.Debug.Log("Error writing " + num4 + " to pos " + num + " with size " + num2);
			}
		}
		UnityEngine.Debug.Log("Integrity check complete.");
	}

	private static void TestLog2()
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		for (uint num = 0u; num <= uint.MaxValue; num += 3000)
		{
			num.UsedBitCount();
			num.UsedBitCount();
			num.UsedBitCount();
			num.UsedBitCount();
			num.UsedBitCount();
			if ((uint)(-1 - (int)num) < 4000u)
			{
				break;
			}
		}
		stopwatch.Stop();
		UnityEngine.Debug.Log("Log2 nifty: time=" + stopwatch.ElapsedMilliseconds + " ms");
	}

	private static void ArrayCopy()
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		for (int i = 0; i < 1000000; i++)
		{
			int targetPos = 0;
			ubuffer.ReadOutUnsafe(0, buffer, ref targetPos, 960);
		}
		stopwatch.Stop();
		UnityEngine.Debug.Log("Array Copy Unsafe: time=" + stopwatch.ElapsedMilliseconds + " ms");
	}

	private static void ArrayCopySafe()
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		for (int i = 0; i < 1000000; i++)
		{
			int bitposition = 0;
			ubuffer.ReadOutSafe(0, buffer, ref bitposition, 960);
		}
		stopwatch.Stop();
		UnityEngine.Debug.Log("Array Copy Safe: time=" + stopwatch.ElapsedMilliseconds + " ms");
	}

	public static void ByteForByteWrite()
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		for (int i = 0; i < 1000000; i++)
		{
			BasicWriter.Reset();
			for (int j = 0; j < 128; j++)
			{
				BasicWriter.BasicWrite(buffer, byte.MaxValue);
			}
			BasicWriter.Reset();
			for (int k = 0; k < 128; k++)
			{
				BasicWriter.BasicRead(buffer);
			}
		}
		stopwatch.Stop();
		UnityEngine.Debug.Log("Byte For Byte: time=" + stopwatch.ElapsedMilliseconds + " ms");
	}

	public static void BitpackBytesEven()
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		for (int i = 0; i < 1000000; i++)
		{
			int bitposition = 0;
			for (int j = 0; j < 128; j++)
			{
				buffer.Write(255uL, ref bitposition, 8);
			}
			bitposition = 0;
			for (int k = 0; k < 127; k++)
			{
				buffer.Read(ref bitposition, 8);
			}
		}
		stopwatch.Stop();
		UnityEngine.Debug.Log("Even Bitpack byte: time=" + stopwatch.ElapsedMilliseconds + " ms");
	}

	public static void BitpackBytesToULongUneven()
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		for (int i = 0; i < 1000000; i++)
		{
			int bitposition = 0;
			ubuffer.Write(1uL, ref bitposition, 1);
			for (int j = 0; j < 127; j++)
			{
				ubuffer.Write(255uL, ref bitposition, 33);
			}
			bitposition = 0;
			ubuffer.Read(ref bitposition, 1);
			for (int k = 0; k < 127; k++)
			{
				ubuffer.Read(ref bitposition, 33);
			}
		}
		stopwatch.Stop();
		UnityEngine.Debug.Log("Uneven Bitpack ulong[]: time=" + stopwatch.ElapsedMilliseconds + " ms");
	}

	public static void BitpackBytesUnEven()
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		for (int i = 0; i < 1000000; i++)
		{
			int bitposition = 0;
			buffer.Write(1uL, ref bitposition, 1);
			for (int j = 0; j < 127; j++)
			{
				buffer.Write(255uL, ref bitposition, 8);
			}
			bitposition = 0;
			buffer.Read(ref bitposition, 1);
			for (int k = 0; k < 127; k++)
			{
				buffer.Read(ref bitposition, 8);
			}
		}
		stopwatch.Stop();
		UnityEngine.Debug.Log("Uneven Bitpack byte: time=" + stopwatch.ElapsedMilliseconds + " ms");
	}
}
