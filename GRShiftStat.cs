using System.Collections.Generic;
using System.IO;

public class GRShiftStat
{
	public Dictionary<GRShiftStatType, int> shiftStats = new Dictionary<GRShiftStatType, int>();

	public void Serialize(BinaryWriter writer)
	{
		writer.Write(GetShiftStat(GRShiftStatType.EnemyDeaths));
		writer.Write(GetShiftStat(GRShiftStatType.PlayerDeaths));
		writer.Write(GetShiftStat(GRShiftStatType.CoresCollected));
		writer.Write(GetShiftStat(GRShiftStatType.SentientCoresCollected));
	}

	public void Deserialize(BinaryReader reader)
	{
		shiftStats[GRShiftStatType.EnemyDeaths] = reader.ReadInt32();
		shiftStats[GRShiftStatType.PlayerDeaths] = reader.ReadInt32();
		shiftStats[GRShiftStatType.CoresCollected] = reader.ReadInt32();
		shiftStats[GRShiftStatType.SentientCoresCollected] = reader.ReadInt32();
	}

	public void SetShiftStat(GRShiftStatType stat, int newValue)
	{
		shiftStats[stat] = newValue;
	}

	public void IncrementShiftStat(GRShiftStatType stat)
	{
		if (shiftStats.ContainsKey(stat))
		{
			shiftStats[stat]++;
		}
		else
		{
			shiftStats[stat] = 1;
		}
	}

	public void ResetShiftStats()
	{
		shiftStats[GRShiftStatType.EnemyDeaths] = 0;
		shiftStats[GRShiftStatType.PlayerDeaths] = 0;
		shiftStats[GRShiftStatType.CoresCollected] = 0;
		shiftStats[GRShiftStatType.SentientCoresCollected] = 0;
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
