using System;
using System.Collections.Generic;
using System.Linq;
using GorillaExtensions;
using GorillaGameModes;
using GorillaLocomotion;
using GorillaTag.CosmeticSystem;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Serialization;

namespace GorillaTag.Cosmetics;

public class CosmeticEffectsOnPlayers : MonoBehaviour, ISpawnable
{
	[Serializable]
	public enum TargetType
	{
		Owner,
		Others,
		All
	}

	[Serializable]
	public class CosmeticEffect
	{
		public GameModeType[] excludeForGameModes;

		public EFFECTTYPE effectType;

		public float effectDistanceRadius;

		public TargetType target = TargetType.All;

		public float effectDurationOthers;

		public float effectDurationOwner;

		public GorillaSkin newSkin;

		[Tooltip("Use object pools")]
		public GameObject knockbackVFX;

		[FormerlySerializedAs("knockbackStrengthMultiplier")]
		public float knockbackStrength;

		public bool applyScaleToKnockbackStrength;

		[Tooltip("force pushing players with hands on the ground")]
		public bool forceOffTheGround;

		[Tooltip("Take the horizontal magnitude of the knockback, and add it opposite gravity. For example, being hit sideways will also impart a large upwards force. Breaks conservation of energy, but feels better to the player.")]
		public bool specialVerticalForce;

		[FormerlySerializedAs("minStrengthClamp")]
		public float minKnockbackStrength = 0.5f;

		[FormerlySerializedAs("maxStrengthClamp")]
		public float maxKnockbackStrength = 6f;

		public AudioClip[] voiceOverrideNormalClips;

		public AudioClip[] voiceOverrideLoudClips;

		public float voiceOverrideNormalVolume = 0.5f;

		public float voiceOverrideLoudVolume = 0.8f;

		public float voiceOverrideLoudThreshold = 0.175f;

		[Tooltip("plays sfx on player")]
		public List<AudioClip> sfxAudioClip;

		[Tooltip("plays vfx on player, must be in the global object pool and have a tag.")]
		public GameObject VFXGameObject;

		private HashSet<GameModeType> modesHash;

		public float knockbackStrengthMultiplier { get; set; }

		public float EffectDuration
		{
			get
			{
				return effectDurationOthers;
			}
			set
			{
				effectDurationOthers = value;
			}
		}

		public float EffectStartedTime { get; set; }

		private HashSet<GameModeType> Modes
		{
			get
			{
				if (modesHash == null)
				{
					modesHash = new HashSet<GameModeType>(excludeForGameModes);
				}
				return modesHash;
			}
		}

		public bool IsGameModeAllowed()
		{
			GameModeType value = ((GameMode.ActiveGameMode != null) ? GameMode.ActiveGameMode.GameType() : GameModeType.Casual);
			if (Enumerable.Contains(excludeForGameModes, value))
			{
				return false;
			}
			return true;
		}

		private bool IsSkin()
		{
			return effectType == EFFECTTYPE.Skin;
		}

		private bool IsTagKnockback()
		{
			return effectType == EFFECTTYPE.TagWithKnockback;
		}

		private bool IsInstantKnockback()
		{
			return effectType == EFFECTTYPE.InstantKnockback;
		}

		private bool HasKnockback()
		{
			EFFECTTYPE eFFECTTYPE = effectType;
			return eFFECTTYPE == EFFECTTYPE.TagWithKnockback || eFFECTTYPE == EFFECTTYPE.InstantKnockback;
		}

		private bool IsVO()
		{
			return effectType == EFFECTTYPE.VoiceOverride;
		}

		private bool IsSFX()
		{
			return effectType == EFFECTTYPE.SFX;
		}

		private bool IsVFX()
		{
			return effectType == EFFECTTYPE.VFX;
		}
	}

	public enum EFFECTTYPE
	{
		Skin = 0,
		[Obsolete("FPV has been removed, do not use, use Stick Object To Player instead")]
		TagWithKnockback = 2,
		InstantKnockback = 3,
		VoiceOverride = 4,
		SFX = 5,
		VFX = 6
	}

	public CosmeticEffect[] allEffects = new CosmeticEffect[0];

	private VRRig myRig;

