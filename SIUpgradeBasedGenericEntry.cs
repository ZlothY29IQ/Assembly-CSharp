using System;
using UnityEngine;

[Serializable]
internal struct SIUpgradeBasedGenericEntry<T>
{
	public T value;

	[Tooltip("For the objects to become activated, you must match AT LEAST ONE appearRequirement (if there are any), and not match any disappearRequirements.")]
	public SIUpgradeType[] activeRequirements;

	[Tooltip("For the objects to become deactivated, you must match AT LEAST ONE disappearRequirement (if there are any).")]
	public SIUpgradeType[] inactiveRequirements;

	public bool IsActive(SIUpgradeSet withUpgrades)
	{
		bool flag = true;
		if (activeRequirements.Length != 0)
		{
			flag = false;
			SIUpgradeType[] array = activeRequirements;
			foreach (SIUpgradeType upgrade in array)
			{
				if (withUpgrades.Contains(upgrade))
				{
					flag = true;
					break;
				}
			}
		}
		if (flag)
		{
			SIUpgradeType[] array = inactiveRequirements;
			foreach (SIUpgradeType upgrade2 in array)
			{
				if (withUpgrades.Contains(upgrade2))
				{
					flag = false;
					break;
				}
			}
		}
		return flag;
	}
}
