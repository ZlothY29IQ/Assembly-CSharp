using System;
using UnityEngine;

public class GRToolUpgrade : ScriptableObject
{
	[Serializable]
	public struct ToolUpgradeLevel
	{
		[SerializeField]
		public int Cost;

		[SerializeField]
		public float upgradeAmount;
	}

	public string upgradeName;

	public string description;

	public string upgradeId;

	[SerializeField]
	public ToolUpgradeLevel[] upgradeLevels;
}
