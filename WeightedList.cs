using System;
using System.Collections.Generic;
using UnityEngine;

public class WeightedList<T>
{
	private List<T> items = new List<T>();

	private List<float> weights = new List<float>();

	private List<float> cumulativeWeights = new List<float>();

	private float totalWeight;

	public int Count => items.Count;

	public List<T> Items => items;

	public (T Item, float Weight) this[int index]
	{
		get
		{
			if (index < 0 || index >= items.Count)
			{
				throw new IndexOutOfRangeException();
			}
			return (Item: items[index], Weight: weights[index]);
		}
	}

	public void Add(T item, float weight)
	{
		if (weight <= 0f)
		{
			throw new ArgumentException("Weight must be greater than zero.");
		}
		totalWeight += weight;
		items.Add(item);
		weights.Add(weight);
		cumulativeWeights.Add(totalWeight);
	}

	public T GetRandomItem()
	{
		return items[GetRandomIndex()];
	}

	public int GetRandomIndex()
	{
		if (items.Count == 0)
		{
			throw new InvalidOperationException("The list is empty.");
		}
		float item = UnityEngine.Random.value * totalWeight;
		int num = cumulativeWeights.BinarySearch(item);
		if (num < 0)
		{
			num = ~num;
		}
		return num;
	}

	public bool Remove(T item)
	{
		int num = items.IndexOf(item);
		if (num == -1)
		{
			return false;
		}
		RemoveAt(num);
		return true;
	}

	public void RemoveAt(int index)
	{
		if (index < 0 || index >= items.Count)
		{
			throw new ArgumentOutOfRangeException("index");
		}
		totalWeight -= weights[index];
		items.RemoveAt(index);
		weights.RemoveAt(index);
		RecalculateCumulativeWeights();
	}

	private void RecalculateCumulativeWeights()
	{
		cumulativeWeights.Clear();
		float num = 0f;
		foreach (float weight in weights)
		{
			num += weight;
			cumulativeWeights.Add(num);
		}
		totalWeight = num;
	}

	public void Clear()
	{
		items.Clear();
		weights.Clear();
		cumulativeWeights.Clear();
		totalWeight = 0f;
	}
}
