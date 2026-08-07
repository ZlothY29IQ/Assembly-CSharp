using System.Collections.Generic;
using System.Linq;
using GorillaTag;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

namespace Pooling;

public static class ComponentPool<T> where T : Component
{
	private static readonly Dictionary<T, IObjectPool<T>> poolDict;

	private const Transform PoolRoot = null;

	static ComponentPool()
	{
		poolDict = new Dictionary<T, IObjectPool<T>>();
		SceneManager.sceneUnloaded += OnSceneManagerSceneUnload;
	}

	private static void OnSceneManagerSceneUnload(Scene scene)
	{
		OnSceneWillChange();
	}

	private static void OnSceneWillChange()
	{
		T[] array = poolDict.Keys.ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			DestroyPool(array[i]);
		}
	}

	public static IObjectPool<T> CreatePool(T prefab, bool collectionChecks = true, int defaultCapacity = 10, int maxPoolSize = 10000)
	{
		UnityEngine.Pool.ObjectPool<T> objectPool = new UnityEngine.Pool.ObjectPool<T>(prefab.CreateComponentInstance, OnGet, OnRelease, OnDestroy, collectionChecks, defaultCapacity, maxPoolSize);
		poolDict[prefab] = objectPool;
		return objectPool;
	}

	public static IObjectPool<T> GetOrCreatePool(T prefab)
	{
		if (!poolDict.TryGetValue(prefab, out var value))
		{
			return CreatePool(prefab);
		}
		return value;
	}

	public static IObjectPool<T> GetPool(T prefab)
	{
		poolDict.TryGetValue(prefab, out var value);
		return value;
	}

	public static void ChildToPoolRoot(Transform transform)
	{
		if (transform.parent != null)
		{
			transform.SetParent(null);
		}
	}

	public static void Release(T prefab, T instance)
	{
		if (poolDict.TryGetValue(prefab, out var value))
		{
			value.Release(instance);
		}
	}

	public static void DestroyPool(T prefab)
	{
		IObjectPool<T> pool = GetPool(prefab);
		if (pool != null)
		{
			pool.Clear();
			poolDict.Remove(prefab);
		}
	}

	private static void OnGet(T instance)
	{
	}

	private static void OnRelease(T instance)
	{
		if (!GTAppState.isQuitting)
		{
			instance.gameObject.SetActive(value: false);
			ChildToPoolRoot(instance.transform);
		}
	}

	private static void OnDestroy(T instance)
	{
		Object.Destroy(instance.gameObject);
	}
}
