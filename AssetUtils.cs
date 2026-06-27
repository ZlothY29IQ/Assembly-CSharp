using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public static class AssetUtils
{
	[Conditional("UNITY_EDITOR")]
	public static void ExecAndUnloadUnused(Action action)
	{
	}

	[Conditional("UNITY_EDITOR")]
	public static void LoadAssetOfType<T>(ref T result, ref string resultPath) where T : UnityEngine.Object
	{
		result = null;
		resultPath = null;
	}

	[Conditional("UNITY_EDITOR")]
	public static void FindAllAssetsOfType<T>(ref T[] results, ref string[] assetPaths) where T : UnityEngine.Object
	{
		results = Array.Empty<T>();
	}

	public static T[] FindAllAssetsOfType<T>() where T : UnityEngine.Object
	{
		return Array.Empty<T>();
	}

	[HideInCallstack]
	[Conditional("UNITY_EDITOR")]
	public static void ForceSave<T>(this IList<T> assets, Action<T> onPreSave = null, bool unloadUnusedAfter = false) where T : UnityEngine.Object
	{
	}

	[HideInCallstack]
	[Conditional("UNITY_EDITOR")]
	public static void ForceSave(this UnityEngine.Object asset)
	{
	}

	public static long ComputeAssetId(this UnityEngine.Object asset, bool unsigned = false)
	{
		return 0L;
	}

	public static string GetGameObjectPath(GameObject obj)
	{
		string text = "/" + obj.name;
		while (obj.transform.parent != null)
		{
			obj = obj.transform.parent.gameObject;
			text = "/" + obj.name + text;
		}
		return text;
	}
}
