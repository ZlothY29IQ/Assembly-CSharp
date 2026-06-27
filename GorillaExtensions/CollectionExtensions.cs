using System.Collections.Generic;

namespace GorillaExtensions;

public static class CollectionExtensions
{
	public static void AddAll<T>(this ICollection<T> collection, IEnumerable<T> ts)
	{
		foreach (T t in ts)
		{
			collection.Add(t);
		}
	}

	public static void CopyStringKeepDelimiterAtEnd(this HashSet<string> hash, string str, char delimiter)
	{
		if (string.IsNullOrEmpty(str))
		{
			return;
		}
		int i = 0;
		int num = 0;
		for (int length = str.Length; i < length; i++)
		{
			if (str[i] == delimiter)
			{
				hash.Add(str.Substring(num, i - num));
				num = i + 1;
			}
		}
	}

	public static bool ContainsAll<T>(this ICollection<T> collection, IEnumerable<T> ts)
	{
		foreach (T t in ts)
		{
			if (!collection.Contains(t))
			{
				return false;
			}
		}
		return true;
	}
}
