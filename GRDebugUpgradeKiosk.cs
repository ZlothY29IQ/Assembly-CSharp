using System;
using System.Collections.Generic;
using UnityEngine;

public class GRDebugUpgradeKiosk : MonoBehaviour
{
	public Transform upgradeSpawnNode;

	public Transform toolSpawnNode;

	public Transform enemySpawnNode;

	private GhostReactorManager grManager;

	private GhostReactor reactor;

	private List<GameEntityId> spawnedEntities = new List<GameEntityId>();

	public void Init(GhostReactorManager grManager, GhostReactor reactor)
	{
		this.grManager = grManager;
		this.reactor = reactor;
	}

	private void Start()
	{
	}

	public void OnButtonSpawnClub()
	{
		OnButtonSpawnEntity("GhostReactorToolClub", toolSpawnNode);
	}

	public void OnButtonSpawnCollector()
	{
		OnButtonSpawnEntity("GhostReactorToolCollector", toolSpawnNode);
	}

	public void OnButtonSpawnLantern()
	{
		OnButtonSpawnEntity("GhostReactorToolLantern", toolSpawnNode);
	}

	public void OnButtonSpawnFlash()
	{
		OnButtonSpawnEntity("GhostReactorToolFlash", toolSpawnNode);
	}

	public void OnButtonSpawnShieldGun()
	{
		OnButtonSpawnEntity("GhostReactorToolShieldGun", toolSpawnNode);
	}

	public void OnButtonSpawnRevive()
	{
		OnButtonSpawnEntity("GhostReactorToolRevive", toolSpawnNode);
	}

	public void OnButtonSpawnDirectionalShield()
	{
		OnButtonSpawnEntity("GhostReactorToolDirectionalShield", toolSpawnNode);
	}

	public void OnButtonSpawnStatusWatch()
	{
		OnButtonSpawnEntity("GhostReactorToolStatusWatch", toolSpawnNode);
	}

	public void OnButtonSpawnDockWrist()
	{
		OnButtonSpawnEntity("GhostReactorToolDockWrist", toolSpawnNode);
	}

	public void OnButtonSpawnSmallBackpack()
	{
		OnButtonSpawnEntity("GhostReactorToolSmallBackpack", toolSpawnNode);
	}

	public void OnButtonKillAllEnemies()
	{
		KillAllEnemies();
	}

	public void OnButtonSpawnPest()
	{
		OnButtonSpawnEntity("GhostReactorEnemyPest", enemySpawnNode);
	}

	public void OnButtonSpawnChaser()
	{
		OnButtonSpawnEntity("GhostReactorEnemyChaser", enemySpawnNode);
	}

	public void OnButtonSpawnPhantom()
	{
		OnButtonSpawnEntity("GhostReactorEnemyPhantom", enemySpawnNode);
	}

	public void OnButtonSpawnRanged()
	{
		OnButtonSpawnEntity("GhostReactorEnemyRanged", enemySpawnNode);
	}

	public void OnButtonSpawnSummoner()
	{
		OnButtonSpawnEntity("GhostReactorEnemySummoner", enemySpawnNode);
	}

	public void OnButtonSpawnIceRanged()
	{
		OnButtonSpawnEntity("GhostReactorEnemyRangedIce", enemySpawnNode);
	}

	public void OnButtonSpawnUpgEff1()
	{
		OnButtonSpawnEntity("GRUPowerEff1", upgradeSpawnNode);
	}

	public void OnButtonSpawnUpgEff2()
	{
		OnButtonSpawnEntity("GRUPowerEff2", upgradeSpawnNode);
	}

	public void OnButtonSpawnUpgEff3()
	{
		OnButtonSpawnEntity("GRUPowerEff3", upgradeSpawnNode);
	}

	public void OnButtonSpawnUpgBatonDmg1()
	{
		OnButtonSpawnEntity("GRUBatonDamage1", upgradeSpawnNode);
	}

	public void OnButtonSpawnUpgBatonDmg2()
	{
		OnButtonSpawnEntity("GRUBatonDamage2", upgradeSpawnNode);
	}

	public void OnButtonSpawnUpgBatonDmg3()
	{
		OnButtonSpawnEntity("GRUBatonDamage3", upgradeSpawnNode);
	}

	public void OnButtonSpawnUpgEfficiency1()
	{
		OnButtonSpawnEntity("GRUPowerEff1", upgradeSpawnNode);
	}

	public void OnButtonSpawnUpgEfficiency2()
	{
		OnButtonSpawnEntity("GRUPowerEff2", upgradeSpawnNode);
	}

	public void OnButtonSpawnUpgEfficiency3()
	{
		OnButtonSpawnEntity("GRUPowerEff3", upgradeSpawnNode);
	}

	public void OnButtonSpawnChaosSeed()
	{
		OnButtonSpawnEntity("GhostReactorCollectibleSentientCore", enemySpawnNode);
	}

	public void OnButtonSpawnEntity(string entityName, Transform location)
	{
		if (location == null)
		{
			return;
		}
		Debug.Log("GRDebugUpgradeKiosk attempting to spawn " + entityName);
		int staticHash = entityName.GetStaticHash();
		GameEntityId gameEntityId = grManager.gameEntityManager.RequestCreateItem(staticHash, location.position, Quaternion.identity, 0L);
		GameAgent component = grManager.gameEntityManager.GetGameEntity(gameEntityId).gameObject.GetComponent<GameAgent>();
		if (component != null)
		{
			if (entityName.Contains("enemy", StringComparison.OrdinalIgnoreCase))
			{
				GhostReactorManager.entityDebugEnabled = true;
			}
			spawnedEntities.Add(gameEntityId);
			component.ApplyDestination(location.position);
		}
		else
		{
			Debug.Log("GRDebugUpgradeKiosk failed to spawn " + entityName);
		}
	}

	public void KillAllEnemies()
	{
		foreach (GameEntityId spawnedEntity in spawnedEntities)
		{
			grManager.gameEntityManager.RequestDestroyItem(spawnedEntity);
		}
		spawnedEntities.Clear();
	}
}
