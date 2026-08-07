using System;
using UnityEngine;

namespace Pooling;

public static class PoolableExts
{
	public static T CreateInstance<T>(this T prefab) where T : Component, IPoolable<T>
	{
		T val = UnityEngine.Object.Instantiate(prefab);
		val.gameObject.SetActive(value: false);
		val.name = "[Pooled] " + prefab.name;
		val.Pool = Pool<T>.GetOrCreatePool(prefab);
		val.OnCreate();
		return val;
	}

	public static T Get<T>(this T prefab) where T : Component, IPoolable<T>
	{
		return prefab.Get(Vector3.zero, Quaternion.identity);
	}

	public static T Get<T>(this T prefab, Transform parent) where T : Component, IPoolable<T>
	{
		return prefab.Get(parent.position, parent.rotation, parent);
	}

	public static T Get<T>(this T prefab, Vector3 position, Quaternion rotation, Action<T> beforeEnable = null) where T : Component, IPoolable<T>
	{
		return Pool<T>.Get(prefab, position, rotation, beforeEnable);
	}

	public static T Get<T>(this T prefab, Vector3 position, Quaternion rotation, Transform parent, Action<T> beforeEnable = null) where T : Component, IPoolable<T>
	{
		return Pool<T>.Get(prefab, position, rotation, parent, beforeEnable);
	}

	public static void Release<T>(this T poolable) where T : Component, IPoolable<T>
	{
		poolable.Pool.Release(poolable);
	}

	public static void DestroyPool<T>(this T prefab) where T : Component, IPoolable<T>
	{
		Pool<T>.DestroyPool(prefab);
	}
}
