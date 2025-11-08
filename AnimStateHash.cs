using System;
using UnityEngine;

[Serializable]
public struct AnimStateHash
{
	[SerializeField]
	private int _hash;

	public static implicit operator AnimStateHash(string s)
	{
		AnimStateHash result = default(AnimStateHash);
		result._hash = Animator.StringToHash(s);
		return result;
	}

	public static implicit operator int(AnimStateHash ash)
	{
		return ash._hash;
	}
}
