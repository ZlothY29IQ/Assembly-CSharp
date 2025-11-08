using GorillaLocomotion.Swimming;
using UnityEngine;

public class WaterSplashEffect : MonoBehaviour
{
	private static int lastPlayedBigSplashAudioClipIndex = -1;

	private static int lastPlayedSmallSplashEntryAudioClipIndex = -1;

	private static int lastPlayedSmallSplashExitAudioClipIndex = -1;

	public ParticleSystem[] bigSplashParticleSystems;

	public ParticleSystem[] smallSplashParticleSystems;

	public float bigSplashBaseGravityMultiplier = 0.9f;

	public float bigSplashBaseStartSpeed = 1.9f;

	public float bigSplashBaseSimulationSpeed = 0.9f;

	public float smallSplashBaseGravityMultiplier = 0.6f;

	public float smallSplashBaseStartSpeed = 0.6f;

	public float smallSplashBaseSimulationSpeed = 0.6f;

	public float lifeTime = 1f;

	private float startTime = -1f;

	public AudioSource audioSource;

	public AudioClip[] bigSplashAudioClips;

	public AudioClip[] smallSplashEntryAudioClips;

	public AudioClip[] smallSplashExitAudioClips;

	private WaterVolume waterVolume;

	private void OnEnable()
	{
		startTime = Time.time;
	}

	public void Destroy()
	{
		DeactivateParticleSystems(bigSplashParticleSystems);
		DeactivateParticleSystems(smallSplashParticleSystems);
		waterVolume = null;
		ObjectPools.instance.Destroy(base.gameObject);
	}

	public void PlayEffect(bool isBigSplash, bool isEntry, float scale, WaterVolume volume = null)
	{
		waterVolume = volume;
		if (isBigSplash)
		{
			DeactivateParticleSystems(smallSplashParticleSystems);
			SetParticleEffectParameters(bigSplashParticleSystems, scale, bigSplashBaseGravityMultiplier, bigSplashBaseStartSpeed, bigSplashBaseSimulationSpeed, waterVolume);
			PlayParticleEffects(bigSplashParticleSystems);
			PlayRandomAudioClipWithoutRepeats(bigSplashAudioClips, ref lastPlayedBigSplashAudioClipIndex);
		}
		else if (isEntry)
		{
			DeactivateParticleSystems(bigSplashParticleSystems);
			SetParticleEffectParameters(smallSplashParticleSystems, scale, smallSplashBaseGravityMultiplier, smallSplashBaseStartSpeed, smallSplashBaseSimulationSpeed, waterVolume);
			PlayParticleEffects(smallSplashParticleSystems);
			PlayRandomAudioClipWithoutRepeats(smallSplashEntryAudioClips, ref lastPlayedSmallSplashEntryAudioClipIndex);
		}
		else
		{
			DeactivateParticleSystems(bigSplashParticleSystems);
			SetParticleEffectParameters(smallSplashParticleSystems, scale, smallSplashBaseGravityMultiplier, smallSplashBaseStartSpeed, smallSplashBaseSimulationSpeed, waterVolume);
			PlayParticleEffects(smallSplashParticleSystems);
			PlayRandomAudioClipWithoutRepeats(smallSplashExitAudioClips, ref lastPlayedSmallSplashExitAudioClipIndex);
		}
	}

	private void Update()
	{
		if (waterVolume != null && !waterVolume.isStationary && waterVolume.surfacePlane != null)
		{
			Vector3 vector = Vector3.Dot(base.transform.position - waterVolume.surfacePlane.position, waterVolume.surfacePlane.up) * waterVolume.surfacePlane.up;
			base.transform.position = base.transform.position - vector;
		}
		if ((Time.time - startTime) / lifeTime >= 1f)
		{
			Destroy();
		}
	}

	private void DeactivateParticleSystems(ParticleSystem[] particleSystems)
	{
		if (particleSystems != null)
		{
			for (int i = 0; i < particleSystems.Length; i++)
			{
				particleSystems[i].gameObject.SetActive(value: false);
			}
		}
	}

	private void PlayParticleEffects(ParticleSystem[] particleSystems)
	{
		if (particleSystems != null)
		{
			for (int i = 0; i < particleSystems.Length; i++)
			{
				particleSystems[i].gameObject.SetActive(value: true);
				particleSystems[i].Play();
			}
		}
	}

	private void SetParticleEffectParameters(ParticleSystem[] particleSystems, float scale, float baseGravMultiplier, float baseStartSpeed, float baseSimulationSpeed, WaterVolume waterVolume = null)
	{
		if (particleSystems == null)
		{
			return;
		}
		for (int i = 0; i < particleSystems.Length; i++)
		{
			ParticleSystem.MainModule main = particleSystems[i].main;
			main.startSpeed = baseStartSpeed;
			main.gravityModifier = baseGravMultiplier;
			if (scale < 0.99f)
			{
				main.startSpeed = baseStartSpeed * scale * 2f;
				main.gravityModifier = baseGravMultiplier * scale * 0.5f;
			}
			if (waterVolume != null && waterVolume.Parameters != null)
			{
				ParticleSystem.ColorBySpeedModule colorBySpeed = particleSystems[i].colorBySpeed;
				colorBySpeed.color = waterVolume.Parameters.splashColorBySpeedGradient;
			}
		}
	}

	private void PlayRandomAudioClipWithoutRepeats(AudioClip[] audioClips, ref int lastPlayedAudioClipIndex)
	{
		if (!(audioSource != null) || audioClips == null || audioClips.Length == 0)
		{
			return;
		}
		int num = 0;
		if (audioClips.Length > 1)
		{
			int num2 = Random.Range(0, audioClips.Length);
			if (num2 == lastPlayedAudioClipIndex)
			{
				num2 = ((Random.Range(0f, 1f) > 0.5f) ? ((num2 + 1) % audioClips.Length) : (num2 - 1));
				if (num2 < 0)
				{
					num2 = audioClips.Length - 1;
				}
			}
			num = num2;
		}
		lastPlayedAudioClipIndex = num;
		audioSource.clip = audioClips[num];
		audioSource.GTPlay();
	}
}
