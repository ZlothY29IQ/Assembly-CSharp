using System.Collections.Generic;
using Unity.Collections;

public static class NativeCollectionExtensions
{
	public static T[] ToArray<T>(this NativeList<T> list) where T : unmanaged
	{
		return list.AsArray().ToArray();
	}

	public static List<T> ToList<T>(this NativeList<T> list) where T : unmanaged
	{
		List<T> list2 = new List<T>(list.Length);
		for (int i = 0; i < list.Length; i++)
		{
			list2.Add(list[i]);
		}
		return list2;
	}
}
