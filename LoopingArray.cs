using GorillaTag;

public class LoopingArray<T> : ObjectPoolEvents
{
	public class Pool : ObjectPool<LoopingArray<T>>
	{
		private readonly int m_size;

		private Pool(int amount)
			: base(amount)
		{
		}

		public Pool(int size, int amount)
			: this(size, amount, amount)
		{
		}

		public Pool(int size, int initialAmount, int maxAmount)
		{
			m_size = size;
			InitializePool(initialAmount, maxAmount);
		}

		public override LoopingArray<T> CreateInstance()
		{
			return new LoopingArray<T>(m_size);
		}
	}

	private int m_length;

	private int m_currentIndex;

	private T[] m_array;

	public int Length => m_length;

	public int CurrentIndex => m_currentIndex;

	public T this[int index]
	{
		get
		{
			return m_array[index];
		}
		set
		{
			m_array[index] = value;
		}
	}

	public LoopingArray()
		: this(0)
	{
	}

	public LoopingArray(int capicity)
	{
		m_length = capicity;
		m_array = new T[capicity];
		Clear();
	}

	public int AddAndIncrement(in T value)
	{
		int currentIndex = m_currentIndex;
		m_array[m_currentIndex] = value;
		m_currentIndex = (m_currentIndex + 1) % m_length;
		return currentIndex;
	}

	public int IncrementAndAdd(in T value)
	{
		m_currentIndex = (m_currentIndex + 1) % m_length;
		m_array[m_currentIndex] = value;
		return m_currentIndex;
	}

	public void Clear()
	{
		m_currentIndex = 0;
		for (int i = 0; i < m_array.Length; i++)
		{
			m_array[i] = default(T);
		}
	}

	void ObjectPoolEvents.OnTaken()
	{
		Clear();
	}

	void ObjectPoolEvents.OnReturned()
	{
	}
}
