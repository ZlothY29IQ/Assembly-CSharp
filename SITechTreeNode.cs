using System;
using GorillaGameModes;
using UnityEngine;

[Serializable]
public class SITechTreeNode
{
	[SerializeField]
	private EAssetReleaseTier m_edReleaseTier = (EAssetReleaseTier)(-1);

	public SIUpgradeType upgradeType;

	public string nickName;

	public string description;

	public ESuperGameModes excludedGameModes;

	public SIUpgradeType[] parentUpgrades;

	public GameEntity unlockedGadgetPrefab;

	public SIResource.ResourceCost[] nodeCost;

	public bool costOverride;

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
			if (edReleaseTier != EAssetReleaseTier.Disabled && edReleaseTier <= EAssetReleaseTier.PublicRC)
			{
				return ((uint)excludedGameModes & (uint)GameMode.CurrentGameModeFlag) == 0;
			}
			return false;
		}
	}

	public bool IsAllowed => ((uint)excludedGameModes & (uint)GameMode.CurrentGameModeFlag) == 0;

	public bool IsDispensableGadget
	{
		get
		{
			if (IsValid && (bool)unlockedGadgetPrefab)
			{
				return IsAllowed;
			}
			return false;
		}
	}
}
