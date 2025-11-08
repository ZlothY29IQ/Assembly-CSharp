using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GorillaExtensions;

public static class GTTryFindByExactPath
{
	public static bool WithSiblingIndexAndTypeName<T>(string path, out T out_component) where T : Component
	{
		out_component = null;
		if (string.IsNullOrEmpty(path))
		{
			return false;
		}
		int num = path.IndexOf("/->/", StringComparison.Ordinal);
		if (num < 0)
		{
			return WithSiblingIndex<T>(path, out out_component);
		}
		string xformPath = path.Substring(0, num);
		string text = path.Substring(num + 4);
		int result = -1;
		int num2 = text.IndexOf('#');
		string text2;
		if (num2 >= 0)
		{
			text2 = text.Substring(0, num2);
			if (!int.TryParse(text.Substring(num2 + 1), out result))
			{
				result = -1;
			}
		}
		else
		{
			text2 = text;
		}
		if (!XformWithSiblingIndex(xformPath, out var finalXform))
		{
			return false;
		}
		Type type = typeof(T);
		if (!string.Equals(type.Name, text2, StringComparison.Ordinal))
		{
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			Type type2 = null;
			Assembly[] array = assemblies;
			for (int i = 0; i < array.Length; i++)
			{
				type2 = array[i].GetType(text2);
				if (type2 != null && typeof(Component).IsAssignableFrom(type2))
				{
					type = type2;
					break;
				}
			}
			if (type2 == null)
			{
				out_component = finalXform.GetComponent<T>();
				return out_component != null;
			}
		}
		Component[] components = finalXform.GetComponents(type);
		T val = null;
		if (components.Length != 0)
		{
			if (result < 0)
			{
				val = components[0] as T;
			}
			else
			{
				if (result >= components.Length)
				{
					return false;
				}
				val = components[result] as T;
			}
		}
		out_component = val;
		return out_component != null;
	}

	private static bool WithSiblingIndex<T>(string xformPath, out T component) where T : Component
	{
		component = null;
		if (XformWithSiblingIndex(xformPath, out var finalXform))
		{
			component = finalXform.GetComponent<T>();
			return component != null;
		}
		return false;
	}

	public static bool XformWithSiblingIndex(string xformPath, out Transform finalXform)
	{
		finalXform = null;
		if (string.IsNullOrEmpty(xformPath))
		{
			return false;
		}
		string[] array = xformPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
		if (array.Length == 0)
		{
			return false;
		}
		Transform transform = null;
		for (int i = 0; i < array.Length; i++)
		{
			string text = array[i];
			int num = text.IndexOf('|');
			if (num < 0)
			{
				return false;
			}
			string s = text.Substring(0, num);
			string b = text.Substring(num + 1);
			if (!int.TryParse(s, out var result))
			{
				return false;
			}
			if (i == 0)
			{
				Transform transform2 = null;
				for (int j = 0; j < SceneManager.sceneCount; j++)
				{
					if (!(transform2 == null))
					{
						break;
					}
					Scene sceneAt = SceneManager.GetSceneAt(j);
					if (!sceneAt.IsValid() || !sceneAt.isLoaded)
					{
						continue;
					}
					GameObject[] rootGameObjects = sceneAt.GetRootGameObjects();
					if (result >= 0 && result < rootGameObjects.Length)
					{
						Transform transform3 = rootGameObjects[result].transform;
						if (string.Equals(transform3.name, b, StringComparison.Ordinal))
						{
							transform2 = transform3;
						}
					}
				}
				if (transform2 == null)
				{
					return false;
				}
				transform = transform2;
			}
			else
			{
				if (result < 0 || result >= transform.childCount)
				{
					return false;
				}
				Transform child = transform.GetChild(result);
				if (!string.Equals(child.name, b, StringComparison.Ordinal))
				{
					return false;
				}
				transform = child;
			}
		}
		finalXform = transform;
		return finalXform != null;
	}
}
