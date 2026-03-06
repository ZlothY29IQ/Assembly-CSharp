using System;
using System.Collections.Generic;
using UnityEngine;

public class GhostReactorLevelSection : MonoBehaviour
{
	public enum SectionType
	{
		Hub,
		EndCap,
		Blocker
	}

	[Serializable]
	public class SpawnPointGroup
	{
		public GhostReactorSpawnConfig.SpawnPointType type;

		public List<GREntitySpawnPoint> spawnPoints;

		private List<int> spawnPointIndexes;

		private bool needsRandomization;

		private int currentIndex;

		public bool NeedsRandomization
		{
			get
			{
				return needsRandomization;
			}
			set
			{
				needsRandomization = value;
			}
		}

		public int CurrentIndex
		{
			get
			{
				return currentIndex;
			}
			set
			{
				currentIndex = value;
			}
		}

		public List<int> SpawnPointIndexes
		{
			get
			{
				return spawnPointIndexes;
			}
			set
			{
				spawnPointIndexes = value;
			}
		}

		public GREntitySpawnPoint GetNextSpawnPoint()
		{
			GREntitySpawnPoint result = spawnPoints[spawnPointIndexes[currentIndex]];
			currentIndex = (currentIndex + 1) % spawnPointIndexes.Count;
			return result;
		}
	}

	private const float SHOW_DIST = 32f;

	private const float HIDE_DIST = 36f;

	private const int MAX_CREATE_PER_RPC = 25;

	[SerializeField]
	private SectionType sectionType;

	[SerializeField]
	[Tooltip("Single Anchor Transform used for End Caps and Blockers")]
	private Transform anchorTransform;

	[SerializeField]
	[Tooltip("A List of Anchors used as in and out connections for Hubs")]
	private List<Transform> anchors = new List<Transform>();

	[SerializeField]
	private List<SpawnPointGroup> spawnPointGroups;

	[SerializeField]
	private List<GhostReactorSpawnConfig> spawnConfigs;

	[SerializeField]
	private List<GRPatrolPath> patrolPaths;

	[SerializeField]
	private BoxCollider boundingCollider;

	private List<Renderer> renderers;

	private bool hidden;

	private List<GRHazardousMaterial> hazardousMaterials;

	[HideInInspector]
	public GhostReactorLevelSectionConnector sectionConnector;

	[HideInInspector]
	public int hubAnchorIndex;

	private int index;

	private SpawnPointGroup[] spawnPointGroupLookup;

	private List<GameEntity> prePlacedGameEntities;

	public static List<GameEntityCreateData> tempCreateEntitiesList = new List<GameEntityCreateData>(32);

	private int rotatingIndexForRespawn;

	public Transform Anchor => anchorTransform;

	public List<Transform> Anchors => anchors;

	public SectionType Type => sectionType;

	public BoxCollider BoundingCollider => boundingCollider;

	private void Awake()
	{
		spawnPointGroupLookup = new SpawnPointGroup[11];
		for (int i = 0; i < spawnPointGroups.Count; i++)
		{
			spawnPointGroups[i].SpawnPointIndexes = new List<int>();
			int type = (int)spawnPointGroups[i].type;
			if (type < spawnPointGroupLookup.Length)
			{
				spawnPointGroupLookup[type] = spawnPointGroups[i];
			}
		}
		hazardousMaterials = new List<GRHazardousMaterial>(32);
		GetComponentsInChildren(hazardousMaterials);
		for (int j = 0; j < patrolPaths.Count; j++)
		{
			if (patrolPaths[j] == null)
			{
				Debug.LogErrorFormat("Why does {0} have a null patrol path at index {1}", base.gameObject.name, j);
			}
			else
			{
				patrolPaths[j].index = j;
			}
		}
		prePlacedGameEntities = new List<GameEntity>(128);
		GetComponentsInChildren(prePlacedGameEntities);
		for (int k = 0; k < prePlacedGameEntities.Count; k++)
		{
			prePlacedGameEntities[k].gameObject.SetActive(value: false);
		}
		renderers = new List<Renderer>(512);
		hidden = false;
		GetComponentsInChildren(includeInactive: false, renderers);
		for (int num = renderers.Count - 1; num >= 0; num--)
		{
			if (renderers[num] == null || !renderers[num].enabled)
			{
				renderers.RemoveAt(num);
			}
		}
		if (boundingCollider == null)
		{
			Debug.LogWarningFormat("Missing Bounding Collider for section {0}", base.gameObject.name);
		}
	}

