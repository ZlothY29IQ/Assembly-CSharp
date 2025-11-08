using System;
using UnityEngine;

[Serializable]
public class OptionalRef<T> where T : UnityEngine.Object
{
	[SerializeField]
	private bool _enabled;

	[SerializeField]
	private T _target;

	public bool enabled
	{
		get
		{
			return _enabled;
		}
		set
		{
			_enabled = value;
		}
	}

	public T Value
	{
		get
		{
			if ((bool)this)
			{
				return _target;
			}
			return null;
		}
		set
		{
			_target = (value ? value : null);
		}
	}

	public static implicit operator bool(OptionalRef<T> r)
	{
		if (r == null)
		{
			return false;
		}
		if (!r._enabled)
		{
			return false;
		}
		UnityEngine.Object target = r._target;
		if ((object)target == null)
		{
			return false;
		}
		return target;
	}

	public static implicit operator T(OptionalRef<T> r)
	{
		if (r == null)
		{
			return null;
		}
		if (!r._enabled)
		{
			return null;
		}
		UnityEngine.Object target = r._target;
		if ((object)target == null)
		{
			return null;
		}
		if (!target)
		{
			return null;
		}
		return target as T;
	}

	public static implicit operator UnityEngine.Object(OptionalRef<T> r)
	{
		if (r == null)
		{
			return null;
		}
		if (!r._enabled)
		{
			return null;
		}
		UnityEngine.Object target = r._target;
		if ((object)target == null)
		{
			return null;
		}
		if (!target)
		{
			return null;
		}
		return target;
	}
}
