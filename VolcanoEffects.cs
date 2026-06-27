using System;
using System.Collections;
using System.Collections.Generic;
using GorillaExtensions;
using UnityEngine;
using UnityEngine.Serialization;

public class VolcanoEffects : MonoBehaviour
{
	[Serializable]
	public class LavaStateFX
	{
		public AudioClip startSound;

		public AudioSource startSoundAudioSrc;

		[Tooltip("Multiplied by the AudioSource's volume.")]
		public float startSoundVol = 1f;

		[FormerlySerializedAs("startSoundPad")]
		public float startSoundDelay;

		public AudioClip endSound;

		public AudioSource endSoundAudioSrc;

		[Tooltip("Multiplied by the AudioSource's volume.")]
		public float endSoundVol = 1f;

		[Tooltip("How much time should there be between the end of the clip playing and the end of the state.")]
		public float endSoundPadTime;

		public AudioSource loop1AudioSrc;

		public AnimationCurve loop1VolAnim;

		public AudioSource loop2AudioSrc;

		public AnimationCurve loop2VolAnim;

		public AnimationCurve lavaSpewEmissionAnim;

		public AnimationCurve smokeEmissionAnim;

		public Gradient smokeStartColorAnim;

		public Gradient lavaLightColor;

		public AnimationCurve lavaLightIntensityAnim = AnimationCurve.Constant(0f, 1f, 60f);

		public AnimationCurve lavaLightAttenuationAnim = AnimationCurve.Constant(0f, 1f, 0.1f);

		[NonSerialized]
		public bool startSoundExists;

		[NonSerialized]
		public bool startSoundPlayed;

		[NonSerialized]
		public bool endSoundExists;

		[NonSerialized]
		public bool endSoundPlayed;

		[NonSerialized]
		public bool loop1Exists;

		[NonSerialized]
		public float loop1DefaultVolume;

		[NonSerialized]
		public bool loop2Exists;

		[NonSerialized]
		public float loop2DefaultVolume;
	}

	[Tooltip("Only one VolcanoEffects should change shader globals in the scene (lava color, lava light) at a time.")]
	[SerializeField]
	private bool applyShaderGlobals = true;

	[Tooltip("Game trigger notification sounds will play through this.")]
	[SerializeField]
	private AudioSource forestSpeakerAudioSrc;

	[Tooltip("The accumulator value of rocks being thrown into the volcano has been reset.")]
	[SerializeField]
	private AudioClip warnVolcanoBellyEmptied;

	[Tooltip("Accept stone sounds will play through here.")]
	[SerializeField]
	private AudioSource volcanoAudioSource;

	[Tooltip("volcano ate rock but needs more.")]
	[SerializeField]
	private AudioClip volcanoAcceptStone;

	[Tooltip("volcano ate last needed rock.")]
	[SerializeField]
	private AudioClip volcanoAcceptLastStone;

	[Tooltip("This will be faded in while lava is rising.")]
	[SerializeField]
	private AudioSource[] lavaSurfaceAudioSrcs;

	[Tooltip("Emission will be adjusted for these particles during eruption.")]
	[SerializeField]
	private ParticleSystem[] lavaSpewParticleSystems;

	[Tooltip("Smoke emits during all states but it's intensity and color will change when erupting/idling.")]
	[SerializeField]
	private ParticleSystem[] smokeParticleSystems;

	[SerializeField]
	private LavaStateFX drainedStateFX;

	[SerializeField]
	private LavaStateFX eruptingStateFX;

	[SerializeField]
	private LavaStateFX risingStateFX;

	[SerializeField]
	private LavaStateFX fullStateFX;

	[SerializeField]
	private LavaStateFX drainingStateFX;

	private LavaStateFX currentStateFX;

	private ParticleSystem.EmissionModule[] lavaSpewEmissionModules;

	private float[] lavaSpewEmissionDefaultRateMultipliers;

	private ParticleSystem.Burst[][] lavaSpewDefaultEmitBursts;

	private ParticleSystem.Burst[][] lavaSpewAdjustedEmitBursts;

