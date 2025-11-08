using System;
using UnityEngine;

[Serializable]
public struct AnimHashId
{
	[SerializeField]
	private string _text;

	[NonSerialized]
	private int _hash;

	public string text => _text;

	public int hash => _hash;

	public AnimHashId(string text)
	{
		_text = text;
		_hash = Animator.StringToHash(text);
	}

	public override string ToString()
	{
		return _text;
	}

	public override int GetHashCode()
	{
		return _hash;
	}

	public static implicit operator int(AnimHashId h)
	{
		return h._hash;
	}

	public static implicit operator AnimHashId(string s)
	{
		return new AnimHashId(s);
	}
}
