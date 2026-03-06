using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace GorillaTag;

[Serializable]
public struct GTDirectAssetRef<T> : IEquatable<T> where T : UnityEngine.Object
{
	[SerializeField]
	[HideInInspector]
	internal T _obj;

	[FormerlySerializedAs("assetPath")]
	public string edAssetPath;

	public T obj
	{
		get
		{
			return _obj;
		}
		set
		{
			_obj = value;
			edAssetPath = null;
		}
	}

	public GTDirectAssetRef(T theObj)
	{
		_obj = theObj;
		edAssetPath = null;
	}

	public static implicit operator T(GTDirectAssetRef<T> refObject)
	{
		return refObject.obj;
	}

	public static implicit operator GTDirectAssetRef<T>(T other)
	{
		return new GTDirectAssetRef<T>
		{
			obj = other
		};
	}

	public bool Equals(T other)
	{
		return obj == other;
	}

	public override bool Equals(object other)
	{
		if (other is T other2)
		{
			return Equals(other2);
		}
		return false;
	}

	public override int GetHashCode()
	{
		if (!(obj != null))
		{
			return 0;
		}
		return obj.GetHashCode();
	}

	public static bool operator ==(GTDirectAssetRef<T> left, T right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(GTDirectAssetRef<T> left, T right)
	{
		return !(left == right);
	}
}
