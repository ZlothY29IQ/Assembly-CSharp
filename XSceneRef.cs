using System;
using UnityEngine;

[Serializable]
public struct XSceneRef
{
	public SceneIndex TargetScene;

	public int TargetID;

	private XSceneRefTarget cached;

	private bool didCache;

	public bool TryResolve(out XSceneRefTarget result)
	{
		if (TargetID == 0)
		{
			result = null;
			return true;
		}
		if (didCache && cached != null)
		{
			result = cached;
			return true;
		}
		if (!XSceneRefGlobalHub.TryResolve(TargetScene, TargetID, out var result2))
		{
			result = null;
			return false;
		}
		cached = result2;
		didCache = true;
		result = result2;
		return true;
	}

	public bool TryResolve(out GameObject result)
	{
		if (TryResolve(out XSceneRefTarget result2))
		{
			result = ((result2 == null) ? null : result2.gameObject);
			return true;
		}
		result = null;
		return false;
	}

	public bool TryResolve<T>(out T result) where T : Component
	{
		if (TryResolve(out XSceneRefTarget result2))
		{
			result = ((result2 == null) ? null : result2.GetComponent<T>());
			return true;
		}
		result = null;
		return false;
	}

	public void AddCallbackOnLoad(Action callback)
	{
		TargetScene.AddCallbackOnSceneLoad(callback);
	}

	public void RemoveCallbackOnLoad(Action callback)
	{
		TargetScene.RemoveCallbackOnSceneLoad(callback);
	}

	public void AddCallbackOnUnload(Action callback)
	{
		TargetScene.AddCallbackOnSceneUnload(callback);
	}

	public void RemoveCallbackOnUnload(Action callback)
	{
		TargetScene.RemoveCallbackOnSceneUnload(callback);
	}
}
