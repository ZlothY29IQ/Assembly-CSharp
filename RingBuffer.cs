using System;
using System.Collections.Generic;

public class RingBuffer<T>
{
	private T[] _items;

	private int _head;

	private int _tail;

	private int _size;

	private readonly int _capacity;

	public int Size => _size;

	public int Capacity => _capacity;

	public bool IsFull => _size == _capacity;

	public bool IsEmpty => _size == 0;

	public RingBuffer(int capacity)
	{
		if (capacity < 1)
		{
			throw new ArgumentException("Can't be zero or negative", "capacity");
		}
		_size = 0;
		_capacity = capacity;
		_items = new T[capacity];
	}

	public RingBuffer(IList<T> list)
		: this(list.Count)
	{
		if (list == null)
		{
			throw new ArgumentNullException("list");
		}
		list.CopyTo(_items, 0);
	}

	public ref T PeekFirst()
	{
		return ref _items[_head];
	}

	public ref T PeekLast()
	{
		return ref _items[_tail];
	}

	public bool Push(T item)
	{
		if (_size == _capacity)
		{
			return false;
		}
		_items[_tail] = item;
		_tail = (_tail + 1) % _capacity;
		_size++;
		return true;
	}

	public T Pop()
	{
		if (_size == 0)
		{
			return default(T);
		}
		T result = _items[_head];
		_head = (_head + 1) % _capacity;
		_size--;
		return result;
	}

	public bool TryPop(out T item)
	{
		if (_size == 0)
		{
			item = default(T);
			return false;
		}
		item = _items[_head];
		_head = (_head + 1) % _capacity;
		_size--;
		return true;
	}

	public void Clear()
	{
		_head = 0;
		_tail = 0;
		_size = 0;
		Array.Clear(_items, 0, _capacity);
	}

	public bool TryGet(int i, out T item)
	{
		if (_size == 0)
		{
			item = default(T);
			return false;
		}
		item = _items[_head + i % _size];
		return true;
	}

	public ArraySegment<T> AsSegment()
	{
		return new ArraySegment<T>(_items);
	}
}
