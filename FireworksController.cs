using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

public class FireworksController : MonoBehaviour
{
	[Serializable]
	public struct ExplosionEvent
	{
		public TimeSince timeSince;

		public double delay;

		public int explosionIndex;

		public int burstIndex;

		public bool active;

		public Firework firework;
	}

	public Firework[] fireworks;

	public AudioClip[] whistles;

	public AudioClip[] bursts;

	[Space]
	[Range(0f, 1f)]
	public float whistleVolumeMin = 0.1f;

	[Range(0f, 1f)]
	public float whistleVolumeMax = 0.15f;

	public float minWhistleDelay = 1f;

	[NonSerialized]
	[Space]
	private AudioClip _lastWhistle;

	[NonSerialized]
	private AudioClip _lastBurst;

	[NonSerialized]
	private Firework[] _launchOrder;

	[NonSerialized]
	private SRand _rnd;

	[NonSerialized]
	private ExplosionEvent[] _explosionQueue = new ExplosionEvent[8];

	[NonSerialized]
	private TimeSince _timeSinceLastWhistle = 10f;

	[Space]
	public string seed = "Fireworks.Summer23";

	[Space]
	public uint roundNumVolleys = 6u;

	public uint roundLength = 6u;

	[FormerlySerializedAs("_timeOfDayEvent")]
	[FormerlySerializedAs("_timeOfDay")]
	[Space]
	[SerializeField]
	private TimeEvent _fireworksEvent;

	private void Awake()
	{
		_launchOrder = fireworks.ToArray();
		_rnd = new SRand(seed);
	}

	public void LaunchVolley()
	{
		if (Application.isPlaying)
		{
			_rnd.Shuffle(_launchOrder);
			for (int i = 0; i < _launchOrder.Length; i++)
			{
				Firework obj = _launchOrder[i];
				float time = _rnd.NextFloat() * (float)roundLength;
				obj.Invoke("Launch", time);
			}
		}
	}

	public void LaunchVolleyRound()
	{
		for (int i = 0; i < roundNumVolleys; i++)
		{
			float time = _rnd.NextFloat() * (float)roundLength;
			Invoke("LaunchVolley", time);
		}
	}

	public void Launch(Firework fw)
	{
		if ((bool)fw)
		{
			_ = fw.origin.position;
			Vector3 position = fw.target.position;
			AudioSource sourceOrigin = fw.sourceOrigin;
			int num = _rnd.NextInt(bursts.Length);
			AudioClip audioClip = whistles[_rnd.NextInt(whistles.Length)];
			AudioClip audioClip2 = bursts[num];
			while (_lastWhistle == audioClip)
			{
				audioClip = whistles[_rnd.NextInt(whistles.Length)];
			}
			while (_lastBurst == audioClip2)
			{
				num = _rnd.NextInt(bursts.Length);
				audioClip2 = bursts[num];
			}
			_lastWhistle = audioClip;
			_lastBurst = audioClip2;
			int num2 = _rnd.NextInt(fw.explosions.Length);
			ParticleSystem obj = fw.explosions[num2];
			if (fw.doTrail)
			{
				ParticleSystem trail = fw.trail;
				trail.startColor = fw.colorOrigin;
				ParticleSystem.ColorOverLifetimeModule colorOverLifetime = trail.subEmitters.GetSubEmitterSystem(0).colorOverLifetime;
				colorOverLifetime.color = new ParticleSystem.MinMaxGradient(fw.colorOrigin, fw.colorTarget);
				trail.Stop();
				trail.Play();
			}
			sourceOrigin.pitch = _rnd.NextFloat(0.92f, 1f);
			fw.doTrailAudio = _rnd.NextBool();
			ExplosionEvent ev = new ExplosionEvent
			{
				firework = fw,
				timeSince = TimeSince.Now(),
				burstIndex = num,
				explosionIndex = num2,
				delay = (fw.doTrail ? audioClip.length : 0f),
				active = true
			};
			if (fw.doExplosion)
			{
				PostExplosionEvent(ev);
			}
			if (fw.doTrailAudio && (float)_timeSinceLastWhistle > minWhistleDelay)
			{
				_timeSinceLastWhistle = TimeSince.Now();
				sourceOrigin.PlayOneShot(audioClip, _rnd.NextFloat(whistleVolumeMin, whistleVolumeMax));
			}
			obj.Stop();
			obj.transform.position = position;
		}
	}

	private void PostExplosionEvent(ExplosionEvent ev)
	{
		for (int i = 0; i < _explosionQueue.Length; i++)
		{
			if (!_explosionQueue[i].active)
			{
				_explosionQueue[i] = ev;
				break;
			}
		}
	}

	private void Update()
	{
		ProcessEvents();
	}

	private void ProcessEvents()
	{
		if (_explosionQueue == null || _explosionQueue.Length == 0)
		{
			return;
		}
		for (int i = 0; i < _explosionQueue.Length; i++)
		{
			ExplosionEvent ev = _explosionQueue[i];
			if (ev.active && !((double)ev.timeSince < ev.delay))
			{
				DoExplosion(ev);
				_explosionQueue[i] = default(ExplosionEvent);
			}
		}
	}

	private void DoExplosion(ExplosionEvent ev)
	{
		Firework firework = ev.firework;
		ParticleSystem obj = firework.explosions[ev.explosionIndex];
		ParticleSystem.MinMaxGradient color = new ParticleSystem.MinMaxGradient(firework.colorOrigin, firework.colorTarget);
		ParticleSystem.ColorOverLifetimeModule colorOverLifetime = obj.colorOverLifetime;
		ParticleSystem.ColorOverLifetimeModule colorOverLifetime2 = obj.subEmitters.GetSubEmitterSystem(0).colorOverLifetime;
		colorOverLifetime.color = color;
		colorOverLifetime2.color = color;
		ParticleSystem obj2 = firework.explosions[ev.explosionIndex];
		obj2.Stop();
		obj2.Play();
		firework.sourceTarget.PlayOneShot(bursts[ev.burstIndex]);
	}

	public void RenderGizmo(Firework fw, Color c)
	{
		if ((bool)fw && (bool)fw.origin && (bool)fw.target)
		{
			Gizmos.color = c;
			Vector3 position = fw.origin.position;
			Vector3 position2 = fw.target.position;
			Gizmos.DrawLine(position, position2);
			Gizmos.DrawWireCube(position, Vector3.one * 0.5f);
			Gizmos.DrawWireCube(position2, Vector3.one * 0.5f);
		}
	}
}