	private ParticleSystem.MainModule[] smokeMainModules;

	private ParticleSystem.EmissionModule[] smokeEmissionModules;

	private float[] smokeEmissionDefaultRateMultipliers;

	private readonly int shaderProp_ZoneLiquidLightColor = Shader.PropertyToID("_ZoneLiquidLightColor");

	private readonly int shaderProp_ZoneLiquidLightDistScale = Shader.PropertyToID("_ZoneLiquidLightDistScale");

	private float timeVolcanoBellyWasLastEmpty;

	private bool hasVolcanoAudioSrc;

	private bool hasForestSpeakerAudioSrc;

	private Coroutine prewarmCoroutine;

	private void Awake()
	{
		if (RemoveNullsFromArray(ref lavaSpewParticleSystems))
		{
			LogNullsFoundInArray("lavaSpewParticleSystems");
		}
		if (RemoveNullsFromArray(ref smokeParticleSystems))
		{
			LogNullsFoundInArray("smokeParticleSystems");
		}
		hasVolcanoAudioSrc = volcanoAudioSource != null;
		hasForestSpeakerAudioSrc = forestSpeakerAudioSrc != null;
		lavaSpewEmissionModules = new ParticleSystem.EmissionModule[lavaSpewParticleSystems.Length];
		lavaSpewEmissionDefaultRateMultipliers = new float[lavaSpewParticleSystems.Length];
		lavaSpewDefaultEmitBursts = new ParticleSystem.Burst[lavaSpewParticleSystems.Length][];
		lavaSpewAdjustedEmitBursts = new ParticleSystem.Burst[lavaSpewParticleSystems.Length][];
		for (int i = 0; i < lavaSpewParticleSystems.Length; i++)
		{
			ParticleSystem.EmissionModule emission = lavaSpewParticleSystems[i].emission;
			lavaSpewEmissionDefaultRateMultipliers[i] = emission.rateOverTimeMultiplier;
			lavaSpewDefaultEmitBursts[i] = new ParticleSystem.Burst[emission.burstCount];
			lavaSpewAdjustedEmitBursts[i] = new ParticleSystem.Burst[emission.burstCount];
			for (int j = 0; j < emission.burstCount; j++)
			{
				ParticleSystem.Burst burst = emission.GetBurst(j);
				lavaSpewDefaultEmitBursts[i][j] = burst;
				lavaSpewAdjustedEmitBursts[i][j] = new ParticleSystem.Burst(burst.time, burst.minCount, burst.maxCount, burst.cycleCount, burst.repeatInterval);
				lavaSpewAdjustedEmitBursts[i][j].count = burst.count;
			}
			lavaSpewEmissionModules[i] = emission;
		}
		smokeMainModules = new ParticleSystem.MainModule[smokeParticleSystems.Length];
		smokeEmissionModules = new ParticleSystem.EmissionModule[smokeParticleSystems.Length];
		smokeEmissionDefaultRateMultipliers = new float[smokeParticleSystems.Length];
		for (int k = 0; k < smokeParticleSystems.Length; k++)
		{
			smokeMainModules[k] = smokeParticleSystems[k].main;
			smokeEmissionModules[k] = smokeParticleSystems[k].emission;
			smokeEmissionDefaultRateMultipliers[k] = smokeEmissionModules[k].rateOverTimeMultiplier;
		}
		InitState(drainedStateFX);
		InitState(eruptingStateFX);
		InitState(risingStateFX);
		InitState(fullStateFX);
		InitState(drainingStateFX);
		currentStateFX = drainedStateFX;
		UpdateDrainedState(0f);
	}

