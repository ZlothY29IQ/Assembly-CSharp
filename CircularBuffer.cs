using System;

internal class CircularBuffer<T>
{
	private T[] backingArray;

	private int nextWriteIdx;

	private int lastWriteIdx;

	public int Count { get; private set; }

	public int Capacity { get; private set; }

	public T this[int logicalIdx]
	{
		get
		{
			if (logicalIdx < 0 || logicalIdx >= Count)
			{
				throw new ArgumentOutOfRangeException("logicalIdx", logicalIdx, $"Out of bounds index {logicalIdx} into CircularBuffer with length {Count}");
			}
			int num = (lastWriteIdx + Capacity - logicalIdx) % Capacity;
			return backingArray[num];
		}
	}

	public CircularBuffer(int capacity)
	{
		backingArray = new T[capacity];
		Capacity = capacity;
		Count = 0;
	}

	public void Add(T value)
	{
		backingArray[nextWriteIdx] = value;
		lastWriteIdx = nextWriteIdx;
		nextWriteIdx = (nextWriteIdx + 1) % Capacity;
		if (Count < Capacity)
		{
			Count++;
		}
	}

	public void Clear()
	{
		Count = 0;
	}

	public T Last()
	{
		return backingArray[lastWriteIdx];
	}
}
