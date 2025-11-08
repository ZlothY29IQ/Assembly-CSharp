public class CosmeticCritterSpawnerIndependent : CosmeticCritterSpawner
{
	public virtual bool CanSpawnLocal()
	{
		return numCritters < maxCritters;
	}

	public virtual bool CanSpawnRemote(double serverTime)
	{
		if (numCritters < maxCritters)
		{
			return callLimiter.CheckCallServerTime(serverTime);
		}
		return false;
	}

	protected override void OnEnable()
	{
		base.OnEnable();
		CosmeticCritterManager.Instance.RegisterIndependentSpawner(this);
	}

	protected override void OnDisable()
	{
		base.OnDisable();
		CosmeticCritterManager.Instance.UnregisterIndependentSpawner(this);
	}
}