	public void PreloadAssets()
	{
		PreloadClip(warnVolcanoBellyEmptied);
		PreloadClip(volcanoAcceptStone);
		PreloadClip(volcanoAcceptLastStone);
		PreloadStateFXClips(drainedStateFX);
		PreloadStateFXClips(eruptingStateFX);
		PreloadStateFXClips(risingStateFX);
		PreloadStateFXClips(fullStateFX);
		PreloadStateFXClips(drainingStateFX);
		WarmUpAudioSourceGO(forestSpeakerAudioSrc);
		WarmUpAudioSourceGO(volcanoAudioSource);
		WarmUpStateFXSources(drainedStateFX);
		WarmUpStateFXSources(eruptingStateFX);
		WarmUpStateFXSources(risingStateFX);
		WarmUpStateFXSources(fullStateFX);
		WarmUpStateFXSources(drainingStateFX);
		for (int i = 0; i < lavaSurfaceAudioSrcs.Length; i++)
		{
			WarmUpAudioSourceGO(lavaSurfaceAudioSrcs[i]);
		}
		if (prewarmCoroutine != null)
		{
			StopCoroutine(prewarmCoroutine);
		}
		prewarmCoroutine = StartCoroutine(_PrewarmLavaSpewRenderers());
	}

	private static void PreloadClip(AudioClip clip)
	{
		if (clip != null && clip.loadState != AudioDataLoadState.Loaded)
		{
			clip.LoadAudioData();
		}
	}

	private static void PreloadStateFXClips(LavaStateFX fx)
	{
		PreloadClip(fx.startSound);
		PreloadClip(fx.endSound);
		if (fx.loop1AudioSrc != null && fx.loop1AudioSrc.clip != null)
		{
			PreloadClip(fx.loop1AudioSrc.clip);
		}
		if (fx.loop2AudioSrc != null && fx.loop2AudioSrc.clip != null)
		{
			PreloadClip(fx.loop2AudioSrc.clip);
		}
	}

	private static void WarmUpAudioSourceGO(AudioSource src)
	{
		if (!(src == null))
		{
			GameObject gameObject = src.gameObject;
			if (!gameObject.activeSelf)
			{
				gameObject.SetActive(value: true);
				gameObject.SetActive(value: false);
			}
		}
	}

	private static void WarmUpStateFXSources(LavaStateFX fx)
	{
		if (fx.startSoundExists)
		{
			WarmUpAudioSourceGO(fx.startSoundAudioSrc);
		}
		if (fx.endSoundExists)
		{
			WarmUpAudioSourceGO(fx.endSoundAudioSrc);
		}
		if (fx.loop1Exists)
		{
			WarmUpAudioSourceGO(fx.loop1AudioSrc);
		}
		if (fx.loop2Exists)
		{
			WarmUpAudioSourceGO(fx.loop2AudioSrc);
		}
	}

	private IEnumerator _PrewarmLavaSpewRenderers()
	{
		for (int i = 0; i < lavaSpewParticleSystems.Length; i++)
		{
			lavaSpewParticleSystems[i].Emit(1);
		}
		yield return null;
		for (int j = 0; j < lavaSpewParticleSystems.Length; j++)
		{
			lavaSpewParticleSystems[j].Clear(withChildren: true);
		}
		prewarmCoroutine = null;
	}

	private void OnDisable()
	{
		if (prewarmCoroutine != null)
		{
			StopCoroutine(prewarmCoroutine);
			prewarmCoroutine = null;
		}
	}

	public void OnVolcanoBellyEmpty()
	{
		if (hasForestSpeakerAudioSrc && !(Time.time - timeVolcanoBellyWasLastEmpty < warnVolcanoBellyEmptied.length))
		{
			forestSpeakerAudioSrc.gameObject.SetActive(value: true);
			forestSpeakerAudioSrc.GTPlayOneShot(warnVolcanoBellyEmptied);
		}
	}

	public void OnStoneAccepted(float activationProgress)
	{
		if (hasVolcanoAudioSrc)
		{
			volcanoAudioSource.gameObject.SetActive(value: true);
			if (activationProgress > 1f)
			{
				volcanoAudioSource.GTPlayOneShot(volcanoAcceptLastStone);
			}
			else
			{
				volcanoAudioSource.GTPlayOneShot(volcanoAcceptStone);
			}
		}
	}

