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
		UnityEngine.Object obj = r?._target;
		if ((object)obj == null)
		{
			return false;
		}
		return obj != null;
	}

	public static implicit operator T(Ref<T> r)
	{
		UnityEngine.Object obj = r?._target;
		if ((object)obj == null)
		{
			return null;
		}
		if (obj == null)
		{
			return null;
		}
		return obj as T;
	}

	public static implicit operator UnityEngine.Object(Ref<T> r)
	{
		UnityEngine.Object obj = r?._target;
		if ((object)obj == null)
		{
			return null;
		}
		if (obj == null)
		{
			return null;
		}
		return obj;
	}
}
