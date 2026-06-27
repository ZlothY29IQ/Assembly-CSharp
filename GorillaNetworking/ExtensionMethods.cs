using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace GorillaNetworking;

public static class ExtensionMethods
{
	public static void SafeInvoke<T>(this Action<T> action, T data)
	{
		try
		{
			action?.Invoke(data);
		}
		catch (Exception arg)
		{
			Debug.LogError($"[PlayFabTitleDataCache::SafeInvoke] Failure invoking action: {arg}");
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void AddOrUpdate<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue value)
	{
		dict[key] = value;
	}
}