	public static void RandomizeIndices(List<int> list, int count, ref SRand randomGenerator)
	{
		list.Clear();
		for (int i = 0; i < count; i++)
		{
			list.Add(i);
		}
		randomGenerator.Shuffle(list);
	}

	public void InitLevelSection(int sectionIndex, GhostReactor reactor)
	{
		index = sectionIndex;
		for (int i = 0; i < hazardousMaterials.Count; i++)
		{
			hazardousMaterials[i].Init(reactor);
		}
	}

	public void SpawnSectionEntities(ref SRand randomGenerator, GameEntityManager gameEntityManager, GhostReactor reactor, List<GhostReactorSpawnConfig> spawnConfigs, float respawnCount)
	{
		if (spawnConfigs == null)
		{
			spawnConfigs = this.spawnConfigs;
		}
		if (spawnConfigs == null || spawnConfigs.Count <= 0)
		{
			return;
		}
		GhostReactorSpawnConfig ghostReactorSpawnConfig = spawnConfigs[randomGenerator.NextInt(spawnConfigs.Count)];
		Debug.LogFormat("Spawn Ghost Reactor Level Section {0} {1}", base.gameObject.name, ghostReactorSpawnConfig.name);
		for (int i = 0; i < spawnPointGroups.Count; i++)
		{
			spawnPointGroups[i].CurrentIndex = 0;
			spawnPointGroups[i].NeedsRandomization = true;
		}
		GhostReactor.EnemyEntityCreateData enemyEntityCreateData = default(GhostReactor.EnemyEntityCreateData);
		for (int j = 0; j < ghostReactorSpawnConfig.entitySpawnGroups.Count; j++)
		{
			int spawnCount = ghostReactorSpawnConfig.entitySpawnGroups[j].spawnCount;
			if (spawnCount <= 0)
			{
				continue;
			}
			int spawnPointType = (int)ghostReactorSpawnConfig.entitySpawnGroups[j].spawnPointType;
			if (spawnPointType >= spawnPointGroupLookup.Length)
			{
				continue;
			}
			SpawnPointGroup spawnPointGroup = spawnPointGroupLookup[spawnPointType];
			if (spawnPointGroup == null)
			{
				continue;
			}
			if (spawnPointGroup.NeedsRandomization)
			{
				spawnPointGroup.NeedsRandomization = false;
				RandomizeIndices(spawnPointGroup.SpawnPointIndexes, spawnPointGroup.spawnPoints.Count, ref randomGenerator);
			}
			spawnCount = Mathf.Min(spawnCount, spawnPointGroup.spawnPoints.Count);
			for (int k = 0; k < spawnCount; k++)
			{
				_ = spawnPointGroup.CurrentIndex;
				GREntitySpawnPoint nextSpawnPoint = spawnPointGroup.GetNextSpawnPoint();
				_ = nextSpawnPoint == null;
				GameEntity entity = ghostReactorSpawnConfig.entitySpawnGroups[j].entity;
				if (ghostReactorSpawnConfig.entitySpawnGroups[j].randomEntity != null)
				{
					ghostReactorSpawnConfig.entitySpawnGroups[j].randomEntity.TryForRandomItem(reactor, ref randomGenerator, out entity);
				}
				if (entity == null)
				{
					continue;
				}
				int staticHash = entity.name.GetStaticHash();
				long createData = -1L;
				if (nextSpawnPoint.applyScale)
				{
					createData = BitPackUtils.PackWorldPosForNetwork(nextSpawnPoint.transform.localScale);
				}
				else if (spawnPointGroup.type == GhostReactorSpawnConfig.SpawnPointType.Enemy || spawnPointGroup.type == GhostReactorSpawnConfig.SpawnPointType.Pest || nextSpawnPoint.patrolPath != null)
				{
					int patrolIndex = 255;
					if (nextSpawnPoint.patrolPath != null)
					{
						patrolIndex = nextSpawnPoint.patrolPath.index;
					}
					int num = (int)respawnCount;
					if (randomGenerator.NextFloat() < respawnCount - (float)num)
					{
						num++;
					}
					enemyEntityCreateData.respawnCount = num;
					enemyEntityCreateData.sectionIndex = index;
					enemyEntityCreateData.patrolIndex = patrolIndex;
					createData = enemyEntityCreateData.Pack();
				}
				GameEntityCreateData item = new GameEntityCreateData
				{
					entityTypeId = staticHash,
					position = nextSpawnPoint.transform.position,
					rotation = nextSpawnPoint.transform.rotation,
					createData = createData,
					createdByEntityId = -1,
					slotIndex = -1
				};
				tempCreateEntitiesList.Add(item);
				if (tempCreateEntitiesList.Count > 25)
				{
					gameEntityManager.RequestCreateItems(tempCreateEntitiesList);
					tempCreateEntitiesList.Clear();
				}
			}
		}
		for (int l = 0; l < prePlacedGameEntities.Count; l++)
		{
			if (prePlacedGameEntities[l].isBuiltIn)
			{
				continue;
			}
			int staticHash2 = prePlacedGameEntities[l].gameObject.name.GetStaticHash();
			if (!gameEntityManager.FactoryHasEntity(staticHash2))
			{
				Debug.LogErrorFormat("Cannot Find Entity in Factory {0} {1} Trying to spawn in {2}", prePlacedGameEntities[l].gameObject.name, staticHash2, base.gameObject.name);
				continue;
			}
			GameEntityCreateData item2 = new GameEntityCreateData
			{
				entityTypeId = staticHash2,
				position = prePlacedGameEntities[l].transform.position,
				rotation = prePlacedGameEntities[l].transform.rotation,
				createData = 0L,
				createdByEntityId = -1,
				slotIndex = -1
			};
			tempCreateEntitiesList.Add(item2);
			if (tempCreateEntitiesList.Count > 25)
			{
				gameEntityManager.RequestCreateItems(tempCreateEntitiesList);
				tempCreateEntitiesList.Clear();
			}
		}
	}

