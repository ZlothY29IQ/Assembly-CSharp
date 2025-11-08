public static class SIUpgradeTypeSystem
{
	public static int GetPageId(this SIUpgradeType self)
	{
		return (int)self / 100;
	}

	public static int GetNodeId(this SIUpgradeType self)
	{
		return (int)self % 100;
	}

	public static SIUpgradeType GetUpgradeType(int pageId, int nodeId)
	{
		return (SIUpgradeType)(pageId * 100 + nodeId);
	}
}
