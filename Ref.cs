using System;
using UnityEngine;

[Serializable]
public class Ref<T> where T : class
{
	[SerializeField]
	private UnityEngine.Object _target;

	public T AsT
	{
		get
		{
			return this;
		}
		set
		{
			_target = value as UnityEngine.Object;
		}
	}

	public static implicit operator bool(Ref<T> r)
	{
		UnityEngine.Object @object = r?._target;
		if ((object)@object == null)
		{
			return false;
		}
		return @object != null;
	}

	public static implicit operator T(Ref<T> r)
	{
		UnityEngine.Object @object = r?._target;
		if ((object)@object == null)
		{
			return null;
		}
		if (@object == null)
		{
			return null;
		}
		return @object as T;
	}

	public static implicit operator UnityEngine.Object(Ref<T> r)
	{
		UnityEngine.Object @object = r?._target;
		if ((object)@object == null)
		{
			return null;
		}
		if (@object == null)
		{
			return null;
		}
		return @object;
	}
}
