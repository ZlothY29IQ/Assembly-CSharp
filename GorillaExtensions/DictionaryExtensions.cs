using System.Collections.Generic;

namespace GorillaExtensions;

public static class DictionaryExtensions
{
	public static TValue GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key) where TValue : new()
	{
		if (dict.TryGetValue(key, out var value))
		{
			return value;
		}
		dict[key] = new TValue();
		return dict[key];
	}
}
