using Photon.Realtime;

public class LegacyWorldTargetItem
{
	public Player owner;

	public int itemIdx;

	public bool IsValid()
	{
		if (itemIdx != -1)
		{
			return owner != null;
		}
		return false;
	}

	public void Invalidate()
	{
		itemIdx = -1;
		owner = null;
	}
}
