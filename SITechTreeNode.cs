using System;
using UnityEngine;

[Serializable]
public class SITechTreeNode
{
	[SerializeField]
	private EAssetReleaseTier m_edReleaseTier = (EAssetReleaseTier)(-1);

	public SIUpgradeType upgradeType;

	public string nickName;

	public string description;

	public SIUpgradeType[] parentUpgrades;

	public GameEntity unlockedGadgetPrefab;

	public SIResource.ResourceCost[] nodeCost;

	public EAssetReleaseTier EdReleaseTier
	{
		get
		{
			return m_edReleaseTier;
		}
		set
		{
			m_edReleaseTier = value;
		}
	}

	public bool IsValid
	{
		get
		{
			EAssetReleaseTier edReleaseTier = m_edReleaseTier;
			if (edReleaseTier != 0)
			{
				return edReleaseTier <= EAssetReleaseTier.PublicRC;
			}
			return false;
		}
	}

	public bool IsDispensableGadget
	{
		get
		{
			if (IsValid)
			{
				return unlockedGadgetPrefab;
			}
			return false;
		}
	}
}
