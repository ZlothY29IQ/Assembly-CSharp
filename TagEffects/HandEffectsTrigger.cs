using System;
using GorillaExtensions;
using UnityEngine;

namespace TagEffects;

public class HandEffectsTrigger : MonoBehaviour, IHandEffectsTrigger
{
	[SerializeField]
	private float triggerRadius = 0.07f;

	[SerializeField]
	private bool rightHand;

	[SerializeField]
	private bool isStatic;

	private VRRig rig;

	public GorillaVelocityEstimator velocityEstimator;

	[SerializeField]
	private GameObject[] debugVisuals;

	private static HandEffectsOverrideCosmetic.HandEffectType[] mappingArray = new HandEffectsOverrideCosmetic.HandEffectType[4]
	{
		HandEffectsOverrideCosmetic.HandEffectType.None,
		HandEffectsOverrideCosmetic.HandEffectType.None,
		HandEffectsOverrideCosmetic.HandEffectType.HighFive,
		HandEffectsOverrideCosmetic.HandEffectType.FistBump
	};

	public bool Static => isStatic;

	public bool FingersDown
	{
		get
		{
			if (rig == null)
			{
				return false;
			}
			if (rightHand && rig.IsMakingFistRight())
			{
				return true;
			}
			if (!rightHand && rig.IsMakingFistLeft())
			{
				return true;
			}
			return false;
		}
	}

	public bool FingersUp
	{
		get
		{
			if (rig == null)
			{
				return false;
			}
			if (rightHand && rig.IsMakingFiveRight())
			{
				return true;
			}
			if (!rightHand && rig.IsMakingFiveLeft())
			{
				return true;
			}
			return false;
		}
	}

	public Vector3 Velocity
	{
		get
		{
			if (velocityEstimator != null && rig != null && rig.scaleFactor > 0.001f)
			{
				return velocityEstimator.linearVelocity / rig.scaleFactor;
			}
			return Vector3.zero;
		}
	}

	bool IHandEffectsTrigger.RightHand => rightHand;

	public Action<IHandEffectsTrigger.Mode> OnTrigger { get; set; }

	public IHandEffectsTrigger.Mode EffectMode { get; }

	public Transform Transform => base.transform;

	public VRRig Rig => rig;

	public TagEffectPack CosmeticEffectPack
	{
		get
		{
			if (rig == null)
			{
				return null;
			}
			return rig.CosmeticEffectPack;
		}
	}

	private void Awake()
	{
		rig = GetComponentInParent<VRRig>();
		if (velocityEstimator == null)
		{
			velocityEstimator = GetComponentInParent<GorillaVelocityEstimator>();
		}
		for (int i = 0; i < debugVisuals.Length; i++)
		{
			debugVisuals[i].SetActive(TagEffectsLibrary.DebugMode);
		}
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
		if (!(rig == other.Rig))
		{
			if (FingersDown && other.FingersDown && (other.Static || (Vector3.Dot(Vector3.Dot(Velocity, base.transform.up) * base.transform.up - Vector3.Dot(other.Velocity, other.Transform.up) * other.Transform.up, -other.Transform.up) > TagEffectsLibrary.FistBumpSpeedThreshold && Vector3.Dot(base.transform.up, other.Transform.up) < -0.01f)))
			{
				PlayHandEffects(TagEffectsLibrary.EffectType.FIST_BUMP, other);
			}
			if (FingersUp && other.FingersUp && (other.Static || Mathf.Abs(Vector3.Dot(Vector3.Dot(Velocity, base.transform.right) * base.transform.right - Vector3.Dot(other.Velocity, other.Transform.right) * other.Transform.right, other.Transform.right)) > TagEffectsLibrary.HighFiveSpeedThreshold))
			{
				PlayHandEffects(TagEffectsLibrary.EffectType.HIGH_FIVE, other);
			}
		}
	}

