using System;
using UnityEngine;
using UnityEngine.Events;

public abstract class RandomComponent<T> : MonoBehaviour
{
	public T[] items = new T[0];

	public int seed;

	public bool staticSeed;

	public bool distinct = true;

	[NonSerialized]
	[Space]
	private int _seed;

	[NonSerialized]
	private T _lastItem;

	[NonSerialized]
	private int _lastItemIndex = -1;

	[NonSerialized]
	private SRand _rnd;

	public UnityEvent<T> onNextItem;

	public T lastItem => _lastItem;

	public int lastItemIndex => _lastItemIndex;

	public void ResetRandom(int? seedValue = null)
	{
		if (!staticSeed)
		{
			_seed = seedValue ?? StaticHash.Compute(DateTime.UtcNow.Ticks);
		}
		else
		{
			_seed = seed;
		}
		_rnd = new SRand(_seed);
	}

	public void Reset()
	{
		ResetRandom();
		_lastItem = default(T);
		_lastItemIndex = -1;
	}

	private void Awake()
	{
		Reset();
	}

	protected virtual void OnNextItem(T item)
	{
	}

	public virtual T GetItem(int index)
	{
		return items[index];
	}

	public virtual T NextItem()
	{
		_lastItemIndex = (distinct ? _rnd.NextIntWithExclusion(0, items.Length, _lastItemIndex) : _rnd.NextInt(0, items.Length));
		T val = (_lastItem = items[_lastItemIndex]);
		OnNextItem(val);
		onNextItem?.Invoke(val);
		return val;
	}
}
