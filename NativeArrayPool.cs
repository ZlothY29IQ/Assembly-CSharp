using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public static class NativeArrayPool<T> where T : struct
{
	private static Dictionary<int, Stack<NativeArray<T>>> _lookup;

	static NativeArrayPool()
	{
		_lookup = new Dictionary<int, Stack<NativeArray<T>>>();
		Application.quitting += OnQuit;
	}

	private static void OnQuit()
	{
		Dispose();
	}

	[OnEnterPlay_Run]
	public static void Dispose()
	{
		if (_lookup == null)
		{
			return;
		}
		foreach (Stack<NativeArray<T>> value in _lookup.Values)
		{
			foreach (NativeArray<T> item in value)
			{
				item.Dispose();
			}
		}
		_lookup.Clear();
	}

	public static NativeArray<T> Get(int length)
	{
		if (!GetCollectionForLength(length).TryPop(out var result))
		{
			result = new NativeArray<T>(length, Allocator.Persistent);
		}
		return result;
	}

	public static void Return(NativeArray<T> item)
	{
		GetCollectionForLength(item.Length).Push(item);
	}

	private static Stack<NativeArray<T>> GetCollectionForLength(int length)
	{
		if (!_lookup.TryGetValue(length, out var value))
		{
			value = new Stack<NativeArray<T>>();
			_lookup.Add(length, value);
		}
		return value;
	}
}
