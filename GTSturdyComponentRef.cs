using System;
using GorillaExtensions;
using UnityEngine;

[Serializable]
public struct GTSturdyComponentRef<T> where T : Component
{
	[SerializeField]
	private T _value;

	[SerializeField]
	private string _relativePath;

	[SerializeField]
	private Transform _baseXform;

	public Transform BaseXform
	{
		get
		{
			return _baseXform;
		}
		set
		{
			_baseXform = value;
		}
	}

	public T Value
	{
		get
		{
			if (!_value)
			{
				return _value;
			}
			if (string.IsNullOrEmpty(_relativePath))
			{
				return null;
			}
			if (!_baseXform.TryFindByPath(_relativePath, out var result))
			{
				return null;
			}
			_value = result.GetComponent<T>();
			return _value;
		}
		set
		{
			_value = value;
			_relativePath = ((!value) ? _baseXform.GetRelativePath(value.transform) : string.Empty);
		}
	}

	public static implicit operator T(GTSturdyComponentRef<T> sturdyRef)
	{
		return sturdyRef.Value;
	}

	public static implicit operator GTSturdyComponentRef<T>(T component)
	{
		return new GTSturdyComponentRef<T>
		{
			Value = component
		};
	}
}
