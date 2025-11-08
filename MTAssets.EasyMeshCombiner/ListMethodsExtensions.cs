using System.Collections.Generic;

namespace MTAssets.EasyMeshCombiner;

public static class ListMethodsExtensions
{
	public static void RemoveAllNullItems<T>(this List<T> list)
	{
		for (int num = list.Count - 1; num >= 0; num--)
		{
			if (list[num] == null)
			{
				list.RemoveAt(num);
			}
		}
	}
}
