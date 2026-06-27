using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneIndexExtensions
{
	private const int SceneIndex_COUNT = 23;

	[OnEnterPlay_SetNull]
	private static List<Action>[] onSceneLoadCallbacks;

	[OnEnterPlay_SetNull]
	private static List<Action>[] onSceneUnloadCallbacks;

	public static SceneIndex GetSceneIndex(this Scene scene)
	{
		return (SceneIndex)scene.buildIndex;
	}

	public static SceneIndex GetSceneIndex(this GameObject obj)
	{
		return (SceneIndex)obj.scene.buildIndex;
	}

	public static SceneIndex GetSceneIndex(this Component cmp)
	{
		return (SceneIndex)cmp.gameObject.scene.buildIndex;
	}

	public static void AddCallbackOnSceneLoad(this SceneIndex scene, Action callback)
	{
		if (onSceneLoadCallbacks == null)
		{
			onSceneLoadCallbacks = new List<Action>[23];
			for (int i = 0; i < onSceneLoadCallbacks.Length; i++)
			{
				onSceneLoadCallbacks[i] = new List<Action>();
			}
			SceneManager.sceneLoaded += OnSceneLoad;
		}
		onSceneLoadCallbacks[(int)scene].Add(callback);
	}

	public static void RemoveCallbackOnSceneLoad(this SceneIndex scene, Action callback)
	{
		if (onSceneLoadCallbacks != null)
		{
			onSceneLoadCallbacks[(int)scene].Remove(callback);
		}
	}

	public static void OnSceneLoad(Scene scene, LoadSceneMode mode)
	{
		if (scene.buildIndex == -1)
		{
			return;
		}
		foreach (Action item in onSceneLoadCallbacks[scene.buildIndex])
		{
			item();
		}
	}

	public static void AddCallbackOnSceneUnload(this SceneIndex scene, Action callback)
	{
		if (onSceneUnloadCallbacks == null)
		{
			onSceneUnloadCallbacks = new List<Action>[23];
			for (int i = 0; i < onSceneUnloadCallbacks.Length; i++)
			{
				onSceneUnloadCallbacks[i] = new List<Action>();
			}
			SceneManager.sceneUnloaded += OnSceneUnload;
		}
		onSceneUnloadCallbacks[(int)scene].Add(callback);
	}

	public static void RemoveCallbackOnSceneUnload(this SceneIndex scene, Action callback)
	{
		onSceneUnloadCallbacks[(int)scene].Remove(callback);
	}

	public static void OnSceneUnload(Scene scene)
	{
		if (scene.buildIndex == -1)
		{
			return;
		}
		foreach (Action item in onSceneUnloadCallbacks[scene.buildIndex])
		{
			item();
		}
	}

	[OnEnterPlay_Run]
	private static void Reset()
	{
		if (onSceneLoadCallbacks != null)
		{
			onSceneLoadCallbacks = null;
			SceneManager.sceneLoaded -= OnSceneLoad;
		}
		if (onSceneUnloadCallbacks != null)
		{
			onSceneUnloadCallbacks = null;
			SceneManager.sceneUnloaded -= OnSceneUnload;
		}
	}
}
