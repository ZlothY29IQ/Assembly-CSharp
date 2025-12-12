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
		return component.enemyType;
	}
}
