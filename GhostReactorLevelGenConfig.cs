using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GhostReactorLevelGenConfig", menuName = "ScriptableObjects/GhostReactorLevelGenConfig")]
public class GhostReactorLevelGenConfig : ScriptableObject
{
	public int shiftDuration;

	public int coresRequired;

	public int shiftBonus;

	public int sentientCoresRequired;

	public int maxPlayerDeaths = -1;

	public List<GREnemyCount> minEnemyKills = new List<GREnemyCount>();

	[ColorUsage(true, true)]
	public Color ambientLight = Color.black;

	public List<GhostReactorLevelGeneratorV2.TreeLevelConfig> treeLevels = new List<GhostReactorLevelGeneratorV2.TreeLevelConfig>();

	public List<GRBonusEntry> enemyGlobalBonuses = new List<GRBonusEntry>();

	public GRDropTableOverrides dropTableOverrides;

	private void OnValidate()
	{
		for (int i = 0; i < treeLevels.Count; i++)
		{
			GhostReactorLevelGeneratorV2.TreeLevelConfig value = treeLevels[i];
			value.minHubs = Mathf.Abs(value.minHubs);
			value.maxHubs = Mathf.Abs(value.maxHubs);
			value.minCaps = Mathf.Abs(value.minCaps);
			value.maxCaps = Mathf.Abs(value.maxCaps);
			if (value.minHubs > value.maxHubs)
			{
				value.maxHubs = value.minHubs;
			}
			if (value.minCaps > value.maxCaps)
			{
				value.maxCaps = value.minCaps;
			}
			treeLevels[i] = value;
		}
		GhostReactorLevelGeneratorV2.TreeLevelConfig value2 = treeLevels[treeLevels.Count - 1];
		if (value2.minHubs > 0 || value2.maxHubs > 0)
		{
			Debug.LogError("Ghost Reactor Level Gen Setup Error: The last tree level can only spawn end caps around the furthest level of hubs. Otherwise it would spawn hubs without a further level to spawn end caps around them");
			value2.minHubs = 0;
			value2.maxHubs = 0;
			treeLevels[treeLevels.Count - 1] = value2;
		}
		foreach (GREnemyCount minEnemyKill in minEnemyKills)
		{
			if (minEnemyKill.Count < 0)
			{
				Debug.LogError("Ghost Reactor Level Gen Setup Error: cannot have negative required enemy kills");
			}
		}
	}
}
