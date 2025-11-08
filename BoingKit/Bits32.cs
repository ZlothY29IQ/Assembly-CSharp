using System;
using UnityEngine;

namespace BoingKit;

[Serializable]
public struct Bits32
{
	[SerializeField]
	private int m_bits;

	public int IntValue => m_bits;

	public Bits32(int bits = 0)
	{
		m_bits = bits;
	}

	public void Clear()
	{
		m_bits = 0;
	}

	public void SetBit(int index, bool value)
	{
		if (value)
		{
			m_bits |= 1 << index;
		}
		else
		{
			m_bits &= ~(1 << index);
		}
	}

	public bool IsBitSet(int index)
	{
		return (m_bits & (1 << index)) != 0;
	}
}
