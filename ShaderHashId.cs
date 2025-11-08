using System;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public struct ShaderHashId : IEquatable<ShaderHashId>
{
	[FormerlySerializedAs("_hashText")]
	[SerializeField]
	private string _text;

	[NonSerialized]
	private int _hash;

	public string text => _text;

	public int hash => _hash;

	public ShaderHashId(string text)
	{
		_text = text;
		_hash = Shader.PropertyToID(text);
	}

	public override string ToString()
	{
		return _text;
	}

	public override int GetHashCode()
	{
		return _hash;
	}

	public static implicit operator int(ShaderHashId h)
	{
		return h._hash;
	}

	public static implicit operator ShaderHashId(string s)
	{
		return new ShaderHashId(s);
	}

	public bool Equals(ShaderHashId other)
	{
		return _hash == other._hash;
	}

	public override bool Equals(object obj)
	{
		if (obj is ShaderHashId other)
		{
			return Equals(other);
		}
		return false;
	}

	public static bool operator ==(ShaderHashId x, ShaderHashId y)
	{
		return x.Equals(y);
	}

	public static bool operator !=(ShaderHashId x, ShaderHashId y)
	{
		return !x.Equals(y);
	}
}
