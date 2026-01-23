namespace GorillaTagScripts.GhostReactor;

public static class GREnemyTypeExtensions
{
	public static GREnemyType GetEnemyType(this GameEntity entity)
	{
		if (entity == null)
		{
			return GREnemyType.None;
		}
		GREnemy component = entity.GetComponent<GREnemy>();
		if (component == null)
		{
			return GREnemyType.None;
		}
		if (component.enemyType == GREnemyType.MoonBoss_Phase1 || component.enemyType == GREnemyType.MoonBoss_Phase2)
		{
			return GREnemyType.MoonBoss;
		}
		return component.enemyType;
	}

	public static string Pluralize(this GREnemyType t)
	{
		if (t == GREnemyType.MoonBoss)
		{
			return "Meteor Monsters";
		}
		return $"{t}s";
	}
}
