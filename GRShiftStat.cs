using System.Collections.Generic;
using System.IO;
using GorillaTagScripts.GhostReactor;

public class GRShiftStat
{
	public Dictionary<GRShiftStatType, int> shiftStats = new Dictionary<GRShiftStatType, int>();

	private Dictionary<GREnemyType, int> enemyKills = new Dictionary<GREnemyType, int>();

	public IReadOnlyDictionary<GREnemyType, int> EnemyKills => enemyKills;

	public void Serialize(BinaryWriter writer)
	{
		writer.Write(GetShiftStat(GRShiftStatType.EnemyDeaths));
		writer.Write(GetShiftStat(GRShiftStatType.PlayerDeaths));
		writer.Write(GetShiftStat(GRShiftStatType.CoresCollected));
		writer.Write(GetShiftStat(GRShiftStatType.SentientCoresCollected));
		writer.Write(enemyKills.Count);
		foreach (KeyValuePair<GREnemyType, int> enemyKill in enemyKills)
		{
			writer.Write((int)enemyKill.Key);
			writer.Write(enemyKill.Value);
		}
	}

	public void Deserialize(BinaryReader reader)
	{
		shiftStats[GRShiftStatType.EnemyDeaths] = reader.ReadInt32();
		shiftStats[GRShiftStatType.PlayerDeaths] = reader.ReadInt32();
		shiftStats[GRShiftStatType.CoresCollected] = reader.ReadInt32();
		shiftStats[GRShiftStatType.SentientCoresCollected] = reader.ReadInt32();
		int num = reader.ReadInt32();
		for (int i = 0; i < num; i++)
		{
			GREnemyType key = (GREnemyType)reader.ReadInt32();
			enemyKills[key] = reader.ReadInt32();
		}
	}

	public void SetShiftStat(GRShiftStatType stat, int newValue)
	{
		shiftStats[stat] = newValue;
		GhostReactor.instance.shiftManager.RefreshDepthDisplay();
	}

	public void IncrementShiftStat(GRShiftStatType stat)
	{
		if (shiftStats.ContainsKey(stat))
		{
			shiftStats[stat]++;
			return;
		}
		shiftStats[stat] = 1;
		GhostReactor.instance.shiftManager.RefreshDepthDisplay();
	}

	public void IncrementEnemyKills(GREnemyType type)
	{
		if (type != GREnemyType.None)
		{
			if (!enemyKills.TryAdd(type, 1))
			{
				enemyKills[type]++;
			}
			GhostReactor.instance.shiftManager.RefreshDepthDisplay();
		}
	}

	public void ResetShiftStats()
	{
		shiftStats[GRShiftStatType.EnemyDeaths] = 0;
		shiftStats[GRShiftStatType.PlayerDeaths] = 0;
		shiftStats[GRShiftStatType.CoresCollected] = 0;
		shiftStats[GRShiftStatType.SentientCoresCollected] = 0;
		enemyKills.Clear();
		GhostReactor.instance.shiftManager.RefreshDepthDisplay();
	}

	public int GetShiftStat(GRShiftStatType stat)
	{
		if (shiftStats.ContainsKey(stat))
		{
			return shiftStats[stat];
		}
		return 0;
	}
}
