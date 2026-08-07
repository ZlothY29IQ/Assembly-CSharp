using UnityEngine;

namespace Pooling;

public static class ComponentPoolExts
{
	public static T CreateComponentInstance<T>(this T prefab) where T : Component
	{
		T val = Object.Instantiate(prefab);
		val.gameObject.SetActive(value: false);
		val.name = "[Pooled] " + prefab.name;
		return val;
	}

	public static T GetInstance<T>(this T prefab) where T : Component
	{
		return prefab.GetInstance(Vector3.zero, Quaternion.identity);
	}

	public static T GetInstance<T>(this T prefab, Vector3 position, Quaternion rotation) where T : Component
	{
		ComponentPool<T>.GetOrCreatePool(prefab).Get(out var v);
		v.transform.SetPositionAndRotation(position, rotation);
		v.gameObject.SetActive(value: true);
		return v;
	}

	public static T GetUninstantiated<T>(this T prefab, Transform parent) where T : Component
	{
		return prefab.GetUninstantiated(parent.position, parent.rotation, parent);
	}

	public static T GetUninstantiated<T>(this T prefab, Vector3 position, Quaternion rotation, Transform parent) where T : Component
	{
		ComponentPool<T>.GetOrCreatePool(prefab).Get(out var v);
		v.transform.SetParent(parent);
		v.transform.SetPositionAndRotation(position, rotation);
		return v;
	}

	public static T GetUninstantiated<T>(this T prefab, Vector3 position, Quaternion rotation) where T : Component
	{
		ComponentPool<T>.GetOrCreatePool(prefab).Get(out var v);
		if (v.gameObject.activeSelf)
		{
			v.gameObject.SetActive(value: false);
		}
		v.transform.SetPositionAndRotation(position, rotation);
		return v;
	}

	public static void ReleaseInstance<T>(this T prefab, T instance) where T : Component
	{
		ComponentPool<T>.Release(prefab, instance);
	}
}
