using System;

public struct ResettableUseCounter(int maxRegularUses, int maxSuperchargeUses, Action<bool> onReadyChanged = null)
{
	private int usesRemaining = maxRegularUses;

	private int maxRegularUses = maxRegularUses;

	private int maxSuperchargeUses = maxSuperchargeUses;

	private Action<bool> onReadyChanged = onReadyChanged;

	public bool IsReady => usesRemaining > 0;

	public bool TryUse()
	{
		if (!IsReady)
		{
			return false;
		}
		bool flag = SuperInfectionManager.activeSuperInfectionManager?.IsSupercharged ?? false;
		if (usesRemaining > maxRegularUses && !flag)
		{
			usesRemaining = maxRegularUses;
		}
		usesRemaining--;
		if (!IsReady)
		{
			onReadyChanged?.Invoke(obj: false);
		}
		return true;
	}

	public void Reset()
	{
		bool isReady = IsReady;
		usesRemaining = maxSuperchargeUses;
		if (!isReady)
		{
			onReadyChanged?.Invoke(obj: true);
		}
	}
}
