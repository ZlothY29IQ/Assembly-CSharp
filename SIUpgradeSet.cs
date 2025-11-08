public struct SIUpgradeSet
{
	private int backingBits;

	public void Clear()
	{
		backingBits = 0;
	}

	public SIUpgradeSet(int bits)
	{
		backingBits = bits;
	}

	public int GetBits()
	{
		return backingBits;
	}

	public void SetBits(int bits)
	{
		backingBits = bits;
	}

	public long GetCreateData(SIPlayer player)
	{
		return ((long)backingBits << 32) | player.ActorNr;
	}

	public void Add(SIUpgradeType upgrade)
	{
		backingBits |= 1 << upgrade.GetNodeId();
	}

	public void Add(int nodeId)
	{
		backingBits |= 1 << nodeId;
	}

	public void Remove(SIUpgradeType upgrade)
	{
		backingBits &= ~(1 << upgrade.GetNodeId());
	}

	public bool Contains(SIUpgradeType upgrade)
	{
		return (backingBits & (1 << upgrade.GetNodeId())) != 0;
	}

	public bool ContainsAny(params SIUpgradeType[] upgrades)
	{
		int num = 0;
		foreach (SIUpgradeType self in upgrades)
		{
			num |= 1 << self.GetNodeId();
		}
		return (backingBits & num) != 0;
	}

	public string GetString(SITechTreePageId pageId)
	{
		string text = "";
		int num = backingBits;
		int num2 = 0;
		bool flag = true;
		while (num > 0)
		{
			if ((num & 1) != 0)
			{
				if (!flag)
				{
					text += "|";
				}
				text += SIUpgradeTypeSystem.GetUpgradeType((int)pageId, num2);
				flag = false;
			}
			num >>= 1;
			num2++;
		}
		return text;
	}
}
