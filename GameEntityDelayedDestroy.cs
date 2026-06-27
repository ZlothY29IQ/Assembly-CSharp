using System;
using GorillaTag.Audio;
using UnityEngine;

public class GameEntityDelayedDestroy : MonoBehaviour, IDelayedExecListener
{
	[Serializable]
	public struct Options
	{
		public float delay;

		[Tooltip("Optional. If not set then a sound will be played at the transforms position. Which if it is a long clip on a transform that moves a lot then it will feel wrong without this set.")]
		public AudioSource audioSource;

		public AudioClip explosionSound;

		public float explosionVolume;

		public GameObject pooledExplosionPrefab;

		public AudioClip beepSound;

		public float beepVolume;

		[Tooltip("Beep phases keyed by seconds remaining. Must be ordered from most to least time remaining.")]
		public BeepPhase[] beepPhases;
	}

	[Serializable]
	public struct BeepPhase
	{
		[Tooltip("Beeping starts when this many seconds remain.")]
		public float timeRemaining;

		[Tooltip("Seconds between beeps during this phase.")]
		public float interval;
	}

	[SerializeField]
	private Options m_options = new Options
	{
		delay = 3f,
		audioSource = null,
		explosionSound = null,
		explosionVolume = 1f,
		pooledExplosionPrefab = null,
		beepSound = null,
		beepVolume = 1f,
		beepPhases = null
	};

	private GameEntity _entity;

	private int _callGenerationId;

	private int _delayedExplosionAudioIndex = -1;

	private int _delayedExplosionPoolIndex = -1;

	private const int k_contextId_deferredStart = 0;

	internal void Configure(Options options)
	{
		m_options = options;
		if ((m_options.beepSound != null || m_options.explosionSound != null) && m_options.audioSource == null)
		{
			m_options.audioSource = GetComponentInChildren<AudioSource>();
		}
	}

	protected void OnDestroy()
	{
		if (_delayedExplosionAudioIndex >= 0)
		{
			GTAudioOneShot.CancelDelayed(_delayedExplosionAudioIndex);
			_delayedExplosionAudioIndex = -1;
		}
		if (_delayedExplosionPoolIndex >= 0)
		{
			ObjectPools.CancelDelayedInstantiate(_delayedExplosionPoolIndex);
			_delayedExplosionPoolIndex = -1;
		}
	}

	protected void Start()
	{
		_entity = GetComponent<GameEntity>();
		if (_entity == null)
		{
			Debug.LogError("GameEntityDelayedDestroy: No GameEntity found. Must be added to the same GameObject of the GameEntity you are trying to destroy with a delay.");
		}
		else
		{
			GTDelayedExec.Add(this, 0f, 0);
		}
	}

	internal void ResetTimer()
	{
		_callGenerationId++;
		int callGenerationId = _callGenerationId;
		int contextId = (callGenerationId << 1) | 1;
		Options options = m_options;
		GTDelayedExec.Add(this, options.delay, contextId);
		if (options.explosionSound != null)
		{
			_delayedExplosionAudioIndex = GTAudioOneShot.PlayDelayed(options.explosionSound, base.transform.parent, base.transform.localPosition, options.delay, options.explosionVolume);
		}
		else
		{
			_delayedExplosionAudioIndex = -1;
		}
		if (options.pooledExplosionPrefab != null)
		{
			_delayedExplosionPoolIndex = ObjectPools.InstantiateDelayed(options.pooledExplosionPrefab, base.transform.parent, base.transform.localPosition, options.delay);
		}
		else
		{
			_delayedExplosionPoolIndex = -1;
		}
		if (options.beepSound == null || options.beepPhases == null || options.beepPhases.Length == 0)
		{
			return;
		}
		int contextId2 = callGenerationId << 1;
		for (int i = 0; i < options.beepPhases.Length; i++)
		{
			float interval = options.beepPhases[i].interval;
			if (interval <= 0f)
			{
				continue;
			}
			float num = ((i + 1 < options.beepPhases.Length) ? options.beepPhases[i + 1].timeRemaining : 0f);
			float num2 = Mathf.Min(options.beepPhases[i].timeRemaining, options.delay);
			if (!(num2 <= num))
			{
				float num3 = options.delay - num2;
				float num4 = options.delay - num;
				for (float num5 = num3; num5 < num4; num5 += interval)
				{
					GTDelayedExec.Add(this, num5, contextId2);
				}
			}
		}
	}

	void IDelayedExecListener.OnDelayedAction(int contextId)
	{
		if (contextId == 0)
		{
			if (_callGenerationId == 0 && _entity != null)
			{
				ResetTimer();
			}
		}
		else
		{
			if (contextId >> 1 != _callGenerationId || _entity == null)
			{
				return;
			}
			Options options = m_options;
			if ((contextId & 1) != 0)
			{
				_entity.manager.RequestDestroyItem(_entity.id);
				return;
			}
			if (_delayedExplosionAudioIndex >= 0)
			{
				GTAudioOneShot.UpdateDelayed(_delayedExplosionAudioIndex, base.transform.parent, base.transform.localPosition);
			}
			if (_delayedExplosionPoolIndex >= 0)
			{
				ObjectPools.UpdateDelayedInstantiate(_delayedExplosionPoolIndex, base.transform.parent, base.transform.localPosition);
			}
			if (options.beepSound != null)
			{
				if (options.audioSource != null && options.audioSource.isActiveAndEnabled)
				{
					options.audioSource.GTPlayOneShot(options.beepSound, options.beepVolume);
				}
				else
				{
					GTAudioOneShot.Play(options.beepSound, base.transform.position, options.beepVolume);
				}
			}
		}
	}
}