	private Dictionary<EFFECTTYPE, CosmeticEffect> allEffectsDict = new Dictionary<EFFECTTYPE, CosmeticEffect>();

	public bool IsSpawned { get; set; }

	public ECosmeticSelectSide CosmeticSelectedSide { get; set; }

	private bool ShouldAffectRig(VRRig rig, TargetType target)
	{
		bool flag = rig == myRig;
		return target switch
		{
			TargetType.Owner => flag, 
			TargetType.Others => !flag, 
			TargetType.All => true, 
			_ => false, 
		};
	}

	private void Awake()
	{
		CosmeticEffect[] array = allEffects;
		foreach (CosmeticEffect cosmeticEffect in array)
		{
			allEffectsDict.TryAdd(cosmeticEffect.effectType, cosmeticEffect);
		}
	}

	public void SetKnockbackStrengthMultiplier(float value)
	{
		foreach (KeyValuePair<EFFECTTYPE, CosmeticEffect> item in allEffectsDict)
		{
			item.Value.knockbackStrengthMultiplier = value;
		}
	}

	public void ApplyAllEffects()
	{
		ApplyAllEffectsByDistance(base.transform.position);
	}

	public void ApplyAllEffectsByDistance(Transform _transform)
	{
		ApplyAllEffectsByDistance(_transform.position);
	}

	public void ApplyAllEffectsByDistance(Vector3 position)
	{
		foreach (KeyValuePair<EFFECTTYPE, CosmeticEffect> item in allEffectsDict)
		{
			switch (item.Key)
			{
			case EFFECTTYPE.Skin:
				ApplySkinByDistance(item, position);
				break;
			case EFFECTTYPE.TagWithKnockback:
				ApplyTagWithKnockbackByDistance(item, position);
				break;
			case EFFECTTYPE.InstantKnockback:
				ApplyInstantKnockbackByDistance(item, position);
				break;
			case EFFECTTYPE.SFX:
				PlaySfxByDistance(item, position);
				break;
			case EFFECTTYPE.VFX:
				PlayVFXByDistance(item, position);
				break;
			}
		}
	}

	public void ApplyAllEffectsForRig(VRRig rig)
	{
		foreach (KeyValuePair<EFFECTTYPE, CosmeticEffect> item in allEffectsDict)
		{
			switch (item.Key)
			{
			case EFFECTTYPE.Skin:
				ApplySkinForRig(item, rig);
				break;
			case EFFECTTYPE.TagWithKnockback:
				ApplyTagWithKnockbackForRig(item, rig);
				break;
			case EFFECTTYPE.InstantKnockback:
				ApplyInstantKnockbackForRig(item, rig);
				break;
			case EFFECTTYPE.VoiceOverride:
				ApplyVOForRig(item, rig);
				break;
			case EFFECTTYPE.SFX:
				PlaySfxForRig(item, rig);
				break;
			case EFFECTTYPE.VFX:
				PlayVFXForRig(item, rig);
				break;
			}
		}
	}

	private void ApplySkinByDistance(KeyValuePair<EFFECTTYPE, CosmeticEffect> effect, Vector3 position)
	{
		if (!effect.Value.IsGameModeAllowed())
		{
			return;
		}
		effect.Value.EffectStartedTime = Time.time;
		IReadOnlyList<VRRig> readOnlyList2;
		if (!PhotonNetwork.InRoom)
		{
			IReadOnlyList<VRRig> readOnlyList = new VRRig[1] { GorillaTagger.Instance.offlineVRRig };
			readOnlyList2 = readOnlyList;
		}
		else
		{
			readOnlyList2 = VRRigCache.ActiveRigs;
		}
		foreach (VRRig item in readOnlyList2)
		{
			if (ShouldAffectRig(item, effect.Value.target) && (item.transform.position - position).IsShorterThan(effect.Value.effectDistanceRadius))
			{
				if (item == myRig)
				{
					effect.Value.EffectDuration = effect.Value.effectDurationOwner;
				}
				item.SpawnSkinEffects(effect);
			}
		}
	}

	private void ApplySkinForRig(KeyValuePair<EFFECTTYPE, CosmeticEffect> effect, VRRig vrRig)
	{
		if (effect.Value.IsGameModeAllowed() && ShouldAffectRig(vrRig, effect.Value.target))
		{
			effect.Value.EffectStartedTime = Time.time;
			if (vrRig == myRig)
			{
				effect.Value.EffectDuration = effect.Value.effectDurationOwner;
			}
			vrRig.SpawnSkinEffects(effect);
		}
	}

