using System;
using UnityEngine;

[Serializable]
public class CritterConfiguration
{
	public enum AnimalType
	{
		Raccoon = 0,
		Cat = 1,
		Bird = 2,
		Goblin = 3,
		Egg = 4,
		UNKNOWN = -1
	}

	[Tooltip("Basic internal description of critter.  Could be role, purpose, player experience, etc.")]
	public string internalDescription;

	public string critterName = "UNNAMED CRITTER";

	public AnimalType animalType;

	public CritterTemplate behaviour;

	public CritterSpawnCriteria spawnCriteria;

	public RealWorldDateTimeWindow dateLimit;

	public CrittersBiome biome = CrittersBiome.Any;

	public float spawnWeight = 1f;

	public Material critterMat;

	public CritterConfiguration()
	{
		animalType = AnimalType.UNKNOWN;
	}

	public int GetIndex()
	{
		return CrittersManager.instance.creatureIndex.critterTypes.IndexOf(this);
	}

	private bool RegionMatches(CrittersRegion region)
	{
		if ((bool)region)
		{
			return (region.Biome & biome) != 0;
		}
		return true;
	}

	private bool SpawnCriteriaMatches()
	{
		if ((bool)spawnCriteria)
		{
			return spawnCriteria.CanSpawn();
		}
		return true;
	}

	public bool CanSpawn()
	{
		return SpawnCriteriaMatches();
	}

	public bool CanSpawn(CrittersRegion region)
	{
		if (RegionMatches(region))
		{
			return SpawnCriteriaMatches();
		}
		return false;
	}

	public bool DateConditionsMet(DateTime utcDate)
	{
		if ((bool)dateLimit)
		{
			return dateLimit.MatchesDate(utcDate);
		}
		return true;
	}

	public bool ShouldDespawn()
	{
		return !SpawnCriteriaMatches();
	}

	public void ApplyToCreature(CrittersPawn crittersPawn)
	{
		behaviour.ApplyToCritter(crittersPawn);
		if (CrittersManager.instance.LocalAuthority())
		{
			ApplyVisualsTo(crittersPawn);
		}
		else
		{
			ApplyVisualsTo(crittersPawn, generateAppearance: false);
		}
	}

	private void ApplyVisualsTo(CrittersPawn critter, bool generateAppearance = true)
	{
		ApplyVisualsTo(critter.visuals, generateAppearance);
	}

	public void ApplyVisualsTo(CritterVisuals visuals, bool generateAppearance = true)
	{
		visuals.critterType = GetIndex();
		visuals.ApplyMesh(CritterIndex.GetMesh(animalType));
		visuals.ApplyMaterial(critterMat);
		if (generateAppearance)
		{
			visuals.SetAppearance(GenerateAppearance());
		}
	}

	public CritterAppearance GenerateAppearance()
	{
		string hatName = "";
		if (UnityEngine.Random.value <= behaviour.GetTemplateValue<float>("hatChance"))
		{
			GameObject[] templateValue = behaviour.GetTemplateValue<GameObject[]>("hats");
			if (!templateValue.IsNullOrEmpty())
			{
				hatName = templateValue[UnityEngine.Random.Range(0, templateValue.Length)].name;
			}
		}
		float templateValue2 = behaviour.GetTemplateValue<float>("minSize");
		float templateValue3 = behaviour.GetTemplateValue<float>("maxSize");
		float size = UnityEngine.Random.Range(templateValue2, templateValue3);
		return new CritterAppearance(hatName, size);
	}

	public override string ToString()
	{
		return $"{critterName} B:{behaviour} C:{spawnCriteria}";
	}
}
