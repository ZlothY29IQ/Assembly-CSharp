using System;
using UnityEngine;

namespace GorillaTagScripts.GhostReactor;

public static class GREnemyTypeExtensions
{
	public static Type GetComponentType(this GREnemyType enemyType)
	{
		return enemyType switch
		{
			GREnemyType.Chaser => typeof(GREnemyChaser), 
			GREnemyType.Pest => typeof(GREnemyPest), 
			GREnemyType.Phantom => typeof(GREnemyPhantom), 
			GREnemyType.Ranged => typeof(GREnemyRanged), 
			GREnemyType.Summoner => typeof(GREnemySummoner), 
			_ => null, 
		};
	}

	public static GREnemyType? GetEnemyType(this GameEntity entity)
	{
		GameObject gameObject = entity.gameObject;
		foreach (GREnemyType value in Enum.GetValues(typeof(GREnemyType)))
		{
			Type componentType = value.GetComponentType();
			if ((object)componentType != null && (object)gameObject.GetComponent(componentType) != null)
			{
				return value;
			}
		}
		return null;
	}
}
