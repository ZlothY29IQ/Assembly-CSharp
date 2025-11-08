using System;
using System.Collections.Generic;
using UnityEngine;

public class TestSpawnGadget : MonoBehaviour
{
	[Serializable]
	public struct SpawnTypeWithUpgrades
	{
		public GameEntity prefab;

		public SIUpgradeType[] upgrades;
	}

	public int spawnBatchSize = 4;

	public List<SpawnTypeWithUpgrades> testSpawnList = new List<SpawnTypeWithUpgrades>();

	public bool spawnAllGadgets;

	public List<GameEntity> skipEntityList = new List<GameEntity>();

	public void Spawn(GameEntityManager gameEntityManager)
	{
		SIUpgradeSet upgrades = default(SIUpgradeSet);
		foreach (SpawnTypeWithUpgrades testSpawn in testSpawnList)
		{
			if (!(testSpawn.prefab == null))
			{
				upgrades.Clear();
				SIUpgradeType[] upgrades2 = testSpawn.upgrades;
				foreach (SIUpgradeType upgrade in upgrades2)
				{
					upgrades.Add(upgrade);
				}
				SpawnGadgetBatch(gameEntityManager, testSpawn.prefab, upgrades);
			}
		}
		if (!spawnAllGadgets)
		{
			return;
		}
		upgrades.Clear();
		foreach (GameEntity tempFactoryItem in gameEntityManager.tempFactoryItems)
		{
			if (!skipEntityList.Contains(tempFactoryItem))
			{
				SpawnGadgetBatch(gameEntityManager, tempFactoryItem, upgrades);
			}
		}
	}

	private void SpawnGadgetBatch(GameEntityManager gameEntityManager, GameEntity entityToSpawn, SIUpgradeSet upgrades)
	{
		for (int i = 0; i < spawnBatchSize; i++)
		{
			gameEntityManager.RequestCreateItem(entityToSpawn.gameObject.name.GetStaticHash(), base.transform.position + UnityEngine.Random.insideUnitSphere, base.transform.rotation, (long)upgrades.GetBits() << 32);
		}
	}
}
