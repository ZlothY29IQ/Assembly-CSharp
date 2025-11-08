using UnityEngine;

public class CritterSpawnCriteria : ScriptableObject
{
	public string[] spawnTimings;

	public bool CanSpawn()
	{
		if (spawnTimings.Length == 0)
		{
			return true;
		}
		string currentTimeOfDay = BetterDayNightManager.instance.currentTimeOfDay;
		string[] array = spawnTimings;
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] == currentTimeOfDay)
			{
				return true;
			}
		}
		return false;
	}
}
