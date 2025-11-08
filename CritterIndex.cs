using System;
using System.Collections.Generic;
using GorillaNetworking;
using UnityEngine;

public class CritterIndex : ScriptableObject
{
	[Serializable]
	public class AnimalTypeMeshEntry
	{
		public CritterConfiguration.AnimalType animalType;

		public Mesh mesh;
	}

	public List<AnimalTypeMeshEntry> animalMeshes = new List<AnimalTypeMeshEntry>();

	public List<CritterConfiguration> critterTypes;

	private WeightedList<CritterConfiguration> _currentConfigs = new WeightedList<CritterConfiguration>();

	private static CritterIndex _instance;

	public CritterConfiguration this[int index]
	{
		get
		{
			if (index < 0 || index >= critterTypes.Count)
			{
				return null;
			}
			return critterTypes[index];
		}
	}

	private void OnEnable()
	{
		_instance = this;
	}

	public static Mesh GetMesh(CritterConfiguration.AnimalType animalType)
	{
		if (animalType < CritterConfiguration.AnimalType.Raccoon || (int)animalType >= _instance.animalMeshes.Count)
		{
			return null;
		}
		return _instance.animalMeshes[(int)animalType].mesh;
	}

	public int GetRandomCritterType(CrittersRegion region = null)
	{
		return critterTypes.IndexOf(GetRandomConfiguration(region));
	}

	public CritterConfiguration GetRandomConfiguration(CrittersRegion region = null)
	{
		WeightedList<CritterConfiguration> validCritterTypes = GetValidCritterTypes(region);
		if (validCritterTypes.Count == 0)
		{
			return null;
		}
		return validCritterTypes.GetRandomItem();
	}

	public static DateTime GetCritterDateTime()
	{
		if (!GorillaComputer.instance)
		{
			return DateTime.UtcNow;
		}
		return GorillaComputer.instance.GetServerTime();
	}

	private WeightedList<CritterConfiguration> GetValidCritterTypes(CrittersRegion region = null)
	{
		_currentConfigs.Clear();
		DateTime critterDateTime = GetCritterDateTime();
		foreach (CritterConfiguration critterType in critterTypes)
		{
			if (critterType.DateConditionsMet(critterDateTime) && critterType.CanSpawn(region))
			{
				_currentConfigs.Add(critterType, critterType.spawnWeight);
			}
		}
		return _currentConfigs;
	}
}
