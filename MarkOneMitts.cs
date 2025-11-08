using System;
using UnityEngine;

public class MarkOneMitts : HandTapBehaviour, ITickSystemTick, IProximityEffectReceiver
{
	[Serializable]
	private class Mitt
	{
		public ParticleSystem burst;

		public ParticleSystem flame;

		public ThermalSourceVolume thermalSource;

		[NonSerialized]
		public float lastTapStrength;

		[NonSerialized]
		public ParticleSystem.Burst[] bursts;

		[NonSerialized]
		public Transform burstTransform;

		[NonSerialized]
		public Transform flameTransform;

		[NonSerialized]
		public float timer;

		[NonSerialized]
		public ParticleSystem.MainModule flameMain;

		[NonSerialized]
		public ParticleSystem.ForceOverLifetimeModule flameForce;

		public void Init()
		{
			bursts = new ParticleSystem.Burst[2];
			burst.emission.GetBursts(bursts);
			burstTransform = burst.transform;
			flameTransform = flame.transform;
			flameMain = flame.main;
			flameForce = flame.forceOverLifetime;
		}
	}

	[SerializeField]
	private Mitt leftMitt;

	[SerializeField]
	private Mitt rightMitt;

	[SerializeField]
	private ProximityEffect proximityEffect;

	[SerializeField]
	private AnimationCurve handSpeedToEffectStrength;

	[SerializeField]
	private float minEffectStrength = 0.5f;

	[SerializeField]
	private float flameScale = 3f;

	[SerializeField]
	private float flameTime = 0.5f;

	[SerializeField]
	private float flameSpeed = 5f;

	[SerializeField]
	private float heatMultiplier = 100f;

	[SerializeField]
	private AnimationCurve proximitySpeedCurve;

	[SerializeField]
	private AnimationCurve proximitySpreadCurve;

	[Space]
	[SerializeField]
	private bool vibrateController;

	[SerializeField]
	private float vibrationStrengthMult = 1f;

	[Space]
	[SerializeField]
	private AudioSource proximityAudioSource;

	[SerializeField]
	private AnimationCurve proximityAudioPitch;

	[SerializeField]
	private AnimationCurve proximityAudioVolume;

	[SerializeField]
	private float proximityAudioReactionSpeed = 0.2f;

	[Space]
	[SerializeField]
	private AudioSource proximityStartStopAudioSource;

	[SerializeField]
	private AudioClip proximityStartAudioClip;

	[SerializeField]
	private float proximityStartAudioVolume = 0.5f;

	[SerializeField]
	private AudioClip proximityStopAudioClip;

	[SerializeField]
	private float proximityStopAudioVolume = 0.5f;

	private VRRig rig;

	private ParticleSystem.MinMaxCurve emptyParticleCurve = new ParticleSystem.MinMaxCurve(0f);

	public bool TickRunning { get; set; }

	private void Awake()
	{
		leftMitt.Init();
		rightMitt.Init();
		rig = GetComponentInParent<VRRig>();
		vibrateController = vibrateController && rig.isOfflineVRRig;
		proximityEffect.AddReceiver(this);
	}

	private void OnEnable()
	{
		TickSystem<object>.AddTickCallback(this);
	}

	private void OnDisable()
	{
		TickSystem<object>.RemoveTickCallback(this);
	}

	public void OnProximityCalculated(float distance, float alignment, float parallel)
	{
		float num = distance * alignment * parallel;
		if (num > 0.1f)
		{
			float speed = proximitySpeedCurve.Evaluate(num);
			float num2 = proximitySpreadCurve.Evaluate(num);
			ParticleSystem.MinMaxCurve xy = new ParticleSystem.MinMaxCurve(0f - num2, num2);
			StartFlame(leftMitt, num, speed, xy);
			StartFlame(rightMitt, num, speed, xy);
			if (vibrateController && vibrationStrengthMult > 0f)
			{
				GorillaTagger.Instance.StartVibration(forLeftController: true, vibrationStrengthMult * 0.5f * num, Time.deltaTime);
				GorillaTagger.Instance.StartVibration(forLeftController: false, vibrationStrengthMult * 0.5f * num, Time.deltaTime);
			}
			SetInterferenceAudio(active: true);
			float t = 1f - Mathf.Exp((0f - proximityAudioReactionSpeed) * Time.deltaTime);
			proximityAudioSource.pitch = Mathf.Lerp(proximityAudioSource.pitch, proximityAudioPitch.Evaluate(num), t);
			proximityAudioSource.volume = Mathf.Lerp(proximityAudioSource.volume, proximityAudioVolume.Evaluate(num), t);
		}
		else if (leftMitt.thermalSource.enabled || rightMitt.thermalSource.enabled)
		{
			leftMitt.flame.Stop();
			leftMitt.thermalSource.enabled = false;
			rightMitt.flame.Stop();
			rightMitt.thermalSource.enabled = false;
			SetInterferenceAudio(active: false);
		}
	}

