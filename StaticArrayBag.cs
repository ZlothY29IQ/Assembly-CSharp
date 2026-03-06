using System.Collections.Generic;

public class StaticArrayBag<T>
{
	private Dictionary<int, T[]> m_bag = new Dictionary<int, T[]>(1);

	public T[] GetStaticArray(int size)
	{
		T[] array;
		if (!m_bag.ContainsKey(size))
		{
			array = new T[size];
			m_bag[size] = array;
		}
		else
		{
			array = m_bag[size];
		}
		return array;
	}
}
