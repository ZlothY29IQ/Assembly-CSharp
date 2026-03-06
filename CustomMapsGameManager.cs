using System.Collections;
using System.Collections.Generic;
using System.IO;
using GorillaExtensions;
using GT_CustomMapSupportRuntime;
using UnityEngine;

public class CustomMapsGameManager : MonoBehaviour, IGameEntityZoneComponent
{
	public GameEntityManager gameEntityManager;

	public GameAgentManager gameAgentManager;

	public GhostReactorManager ghostReactorManager;

	public static CustomMapsGameManager instance;

	private const string AGENT_PREFAB_NAME = "CustomMapsAIAgent";

	private const string GRABBABLE_PREFAB_NAME = "CustomMapsGrabbableEntity";

	private Dictionary<int, AIAgent> customMapsAgents;

	private static List<GameEntityCreateData> tempCreateEntitiesList = new List<GameEntityCreateData>(128);

	private static List<MapEntity> agentsToCreateOnZoneInit = new List<MapEntity>(128);

	private int TEST_index;

	private int spawnCount;

	private void Awake()
	{
		if (instance.IsNotNull())
		{
			Object.Destroy(this);
			return;
		}
		instance = this;
		customMapsAgents = new Dictionary<int, AIAgent>(GT_CustomMapSupportRuntime.Constants.aiAgentLimit);
		tempCreateEntitiesList = new List<GameEntityCreateData>(GT_CustomMapSupportRuntime.Constants.aiAgentLimit);
	}

	private void Start()
	{
	}

	public void CreatePlacedEntities(List<MapEntity> entities)
	{
		if (!gameEntityManager.IsAuthority())
		{
			GTDev.LogError("CustomMapsManager::CreateAIAgents not the authority");
			return;
		}
		int gameAgentCount = gameAgentManager.GetGameAgentCount();
		if (gameAgentCount >= GT_CustomMapSupportRuntime.Constants.aiAgentLimit)
		{
			GTDev.LogError("[CustomMapsGameManager::CreateAIAgents] Failed to create agent. Max Agent count " + $"({GT_CustomMapSupportRuntime.Constants.aiAgentLimit}) has been reached!");
			return;
		}
		tempCreateEntitiesList.Clear();
		int b = ((GT_CustomMapSupportRuntime.Constants.aiAgentLimit - gameAgentCount >= 0) ? (GT_CustomMapSupportRuntime.Constants.aiAgentLimit - gameAgentCount) : 0);
		int num = Mathf.Min(entities.Count, b);
		if (num < entities.Count)
		{
			GTDev.LogWarning($"[CustomMapsGameManager::CreateAIAgents] Only creating {num} out of the " + $"requested {entities.Count} agents. Max Agent count ({GT_CustomMapSupportRuntime.Constants.aiAgentLimit}) has been reached.!");
		}
		for (int i = 0; i < num; i++)
		{
			if (entities[i].IsNull())
			{
				Debug.Log($"[CustomMapsGameManager::CreateAIAgents] Requested entity to create is null! {i}/{entities.Count}");
				continue;
			}
			int num2 = ((entities[i] is AIAgent) ? "CustomMapsAIAgent".GetStaticHash() : "CustomMapsGrabbableEntity".GetStaticHash());
			if (!gameEntityManager.FactoryHasEntity(num2))
			{
				Debug.LogErrorFormat("[CustomMapsManager::CreateAIAgents] Cannot Find Entity in Factory {0} {1}", entities[i].gameObject.name, num2);
				continue;
			}
			GameEntityCreateData item = new GameEntityCreateData
			{
				entityTypeId = num2,
				position = entities[i].transform.position,
				rotation = entities[i].transform.rotation,
				createData = entities[i].GetPackedCreateData(),
				createdByEntityId = -1,
				slotIndex = -1
			};
			tempCreateEntitiesList.Add(item);
		}
		if (tempCreateEntitiesList.Count > 0)
		{
			gameEntityManager.RequestCreateItems(tempCreateEntitiesList);
			tempCreateEntitiesList.Clear();
		}
	}

