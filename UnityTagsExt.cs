using System.ComponentModel;
using UnityEngine;

public static class UnityTagsExt
{
	public static UnityTag ToTag(this string s)
	{
		if (string.IsNullOrWhiteSpace(s))
		{
			return UnityTag.Invalid;
		}
		if (!UnityTags.StringToTag.TryGetValue(s, out var value))
		{
			return UnityTag.Invalid;
		}
		return value;
	}

	public static void SetTag(this UnityEngine.Component c, UnityTag tag)
	{
		if (!(c == null))
		{
			if (tag == UnityTag.Invalid)
			{
				throw new InvalidEnumArgumentException("tag");
			}
			c.tag = UnityTags.StringValues[(int)tag];
		}
	}

	public static void SetTag(this GameObject g, UnityTag tag)
	{
		if (!(g == null))
		{
			if (tag == UnityTag.Invalid)
			{
				throw new InvalidEnumArgumentException("tag");
			}
			g.tag = UnityTags.StringValues[(int)tag];
		}
	}

	public static bool TryGetTag(this GameObject g, out UnityTag tag)
	{
		tag = UnityTag.Invalid;
		if (g == null)
		{
			return false;
		}
		return UnityTags.StringToTag.TryGetValue(g.tag, out tag);
	}

	public static bool TryGetTag(this UnityEngine.Component c, out UnityTag tag)
	{
		tag = UnityTag.Invalid;
		if (c == null)
		{
			return false;
		}
		return UnityTags.StringToTag.TryGetValue(c.tag, out tag);
	}

	public static bool CompareTag(this GameObject g, UnityTag tag)
	{
		if (g == null)
		{
			return false;
		}
		if (tag == UnityTag.Invalid)
		{
			throw new InvalidEnumArgumentException("tag");
		}
		return g.CompareTag(UnityTags.StringValues[(int)tag]);
	}

	public static bool CompareTag(this UnityEngine.Component c, UnityTag tag)
	{
		if (c == null)
		{
			return false;
		}
		if (tag == UnityTag.Invalid)
		{
			throw new InvalidEnumArgumentException("tag");
		}
		return c.CompareTag(UnityTags.StringValues[(int)tag]);
	}
}
