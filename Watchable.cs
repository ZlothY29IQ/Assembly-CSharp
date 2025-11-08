using System;
using System.Collections.Generic;

public class Watchable<T>
{
	private T _value;

	private List<Action<T>> callbacks = new List<Action<T>>();

	public T value
	{
		get
		{
			return _value;
		}
		set
		{
			_ = _value;
			_value = value;
			foreach (Action<T> callback in callbacks)
			{
				callback(value);
			}
		}
	}

	public Watchable()
	{
	}

	public Watchable(T initial)
	{
		_value = initial;
	}

	public void AddCallback(Action<T> callback, bool shouldCallbackNow = false)
	{
		callbacks.Add(callback);
		if (!shouldCallbackNow)
		{
			return;
		}
		foreach (Action<T> callback2 in callbacks)
		{
			callback2(_value);
		}
	}

	public void RemoveCallback(Action<T> callback)
	{
		callbacks.Remove(callback);
	}
}
