using UnityEngine;

public abstract class CosmeticCritterSpawnerTimed : CosmeticCritterSpawnerIndependent
{
	[Tooltip("The minimum and maximum time to wait between spawn attempts.")]
	[SerializeField]
	private Vector2 spawnIntervalMinMax = new Vector2(2f, 5f);

	[Tooltip("Currently does nothing.")]
	[SerializeField]
	[Range(0f, 1f)]
	private float spawnChance = 1f;

	protected override CallLimiter CreateCallLimiter()
	{
		return new CallLimiter(5, spawnIntervalMinMax.x);
	}

	public override bool CanSpawnLocal()
	{
		if (Time.time >= nextLocalSpawnTime)
		{
			nextLocalSpawnTime = Time.time + Random.Range(spawnIntervalMinMax.x, spawnIntervalMinMax.y);
			return base.CanSpawnLocal();
		}
		return false;
	}

	public override bool CanSpawnRemote(double serverTime)
	{
		return base.CanSpawnRemote(serverTime);
	}

	protected override void OnEnable()
	{
		base.OnEnable();
		if (base.IsLocal)
		{
			nextLocalSpawnTime = Time.time + Random.Range(spawnIntervalMinMax.x, spawnIntervalMinMax.y);
		}
	}

	protected override void OnDisable()
	{
		base.OnDisable();
	}
}
