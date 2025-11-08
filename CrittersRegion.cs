using System.Collections.Generic;
using UnityEngine;

public class CrittersRegion : MonoBehaviour
{
	private static List<CrittersRegion> _regions = new List<CrittersRegion>();

	private static Dictionary<int, CrittersRegion> _regionLookup = new Dictionary<int, CrittersRegion>();

	public CrittersBiome Biome = CrittersBiome.Any;

	public int maxCritters = 10;

	public float scale = 10f;

	public List<CrittersPawn> _critters = new List<CrittersPawn>();

	public static List<CrittersRegion> Regions => _regions;

	public int CritterCount => _critters.Count;

	[field: SerializeField]
	public int ID { get; private set; }

	private void OnEnable()
	{
		RegisterRegion(this);
	}

	private void OnDisable()
	{
		UnregisterRegion(this);
	}

	private static void RegisterRegion(CrittersRegion region)
	{
		_regionLookup[region.ID] = region;
		_regions.Add(region);
	}

	private static void UnregisterRegion(CrittersRegion region)
	{
		_regionLookup.Remove(region.ID);
		_regions.Remove(region);
	}

	public static void AddCritterToRegion(CrittersPawn critter, int regionId)
	{
		if (_regionLookup.TryGetValue(regionId, out var value))
		{
			value.AddCritter(critter);
		}
		else
		{
			GTDev.LogError($"Attempted to add critter to non-existing region {regionId}.");
		}
	}

	public static void RemoveCritterFromRegion(CrittersPawn critter)
	{
		if (_regionLookup.TryGetValue(critter.regionId, out var value))
		{
			value.RemoveCritter(critter);
		}
		else
		{
			GTDev.LogError($"Couldn't find region with id {critter.regionId}");
		}
	}

	public void AddCritter(CrittersPawn pawn)
	{
		_critters.Add(pawn);
	}

	public void RemoveCritter(CrittersPawn pawn)
	{
		_critters.Remove(pawn);
	}

	public Vector3 GetSpawnPoint()
	{
		float num = scale / 2f;
		float num2 = base.transform.lossyScale.y * scale;
		Vector3 vector = base.transform.TransformPoint(new Vector3(Random.Range(0f - num, num), num, Random.Range(0f - num, num)));
		if (Physics.Raycast(vector, -base.transform.up, out var hitInfo, num2, -1, QueryTriggerInteraction.Ignore))
		{
			Debug.DrawLine(vector, hitInfo.point, Color.green, 5f);
			return hitInfo.point;
		}
		Debug.DrawLine(vector, vector - base.transform.up * num2, Color.red, 5f);
		return vector;
	}
}
