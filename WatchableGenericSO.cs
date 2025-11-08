using System;
using System.Collections.Generic;
using UnityEngine;

public class WatchableGenericSO<T> : ScriptableObject
{
	public T InitialValue;

	private EnterPlayID enterPlayID;

	private List<Action<T>> callbacks;

	private T _value { get; set; }

	public T Value
	{
		get
		{
			EnsureInitialized();
			return _value;
		}
		set
		{
			EnsureInitialized();
			_value = value;
			foreach (Action<T> callback in callbacks)
			{
				callback(value);
			}
		}
	}

	private void EnsureInitialized()
	{
		if (!enterPlayID.IsCurrent)
		{
			_value = InitialValue;
			callbacks = new List<Action<T>>();
			enterPlayID = EnterPlayID.GetCurrent();
		}
	}

	public void AddCallback(Action<T> callback, bool shouldCallbackNow = false)
	{
		EnsureInitialized();
		callbacks.Add(callback);
		if (!shouldCallbackNow)
		{
			return;
		}
		T value = _value;
		foreach (Action<T> callback2 in callbacks)
		{
			callback2(value);
		}
	}

	public void RemoveCallback(Action<T> callback)
	{
		EnsureInitialized();
		callbacks.Remove(callback);
	}
}
