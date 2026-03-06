using System;
using UnityEngine;
using UnityEngine.Serialization;

public class GameEntityDelayedDestroy : MonoBehaviour, IGorillaSliceableSimple
{
	[Serializable]
	public struct BeepPhase
	{
		[Tooltip("Beeping starts when this many seconds remain.")]
		public float timeRemaining;

		[Tooltip("Seconds between beeps during this phase.")]
		public float interval;
	}

	[FormerlySerializedAs("Lifetime")]
	[SerializeField]
	internal float m_delay = 3f;

	[Header("Countdown Audio")]
	[SerializeField]
	private AudioSource m_audioSource;

	[SerializeField]
	private AudioClip m_beepClip;

	[SerializeField]
	private AudioClip m_explosionClip;

	[Tooltip("Beep phases keyed by seconds remaining. Must be ordered from most to least time remaining.")]
	[SerializeField]
	private BeepPhase[] m_beepPhases = new BeepPhase[3]
	{
		new BeepPhase
		{
			timeRemaining = 10f,
			interval = 1f
		},
		new BeepPhase
		{
			timeRemaining = 5f,
			interval = 0.5f
		},
		new BeepPhase
		{
			timeRemaining = 2f,
			interval = 0.1f
		}
	};

	[SerializeField]
	private float m_beepVolume = 1f;

	[SerializeField]
	private float m_explosionVolume = 1f;

	private GameEntity _entity;

	private float _startTime;

	private float _nextBeepTime;

	public void Configure(float delay, AudioClip beepClip, AudioClip explosionClip, BeepPhase[] beepPhases, float beepVolume, float explosionVolume)
	{
		m_delay = delay;
		m_beepClip = beepClip;
		m_explosionClip = explosionClip;
		if (beepPhases != null)
		{
			m_beepPhases = beepPhases;
		}
		m_beepVolume = beepVolume;
		m_explosionVolume = explosionVolume;
		if ((m_beepClip != null || m_explosionClip != null) && m_audioSource == null)
		{
			m_audioSource = GetComponentInChildren<AudioSource>();
		}
	}

	protected void Start()
	{
		_entity = GetComponent<GameEntity>();
		if (_entity == null)
		{
			Debug.LogError("GameEntityDelayedDestroy: No GameEntity found. Must be added to the same GameObject of the GameEntity you are trying to destroy with a delay.");
			return;
		}
		_startTime = Time.unscaledTime;
		BeepPhase[] beepPhases = m_beepPhases;
		_nextBeepTime = ((beepPhases != null && beepPhases.Length > 0) ? (_startTime + (m_delay - m_beepPhases[0].timeRemaining)) : float.MaxValue);
		GorillaSlicerSimpleManager.RegisterSliceable(this);
	}

	protected void OnDestroy()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this);
	}

	void IGorillaSliceableSimple.SliceUpdate()
	{
		float unscaledTime = Time.unscaledTime;
		float num = unscaledTime - _startTime;
		if (num >= m_delay && _entity != null)
		{
			if (m_audioSource != null && m_explosionClip != null)
			{
				m_audioSource.GTPlayOneShot(m_explosionClip, m_explosionVolume);
			}
			_entity.manager.RequestDestroyItem(_entity.id);
		}
		else if (unscaledTime >= _nextBeepTime && m_audioSource != null && m_beepClip != null)
		{
			m_audioSource.GTPlayOneShot(m_beepClip, m_beepVolume);
			float remaining = m_delay - num;
			float interval = GetInterval(remaining);
			_nextBeepTime = ((interval > 0f) ? (unscaledTime + interval) : (-1f));
		}
	}

	private float GetInterval(float remaining)
	{
		if (m_beepPhases == null || m_beepPhases.Length == 0)
		{
			return float.MaxValue;
		}
		for (int num = m_beepPhases.Length - 1; num >= 0; num--)
		{
			if (remaining <= m_beepPhases[num].timeRemaining)
			{
				return m_beepPhases[num].interval;
			}
		}
		return m_beepPhases[0].interval;
	}
}
