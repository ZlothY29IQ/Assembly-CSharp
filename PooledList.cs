using System.Collections.Generic;
using GorillaTag;

public class PooledList<T> : ObjectPoolEvents
{
	public List<T> List = new List<T>();

	void ObjectPoolEvents.OnTaken()
	{
	}

	void ObjectPoolEvents.OnReturned()
	{
		List.Clear();
	}
}
