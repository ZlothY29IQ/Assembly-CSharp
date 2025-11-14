using System;
using GorillaExtensions;
using TagEffects;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class HandEffectsTester : MonoBehaviour, IHandEffectsTrigger
{
	[SerializeField]
	private TagEffectPack cosmeticEffectPack;

	private Collider triggerZone;

	public IHandEffectsTrigger.Mode mode;

	[SerializeField]
	private float triggerRadius = 0.07f;

	[SerializeField]
	private bool isStatic = true;

	public bool Static => isStatic;

	Transform IHandEffectsTrigger.Transform => base.transform;

	VRRig IHandEffectsTrigger.Rig => null;

	IHandEffectsTrigger.Mode IHandEffectsTrigger.EffectMode => mode;

	bool IHandEffectsTrigger.FingersDown
	{
		get
		{
			if (mode == IHandEffectsTrigger.Mode.FistBump || mode == IHandEffectsTrigger.Mode.HighFive_And_FistBump)
			{
				return true;
			}
			return false;
		}
	}

	bool IHandEffectsTrigger.FingersUp
	{
		get
		{
			if (mode == IHandEffectsTrigger.Mode.HighFive || mode == IHandEffectsTrigger.Mode.HighFive_And_FistBump)
			{
				return true;
			}
			return false;
		}
	}

	public Action<IHandEffectsTrigger.Mode> OnTrigger { get; set; }

	public bool RightHand { get; }

	Vector3 IHandEffectsTrigger.Velocity
	{
		get
		{
			if (mode == IHandEffectsTrigger.Mode.HighFive)
			{
				return Vector3.zero;
			}
			_ = mode;
			_ = 1;
			return Vector3.zero;
		}
	}

	TagEffectPack IHandEffectsTrigger.CosmeticEffectPack => cosmeticEffectPack;

	private void Awake()
	{
		triggerZone = GetComponent<Collider>();
	}

	private void OnEnable()
	{
		if (!HandEffectsTriggerRegistry.HasInstance)
		{
			HandEffectsTriggerRegistry.FindInstance();
		}
		HandEffectsTriggerRegistry.Instance.Register(this);
	}

	private void OnDisable()
	{
		HandEffectsTriggerRegistry.Instance.Unregister(this);
	}

	public void OnTriggerEntered(IHandEffectsTrigger other)
	{
	}

	public bool InTriggerZone(IHandEffectsTrigger t)
	{
		if (!(base.transform.position - t.Transform.position).IsShorterThan(triggerZone.bounds.size))
		{
			return false;
		}
		RaycastHit hitInfo;
		switch (mode)
		{
		case IHandEffectsTrigger.Mode.HighFive_And_FistBump:
			if (!t.FingersUp || !triggerZone.Raycast(new Ray(t.Transform.position, t.Transform.right), out hitInfo, triggerRadius))
			{
				if (t.FingersDown)
				{
					return triggerZone.Raycast(new Ray(t.Transform.position, t.Transform.up), out hitInfo, triggerRadius);
				}
				return false;
			}
			return true;
		case IHandEffectsTrigger.Mode.FistBump:
			if (t.FingersDown)
			{
				return triggerZone.Raycast(new Ray(t.Transform.position, t.Transform.up), out hitInfo, triggerRadius);
			}
			return false;
		case IHandEffectsTrigger.Mode.HighFive:
			if (t.FingersUp)
			{
				return triggerZone.Raycast(new Ray(t.Transform.position, t.Transform.right), out hitInfo, triggerRadius);
			}
			return false;
		default:
			return triggerZone.Raycast(new Ray(t.Transform.position, triggerZone.bounds.center - t.Transform.position), out hitInfo, triggerRadius);
		}
	}
}