	public void RespawnEntity(ref SRand randomGenerator, GameEntityManager gameEntityManager, int entityId, long entityCreateData, GameEntityId createdByEntityId)
	{
		if (0 <= spawnPointGroupLookup.Length)
		{
			SpawnPointGroup spawnPointGroup = spawnPointGroupLookup[0];
			int count = spawnPointGroup.spawnPoints.Count;
			if (count > 3)
			{
				rotatingIndexForRespawn = (rotatingIndexForRespawn + randomGenerator.NextInt(1, 1 + spawnPointGroup.spawnPoints.Count / 2)) % spawnPointGroup.spawnPoints.Count;
			}
			else if (count > 1)
			{
				rotatingIndexForRespawn = (rotatingIndexForRespawn + 1) % count;
			}
			else
			{
				rotatingIndexForRespawn = 0;
			}
			GREntitySpawnPoint gREntitySpawnPoint = spawnPointGroup.spawnPoints[rotatingIndexForRespawn];
			GhostReactor.EnemyEntityCreateData enemyEntityCreateData = GhostReactor.EnemyEntityCreateData.Unpack(entityCreateData);
			enemyEntityCreateData.patrolIndex = ((gREntitySpawnPoint.patrolPath != null) ? gREntitySpawnPoint.patrolPath.index : 255);
			long createData = enemyEntityCreateData.Pack();
			gameEntityManager.RequestCreateItem(entityId, gREntitySpawnPoint.transform.position, gREntitySpawnPoint.transform.rotation, createData, createdByEntityId);
		}
	}

	public GRPatrolPath GetPatrolPath(int patrolPathIndex)
	{
		if (patrolPathIndex >= 0 && patrolPathIndex < patrolPaths.Count)
		{
			return patrolPaths[patrolPathIndex];
		}
		return null;
	}

	public void Hide(bool hide)
	{
		for (int i = 0; i < renderers.Count; i++)
		{
			if (!(renderers[i] == null))
			{
				renderers[i].enabled = !hide;
			}
		}
	}

	public void UpdateDisable(Vector3 playerPos)
	{
		if (!(boundingCollider == null))
		{
			float distSq = GetDistSq(playerPos);
			float num = 1024f;
			float num2 = 1296f;
			if (hidden && distSq < num)
			{
				hidden = false;
				Hide(hide: false);
			}
			else if (!hidden && distSq > num2)
			{
				hidden = true;
				Hide(hide: true);
			}
		}
	}

	public float GetDistSq(Vector3 pos)
	{
		return (boundingCollider.ClosestPoint(pos) - pos).sqrMagnitude;
	}

	public Transform GetAnchor(int anchorIndex)
	{
		return anchors[anchorIndex];
	}
}
