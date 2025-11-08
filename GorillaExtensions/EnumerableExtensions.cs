using System;
using System.Collections.Generic;

namespace GorillaExtensions;

public static class EnumerableExtensions
{
	public static TValue MinBy<TValue, TKey>(this IEnumerable<TValue> ts, Func<TValue, TKey> keyGetter) where TKey : struct, IComparable<TKey>
	{
		TValue result = default(TValue);
		TKey? val = null;
		foreach (TValue t in ts)
		{
			TKey value = keyGetter(t);
			if (!val.HasValue || value.CompareTo(val.Value) < 0)
			{
				result = t;
				val = value;
			}
		}
		if (!val.HasValue)
		{
			throw new ArgumentException("Cannot calculate MinBy on an empty IEnumerable.");
		}
		return result;
	}

	public static IEnumerable<T> Peek<T>(this IEnumerable<T> ts, Action<T> action)
	{
		foreach (T t in ts)
		{
			action(t);
			yield return t;
		}
	}
}