	private void StartFlame(Mitt mitt, float scale, float speed, ParticleSystem.MinMaxCurve xy)
	{
		if (!mitt.thermalSource.enabled)
		{
			mitt.flame.Play();
			mitt.thermalSource.enabled = true;
		}
		mitt.flameTransform.localScale = flameScale * scale * Vector3.one;
		mitt.flameMain.startSpeed = speed;
		mitt.flameForce.x = xy;
		mitt.flameForce.y = xy;
		mitt.thermalSource.celsius = heatMultiplier * scale;
	}

	private void RunTimer(Mitt mitt, bool isLeftHand)
	{
		if (mitt.timer <= 0f)
		{
			return;
		}
		mitt.timer -= Time.deltaTime;
		if (mitt.timer <= 0f)
		{
			mitt.timer = 0f;
			mitt.flame.Stop();
			mitt.thermalSource.enabled = false;
			if (leftMitt.timer <= 0f && rightMitt.timer <= 0f)
			{
				proximityEffect.enabled = true;
			}
		}
		else
		{
			float num = mitt.lastTapStrength * mitt.timer;
			mitt.flameTransform.localScale = flameScale * num * Vector3.one;
			mitt.thermalSource.celsius = heatMultiplier * num;
			if (vibrateController)
			{
				GorillaTagger.Instance.StartVibration(isLeftHand, vibrationStrengthMult * num, 0.1f);
			}
		}
	}

	private void TryPlayProximityStartStopAudio(AudioClip clip, float volume)
	{
		if (!proximityStartStopAudioSource.isPlaying)
		{
			proximityStartStopAudioSource.clip = clip;
			proximityStartStopAudioSource.volume = volume;
			proximityStartStopAudioSource.Play();
		}
	}

	private void SetInterferenceAudio(bool active)
	{
		if (proximityAudioSource.isPlaying != active)
		{
			if (active)
			{
				TryPlayProximityStartStopAudio(proximityStartAudioClip, proximityStartAudioVolume);
				proximityAudioSource.Play();
			}
			else
			{
				TryPlayProximityStartStopAudio(proximityStopAudioClip, proximityStopAudioVolume);
				proximityAudioSource.Stop();
			}
		}
	}

	public void Tick()
	{
		if (leftMitt.timer <= 0f && rightMitt.timer <= 0f)
		{
			TickSystem<object>.RemoveTickCallback(this);
			return;
		}
		RunTimer(leftMitt, isLeftHand: true);
		RunTimer(rightMitt, isLeftHand: false);
	}

	internal override void OnTap(HandEffectContext handContext)
	{
		float num = handSpeedToEffectStrength.Evaluate(handContext.Speed);
		if (num >= minEffectStrength)
		{
			TickSystem<object>.AddTickCallback(this);
			Mitt mitt = (handContext.isLeftHand ? leftMitt : rightMitt);
			mitt.lastTapStrength = num;
			mitt.timer = flameTime;
			mitt.bursts[0].count = num * 10f;
			mitt.bursts[1].count = num * 5f;
			mitt.burst.emission.SetBursts(mitt.bursts);
			mitt.burstTransform.localScale = num * Vector3.one;
			StartFlame(mitt, num * flameScale * flameTime, flameSpeed, emptyParticleCurve);
			mitt.burst.Play();
			float value = handSpeedToEffectStrength.keys[^1].value;
			handContext.soundPitch = Mathf.Clamp(value / num, 1f, 3f);
			proximityEffect.enabled = false;
		}
		else
		{
			handContext.soundFX = null;
		}
	}
}