	private void ApplyTagWithKnockbackForRig(KeyValuePair<EFFECTTYPE, CosmeticEffect> effect, VRRig vrRig)
	{
		if (effect.Value.IsGameModeAllowed() && ShouldAffectRig(vrRig, effect.Value.target))
		{
			effect.Value.EffectStartedTime = Time.time;
			if (vrRig == myRig)
			{
				effect.Value.EffectDuration = effect.Value.effectDurationOwner;
			}
			vrRig.EnableHitWithKnockBack(effect);
		}
	}

	private void ApplyTagWithKnockbackByDistance(KeyValuePair<EFFECTTYPE, CosmeticEffect> effect, Vector3 position)
	{
		if (!effect.Value.IsGameModeAllowed())
		{
			return;
		}
		effect.Value.EffectStartedTime = Time.time;
		IReadOnlyList<VRRig> readOnlyList2;
		if (!PhotonNetwork.InRoom)
		{
			IReadOnlyList<VRRig> readOnlyList = new VRRig[1] { GorillaTagger.Instance.offlineVRRig };
			readOnlyList2 = readOnlyList;
		}
		else
		{
			readOnlyList2 = VRRigCache.ActiveRigs;
		}
		foreach (VRRig item in readOnlyList2)
		{
			if (ShouldAffectRig(item, effect.Value.target) && (item.transform.position - position).IsShorterThan(effect.Value.effectDistanceRadius))
			{
				if (item == myRig)
				{
					effect.Value.EffectDuration = effect.Value.effectDurationOwner;
				}
				item.EnableHitWithKnockBack(effect);
			}
		}
	}

	private void ApplyInstantKnockbackForRig(KeyValuePair<EFFECTTYPE, CosmeticEffect> effect, VRRig vrRig)
	{
		if (effect.Value.IsGameModeAllowed() && ShouldAffectRig(vrRig, effect.Value.target))
		{
			effect.Value.EffectStartedTime = Time.time;
			if (vrRig == myRig)
			{
				effect.Value.EffectDuration = effect.Value.effectDurationOwner;
			}
			Vector3 vector = vrRig.transform.position - base.transform.position;
			float num = (1f / vector.magnitude * effect.Value.knockbackStrength * effect.Value.knockbackStrengthMultiplier).ClampSafe(effect.Value.minKnockbackStrength, effect.Value.maxKnockbackStrength);
			if (effect.Value.applyScaleToKnockbackStrength)
			{
				num *= vrRig.scaleFactor;
			}
			RoomSystem.HitPlayer(vrRig.creator, vector.normalized, num);
			vrRig.ApplyInstanceKnockBack(effect);
		}
	}

	private void ApplyInstantKnockbackByDistance(KeyValuePair<EFFECTTYPE, CosmeticEffect> effect, Vector3 position)
	{
		if (!effect.Value.IsGameModeAllowed() || !ShouldAffectRig(GorillaTagger.Instance.offlineVRRig, effect.Value.target))
		{
			return;
		}
		effect.Value.EffectStartedTime = Time.time;
		if (GorillaTagger.Instance.offlineVRRig == myRig)
		{
			effect.Value.EffectDuration = effect.Value.effectDurationOwner;
		}
		Vector3 vector = GorillaTagger.Instance.offlineVRRig.transform.position - position;
		if (vector.IsShorterThan(effect.Value.effectDistanceRadius))
		{
			float magnitude = vector.magnitude;
			GTPlayer instance = GTPlayer.Instance;
			if (effect.Value.specialVerticalForce && (instance.IsHandTouching(isLeftHand: true) || instance.IsHandTouching(isLeftHand: false) || instance.BodyOnGround))
			{
				Vector3 vector2 = -Physics.gravity.normalized;
				Vector3 vector3 = Vector3.ProjectOnPlane(vector, vector2);
				vector = ((Vector3.Dot(vector / magnitude, vector2) > 0f) ? vector : vector3) + vector3.magnitude * vector2;
			}
			float num = (effect.Value.knockbackStrength * effect.Value.knockbackStrengthMultiplier / magnitude).ClampSafe(effect.Value.minKnockbackStrength, effect.Value.maxKnockbackStrength);
			if (effect.Value.applyScaleToKnockbackStrength)
			{
				num *= instance.scale;
			}
			instance.ApplyKnockback(vector.normalized, num, effect.Value.forceOffTheGround);
			GorillaTagger.Instance.offlineVRRig.ApplyInstanceKnockBack(effect);
		}
	}