	public void TEST_Spawning()
	{
		GTDev.Log("CustomMapsGameManager::TEST_Spawn starting spawn");
		StartCoroutine(TEST_Spawn());
	}

	private IEnumerator TEST_Spawn()
	{
		while (spawnCount < 10)
		{
			yield return new WaitForSeconds(5f);
			GTDev.Log("CustomMapsGameManager::TEST_Spawn spawning enemy");
			TEST_index = ((TEST_index == 5) ? 3 : 5);
			SpawnEnemyFromPoint("79e43963", TEST_index);
			spawnCount++;
		}
	}

	public GameEntityId SpawnEnemyFromPoint(string spawnPointId, int enemyTypeId)
	{
		if (!AISpawnManager.instance.GetSpawnPoint(spawnPointId, out AISpawnPoint spawnPoint))
		{
			GTDev.LogError("CustomMapsGameManager::SpawnEnemyFromPoint cannot find spawn point");
			return GameEntityId.Invalid;
		}
		return SpawnEnemyAtLocation(enemyTypeId, spawnPoint.transform.position, spawnPoint.transform.rotation);
	}

	public GameEntityId SpawnEnemyAtLocation(int enemyTypeId, Vector3 position, Quaternion rotation)
	{
		if (!gameEntityManager.IsAuthority())
		{
			GTDev.LogError("[CustomMapsGameManager::SpawnEnemyAtLocation] Failed: Not Authority");
			return GameEntityId.Invalid;
		}
		if (gameEntityManager.GetGameEntities().Count >= GT_CustomMapSupportRuntime.Constants.aiAgentLimit)
		{
			GTDev.LogError($"[CustomMapsGameManager::SpawnEnemyAtLocation] Failed: Max Agents ({GT_CustomMapSupportRuntime.Constants.aiAgentLimit}) reached.");
			return GameEntityId.Invalid;
		}
		int staticHash = "CustomMapsAIAgent".GetStaticHash();
		if (!gameEntityManager.FactoryHasEntity(staticHash))
		{
			GTDev.LogError("[CustomMapsGameManager::SpawnEnemyAtLocation] Failed cannot find entity type");
			return GameEntityId.Invalid;
		}
		return gameEntityManager.RequestCreateItem(staticHash, position, rotation, enemyTypeId);
	}

	public void SpawnEnemyClient(int enemyTypeId, int agentId)
	{
		if (!gameEntityManager.IsAuthority() && enemyTypeId != -1)
		{
			MapEntity newEnemy2;
			if (AISpawnManager.HasInstance && AISpawnManager.instance.SpawnEnemy(enemyTypeId, out AIAgent newEnemy))
			{
				newEnemy.transform.parent = AISpawnManager.instance.transform;
				customMapsAgents[agentId] = newEnemy;
			}
			else if (MapSpawnManager.instance.SpawnEntity(enemyTypeId, out newEnemy2))
			{
				newEnemy = (AIAgent)newEnemy2;
				newEnemy.transform.parent = AISpawnManager.instance.transform;
				customMapsAgents[agentId] = newEnemy;
			}
		}
	}

	public GameEntityId SpawnGrabbableAtLocation(int enemyTypeId, Vector3 position, Quaternion rotation)
	{
		if (!gameEntityManager.IsAuthority())
		{
			GTDev.LogError("[CustomMapsGameManager::SpawnGrabbableAtLocation] Failed: Not Authority");
			return GameEntityId.Invalid;
		}
		if (gameEntityManager.GetGameEntities().Count >= GT_CustomMapSupportRuntime.Constants.aiAgentLimit)
		{
			GTDev.LogError($"[CustomMapsGameManager::SpawnGrabbableAtLocation] Failed: Max Entities ({GT_CustomMapSupportRuntime.Constants.aiAgentLimit}) reached.");
			return GameEntityId.Invalid;
		}
		int staticHash = "CustomMapsGrabbableEntity".GetStaticHash();
		if (!gameEntityManager.FactoryHasEntity(staticHash))
		{
			GTDev.LogError("[CustomMapsGameManager::SpawnGrabbableAtLocation] Failed cannot find entity type");
			return GameEntityId.Invalid;
		}
		return gameEntityManager.RequestCreateItem(staticHash, position, rotation, enemyTypeId);
	}

