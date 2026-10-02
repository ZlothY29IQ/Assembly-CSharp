using UnityEngine;

public static class ObjectExtensions
{
	public static void Destroy(this Object target)
	{
		Object.Destroy(target);
	}

	public static T Instantiate<T>(this T obj) where T : Object
	{
		return Object.Instantiate(obj);
	}

	public static T Instantiate<T>(this T obj, Transform parent) where T : Object
	{
		return Object.Instantiate(obj, parent);
	}

	public static T Instantiate<T>(this T obj, Vector3 position, Quaternion rotation) where T : Object
	{
		return Object.Instantiate(obj, position, rotation);
	}

	public static T Instantiate<T>(this T obj, Vector3 position, Quaternion rotation, Transform parent) where T : Object
	{
		return Object.Instantiate(obj, position, rotation, parent);
	}

	private static Transform GetTransform(Object obj)
	{
		if (!(obj is GameObject { transform: var transform }))
		{
			if (!(obj is Component { transform: var transform2 }))
			{
				return null;
			}
			return transform2;
		}
		return transform;
	}
}
