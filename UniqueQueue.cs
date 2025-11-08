using System.Collections;
using System.Collections.Generic;

public class UniqueQueue<T> : IEnumerable<T>, IEnumerable
{
	private HashSet<T> queuedItems;

	private Queue<T> queue;

	public int Count => queue.Count;

	public UniqueQueue()
	{
		queuedItems = new HashSet<T>();
		queue = new Queue<T>();
	}

	public UniqueQueue(int capacity)
	{
		queuedItems = new HashSet<T>(capacity);
		queue = new Queue<T>(capacity);
	}

	public void Clear()
	{
		queuedItems.Clear();
		queue.Clear();
	}

	public bool Enqueue(T item)
	{
		if (!queuedItems.Add(item))
		{
			return false;
		}
		queue.Enqueue(item);
		return true;
	}

	public T Dequeue()
	{
		T val = queue.Dequeue();
		queuedItems.Remove(val);
		return val;
	}

	public bool TryDequeue(out T item)
	{
		if (queue.Count < 1)
		{
			item = default(T);
			return false;
		}
		item = Dequeue();
		return true;
	}

	public T Peek()
	{
		return queue.Peek();
	}

	public bool Contains(T item)
	{
		return queuedItems.Contains(item);
	}

	IEnumerator<T> IEnumerable<T>.GetEnumerator()
	{
		return queue.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return queue.GetEnumerator();
	}
}
