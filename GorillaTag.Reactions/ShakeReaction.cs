using UnityEngine;

namespace GorillaTag.Reactions;

public class ShakeReaction : MonoBehaviour, ITickSystemPost
{
	[SerializeField]
	private Transform shakeXform;

	[SerializeField]
	private float velocityThreshold = 5f;

	[SerializeField]
	private SoundBankPlayer shakeSoundBankPlayer;

	[SerializeField]
	private float shakeSoundCooldown = 1f;

	[SerializeField]
	private AudioSource loopSoundAudioSource;

	[SerializeField]
	private float loopSoundBaseVolume = 1f;

	[SerializeField]
	private float loopSoundSustainDuration = 1f;

	[SerializeField]
	private float loopSoundFadeInDuration = 1f;

	[SerializeField]
	private AnimationCurve loopSoundFadeInCurve;

	[SerializeField]
	private float loopSoundFadeOutDuration = 1f;

	[SerializeField]
	private AnimationCurve loopSoundFadeOutCurve;

	[SerializeField]
	private ParticleSystem particles;

	[SerializeField]
	private AnimationCurve emissionCurve;

	[SerializeField]
	private float particleDuration = 5f;

	private const int sampleHistorySize = 256;

	private float[] sampleHistoryTime;

	private Vector3[] sampleHistoryPos;

	private Vector3[] sampleHistoryVel;

	private int currentIndex;

	private float lastShakeSoundTime = float.MinValue;

	private float lastShakeTime = float.MinValue;

	private float maxEmissionRate;

	private bool hasLoopSound;

	private bool hasShakeSound;

	private bool hasParticleSystem;

	[DebugReadout]
	private float poopVelocity;

	private float loopSoundTotalDuration => loopSoundFadeInDuration + loopSoundSustainDuration + loopSoundFadeOutDuration;

	bool ITickSystemPost.PostTickRunning { get; set; }

	protected void Awake()
	{
		sampleHistoryPos = new Vector3[256];
		sampleHistoryTime = new float[256];
		sampleHistoryVel = new Vector3[256];
		if (particles != null)
		{
			maxEmissionRate = particles.emission.rateOverTime.constant;
		}
		Application.quitting += HandleApplicationQuitting;
	}

	protected void OnEnable()
	{
		float unscaledTime = Time.unscaledTime;
		Vector3 position = shakeXform.position;
		for (int i = 0; i < 256; i++)
		{
			sampleHistoryTime[i] = unscaledTime;
			sampleHistoryPos[i] = position;
			sampleHistoryVel[i] = Vector3.zero;
		}
		if (loopSoundAudioSource != null)
		{
			loopSoundAudioSource.loop = true;
			loopSoundAudioSource.GTPlay();
		}
		hasLoopSound = loopSoundAudioSource != null;
		hasShakeSound = shakeSoundBankPlayer != null;
		hasParticleSystem = particles != null;
		TickSystem<object>.AddPostTickCallback(this);
	}

	protected void OnDisable()
	{
		if (loopSoundAudioSource != null)
		{
			loopSoundAudioSource.GTStop();
		}
		TickSystem<object>.RemovePostTickCallback(this);
	}

	private void HandleApplicationQuitting()
	{
		TickSystem<object>.RemovePostTickCallback(this);
	}

	void ITickSystemPost.PostTick()
	{
		float unscaledTime = Time.unscaledTime;
		Vector3 position = shakeXform.position;
		int num = (currentIndex - 1 + 256) % 256;
		currentIndex = (currentIndex + 1) % 256;
		sampleHistoryTime[currentIndex] = unscaledTime;
		float num2 = unscaledTime - sampleHistoryTime[num];
		sampleHistoryPos[currentIndex] = position;
		if (num2 > 0f)
		{
			Vector3 vector = position - sampleHistoryPos[num];
			sampleHistoryVel[currentIndex] = vector / num2;
		}
		else
		{
			sampleHistoryVel[currentIndex] = Vector3.zero;
		}
		float sqrMagnitude = (sampleHistoryVel[num] - sampleHistoryVel[currentIndex]).sqrMagnitude;
		poopVelocity = Mathf.Round(Mathf.Sqrt(sqrMagnitude) * 1000f) / 1000f;
		float num3 = shakeXform.lossyScale.x * velocityThreshold * velocityThreshold;
		if (sqrMagnitude >= num3)
		{
			lastShakeTime = unscaledTime;
		}
		float num4 = unscaledTime - lastShakeTime;
		float time = Mathf.Clamp01(num4 / particleDuration);
		if (hasParticleSystem)
		{
			ParticleSystem.EmissionModule emission = particles.emission;
			emission.rateOverTime = emissionCurve.Evaluate(time) * maxEmissionRate;
		}
		if (hasShakeSound && lastShakeTime - lastShakeSoundTime > shakeSoundCooldown)
		{
			shakeSoundBankPlayer.Play();
			lastShakeSoundTime = unscaledTime;
		}
		if (hasLoopSound)
		{
			if (num4 < loopSoundFadeInDuration)
			{
				loopSoundAudioSource.volume = loopSoundBaseVolume * loopSoundFadeInCurve.Evaluate(Mathf.Clamp01(num4 / loopSoundFadeInDuration));
			}
			else if (num4 < loopSoundFadeInDuration + loopSoundSustainDuration)
			{
				loopSoundAudioSource.volume = loopSoundBaseVolume;
			}
			else
			{
				loopSoundAudioSource.volume = loopSoundBaseVolume * loopSoundFadeOutCurve.Evaluate(Mathf.Clamp01((num4 - loopSoundFadeInDuration - loopSoundSustainDuration) / loopSoundFadeOutDuration));
			}
		}
	}
}
