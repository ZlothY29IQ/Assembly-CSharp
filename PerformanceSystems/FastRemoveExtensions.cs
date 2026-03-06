using System.Collections.Generic;

namespace PerformanceSystems;

public static class FastRemoveExtensions
{
	public static bool FastRemove<T>(this List<T> list, T itemToRemove)
	{
		EqualityComparer<T> equalityComparer = EqualityComparer<T>.Default;
		int count = list.Count;
		if (count == 0)
		{
			return false;
		}
		int index = count - 1;
		for (int i = 0; i < count; i++)
		{
			if (equalityComparer.Equals(list[i], itemToRemove))
			{
				list[i] = list[index];
				list.RemoveAt(index);
				return true;
			}
		}
		return false;
	}

	public static bool FastRemove<T>(this List<T> list, HashSet<T> setToRemove)
	{
		if (setToRemove == null || setToRemove.Count == 0 || list.Count == 0)
		{
			return false;
		}
		bool result = false;
		for (int num = list.Count - 1; num >= 0; num--)
		{
			T item = list[num];
			if (setToRemove.Contains(item))
			{
				int index = list.Count - 1;
				list[num] = list[index];
				list.RemoveAt(index);
				result = true;
			}
		}
		return result;
	}
}
