using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GhostReactorSpawnConfig", menuName = "ScriptableObjects/GhostReactorSpawnConfig")]
public class GhostReactorSpawnConfig : ScriptableObject
{
	public enum SpawnPointType
	{
		Enemy,
		Collectible,
		Barrier,
		HazardLiquid,
		Phantom,
		Pest,
		Crate,
		Tool,
		ChaosSeed,
		HazardTower,
		MiniBoss,
		SpawnPointTypeCount
	}

	[Serializable]
	public struct EntitySpawnGroup
	{
		public SpawnPointType spawnPointType;

		public GameEntity entity;

		public GRBreakableItemSpawnConfig randomEntity;

		public int spawnCount;
	}

	public List<EntitySpawnGroup> entitySpawnGroups;
}
