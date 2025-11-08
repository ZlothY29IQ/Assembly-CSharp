using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class CritterTemplate : ScriptableObject
{
	public CritterTemplate parent;

	[Space]
	[Header("Description")]
	public string temperament = "UNKNOWN";

	[Space]
	[Header("Behaviour")]
	[CritterTemplateParameter]
	public float maxJumpVel;

	[CritterTemplateParameter]
	public float jumpCooldown;

	[CritterTemplateParameter]
	public float scaredJumpCooldown;

	[CritterTemplateParameter]
	public float jumpVariabilityTime;

	[Space]
	[CritterTemplateParameter]
	public float visionConeAngle;

	[FormerlySerializedAs("visionConeHeight")]
	[CritterTemplateParameter]
	public float sensoryRange;

	[Space]
	[CritterTemplateParameter]
	public float maxHunger;

	[CritterTemplateParameter]
	public float hungryThreshold;

	[CritterTemplateParameter]
	public float satiatedThreshold;

	[CritterTemplateParameter]
	public float hungerLostPerSecond;

	[CritterTemplateParameter]
	public float hungerGainedPerSecond;

	[Space]
	[CritterTemplateParameter]
	public float maxFear;

	[CritterTemplateParameter]
	public float scaredThreshold;

	[CritterTemplateParameter]
	public float calmThreshold;

	[CritterTemplateParameter]
	public float fearLostPerSecond;

	[Space]
	[CritterTemplateParameter]
	public float maxAttraction;

	[CritterTemplateParameter]
	public float attractedThreshold;

	[CritterTemplateParameter]
	public float unattractedThreshold;

	[CritterTemplateParameter]
	public float attractionLostPerSecond;

	[Space]
	[CritterTemplateParameter]
	public float maxSleepiness;

	[CritterTemplateParameter]
	public float tiredThreshold;

	[CritterTemplateParameter]
	public float awakeThreshold;

	[CritterTemplateParameter]
	public float sleepinessGainedPerSecond;

	[CritterTemplateParameter]
	public float sleepinessLostPerSecond;

	[Space]
	[CritterTemplateParameter]
	public float struggleGainedPerSecond;

	[CritterTemplateParameter]
	public float maxStruggle;

	[CritterTemplateParameter]
	public float escapeThreshold;

	[CritterTemplateParameter]
	public float catchableThreshold;

	[CritterTemplateParameter]
	public float struggleLostPerSecond;

	[Space]
	[CritterTemplateParameter]
	public float lifeTime;

	[Space]
	public List<crittersAttractorStruct> attractedToList;

	public List<crittersAttractorStruct> afraidOfList;

	[Space]
	[Header("Visual")]
	[CritterTemplateParameter]
	public float minSize;

	[CritterTemplateParameter]
	public float maxSize;

	[CritterTemplateParameter]
	public float hatChance;

	public GameObject[] hats;

	[Space]
	[Header("Behaviour FX")]
	public GameObject eatingStartFX;

	public GameObject eatingOngoingFX;

	public CrittersAnim eatingAnim;

	public GameObject fearStartFX;

	public GameObject fearOngoingFX;

	public CrittersAnim fearAnim;

	public GameObject attractionStartFX;

	public GameObject attractionOngoingFX;

	public CrittersAnim attractionAnim;

	public GameObject sleepStartFX;

	public GameObject sleepOngoingFX;

	public CrittersAnim sleepAnim;

	public GameObject grabbedStartFX;

	public GameObject grabbedOngoingFX;

	public GameObject grabbedStopFX;

	public CrittersAnim grabbedAnim;

	public GameObject hungryStartFX;

	public GameObject hungryOngoingFX;

	public CrittersAnim hungryAnim;

	public GameObject spawningStartFX;

	public GameObject spawningOngoingFX;

	public CrittersAnim spawningAnim;

	public GameObject despawningStartFX;

	public GameObject despawningOngoingFX;

	public CrittersAnim despawningAnim;

	public GameObject capturedStartFX;

	public GameObject capturedOngoingFX;

	public CrittersAnim capturedAnim;

	public GameObject stunnedStartFX;

	public GameObject stunnedOngoingFX;

	public CrittersAnim stunnedAnim;

	public AudioClip grabbedStruggleHaptics;

	public float grabbedStruggleHapticsStrength;

	private Dictionary<string, object> modifiedValues = new Dictionary<string, object>();

	private string HapticsBlurb
	{
		get
		{
			float num = grabbedStruggleHaptics.GetPeakMagnitude() * grabbedStruggleHapticsStrength;
			float num2 = grabbedStruggleHaptics.GetRMSMagnitude() * grabbedStruggleHapticsStrength;
			return $"Peak Strength: {num:0.##} Mean Strength: {num2:0.##}";
		}
	}

	private void SetMaxStrength(float maxStrength = 1f)
	{
		float peakMagnitude = grabbedStruggleHaptics.GetPeakMagnitude();
		Debug.Log($"Clip {grabbedStruggleHaptics} max strength: {peakMagnitude}");
		if (peakMagnitude > 0f)
		{
			grabbedStruggleHapticsStrength = maxStrength / peakMagnitude;
		}
	}

	private void SetMeanStrength(float meanStrength = 1f)
	{
		float rMSMagnitude = grabbedStruggleHaptics.GetRMSMagnitude();
		Debug.Log($"Clip {grabbedStruggleHaptics} mean strength: {rMSMagnitude}");
		if (meanStrength > 0f)
		{
			grabbedStruggleHapticsStrength = meanStrength / rMSMagnitude;
		}
	}

	private void OnValidate()
	{
		modifiedValues.Clear();
		RegisterModifiedBehaviour();
		RegisterModifiedVisual();
	}

	private void OnEnable()
	{
		OnValidate();
	}

	private void RegisterModifiedBehaviour()
	{
		if (maxJumpVel != 0f)
		{
			modifiedValues.Add("maxJumpVel", maxJumpVel);
		}
		if (jumpCooldown != 0f)
		{
			modifiedValues.Add("jumpCooldown", jumpCooldown);
		}
		if (scaredJumpCooldown != 0f)
		{
			modifiedValues.Add("scaredJumpCooldown", scaredJumpCooldown);
		}
		if (jumpVariabilityTime != 0f)
		{
			modifiedValues.Add("jumpVariabilityTime", jumpVariabilityTime);
		}
		if (visionConeAngle != 0f)
		{
			modifiedValues.Add("visionConeAngle", visionConeAngle);
		}
		if (sensoryRange != 0f)
		{
			modifiedValues.Add("sensoryRange", sensoryRange);
		}
		if (maxHunger != 0f)
		{
			modifiedValues.Add("maxHunger", maxHunger);
		}
		if (hungryThreshold != 0f)
		{
			modifiedValues.Add("hungryThreshold", hungryThreshold);
		}
		if (satiatedThreshold != 0f)
		{
			modifiedValues.Add("satiatedThreshold", satiatedThreshold);
		}
		if (hungerLostPerSecond != 0f)
		{
			modifiedValues.Add("hungerLostPerSecond", hungerLostPerSecond);
		}
		if (hungerGainedPerSecond != 0f)
		{
			modifiedValues.Add("hungerGainedPerSecond", hungerGainedPerSecond);
		}
		if (maxFear != 0f)
		{
			modifiedValues.Add("maxFear", maxFear);
		}
		if (scaredThreshold != 0f)
		{
			modifiedValues.Add("scaredThreshold", scaredThreshold);
		}
		if (calmThreshold != 0f)
		{
			modifiedValues.Add("calmThreshold", calmThreshold);
		}
		if (fearLostPerSecond != 0f)
		{
			modifiedValues.Add("fearLostPerSecond", fearLostPerSecond);
		}
		if (maxAttraction != 0f)
		{
			modifiedValues.Add("maxAttraction", maxAttraction);
		}
		if (attractedThreshold != 0f)
		{
			modifiedValues.Add("attractedThreshold", attractedThreshold);
		}
		if (unattractedThreshold != 0f)
		{
			modifiedValues.Add("unattractedThreshold", unattractedThreshold);
		}
		if (attractionLostPerSecond != 0f)
		{
			modifiedValues.Add("attractionLostPerSecond", attractionLostPerSecond);
		}
		if (maxSleepiness != 0f)
		{
			modifiedValues.Add("maxSleepiness", maxSleepiness);
		}
		if (tiredThreshold != 0f)
		{
			modifiedValues.Add("tiredThreshold", tiredThreshold);
		}
		if (awakeThreshold != 0f)
		{
			modifiedValues.Add("awakeThreshold", awakeThreshold);
		}
		if (sleepinessGainedPerSecond != 0f)
		{
			modifiedValues.Add("sleepinessGainedPerSecond", sleepinessGainedPerSecond);
		}
		if (sleepinessLostPerSecond != 0f)
		{
			modifiedValues.Add("sleepinessLostPerSecond", sleepinessLostPerSecond);
		}
		if (maxStruggle != 0f)
		{
			modifiedValues.Add("maxStruggle", maxStruggle);
		}
		if (escapeThreshold != 0f)
		{
			modifiedValues.Add("escapeThreshold", escapeThreshold);
		}
		if (catchableThreshold != 0f)
		{
			modifiedValues.Add("catchableThreshold", catchableThreshold);
		}
		if (struggleGainedPerSecond != 0f)
		{
			modifiedValues.Add("struggleGainedPerSecond", struggleGainedPerSecond);
		}
		if (struggleLostPerSecond != 0f)
		{
			modifiedValues.Add("struggleLostPerSecond", struggleLostPerSecond);
		}
		if (afraidOfList != null)
		{
			modifiedValues.Add("afraidOfList", afraidOfList);
		}
		if (attractedToList != null)
		{
			modifiedValues.Add("attractedToList", attractedToList);
		}
		if (lifeTime != 0f)
		{
			modifiedValues.Add("lifeTime", lifeTime);
		}
	}

	private void RegisterModifiedVisual()
	{
		if (hatChance != 0f)
		{
			modifiedValues.Add("hatChance", hatChance);
		}
		if (hats != null && hats.Length != 0)
		{
			modifiedValues.Add("hats", hats);
		}
		if (minSize != 0f)
		{
			modifiedValues.Add("minSize", minSize);
		}
		if (maxSize != 0f)
		{
			modifiedValues.Add("maxSize", maxSize);
		}
		if (eatingStartFX != null)
		{
			modifiedValues.Add("eatingStartFX", eatingStartFX);
		}
		if (eatingOngoingFX != null)
		{
			modifiedValues.Add("eatingOngoingFX", eatingOngoingFX);
		}
		if (CrittersAnim.IsModified(eatingAnim))
		{
			modifiedValues.Add("eatingAnim", eatingAnim);
		}
		if (fearStartFX != null)
		{
			modifiedValues.Add("fearStartFX", fearStartFX);
		}
		if (fearOngoingFX != null)
		{
			modifiedValues.Add("fearOngoingFX", fearOngoingFX);
		}
		if (CrittersAnim.IsModified(fearAnim))
		{
			modifiedValues.Add("fearAnim", fearAnim);
		}
		if (attractionStartFX != null)
		{
			modifiedValues.Add("attractionStartFX", attractionStartFX);
		}
		if (attractionOngoingFX != null)
		{
			modifiedValues.Add("attractionOngoingFX", attractionOngoingFX);
		}
		if (CrittersAnim.IsModified(attractionAnim))
		{
			modifiedValues.Add("attractionAnim", attractionAnim);
		}
		if (sleepStartFX != null)
		{
			modifiedValues.Add("sleepStartFX", sleepStartFX);
		}
		if (sleepOngoingFX != null)
		{
			modifiedValues.Add("sleepOngoingFX", sleepOngoingFX);
		}
		if (CrittersAnim.IsModified(sleepAnim))
		{
			modifiedValues.Add("sleepAnim", sleepAnim);
		}
		if (grabbedStartFX != null)
		{
			modifiedValues.Add("grabbedStartFX", grabbedStartFX);
		}
		if (grabbedOngoingFX != null)
		{
			modifiedValues.Add("grabbedOngoingFX", grabbedOngoingFX);
		}
		if (grabbedStopFX != null)
		{
			modifiedValues.Add("grabbedStopFX", grabbedStopFX);
		}
		if (CrittersAnim.IsModified(grabbedAnim))
		{
			modifiedValues.Add("grabbedAnim", grabbedAnim);
		}
		if (hungryStartFX != null)
		{
			modifiedValues.Add("hungryStartFX", hungryStartFX);
		}
		if (hungryOngoingFX != null)
		{
			modifiedValues.Add("hungryOngoingFX", hungryOngoingFX);
		}
		if (CrittersAnim.IsModified(hungryAnim))
		{
			modifiedValues.Add("hungryAnim", hungryAnim);
		}
		if (despawningStartFX != null)
		{
			modifiedValues.Add("despawningStartFX", despawningStartFX);
		}
		if (despawningOngoingFX != null)
		{
			modifiedValues.Add("despawningOngoingFX", despawningOngoingFX);
		}
		if (CrittersAnim.IsModified(despawningAnim))
		{
			modifiedValues.Add("despawningAnim", despawningAnim);
		}
		if (spawningStartFX != null)
		{
			modifiedValues.Add("spawningStartFX", spawningStartFX);
		}
		if (spawningOngoingFX != null)
		{
			modifiedValues.Add("spawningOngoingFX", spawningOngoingFX);
		}
		if (CrittersAnim.IsModified(spawningAnim))
		{
			modifiedValues.Add("spawningAnim", spawningAnim);
		}
		if (capturedStartFX != null)
		{
			modifiedValues.Add("capturedStartFX", capturedStartFX);
		}
		if (capturedOngoingFX != null)
		{
			modifiedValues.Add("capturedOngoingFX", capturedOngoingFX);
		}
		if (CrittersAnim.IsModified(capturedAnim))
		{
			modifiedValues.Add("capturedAnim", capturedAnim);
		}
		if (stunnedStartFX != null)
		{
			modifiedValues.Add("stunnedStartFX", stunnedStartFX);
		}
		if (stunnedOngoingFX != null)
		{
			modifiedValues.Add("stunnedOngoingFX", stunnedOngoingFX);
		}
		if (CrittersAnim.IsModified(stunnedAnim))
		{
			modifiedValues.Add("stunnedAnim", stunnedAnim);
		}
		if (grabbedStruggleHaptics != null)
		{
			modifiedValues.Add("grabbedStruggleHaptics", grabbedStruggleHaptics);
		}
		if (grabbedStruggleHapticsStrength != 0f)
		{
			modifiedValues.Add("grabbedStruggleHapticsStrength", grabbedStruggleHapticsStrength);
		}
	}

	public bool IsValueModified(string valueName)
	{
		return modifiedValues.ContainsKey(valueName);
	}

	public T GetParentValue<T>(string valueName)
	{
		if (parent != null)
		{
			return parent.GetTemplateValue<T>(valueName);
		}
		return default(T);
	}

	public T GetTemplateValue<T>(string valueName)
	{
		if (modifiedValues.TryGetValue(valueName, out var value))
		{
			return (T)value;
		}
		if (parent != null)
		{
			return parent.GetTemplateValue<T>(valueName);
		}
		return default(T);
	}

	public void ApplyToCritter(CrittersPawn critter)
	{
		ApplyBehaviour(critter);
		ApplyBehaviourFX(critter);
	}

	private void ApplyBehaviour(CrittersPawn critter)
	{
		critter.maxJumpVel = GetTemplateValue<float>("maxJumpVel");
		critter.jumpCooldown = GetTemplateValue<float>("jumpCooldown");
		critter.scaredJumpCooldown = GetTemplateValue<float>("scaredJumpCooldown");
		critter.jumpVariabilityTime = GetTemplateValue<float>("jumpVariabilityTime");
		critter.visionConeAngle = GetTemplateValue<float>("visionConeAngle");
		critter.sensoryRange = GetTemplateValue<float>("sensoryRange");
		critter.maxHunger = GetTemplateValue<float>("maxHunger");
		critter.hungryThreshold = GetTemplateValue<float>("hungryThreshold");
		critter.satiatedThreshold = GetTemplateValue<float>("satiatedThreshold");
		critter.hungerLostPerSecond = GetTemplateValue<float>("hungerLostPerSecond");
		critter.hungerGainedPerSecond = GetTemplateValue<float>("hungerGainedPerSecond");
		critter.maxFear = GetTemplateValue<float>("maxFear");
		critter.scaredThreshold = GetTemplateValue<float>("scaredThreshold");
		critter.calmThreshold = GetTemplateValue<float>("calmThreshold");
		critter.fearLostPerSecond = GetTemplateValue<float>("fearLostPerSecond");
		critter.maxAttraction = GetTemplateValue<float>("maxAttraction");
		critter.attractedThreshold = GetTemplateValue<float>("attractedThreshold");
		critter.unattractedThreshold = GetTemplateValue<float>("unattractedThreshold");
		critter.attractionLostPerSecond = GetTemplateValue<float>("attractionLostPerSecond");
		critter.maxSleepiness = GetTemplateValue<float>("maxSleepiness");
		critter.tiredThreshold = GetTemplateValue<float>("tiredThreshold");
		critter.awakeThreshold = GetTemplateValue<float>("awakeThreshold");
		critter.sleepinessGainedPerSecond = GetTemplateValue<float>("sleepinessGainedPerSecond");
		critter.sleepinessLostPerSecond = GetTemplateValue<float>("sleepinessLostPerSecond");
		critter.maxStruggle = GetTemplateValue<float>("maxStruggle");
		critter.escapeThreshold = GetTemplateValue<float>("escapeThreshold");
		critter.catchableThreshold = GetTemplateValue<float>("catchableThreshold");
		critter.struggleGainedPerSecond = GetTemplateValue<float>("struggleGainedPerSecond");
		critter.struggleLostPerSecond = GetTemplateValue<float>("struggleLostPerSecond");
		critter.lifeTime = GetTemplateValue<float>("lifeTime");
		critter.attractedToList = GetTemplateValue<List<crittersAttractorStruct>>("attractedToList");
		critter.afraidOfList = GetTemplateValue<List<crittersAttractorStruct>>("afraidOfList");
	}

	private void ApplyBehaviourFX(CrittersPawn critter)
	{
		critter.StartStateFX.Clear();
		critter.OngoingStateFX.Clear();
		critter.stateAnim.Clear();
		critter.StartStateFX.Add(CrittersPawn.CreatureState.Eating, GetTemplateValue<GameObject>("eatingStartFX"));
		critter.OngoingStateFX.Add(CrittersPawn.CreatureState.Eating, GetTemplateValue<GameObject>("eatingOngoingFX"));
		critter.stateAnim.Add(CrittersPawn.CreatureState.Eating, GetTemplateValue<CrittersAnim>("eatingAnim"));
		critter.StartStateFX.Add(CrittersPawn.CreatureState.Running, GetTemplateValue<GameObject>("fearStartFX"));
		critter.OngoingStateFX.Add(CrittersPawn.CreatureState.Running, GetTemplateValue<GameObject>("fearOngoingFX"));
		critter.stateAnim.Add(CrittersPawn.CreatureState.Running, GetTemplateValue<CrittersAnim>("fearAnim"));
		critter.StartStateFX.Add(CrittersPawn.CreatureState.AttractedTo, GetTemplateValue<GameObject>("attractionStartFX"));
		critter.OngoingStateFX.Add(CrittersPawn.CreatureState.AttractedTo, GetTemplateValue<GameObject>("attractionOngoingFX"));
		critter.stateAnim.Add(CrittersPawn.CreatureState.AttractedTo, GetTemplateValue<CrittersAnim>("attractionAnim"));
		critter.StartStateFX.Add(CrittersPawn.CreatureState.Sleeping, GetTemplateValue<GameObject>("sleepStartFX"));
		critter.OngoingStateFX.Add(CrittersPawn.CreatureState.Sleeping, GetTemplateValue<GameObject>("sleepOngoingFX"));
		critter.stateAnim.Add(CrittersPawn.CreatureState.Sleeping, GetTemplateValue<CrittersAnim>("sleepAnim"));
		critter.StartStateFX.Add(CrittersPawn.CreatureState.Grabbed, GetTemplateValue<GameObject>("grabbedStartFX"));
		critter.OngoingStateFX.Add(CrittersPawn.CreatureState.Grabbed, GetTemplateValue<GameObject>("grabbedOngoingFX"));
		critter.OnReleasedFX = GetTemplateValue<GameObject>("grabbedStopFX");
		critter.stateAnim.Add(CrittersPawn.CreatureState.Grabbed, GetTemplateValue<CrittersAnim>("grabbedAnim"));
		critter.StartStateFX.Add(CrittersPawn.CreatureState.SeekingFood, GetTemplateValue<GameObject>("hungryStartFX"));
		critter.OngoingStateFX.Add(CrittersPawn.CreatureState.SeekingFood, GetTemplateValue<GameObject>("hungryOngoingFX"));
		critter.stateAnim.Add(CrittersPawn.CreatureState.SeekingFood, GetTemplateValue<CrittersAnim>("hungryAnim"));
		critter.StartStateFX.Add(CrittersPawn.CreatureState.Despawning, GetTemplateValue<GameObject>("despawningStartFX"));
		critter.OngoingStateFX.Add(CrittersPawn.CreatureState.Despawning, GetTemplateValue<GameObject>("despawningOngoingFX"));
		critter.stateAnim.Add(CrittersPawn.CreatureState.Despawning, GetTemplateValue<CrittersAnim>("despawningAnim"));
		critter.StartStateFX.Add(CrittersPawn.CreatureState.Spawning, GetTemplateValue<GameObject>("spawningStartFX"));
		critter.OngoingStateFX.Add(CrittersPawn.CreatureState.Spawning, GetTemplateValue<GameObject>("spawningOngoingFX"));
		critter.stateAnim.Add(CrittersPawn.CreatureState.Spawning, GetTemplateValue<CrittersAnim>("spawningAnim"));
		critter.StartStateFX.Add(CrittersPawn.CreatureState.Captured, GetTemplateValue<GameObject>("capturedStartFX"));
		critter.OngoingStateFX.Add(CrittersPawn.CreatureState.Captured, GetTemplateValue<GameObject>("capturedOngoingFX"));
		critter.stateAnim.Add(CrittersPawn.CreatureState.Captured, GetTemplateValue<CrittersAnim>("capturedAnim"));
		critter.StartStateFX.Add(CrittersPawn.CreatureState.Stunned, GetTemplateValue<GameObject>("stunnedStartFX"));
		critter.OngoingStateFX.Add(CrittersPawn.CreatureState.Stunned, GetTemplateValue<GameObject>("stunnedOngoingFX"));
		critter.stateAnim.Add(CrittersPawn.CreatureState.Stunned, GetTemplateValue<CrittersAnim>("stunnedAnim"));
		critter.grabbedHaptics = GetTemplateValue<AudioClip>("grabbedStruggleHaptics");
		critter.grabbedHapticsStrength = GetTemplateValue<float>("grabbedStruggleHapticsStrength");
	}
}