	public long ProcessMigratedGameEntityCreateData(GameEntity entity, long createData)
	{
		return createData;
	}

	public bool ValidateMigratedGameEntity(int netId, int entityTypeId, Vector3 position, Quaternion rotation, long createData, int actorNr)
	{
		return false;
	}

	public bool ValidateCreateMultipleItems(int zoneId, byte[] compressedStateData, int EntityCount)
	{
		if (EntityCount > GT_CustomMapSupportRuntime.Constants.aiAgentLimit)
		{
			return false;
		}
		return true;
	}

	public bool ValidateCreateItemBatchSize(int size)
	{
		return true;
	}

	public bool ValidateCreateItem(int nedId, int entityTypeId, Vector3 position, Quaternion rotation, long createData, int createdByEntityNetId)
	{
		return true;
	}

	private bool IsAuthority()
	{
		return gameEntityManager.IsAuthority();
	}

	private bool IsDriver()
	{
		return CustomMapsTerminal.IsDriver;
	}

	public void OnZoneCreate()
	{
	}

	public void OnZoneInit()
	{
		if (!agentsToCreateOnZoneInit.IsNullOrEmpty())
		{
			CreatePlacedEntities(agentsToCreateOnZoneInit);
			agentsToCreateOnZoneInit.Clear();
		}
	}

	public void OnZoneClear(ZoneClearReason reason)
	{
	}

	public bool ShouldClearZone()
	{
		return true;
	}

	public bool IsZoneReady()
	{
		if (CustomMapLoader.CanLoadEntities)
		{
			return NetworkSystem.Instance.InRoom;
		}
		return false;
	}

	public void OnCreateGameEntity(GameEntity entity)
	{
	}

	private void SetupCollisions(GameObject go)
	{
	}

	public void SerializeZoneData(BinaryWriter writer)
	{
	}

	public void DeserializeZoneData(BinaryReader reader)
	{
	}

	public void SerializeZoneEntityData(BinaryWriter writer, GameEntity entity)
	{
	}

	public void DeserializeZoneEntityData(BinaryReader reader, GameEntity entity)
	{
	}

	public void SerializeZonePlayerData(BinaryWriter writer, int actorNumber)
	{
	}

	public void DeserializeZonePlayerData(BinaryReader reader, int actorNumber)
	{
	}

	public static GameEntityManager GetEntityManager()
	{
		if (instance.IsNotNull())
		{
			return instance.gameEntityManager;
		}
		return null;
	}

	public static GameAgentManager GetAgentManager()
	{
		if (instance.IsNotNull())
		{
			return instance.gameAgentManager;
		}
		return null;
	}

	public static CustomMapsAIBehaviourController GetBehaviorControllerForEntity(GameEntityId entityId)
	{
		GameEntityManager entityManager = GetEntityManager();
		if (entityManager.IsNull())
		{
			return null;
		}
		GameEntity gameEntity = entityManager.GetGameEntity(entityId);
		if (gameEntity.IsNull())
		{
			return null;
		}
		return gameEntity.gameObject.GetComponent<CustomMapsAIBehaviourController>();
	}

	public static void AddAgentsToCreate(List<MapEntity> entitiesToCreate)
	{
		if (!instance.IsNull() && !entitiesToCreate.IsNullOrEmpty())
		{
			agentsToCreateOnZoneInit.AddRange(entitiesToCreate);
		}
	}

	public void OnPlayerHit(GameEntityId hitByEntityId, GRPlayer player, Vector3 hitPosition)
	{
		ghostReactorManager.RequestEnemyHitPlayer(GhostReactor.EnemyType.CustomMapsEnemy, hitByEntityId, player, hitPosition);
	}
}
