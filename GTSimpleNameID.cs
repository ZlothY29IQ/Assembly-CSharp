using System;
using Unity.Mathematics;

[Serializable]
public struct GTSimpleNameID
{
	public ulong U0;

	public ulong U1;

	public ulong U2;

	public ulong U3;

	private const string _k_possibleChars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz_-";

	private const int _k_maxLength = 41;

	private const ulong _k_bitmask6Bits = 63uL;

	private const ushort _k_indexOf_A = 10;

	private const ushort _k_indexOf_a = 36;

	private const ushort _k_indexOf_underscore = 62;

	private const ushort _k_indexOf_hyphen = 63;

	static GTSimpleNameID()
	{
		if ("0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz_-".Length != 64 || "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz_-"[0] != '0' || "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz_-"[9] != '9' || "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz_-"[10] != 'A' || "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz_-"[36] != 'a' || "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz_-"[62] != '_' || "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz_-"[63] != '-')
		{
			throw new Exception("GTSimpleNameID: The constant string `_k_possibleChars` does not match the expected format. Did you change something without updating the logic?");
		}
	}

	public unsafe static GTSimpleNameID FromString(string input)
	{
		if (input == null)
		{
			input = string.Empty;
		}
		GTSimpleNameID result = default(GTSimpleNameID);
		int num = math.min(input.Length, 41);
		result.U0 = (ulong)num & 0x3FuL;
		int num2 = 6;
		for (int i = 0; i < num; i++)
		{
			char c = input[i];
			byte b;
			switch (c)
			{
			case '0':
			case '1':
			case '2':
			case '3':
			case '4':
			case '5':
			case '6':
			case '7':
			case '8':
			case '9':
				b = (byte)(c - 48);
				break;
			case 'A':
			case 'B':
			case 'C':
			case 'D':
			case 'E':
			case 'F':
			case 'G':
			case 'H':
			case 'I':
			case 'J':
			case 'K':
			case 'L':
			case 'M':
			case 'N':
			case 'O':
			case 'P':
			case 'Q':
			case 'R':
			case 'S':
			case 'T':
			case 'U':
			case 'V':
			case 'W':
			case 'X':
			case 'Y':
			case 'Z':
				b = (byte)(c - 65 + 10);
				break;
			case 'a':
			case 'b':
			case 'c':
			case 'd':
			case 'e':
			case 'f':
			case 'g':
			case 'h':
			case 'i':
			case 'j':
			case 'k':
			case 'l':
			case 'm':
			case 'n':
			case 'o':
			case 'p':
			case 'q':
			case 'r':
			case 's':
			case 't':
			case 'u':
			case 'v':
			case 'w':
			case 'x':
			case 'y':
			case 'z':
				b = (byte)(c - 97 + 36);
				break;
			case '_':
				b = 62;
				break;
			case '-':
				b = 63;
				break;
			default:
				throw new ArgumentException($"Invalid character '{c}' in input string.", "input");
			}
			byte num3 = b;
			int num4 = num2 + i * 6;
			ulong* ptr = &result.U0;
			int num5 = num4 / 64;
			int num6 = num4 % 64;
			ulong num7 = 63uL;
			ulong num8 = num3 & num7;
			ulong num9 = ~(num7 << num6);
			ptr[num5] &= num9;
			ptr[num5] |= num8 << num6;
			int num10 = 64 - num6;
			if (num10 < 6 && num5 < 3)
			{
				int num11 = 6 - num10;
				ulong num12 = (ulong)((1L << num11) - 1);
				ulong num13 = num8 >> num10;
				ptr[num5 + 1] &= ~num12;
				ptr[num5 + 1] |= num13;
			}
		}
		return result;
	}

	public override string ToString()
	{
		int num = math.min((int)(U0 & 0x3F), 41);
		char[] array = new char[num];
		int num2 = 6;
		for (int i = 0; i < num; i++)
		{
			int bitOffset = num2 + i * 6;
			ulong num3 = _Read6Bits(this, bitOffset);
			array[i] = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz_-"[(int)num3];
		}
		return new string(array);
	}

	private unsafe static ulong _Read6Bits(in GTSimpleNameID cv, int bitOffset)
	{
		fixed (ulong* u = &cv.U0)
		{
			int num = bitOffset / 64;
			int num2 = bitOffset % 64;
			ulong num3 = u[num] >> num2;
			int num4 = 64 - num2;
			if (num4 < 6 && num < 3)
			{
				int num5 = 6 - num4;
				ulong num6 = (ulong)((1L << num5) - 1);
				ulong num7 = u[num + 1] & num6;
				num7 <<= num4;
				num3 |= num7;
			}
			return num3 & 0x3F;
		}
	}
}