	private void InitState(LavaStateFX fx)
	{
		fx.startSoundExists = fx.startSound != null;
		fx.endSoundExists = fx.endSound != null;
		fx.loop1Exists = fx.loop1AudioSrc != null;
		fx.loop2Exists = fx.loop2AudioSrc != null;
		if (fx.loop1Exists)
		{
			fx.loop1DefaultVolume = fx.loop1AudioSrc.volume;
			fx.loop1AudioSrc.volume = 0f;
		}
		if (fx.loop2Exists)
		{
			fx.loop2DefaultVolume = fx.loop2AudioSrc.volume;
			fx.loop2AudioSrc.volume = 0f;
		}
	}

	private void SetLavaAudioEnabled(bool toEnable)
	{
		AudioSource[] array = lavaSurfaceAudioSrcs;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].gameObject.SetActive(toEnable);
		}
	}

	private void SetLavaAudioEnabled(bool toEnable, float volume)
	{
		AudioSource[] array = lavaSurfaceAudioSrcs;
		foreach (AudioSource obj in array)
		{
			obj.volume = volume;
			obj.gameObject.SetActive(toEnable);
		}
	}

	private void ResetState()
	{
		if (currentStateFX != null)
		{
			currentStateFX.startSoundPlayed = false;
			currentStateFX.endSoundPlayed = false;
			if (currentStateFX.startSoundExists)
			{
				currentStateFX.startSoundAudioSrc.gameObject.SetActive(value: false);
			}
			if (currentStateFX.endSoundExists)
			{
				currentStateFX.endSoundAudioSrc.gameObject.SetActive(value: false);
			}
			if (currentStateFX.loop1Exists)
			{
				currentStateFX.loop1AudioSrc.gameObject.SetActive(value: false);
			}
			if (currentStateFX.loop2Exists)
			{
				currentStateFX.loop2AudioSrc.gameObject.SetActive(value: false);
			}
		}
	}

	private void UpdateState(float time, float timeRemaining, float progress)
	{
		if (currentStateFX == null)
		{
			return;
		}
		if (currentStateFX.startSoundExists && !currentStateFX.startSoundPlayed && time >= currentStateFX.startSoundDelay)
		{
			currentStateFX.startSoundPlayed = true;
			currentStateFX.startSoundAudioSrc.gameObject.SetActive(value: true);
			currentStateFX.startSoundAudioSrc.GTPlayOneShot(currentStateFX.startSound, currentStateFX.startSoundVol);
		}
		if (currentStateFX.endSoundExists && !currentStateFX.endSoundPlayed && timeRemaining <= currentStateFX.endSound.length + currentStateFX.endSoundPadTime)
		{
			currentStateFX.endSoundPlayed = true;
			currentStateFX.endSoundAudioSrc.gameObject.SetActive(value: true);
			currentStateFX.endSoundAudioSrc.GTPlayOneShot(currentStateFX.endSound, currentStateFX.endSoundVol);
		}
		if (currentStateFX.loop1Exists)
		{
			currentStateFX.loop1AudioSrc.volume = currentStateFX.loop1VolAnim.Evaluate(progress) * currentStateFX.loop1DefaultVolume;
			if (!currentStateFX.loop1AudioSrc.isPlaying)
			{
				currentStateFX.loop1AudioSrc.gameObject.SetActive(value: true);
				currentStateFX.loop1AudioSrc.GTPlay();
			}
		}
		if (currentStateFX.loop2Exists)
		{
			currentStateFX.loop2AudioSrc.volume = currentStateFX.loop2VolAnim.Evaluate(progress) * currentStateFX.loop2DefaultVolume;
			if (!currentStateFX.loop2AudioSrc.isPlaying)
			{
				currentStateFX.loop2AudioSrc.gameObject.SetActive(value: true);
				currentStateFX.loop2AudioSrc.GTPlay();
			}
		}
		for (int i = 0; i < smokeMainModules.Length; i++)
		{
			smokeMainModules[i].startColor = currentStateFX.smokeStartColorAnim.Evaluate(progress);
			smokeEmissionModules[i].rateOverTimeMultiplier = currentStateFX.smokeEmissionAnim.Evaluate(progress) * smokeEmissionDefaultRateMultipliers[i];
		}
		SetParticleEmissionRateAndBurst(currentStateFX.lavaSpewEmissionAnim.Evaluate(progress), lavaSpewEmissionModules, lavaSpewEmissionDefaultRateMultipliers, lavaSpewDefaultEmitBursts, lavaSpewAdjustedEmitBursts);
		if (applyShaderGlobals)
		{
			Shader.SetGlobalColor(shaderProp_ZoneLiquidLightColor, currentStateFX.lavaLightColor.Evaluate(progress) * currentStateFX.lavaLightIntensityAnim.Evaluate(progress));
			Shader.SetGlobalFloat(shaderProp_ZoneLiquidLightDistScale, currentStateFX.lavaLightAttenuationAnim.Evaluate(progress));
		}
	}

	public void SetDrainedState()
	{
		ResetState();
		SetLavaAudioEnabled(toEnable: false);
		currentStateFX = drainedStateFX;
	}

	public void UpdateDrainedState(float time)
	{
		UpdateState(time, float.MaxValue, float.MinValue);
	}

	public void SetEruptingState()
	{
		ResetState();
		SetLavaAudioEnabled(toEnable: false, 0f);
		currentStateFX = eruptingStateFX;
	}

	public void UpdateEruptingState(float time, float timeRemaining, float progress)
	{
		UpdateState(time, timeRemaining, progress);
	}

	public void SetRisingState()
	{
		ResetState();
		SetLavaAudioEnabled(toEnable: true, 0f);
		currentStateFX = risingStateFX;
	}

	public void UpdateRisingState(float time, float timeRemaining, float progress)
	{
		UpdateState(time, timeRemaining, progress);
		AudioSource[] array = lavaSurfaceAudioSrcs;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].volume = Mathf.Lerp(0f, 1f, Mathf.Clamp01(time));
		}
	}

	public void SetFullState()
	{
		ResetState();
		SetLavaAudioEnabled(toEnable: true, 1f);
		currentStateFX = fullStateFX;
	}

	public void UpdateFullState(float time, float timeRemaining, float progress)
	{
		UpdateState(time, timeRemaining, progress);
	}

	public void SetDrainingState()
	{
		ResetState();
		SetLavaAudioEnabled(toEnable: true, 1f);
		currentStateFX = drainingStateFX;
	}

	public void UpdateDrainingState(float time, float timeRemaining, float progress)
	{
		UpdateState(time, timeRemaining, progress);
		AudioSource[] array = lavaSurfaceAudioSrcs;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].volume = Mathf.Lerp(1f, 0f, progress);
		}
	}

	private void SetParticleEmissionRateAndBurst(float multiplier, ParticleSystem.EmissionModule[] emissionModules, float[] defaultRateMultipliers, ParticleSystem.Burst[][] defaultEmitBursts, ParticleSystem.Burst[][] adjustedEmitBursts)
	{
		for (int i = 0; i < emissionModules.Length; i++)
		{
			emissionModules[i].rateOverTimeMultiplier = multiplier * defaultRateMultipliers[i];
			int num = Mathf.Min(emissionModules[i].burstCount, defaultEmitBursts[i].Length);
			for (int j = 0; j < num; j++)
			{
				adjustedEmitBursts[i][j].probability = defaultEmitBursts[i][j].probability * multiplier;
			}
			emissionModules[i].SetBursts(adjustedEmitBursts[i]);
		}
	}

	private bool RemoveNullsFromArray<T>(ref T[] array) where T : UnityEngine.Object
	{
		List<T> list = new List<T>(array.Length);
		T[] array2 = array;
		foreach (T val in array2)
		{
			if (val != null)
			{
				list.Add(val);
			}
		}
		int num = array.Length;
		array = list.ToArray();
		return num != array.Length;
	}

	private void LogNullsFoundInArray(string nameOfArray)
	{
		Debug.LogError("Null reference found in " + nameOfArray + " array of component: \"" + this.GetComponentPath() + "\"", this);
	}
}
