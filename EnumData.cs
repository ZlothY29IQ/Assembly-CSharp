using System;
using System.Collections.Generic;

public class EnumData<TEnum> where TEnum : struct, Enum
{
	public readonly string[] Names;

	public readonly TEnum[] Values;

	public readonly long[] LongValues;

	public readonly bool IsBitMaskCompatible;

	public readonly Dictionary<TEnum, string> EnumToName;

	public readonly Dictionary<string, TEnum> NameToEnum;

	public readonly Dictionary<TEnum, int> EnumToIndex;

	public readonly Dictionary<int, TEnum> IndexToEnum;

	public readonly Dictionary<TEnum, long> EnumToLong;

	public readonly Dictionary<long, TEnum> LongToEnum;

	public readonly TEnum MinValue;

	public readonly TEnum MaxValue;

	public readonly int MinInt;

	public readonly int MaxInt;

	public readonly long MinLong;

	public readonly long MaxLong;

	public static EnumData<TEnum> Shared { get; } = new EnumData<TEnum>();

	private EnumData()
	{
		Names = Enum.GetNames(typeof(TEnum));
		Values = (TEnum[])Enum.GetValues(typeof(TEnum));
		int num = Names.Length;
		LongValues = new long[num];
		EnumToName = new Dictionary<TEnum, string>(num);
		NameToEnum = new Dictionary<string, TEnum>(num * 2);
		EnumToIndex = new Dictionary<TEnum, int>(num);
		IndexToEnum = new Dictionary<int, TEnum>(num);
		EnumToLong = new Dictionary<TEnum, long>(num);
		LongToEnum = new Dictionary<long, TEnum>(num);
		long num2 = long.MaxValue;
		long num3 = long.MinValue;
		for (int i = 0; i < Names.Length; i++)
		{
			string text = Names[i];
			TEnum val = Values[i];
			long num4 = Convert.ToInt64(val);
			LongValues[i] = num4;
			EnumToName[val] = text;
			NameToEnum[text] = val;
			NameToEnum.TryAdd(text.ToLowerInvariant(), val);
			EnumToIndex[val] = i;
			IndexToEnum[i] = val;
			EnumToLong[val] = num4;
			LongToEnum[num4] = val;
			num2 = Math.Min(num4, num2);
			num3 = Math.Max(num4, num3);
		}
		for (int j = 0; j < Names.Length; j++)
		{
			string key = Names[j];
			TEnum value = Values[j];
			NameToEnum[key] = value;
		}
		MinValue = LongToEnum[num2];
		MaxValue = LongToEnum[num3];
		MinInt = Convert.ToInt32(num2);
		MaxInt = Convert.ToInt32(num3);
		MinLong = num2;
		MaxLong = num3;
		long num5 = 0L;
		bool isBitMaskCompatible = true;
		long[] longValues = LongValues;
		foreach (long num6 in longValues)
		{
			if (num6 != 0L && (num6 & (num6 - 1)) != 0L && (num5 & num6) != num6)
			{
				isBitMaskCompatible = false;
				break;
			}
			num5 |= num6;
		}
		IsBitMaskCompatible = isBitMaskCompatible;
	}
}
