using System;
using GorillaTagScripts.GhostReactor;

[Serializable]
public struct GREnemyCount
{
	public GREnemyType EnemyType;

	public int Count;

	public GREnemyType GetEnemyType()
	{
		if (EnemyType == GREnemyType.MoonBoss_Phase1 || EnemyType == GREnemyType.MoonBoss_Phase2)
		{
			return GREnemyType.MoonBoss;
		}
		return EnemyType;
	}

	public string GetEnemyName()
	{
		if (GetEnemyType() == GREnemyType.MoonBoss)
		{
			return "Meteor Monster";
		}
		return GetEnemyType().ToString();
	}
}
