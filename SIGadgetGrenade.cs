using System;
using UnityEngine;

public abstract class SIGadgetGrenade : SIGadget
{
	public Action GrenadeFinished;

	public Renderer grenadeRenderer;

	[SerializeField]
	protected ThrownGadget thrownGadget;

	protected Rigidbody rb;

	protected GameEntity parentEntity;

	protected override void OnEnable()
	{
		base.OnEnable();
		rb = GetComponent<Rigidbody>();
		activatedLocally = false;
		thrownGadget.OnActivated += HandleActivated;
		thrownGadget.OnThrown += HandleThrown;
		thrownGadget.OnHitSurface += HandleHitSurface;
	}

	protected override void OnDisable()
	{
		thrownGadget.OnActivated -= HandleActivated;
		thrownGadget.OnThrown -= HandleThrown;
		thrownGadget.OnHitSurface -= HandleHitSurface;
		base.OnDisable();
	}

	protected abstract void HandleActivated();

	protected abstract void HandleThrown();

	protected abstract void HandleHitSurface();

	public override void OnEntityInit()
	{
		base.OnEntityInit();
		GameEntityId entityIdFromNetId = gameEntity.manager.GetEntityIdFromNetId((int)gameEntity.createData);
		parentEntity = gameEntity.manager.GetGameEntity(entityIdFromNetId);
		SIGadgetHolsterDisk component = parentEntity.GetComponent<SIGadgetHolsterDisk>();
		if (component != null)
		{
			component.RegisterGadget(this);
		}
	}
}
