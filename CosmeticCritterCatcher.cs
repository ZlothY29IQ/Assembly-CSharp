using UnityEngine;

public abstract class CosmeticCritterCatcher : CosmeticCritterHoldable
{
	[SerializeField]
	[Tooltip("If this catcher is capable of spawning immediately after catching, the linked spawner must be assigned here.")]
	protected CosmeticCritterSpawner optionalLinkedSpawner;

	public CosmeticCritterSpawner GetLinkedSpawner()
	{
		return optionalLinkedSpawner;
	}

	public abstract CosmeticCritterAction GetLocalCatchAction(CosmeticCritter critter);

	public virtual bool ValidateRemoteCatchAction(CosmeticCritter critter, CosmeticCritterAction catchAction, double serverTime)
	{
		return callLimiter.CheckCallServerTime(serverTime);
	}

	public abstract void OnCatch(CosmeticCritter critter, CosmeticCritterAction catchAction, double serverTime);

	protected override void OnEnable()
	{
		base.OnEnable();
		CosmeticCritterManager.Instance.RegisterCatcher(this);
	}

	protected override void OnDisable()
	{
		base.OnDisable();
		CosmeticCritterManager.Instance.UnregisterCatcher(this);
	}
}
