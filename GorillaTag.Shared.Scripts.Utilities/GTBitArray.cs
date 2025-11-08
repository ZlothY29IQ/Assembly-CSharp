using System;

namespace GorillaTag.Shared.Scripts.Utilities;

public sealed class GTBitArray
{
	public readonly int Length;

	private readonly uint[] _data;

	public bool this[int idx]
	{
		get
		{
			if (idx < 0 || idx >= Length)
			{
				throw new ArgumentOutOfRangeException();
			}
			int num = idx / 32;
			int num2 = idx % 32;
			return (_data[num] & (1 << num2)) != 0;
		}
		set
		{
			if (idx < 0 || idx >= Length)
			{
				throw new ArgumentOutOfRangeException();
			}
			int num = idx / 32;
			int num2 = idx % 32;
			if (value)
			{
				_data[num] |= (uint)(1 << num2);
			}
			else
			{
				_data[num] &= (uint)(~(1 << num2));
			}
		}
	}

	public GTBitArray(int length)
	{
		Length = length;
		_data = ((length % 32 == 0) ? new uint[length / 32] : new uint[length / 32 + 1]);
		for (int i = 0; i < _data.Length; i++)
		{
			_data[i] = 0u;
		}
	}

	public void Clear()
	{
		for (int i = 0; i < _data.Length; i++)
		{
			_data[i] = 0u;
		}
	}

	public void CopyFrom(GTBitArray other)
	{
		if (Length != other.Length)
		{
			throw new ArgumentException("Can only copy bit arrays of the same length.");
		}
		for (int i = 0; i < _data.Length; i++)
		{
			_data[i] = other._data[i];
		}
	}
}
