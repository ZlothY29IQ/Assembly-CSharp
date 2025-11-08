using System;
using UnityEngine;

public static class JSonHelper
{
	[Serializable]
	private class Wrapper<T>
	{
		public T[] Items;
	}

	public static T[] FromJson<T>(string json)
	{
		return JsonUtility.FromJson<Wrapper<T>>(json).Items;
	}

	public static string ToJson<T>(T[] array)
	{
		return JsonUtility.ToJson(new Wrapper<T>
		{
			Items = array
		});
	}

	public static string ToJson<T>(T[] array, bool prettyPrint)
	{
		return JsonUtility.ToJson(new Wrapper<T>
		{
			Items = array
		}, prettyPrint);
	}
}
