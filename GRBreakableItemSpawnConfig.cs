using System;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;

[CreateAssetMenu(fileName = "GhostReactorBreakableItemSpawnConfig", menuName = "ScriptableObjects/GhostReactorBreakableItemSpawnConfig")]
public class GRBreakableItemSpawnConfig : ScriptableObject
{
	[Serializable]
	public struct ItemProbability
	{
		public GameEntity entity;

		public float probability;
	}

	[SerializeField]
	[Range(0f, 1f)]
	public float spawnAnythingProbability = 0.2f;

	public List<ItemProbability> perItemProbabilities = new List<ItemProbability>();

	[SerializeField]
	[ReadOnly]
	private float precomputedItemTotalWeight;

	public bool TryForRandomItem(GameEntity spawnFromEntity, out GameEntity entity, int sanity = 0)
	{
		GRBreakableItemSpawnConfig gRBreakableItemSpawnConfig = GetOverride(spawnFromEntity);
		if (sanity <= 5 && gRBreakableItemSpawnConfig != null)
		{
			return gRBreakableItemSpawnConfig.TryForRandomItem(spawnFromEntity, out entity, sanity + 1);
		}
		if (sanity > 5)
		{
			Debug.LogError("Circular override loop");
		}
		if (UnityEngine.Random.Range(0f, 1f) < spawnAnythingProbability)
		{
			float num = UnityEngine.Random.Range(0f, precomputedItemTotalWeight);
			float num2 = 0f;
			for (int i = 0; i < perItemProbabilities.Count; i++)
			{
				num2 += perItemProbabilities[i].probability;
				if (num2 > num || i == perItemProbabilities.Count - 1)
				{
					entity = perItemProbabilities[i].entity;
					return true;
				}
			}
		}
		entity = null;
		return false;
	}

	public bool TryForRandomItem(GhostReactor reactor, ref SRand srand, out GameEntity entity, int sanity = 0)
	{
		GRBreakableItemSpawnConfig gRBreakableItemSpawnConfig = GetOverride(reactor);
		if (sanity <= 5 && gRBreakableItemSpawnConfig != null)
		{
			return gRBreakableItemSpawnConfig.TryForRandomItem(reactor, ref srand, out entity, sanity + 1);
		}
		if (sanity > 5)
		{
			Debug.LogError("Circular override loop");
		}
		if (srand.NextFloat(0f, 1f) < spawnAnythingProbability)
		{
			float num = srand.NextFloat(0f, precomputedItemTotalWeight);
			float num2 = 0f;
			for (int i = 0; i < perItemProbabilities.Count; i++)
			{
				num2 += perItemProbabilities[i].probability;
				if (num2 > num || i == perItemProbabilities.Count - 1)
				{
					entity = perItemProbabilities[i].entity;
					return true;
				}
			}
		}
		entity = null;
		return false;
	}

	private GRBreakableItemSpawnConfig GetOverride(GameEntity entity)
	{
		GhostReactorManager ghostReactorManager = GhostReactorManager.Get(entity);
		if (ghostReactorManager == null)
		{
			return null;
		}
		return GetOverride(ghostReactorManager.reactor);
	}

	private GRBreakableItemSpawnConfig GetOverride(GhostReactor reactor)
	{
		if (reactor == null)
		{
			return null;
		}
		GhostReactorLevelGenConfig currLevelGenConfig = reactor.GetCurrLevelGenConfig();
		if (currLevelGenConfig == null || currLevelGenConfig.dropTableOverrides == null)
		{
			return null;
		}
		return currLevelGenConfig.dropTableOverrides.GetOverride(this);
	}

	private void OnValidate()
	{
		precomputedItemTotalWeight = 0f;
		for (int i = 0; i < perItemProbabilities.Count; i++)
		{
			precomputedItemTotalWeight += perItemProbabilities[i].probability;
		}
	}
}
