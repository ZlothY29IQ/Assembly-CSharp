using System;
using UnityEngine;

public class SIGadgetHolsterDisk : SIGadget, I_SIDisruptable
{
	private enum State
	{
		Unequipped,
		OnCooldown,
		Ready
	}

	public SIGadget referenceGadget;

	public float cooldownTime;

	private State state;

	private float cooldownTimer;

	private SIGadgetGrenade grenadeGadget;

	private Rigidbody gadgetRB;

	private SIGadget cachedGadget;

	private Transform referenceTransform;

	private void Awake()
	{
		SetState(State.Unequipped);
		referenceGadget.gameObject.SetActive(value: false);
		referenceTransform = referenceGadget.transform;
		cooldownTimer = 0f;
	}

	private void Start()
	{
		CreateGadget();
	}

	private void CreateGadget()
	{
		gameEntity.manager.RequestCreateItem(referenceGadget.gameObject.name.GetStaticHash(), referenceGadget.transform.position, referenceGadget.transform.rotation, gameEntity.GetNetId());
	}

	public void RegisterGadget(SIGadget gadget)
	{
		cachedGadget = gadget;
		grenadeGadget = cachedGadget.GetComponent<SIGadgetGrenade>();
		gadgetRB = cachedGadget.GetComponent<Rigidbody>();
		SIGadgetGrenade sIGadgetGrenade = grenadeGadget;
		sIGadgetGrenade.GrenadeFinished = (Action)Delegate.Combine(sIGadgetGrenade.GrenadeFinished, new Action(GadgetRespawn));
		cachedGadget.gameObject.SetActive(value: false);
		GadgetRespawn();
	}

	private new void OnDisable()
	{
		if (grenadeGadget != null)
		{
			SIGadgetGrenade sIGadgetGrenade = grenadeGadget;
			sIGadgetGrenade.GrenadeFinished = (Action)Delegate.Remove(sIGadgetGrenade.GrenadeFinished, new Action(GadgetRespawn));
		}
	}

	protected override void OnUpdateAuthority(float dt)
	{
		base.OnUpdateAuthority(dt);
		switch (state)
		{
		case State.OnCooldown:
			cooldownTimer += dt;
			grenadeGadget.grenadeRenderer.material.SetFloat("_RespawnAmount", cooldownTimer / cooldownTime);
			if (cooldownTimer > cooldownTime)
			{
				SetState(State.Ready);
			}
			break;
		case State.Unequipped:
		case State.Ready:
			break;
		}
	}

	private void SetState(State newState)
	{
		if (state != newState)
		{
			state = newState;
			switch (state)
			{
			case State.Unequipped:
				cooldownTimer = 0f;
				break;
			case State.Ready:
				cachedGadget.gameEntity.pickupable = true;
				break;
			case State.OnCooldown:
				break;
			}
		}
	}

	public void DiskSnappedToHolster()
	{
		cachedGadget.gameObject.SetActive(value: true);
		gameEntity.pickupable = false;
		GadgetRespawn();
	}

	public void DiskRemovedFromHolster()
	{
		SetState(State.Unequipped);
		gameEntity.pickupable = true;
		cachedGadget.gameObject.SetActive(value: false);
	}

	public void GadgetRespawn()
	{
		cachedGadget.transform.parent = base.transform;
		cachedGadget.transform.localPosition = referenceTransform.localPosition;
		cachedGadget.transform.localRotation = referenceTransform.localRotation;
		cachedGadget.gameEntity.pickupable = false;
		gadgetRB.isKinematic = true;
		SetState(State.OnCooldown);
		cooldownTimer = 0f;
	}

	public void Disrupt(float disruptTime)
	{
		SetState(State.OnCooldown);
		cooldownTimer = 0f - disruptTime;
	}
}
