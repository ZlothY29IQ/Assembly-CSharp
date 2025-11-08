using System;
using GorillaTag;
using GorillaTag.CosmeticSystem;
using UnityEngine;

public class HandEffectsOverrideCosmetic : MonoBehaviour, ISpawnable
{
	[Serializable]
	public class EffectsOverride
	{
		public GameObject effectVFX;

		public bool playHaptics;

		public float hapticStrength = 0.5f;

		public float hapticDuration = 0.5f;

		public bool parentEffect;
	}

	public enum HandEffectType
	{
		None,
		FistBump,
		HighFive
	}

	public HandEffectType handEffectType;

	public bool isLeftHand;

	public EffectsOverride firstPerson;

	public EffectsOverride thirdPerson;

	private VRRig _rig;

	public bool IsSpawned { get; set; }

	public ECosmeticSelectSide CosmeticSelectedSide { get; set; }

	public void OnSpawn(VRRig rig)
	{
		_rig = rig;
	}

	public void OnDespawn()
	{
	}

	public void OnEnable()
	{
		if (!isLeftHand)
		{
			_rig.CosmeticHandEffectsOverride_Right.Add(this);
		}
		else
		{
			_rig.CosmeticHandEffectsOverride_Left.Add(this);
		}
	}

	public void OnDisable()
	{
		if (!isLeftHand)
		{
			_rig.CosmeticHandEffectsOverride_Right.Remove(this);
		}
		else
		{
			_rig.CosmeticHandEffectsOverride_Left.Remove(this);
		}
	}
}
