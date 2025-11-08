using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Cysharp.Text;
using UnityEngine;

namespace GorillaExtensions;

public static class GTStringBuilderExtensions
{
	public static IEnumerable<ReadOnlyMemory<char>> GetSegmentsOfMem(this Utf16ValueStringBuilder sb, int maxCharsPerSegment = 16300)
	{
		int num = 0;
		List<ReadOnlyMemory<char>> list = new List<ReadOnlyMemory<char>>(64);
		ReadOnlyMemory<char> readOnlyMemory = sb.AsMemory();
		while (num < readOnlyMemory.Length)
		{
			int num2 = Mathf.Min(num + maxCharsPerSegment, readOnlyMemory.Length);
			if (num2 < readOnlyMemory.Length)
			{
				int num3 = -1;
				for (int num4 = num2 - 1; num4 >= num; num4--)
				{
					if (readOnlyMemory.Span[num4] == '\n')
					{
						num3 = num4;
						break;
					}
				}
				if (num3 != -1)
				{
					num2 = num3;
				}
			}
			list.Add(readOnlyMemory.Slice(num, num2 - num));
			num = num2 + 1;
		}
		return list;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void GTAddPath(this Utf16ValueStringBuilder stringBuilderToAddTo, GameObject gameObject)
	{
		gameObject.transform.GetPathQ(ref stringBuilderToAddTo);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void GTAddPath(this Utf16ValueStringBuilder stringBuilderToAddTo, Transform transform)
	{
		transform.GetPathQ(ref stringBuilderToAddTo);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Q(this Utf16ValueStringBuilder sb, string value)
	{
		sb.Append('"');
		sb.Append(value);
		sb.Append('"');
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void GTMany(this Utf16ValueStringBuilder sb, string a, string b)
	{
		sb.Append(a);
		sb.Append(b);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void GTMany(this Utf16ValueStringBuilder sb, string a, string b, string c)
	{
		sb.Append(a);
		sb.Append(b);
		sb.Append(c);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void GTMany(this Utf16ValueStringBuilder sb, string a, string b, string c, string d)
	{
		sb.Append(a);
		sb.Append(b);
		sb.Append(c);
		sb.Append(d);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void GTMany(this Utf16ValueStringBuilder sb, string a, string b, string c, string d, string e)
	{
		sb.Append(a);
		sb.Append(b);
		sb.Append(c);
		sb.Append(d);
		sb.Append(e);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void GTMany(this Utf16ValueStringBuilder sb, string a, string b, string c, string d, string e, string f)
	{
		sb.Append(a);
		sb.Append(b);
		sb.Append(c);
		sb.Append(d);
		sb.Append(e);
		sb.Append(f);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void GTMany(this Utf16ValueStringBuilder sb, string a, string b, string c, string d, string e, string f, string g)
	{
		sb.Append(a);
		sb.Append(b);
		sb.Append(c);
		sb.Append(d);
		sb.Append(e);
		sb.Append(f);
		sb.Append(g);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void GTMany(this Utf16ValueStringBuilder sb, string a, string b, string c, string d, string e, string f, string g, string h)
	{
		sb.Append(a);
		sb.Append(b);
		sb.Append(c);
		sb.Append(d);
		sb.Append(e);
		sb.Append(f);
		sb.Append(g);
		sb.Append(h);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void GTMany(this Utf16ValueStringBuilder sb, string a, string b, string c, string d, string e, string f, string g, string h, string i)
	{
		sb.Append(a);
		sb.Append(b);
		sb.Append(c);
		sb.Append(d);
		sb.Append(e);
		sb.Append(f);
		sb.Append(g);
		sb.Append(h);
		sb.Append(i);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void GTMany(this Utf16ValueStringBuilder sb, string a, string b, string c, string d, string e, string f, string g, string h, string i, string j)
	{
		sb.Append(a);
		sb.Append(b);
		sb.Append(c);
		sb.Append(d);
		sb.Append(e);
		sb.Append(f);
		sb.Append(g);
		sb.Append(h);
		sb.Append(i);
		sb.Append(j);
	}
}
