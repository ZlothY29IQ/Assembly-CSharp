using System;

[Serializable]
internal struct SIUpgradeBasedGeneric<T>
{
	public SIUpgradeBasedGenericEntry<T>[] entries;

	public bool TryGetActiveValue(SIUpgradeSet withUpgrades, out T out_value)
	{
		out_value = default(T);
		bool result = false;
		for (int i = 0; i < entries.Length; i++)
		{
			if (entries[i].IsActive(withUpgrades))
			{
				result = true;
				out_value = entries[i].value;
			}
		}
		return result;
	}
}
