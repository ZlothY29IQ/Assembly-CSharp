using System;
using UnityEngine;

namespace GorillaTag;

[Serializable]
public struct StaticHashWrapper : IEquatable<int>, IEquatable<StaticHashWrapper>
{
	public const int NULL_HASH = -1;

	[SerializeField]
	private int m_hashcode;

	public override int GetHashCode()
	{
		return m_hashcode;
	}

	public bool Equals(int other)
	{
		return m_hashcode == other;
	}

	public bool Equals(StaticHashWrapper other)
	{
		return this == other;
	}

	public override bool Equals(object obj)
	{
		return m_hashcode.Equals(obj);
	}

	public static implicit operator int(in StaticHashWrapper hash)
	{
		return hash.m_hashcode;
	}

	public static bool operator ==(in StaticHashWrapper hash1, in StaticHashWrapper hash2)
	{
		return hash1.m_hashcode == hash2.m_hashcode;
	}

	public static bool operator ==(in StaticHashWrapper hash1, int hash2)
	{
		return hash1.m_hashcode == hash2;
	}

	public static bool operator ==(int hash1, in StaticHashWrapper hash2)
	{
		return hash1 == hash2.m_hashcode;
	}

	public static bool operator !=(in StaticHashWrapper hash1, in StaticHashWrapper hash2)
	{
		return hash1.m_hashcode != hash2.m_hashcode;
	}

	public static bool operator !=(in StaticHashWrapper hash1, int hash2)
	{
		return hash1.m_hashcode != hash2;
	}

	public static bool operator !=(int hash1, in StaticHashWrapper hash2)
	{
		return hash1 != hash2.m_hashcode;
	}
}
