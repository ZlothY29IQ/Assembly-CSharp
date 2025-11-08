using System;
using UnityEngine;

[Serializable]
public struct GTOption<T>
{
	[Tooltip("When checked, the filter is applied; when unchecked (default), it is ignored.")]
	[SerializeField]
	public bool enabled;

	[SerializeField]
	public T value;

	[NonSerialized]
	public readonly T defaultValue;

	public T ResolvedValue
	{
		get
		{
			if (!enabled)
			{
				return defaultValue;
			}
			return value;
		}
	}

	public GTOption(T defaultValue)
	{
		enabled = false;
		value = defaultValue;
		this.defaultValue = defaultValue;
	}

	public void ResetValue()
	{
		value = defaultValue;
	}
}