	private void PlayHandEffects(TagEffectsLibrary.EffectType effectType, IHandEffectsTrigger other)
	{
		if (rig.IsNull())
		{
			return;
		}
		bool flag = false;
		if (rig.isOfflineVRRig)
		{
			PlayerGameEvents.TriggerHandEffect(effectType.ToString());
		}
		if (OnTrigger != null || (other != null && other.OnTrigger != null))
		{
			switch (effectType)
			{
			case TagEffectsLibrary.EffectType.FIST_BUMP:
				OnTrigger?.Invoke(IHandEffectsTrigger.Mode.FistBump);
				other?.OnTrigger?.Invoke(IHandEffectsTrigger.Mode.FistBump);
				break;
			case TagEffectsLibrary.EffectType.HIGH_FIVE:
				OnTrigger?.Invoke(IHandEffectsTrigger.Mode.HighFive);
				other?.OnTrigger?.Invoke(IHandEffectsTrigger.Mode.HighFive);
				break;
			case TagEffectsLibrary.EffectType.FIRST_PERSON:
				OnTrigger?.Invoke(IHandEffectsTrigger.Mode.Tag1P);
				other?.OnTrigger?.Invoke(IHandEffectsTrigger.Mode.Tag1P);
				break;
			case TagEffectsLibrary.EffectType.THIRD_PERSON:
				OnTrigger?.Invoke(IHandEffectsTrigger.Mode.Tag3P);
				other?.OnTrigger?.Invoke(IHandEffectsTrigger.Mode.Tag3P);
				break;
			}
		}
		HandEffectsOverrideCosmetic handEffectsOverrideCosmetic = null;
		HandEffectsOverrideCosmetic handEffectsOverrideCosmetic2 = null;
		foreach (HandEffectsOverrideCosmetic item in rightHand ? rig.CosmeticHandEffectsOverride_Right : rig.CosmeticHandEffectsOverride_Left)
		{
			if (item.handEffectType == MapEnum(effectType))
			{
				handEffectsOverrideCosmetic2 = item;
				break;
			}
		}
		if (rig.isOfflineVRRig && GorillaTagger.Instance != null)
		{
			if ((bool)other.Rig)
			{
				foreach (HandEffectsOverrideCosmetic item2 in (other.Rig.CosmeticHandEffectsOverride_Right != null) ? other.Rig.CosmeticHandEffectsOverride_Right : other.Rig.CosmeticHandEffectsOverride_Left)
				{
					if (item2.handEffectType == MapEnum(effectType))
					{
						handEffectsOverrideCosmetic = item2;
						break;
					}
				}
				if ((bool)handEffectsOverrideCosmetic && handEffectsOverrideCosmetic.handEffectType == MapEnum(effectType) && ((!handEffectsOverrideCosmetic.isLeftHand && other.RightHand) || (handEffectsOverrideCosmetic.isLeftHand && !other.RightHand)))
				{
					if (handEffectsOverrideCosmetic.thirdPerson.playHaptics)
					{
						GorillaTagger.Instance.StartVibration(!rightHand, handEffectsOverrideCosmetic.thirdPerson.hapticStrength, handEffectsOverrideCosmetic.thirdPerson.hapticDuration);
					}
					TagEffectsLibrary.placeEffects(handEffectsOverrideCosmetic.thirdPerson.effectVFX, base.transform, rig.scaleFactor, flipZAxis: false, handEffectsOverrideCosmetic.thirdPerson.parentEffect, base.transform.rotation);
					flag = true;
				}
			}
			if ((bool)handEffectsOverrideCosmetic2 && handEffectsOverrideCosmetic2.handEffectType == MapEnum(effectType) && ((handEffectsOverrideCosmetic2.isLeftHand && !rightHand) || (!handEffectsOverrideCosmetic2.isLeftHand && rightHand)))
			{
				if (handEffectsOverrideCosmetic2.firstPerson.playHaptics)
				{
					GorillaTagger.Instance.StartVibration(!rightHand, handEffectsOverrideCosmetic2.firstPerson.hapticStrength, handEffectsOverrideCosmetic2.firstPerson.hapticDuration);
				}
				TagEffectsLibrary.placeEffects(handEffectsOverrideCosmetic2.firstPerson.effectVFX, other.Transform, rig.scaleFactor, flipZAxis: false, handEffectsOverrideCosmetic2.firstPerson.parentEffect, other.Transform.rotation);
				flag = true;
			}
		}
		if (!flag)
		{
			if (rig.isOfflineVRRig)
			{
				GorillaTagger.Instance.StartVibration(!rightHand, GorillaTagger.Instance.tapHapticStrength, GorillaTagger.Instance.tapHapticDuration);
			}
			TagEffectsLibrary.PlayEffect(base.transform, !rightHand, rig.scaleFactor, effectType, CosmeticEffectPack, other.CosmeticEffectPack, base.transform.rotation);
		}
	}

	public bool InTriggerZone(IHandEffectsTrigger t)
	{
		return (base.transform.position - t.Transform.position).IsShorterThan(triggerRadius * rig.scaleFactor);
	}

	private HandEffectsOverrideCosmetic.HandEffectType MapEnum(TagEffectsLibrary.EffectType oldEnum)
	{
		return mappingArray[(int)oldEnum];
	}
}
