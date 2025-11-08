using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WatchableStringSO", menuName = "ScriptableObjects/WatchableStringSO")]
public class WatchableStringSO : ScriptableObject
{
	[TextArea]
	public string InitialValue;

	private EnterPlayID enterPlayID;

	private List<Action<string>> callbacks;

	private string _value { get; set; }

	public string Value
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
			foreach (Action<string> callback in callbacks)
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
			callbacks = new List<Action<string>>();
			enterPlayID = EnterPlayID.GetCurrent();
		}
	}

	public void AddCallback(Action<string> callback, bool shouldCallbackNow = false)
	{
		EnsureInitialized();
		callbacks.Add(callback);
		if (!shouldCallbackNow)
		{
			return;
		}
		string value = _value;
		foreach (Action<string> callback2 in callbacks)
		{
			callback2(value);
		}
	}

	public void RemoveCallback(Action<string> callback)
	{
		EnsureInitialized();
		callbacks.Remove(callback);
	}

	public override string ToString()
	{
		return Value;
	}
}
