using System;
using System.Collections.Generic;

public static class LinqUtils
{
	public static IEnumerable<TResult> SelectManyNullSafe<TSource, TResult>(this IEnumerable<TSource> sources, Func<TSource, IEnumerable<TResult>> selector)
	{
		if (sources == null || selector == null)
		{
			yield break;
		}
		foreach (TSource source in sources)
		{
			if (source == null)
			{
				continue;
			}
			IEnumerable<TResult> enumerable = selector(source);
			foreach (TResult item in enumerable)
			{
				if (item != null)
				{
					yield return item;
				}
			}
		}
	}

	public static IEnumerable<TSource> DistinctBy<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
	{
		HashSet<TResult> set = new HashSet<TResult>();
		foreach (TSource item2 in source)
		{
			TResult item = selector(item2);
			if (set.Add(item))
			{
				yield return item2;
			}
		}
	}

	public static IEnumerable<T> ForEach<T>(this IEnumerable<T> source, Action<T> action)
	{
		foreach (T item in source)
		{
			action(item);
		}
		return source;
	}

	public static T[] AsArray<T>(this IEnumerable<T> source)
	{
		return (T[])source;
	}

	public static List<T> AsList<T>(this IEnumerable<T> source)
	{
		return (List<T>)source;
	}

	public static IList<T> Transform<T>(this IList<T> list, Func<T, T> action)
	{
		for (int i = 0; i < list.Count; i++)
		{
			list[i] = action(list[i]);
		}
		return list;
	}

	public static IEnumerable<T> Self<T>(this T value)
	{
		yield return value;
	}
}
