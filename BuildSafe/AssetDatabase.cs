using System;
using System.Diagnostics;
using UnityEngine;

namespace BuildSafe;

public static class AssetDatabase
{
	public static T LoadAssetAtPath<T>(string assetPath) where T : UnityEngine.Object
	{
		return null;
	}

	public static T[] LoadAssetsOfType<T>() where T : UnityEngine.Object
	{
		return Array.Empty<T>();
	}

	public static string[] FindAssetsOfType<T>() where T : UnityEngine.Object
	{
		return Array.Empty<string>();
	}

	[Conditional("UNITY_EDITOR")]
	public static void SaveToDisk(params UnityEngine.Object[] assetsToSave)
	{
		SaveAssetsToDisk(assetsToSave);
	}

	public static void SaveAssetsToDisk(UnityEngine.Object[] assetsToSave, bool saveProject = true)
	{
	}
}
