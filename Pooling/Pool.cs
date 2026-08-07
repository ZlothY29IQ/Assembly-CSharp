using System;
using System.Collections.Generic;
using System.Linq;
using GorillaTag;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

namespace Pooling;

public static class Pool<T> where T : Component, IPoolable<T>
{
	private static readonly Dictionary<T, IObjectPool<T>> poolDict;

	private const Transform PoolRoot = null;

	static Pool()
	{
		poolDict = new Dictionary<T, IObjectPool<T>>();
		SceneManager.sceneUnloaded += OnSceneManagerSceneUnload;
	}

	private static void OnSceneManagerSceneUnload(Scene scene)
	{
		OnSceneChange();
	}

	private static void OnSceneChange()
	{
		T[] array = poolDict.Keys.ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			DestroyPool(array[i]);
		}
	}

	public static IObjectPool<T> CreatePool(T prefab, bool collectionChecks = true, int defaultCapacity = 10, int maxPoolSize = 10000)
	{
		UnityEngine.Pool.ObjectPool<T> objectPool = new UnityEngine.Pool.ObjectPool<T>(prefab.CreateInstance, OnGet, OnRelease, OnDestroy, collectionChecks, defaultCapacity, maxPoolSize);
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

	public static T Get(T prefab, Vector3 position, Quaternion rotation, Action<T> beforeEnable = null)
	{
		GetOrCreatePool(prefab).Get(out var v);
		v.transform.SetPositionAndRotation(position, rotation);
		beforeEnable?.Invoke(v);
		v.gameObject.SetActive(value: true);
		v.OnPostGet();
		return v;
	}

	public static T Get(T prefab, Vector3 position, Quaternion rotation, Transform parent, Action<T> beforeEnable = null)
	{
		GetOrCreatePool(prefab).Get(out var v);
		v.transform.SetParent(parent);
		v.transform.SetPositionAndRotation(position, rotation);
		v.transform.localScale = prefab.transform.localScale;
		beforeEnable?.Invoke(v);
		v.gameObject.SetActive(value: true);
		v.OnPostGet();
		return v;
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
		instance.OnPreGet();
	}

	private static void OnRelease(T instance)
	{
		if (!GTAppState.isQuitting)
		{
			instance.gameObject.SetActive(value: false);
			instance.OnRelease();
			ChildToPoolRoot(instance.transform);
		}
	}

	private static void OnDestroy(T instance)
	{
		if ((bool)instance)
		{
			UnityEngine.Object.Destroy(instance.gameObject);
		}
	}
}
public static class Pool
{
	private static readonly Dictionary<GameObject, IObjectPool<GameObject>> poolDict;

	private const Transform PoolRoot = null;

	static Pool()
	{
		poolDict = new Dictionary<GameObject, IObjectPool<GameObject>>();
		SceneManager.sceneUnloaded += OnSceneManagerSceneUnload;
	}

	private static void OnSceneManagerSceneUnload(Scene scene)
	{
		OnSceneChange();
	}

	private static void OnSceneChange()
	{
		GameObject[] array = poolDict.Keys.ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			DestroyPool(array[i]);
		}
	}

	public static IObjectPool<GameObject> CreatePool(GameObject prefab, bool collectionChecks = true, int defaultCapacity = 10, int maxPoolSize = 10000)
	{
		UnityEngine.Pool.ObjectPool<GameObject> objectPool = new UnityEngine.Pool.ObjectPool<GameObject>(prefab.CreateInstance, OnGet, OnRelease, OnDestroy, collectionChecks, defaultCapacity, maxPoolSize);
		poolDict[prefab] = objectPool;
		return objectPool;
	}

	public static IObjectPool<GameObject> GetOrCreatePool(GameObject prefab)
	{
		if (!poolDict.TryGetValue(prefab, out var value))
		{
			return CreatePool(prefab);
		}
		return value;
	}

	public static IObjectPool<GameObject> GetPool(GameObject prefab)
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

	public static GameObject CreateInstance(this GameObject prefab)
	{
		GameObject gameObject = UnityEngine.Object.Instantiate(prefab);
		gameObject.gameObject.SetActive(value: false);
		gameObject.name = "[Pooled] " + prefab.name;
		return gameObject;
	}

	public static GameObject Get(this GameObject prefab)
	{
		return prefab.Get(Vector3.zero, Quaternion.identity);
	}

	public static GameObject Get(this GameObject prefab, Transform parent)
	{
		return prefab.Get(parent.position, parent.rotation, parent);
	}

	public static GameObject Get(this GameObject prefab, Vector3 position, Quaternion rotation)
	{
		GetOrCreatePool(prefab).Get(out var v);
		v.transform.SetPositionAndRotation(position, rotation);
		v.gameObject.SetActive(value: true);
		return v;
	}

	public static GameObject Get(this GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
	{
		GetOrCreatePool(prefab).Get(out var v);
		v.transform.SetParent(parent);
		v.transform.SetPositionAndRotation(position, rotation);
		v.gameObject.SetActive(value: true);
		return v;
	}

	public static GameObject GetUninstantiated(this GameObject prefab)
	{
		return prefab.GetUninstantiated(Vector3.zero, Quaternion.identity);
	}

	public static GameObject GetUninstantiated(this GameObject prefab, Vector3 position, Quaternion rotation)
	{
		GetOrCreatePool(prefab).Get(out var v);
		v.transform.SetPositionAndRotation(position, rotation);
		return v;
	}

	public static GameObject GetUninstantiated(this GameObject prefab, Transform parent)
	{
		return prefab.GetUninstantiated(parent.position, parent.rotation, parent);
	}

	public static GameObject GetUninstantiated(this GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
	{
		GetOrCreatePool(prefab).Get(out var v);
		v.transform.SetParent(parent);
		v.transform.SetPositionAndRotation(position, rotation);
		return v;
	}

	public static void Release(this GameObject prefab, GameObject instance)
	{
		if ((bool)instance && poolDict.TryGetValue(prefab, out var value))
		{
			value.Release(instance);
			instance.transform.localScale = prefab.transform.localScale;
		}
	}

	public static void DestroyPool(GameObject prefab)
	{
		IObjectPool<GameObject> pool = GetPool(prefab);
		if (pool != null)
		{
			pool.Clear();
			poolDict.Remove(prefab);
		}
	}

	private static void OnGet(GameObject instance)
	{
	}

	private static void OnRelease(GameObject instance)
	{
		if (!GTAppState.isQuitting)
		{
			instance.SetActive(value: false);
			ChildToPoolRoot(instance.transform);
		}
	}

	private static void OnDestroy(GameObject instance)
	{
		UnityEngine.Object.Destroy(instance.gameObject);
	}
}
