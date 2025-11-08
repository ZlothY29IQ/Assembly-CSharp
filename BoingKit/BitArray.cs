namespace BoingKit;

public struct BitArray
{
	private int[] m_aBlock;

	public int[] Blocks => m_aBlock;

	private static int GetBlockIndex(int index)
	{
		return index / 4;
	}

	private static int GetSubIndex(int index)
	{
		return index % 4;
	}

	private static void SetBit(int index, bool value, int[] blocks)
	{
		int blockIndex = GetBlockIndex(index);
		int subIndex = GetSubIndex(index);
		if (value)
		{
			blocks[blockIndex] |= 1 << subIndex;
		}
		else
		{
			blocks[blockIndex] &= ~(1 << subIndex);
		}
	}

	private static bool IsBitSet(int index, int[] blocks)
	{
		return (blocks[GetBlockIndex(index)] & (1 << GetSubIndex(index))) != 0;
	}

	public BitArray(int capacity)
	{
		int num = (capacity + 4 - 1) / 4;
		m_aBlock = new int[num];
		Clear();
	}

	public void Resize(int capacity)
	{
		int num = (capacity + 4 - 1) / 4;
		if (num > m_aBlock.Length)
		{
			int[] array = new int[num];
			int i = 0;
			for (int num2 = m_aBlock.Length; i < num2; i++)
			{
				array[i] = m_aBlock[i];
			}
			m_aBlock = array;
		}
	}

	public void Clear()
	{
		SetAllBits(value: false);
	}

	public void SetAllBits(bool value)
	{
		int num = ((!value) ? 1 : (-1));
		int i = 0;
		for (int num2 = m_aBlock.Length; i < num2; i++)
		{
			m_aBlock[i] = num;
		}
	}

	public void SetBit(int index, bool value)
	{
		SetBit(index, value, m_aBlock);
	}

	public bool IsBitSet(int index)
	{
		return IsBitSet(index, m_aBlock);
	}
}