	private void ApplyVOForRig(KeyValuePair<EFFECTTYPE, CosmeticEffect> effect, VRRig rig)
	{
		if (effect.Value.IsGameModeAllowed() && ShouldAffectRig(rig, effect.Value.target))
		{
			effect.Value.EffectStartedTime = Time.time;
			if (rig == myRig)
			{
				effect.Value.EffectDuration = effect.Value.effectDurationOwner;
			}
			rig.ActivateVOEffect(effect);
		}
	}

	private void PlaySfxForRig(KeyValuePair<EFFECTTYPE, CosmeticEffect> effect, VRRig vrRig)
	{
		if (effect.Value.IsGameModeAllowed() && ShouldAffectRig(vrRig, effect.Value.target))
		{
			effect.Value.EffectStartedTime = Time.time;
			if (vrRig == myRig)
			{
				effect.Value.EffectDuration = effect.Value.effectDurationOwner;
			}
			vrRig.PlayCosmeticEffectSFX(effect);
		}
	}

	private void PlaySfxByDistance(KeyValuePair<EFFECTTYPE, CosmeticEffect> effect, Vector3 position)
	{
		if (!effect.Value.IsGameModeAllowed())
		{
			return;
		}
		effect.Value.EffectStartedTime = Time.time;
		IReadOnlyList<VRRig> readOnlyList2;
		if (!PhotonNetwork.InRoom)
		{
			IReadOnlyList<VRRig> readOnlyList = new VRRig[1] { GorillaTagger.Instance.offlineVRRig };
			readOnlyList2 = readOnlyList;
		}
		else
		{
			readOnlyList2 = VRRigCache.ActiveRigs;
		}
		foreach (VRRig item in readOnlyList2)
		{
			if (ShouldAffectRig(item, effect.Value.target) && (item.transform.position - position).IsShorterThan(effect.Value.effectDistanceRadius))
			{
				if (item == myRig)
				{
					effect.Value.EffectDuration = effect.Value.effectDurationOwner;
				}
				item.PlayCosmeticEffectSFX(effect);
			}
		}
	}

	private void PlayVFXForRig(KeyValuePair<EFFECTTYPE, CosmeticEffect> effect, VRRig vrRig)
	{
		if (effect.Value.IsGameModeAllowed() && ShouldAffectRig(vrRig, effect.Value.target))
		{
			effect.Value.EffectStartedTime = Time.time;
			if (vrRig == myRig)
			{
				effect.Value.EffectDuration = effect.Value.effectDurationOwner;
			}
			vrRig.SpawnVFXEffect(effect);
		}
	}

	private void PlayVFXByDistance(KeyValuePair<EFFECTTYPE, CosmeticEffect> effect, Vector3 position)
	{
		if (!effect.Value.IsGameModeAllowed())
		{
			return;
		}
		effect.Value.EffectStartedTime = Time.time;
		IReadOnlyList<VRRig> readOnlyList2;
		if (!PhotonNetwork.InRoom)
		{
			IReadOnlyList<VRRig> readOnlyList = new VRRig[1] { GorillaTagger.Instance.offlineVRRig };
			readOnlyList2 = readOnlyList;
		}
		else
		{
			readOnlyList2 = VRRigCache.ActiveRigs;
		}
		foreach (VRRig item in readOnlyList2)
		{
			if (ShouldAffectRig(item, effect.Value.target) && (item.transform.position - position).IsShorterThan(effect.Value.effectDistanceRadius))
			{
				if (item == myRig)
				{
					effect.Value.EffectDuration = effect.Value.effectDurationOwner;
				}
				item.SpawnVFXEffect(effect);
			}
		}
	}

	public void OnSpawn(VRRig rig)
	{
		myRig = rig;
	}

	public void OnDespawn()
	{
	}
}
