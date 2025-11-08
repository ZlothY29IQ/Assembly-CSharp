using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace GorillaTag;

public class ObjectPool<T> where T : ObjectPoolEvents, new()
{
	private Stack<T> pool;

	public int maxInstances = 500;

	protected ObjectPool()
	{
	}

	public ObjectPool(int amount)
		: this(amount, amount)
	{
	}

	public ObjectPool(int initialAmount, int maxAmount)
	{
		InitializePool(initialAmount, maxAmount);
	}

	protected void InitializePool(int initialAmount, int maxAmount)
	{
		maxInstances = maxAmount;
		pool = new Stack<T>(initialAmount);
		for (int i = 0; i < initialAmount; i++)
		{
			pool.Push(CreateInstance());
		}
	}

	public T Take()
	{
		T result = ((pool.Count >= 1) ? pool.Pop() : CreateInstance());
		result.OnTaken();
		return result;
	}

	public void Return(T instance)
	{
		instance.OnReturned();
		if (pool.Count != maxInstances)
		{
			pool.Push(instance);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public virtual T CreateInstance()
	{
		return new T();
	}
}
