using System;
using UnityEngine;

public abstract class CosmeticCritterSpawner : CosmeticCritterHoldable
{
	[Tooltip("The critter prefab to spawn.")]
	[SerializeField]
	protected GameObject critterPrefab;

	[Tooltip("The maximum number of critters that this spawner can have active at once.")]
	[SerializeField]
	protected int maxCritters;

	protected CosmeticCritter cachedCritter;

	protected Type cachedType;

	protected int numCritters;

	protected float nextLocalSpawnTime;

	public GameObject GetCritterPrefab()
	{
		return critterPrefab;
	}

	public CosmeticCritter GetCritter()
	{
		return cachedCritter;
	}

	public Type GetCritterType()
	{
		return cachedType;
	}

	public virtual void SetRandomVariables(CosmeticCritter critter)
	{
	}

	public virtual void OnSpawn(CosmeticCritter critter)
	{
		numCritters++;
	}

	public virtual void OnDespawn(CosmeticCritter critter)
	{
		numCritters = Math.Max(numCritters - 1, 0);
	}

	protected override void OnEnable()
	{
		base.OnEnable();
		if (cachedCritter == null)
		{
			cachedCritter = critterPrefab.GetComponent<CosmeticCritter>();
			cachedType = cachedCritter.GetType();
		}
	}

	protected override void OnDisable()
	{
		base.OnDisable();
	}
}
