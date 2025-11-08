using System;
using UnityEngine;

namespace GorillaTag;

[Serializable]
public struct HashWrapper : IEquatable<int>
{
	[SerializeField]
	private int hashCode;

	public const int NULL_HASH = -1;

	public HashWrapper(int hash = -1)
	{
		hashCode = hash;
	}

	public override int GetHashCode()
	{
		return hashCode;
	}

	public override bool Equals(object obj)
	{
		return hashCode.Equals(obj);
	}

	public bool Equals(int i)
	{
		return hashCode.Equals(i);
	}

	public static implicit operator int(in HashWrapper hash)
	{
		return hash.hashCode;
	}
}
