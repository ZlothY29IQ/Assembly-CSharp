using System;
using GorillaTag.Audio;
using UnityEngine;

public class GameEntityDelayedReturn : MonoBehaviour, IGameEntityComponent, IDelayedExecListener
{
	[Serializable]
	public struct Options
	{
		public float delay;

		[Tooltip("Seconds the entity stays hidden between disappear and reappear.")]
		public float reappearDelay;

		public AudioClip disappearSound;

		public float disappearVolume;

		public GameObject pooledDisappearPrefab;

		public AudioClip reappearSound;

		public float reappearVolume;

		public GameObject pooledReappearPrefab;

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

	private const int k_actionBits = 2;

	private const int k_actionMask = 3;

	private const int k_actionBeep = 0;

	private const int k_actionDisappear = 1;

	private const int k_actionReappear = 2;

	public GameEntity gameEntity;

	[SerializeField]
	private Options m_options = new Options
	{
		delay = 30f,
		reappearDelay = 0.5f,
		disappearSound = null,
		disappearVolume = 1f,
		pooledDisappearPrefab = null,
		reappearSound = null,
		reappearVolume = 1f,
		pooledReappearPrefab = null,
		beepSound = null,
		beepVolume = 1f,
		beepPhases = null
	};

	[Tooltip("If set, the entity teleports here instead of its initial position.")]
	public Transform resetTarget;

	[Tooltip("If true, the Rigidbody is forced kinematic after return regardless of its initial state.")]
	public bool forceKinematicOnReset;

	private Vector3 initialPosition;

	private Quaternion initialRotation;

	private Vector3 initialScale;

	private bool initialIsKinematic;

	private bool initialized;

	private int _callGenerationId;

	private int _delayedDisappearAudioIndex = -1;

	private int _delayedDisappearPoolIndex = -1;

	private bool _timerRunning;

	public void OnEntityInit()
	{
		Transform transform = base.transform;
		initialPosition = transform.position;
		initialRotation = transform.rotation;
		initialScale = transform.localScale;
		Rigidbody componentInParent = GetComponentInParent<Rigidbody>();
		initialIsKinematic = componentInParent != null && componentInParent.isKinematic;
		initialized = true;
		GameEntity obj = gameEntity;
		obj.OnGrabbed = (Action)Delegate.Combine(obj.OnGrabbed, new Action(OnInteractionStarted));
		GameEntity obj2 = gameEntity;
		obj2.OnSnapped = (Action)Delegate.Combine(obj2.OnSnapped, new Action(OnInteractionStarted));
		GameEntity obj3 = gameEntity;
		obj3.OnAttached = (Action)Delegate.Combine(obj3.OnAttached, new Action(OnInteractionStarted));
		GameEntity obj4 = gameEntity;
		obj4.OnReleased = (Action)Delegate.Combine(obj4.OnReleased, new Action(OnInteractionEnded));
		GameEntity obj5 = gameEntity;
		obj5.OnUnsnapped = (Action)Delegate.Combine(obj5.OnUnsnapped, new Action(OnInteractionEnded));
		GameEntity obj6 = gameEntity;
		obj6.OnDetached = (Action)Delegate.Combine(obj6.OnDetached, new Action(OnInteractionEnded));
		if (!IsCurrentlyInteracting())
		{
			StartTimer();
		}
	}

	public void OnEntityDestroy()
	{
		if (initialized)
		{
			GameEntity obj = gameEntity;
			obj.OnGrabbed = (Action)Delegate.Remove(obj.OnGrabbed, new Action(OnInteractionStarted));
			GameEntity obj2 = gameEntity;
			obj2.OnSnapped = (Action)Delegate.Remove(obj2.OnSnapped, new Action(OnInteractionStarted));
			GameEntity obj3 = gameEntity;
			obj3.OnAttached = (Action)Delegate.Remove(obj3.OnAttached, new Action(OnInteractionStarted));
			GameEntity obj4 = gameEntity;
			obj4.OnReleased = (Action)Delegate.Remove(obj4.OnReleased, new Action(OnInteractionEnded));
			GameEntity obj5 = gameEntity;
			obj5.OnUnsnapped = (Action)Delegate.Remove(obj5.OnUnsnapped, new Action(OnInteractionEnded));
			GameEntity obj6 = gameEntity;
			obj6.OnDetached = (Action)Delegate.Remove(obj6.OnDetached, new Action(OnInteractionEnded));
			CancelTimer();
		}
	}

	public void OnEntityStateChange(long prevState, long newState)
	{
		RestartTimer();
	}

	private void OnDestroy()
	{
		CancelDelayedFx();
	}

	private void OnInteractionStarted()
	{
		CancelTimer();
	}

	private void OnInteractionEnded()
	{
		if (!IsCurrentlyInteracting())
		{
			StartTimer();
		}
	}

	public void StartTimer()
	{
		CancelDelayedFx();
		_callGenerationId++;
		int callGenerationId = _callGenerationId;
		Options options = m_options;
		GTDelayedExec.Add(this, options.delay, (callGenerationId << 2) | 1);
		GTDelayedExec.Add(this, options.delay + options.reappearDelay, (callGenerationId << 2) | 2);
		_timerRunning = true;
		if (options.disappearSound != null)
		{
			_delayedDisappearAudioIndex = GTAudioOneShot.PlayDelayed(options.disappearSound, base.transform.parent, base.transform.localPosition, options.delay, options.disappearVolume);
		}
		else
		{
			_delayedDisappearAudioIndex = -1;
		}
		if (options.pooledDisappearPrefab != null)
		{
			_delayedDisappearPoolIndex = ObjectPools.InstantiateDelayed(options.pooledDisappearPrefab, base.transform.parent, base.transform.localPosition, options.delay);
		}
		else
		{
			_delayedDisappearPoolIndex = -1;
		}
		if (options.beepSound == null || options.beepPhases == null || options.beepPhases.Length == 0)
		{
			return;
		}
		int contextId = (callGenerationId << 2) | 0;
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
					GTDelayedExec.Add(this, num5, contextId);
				}
			}
		}
	}

	public void RestartTimer()
	{
		if (!IsCurrentlyInteracting())
		{
			StartTimer();
		}
	}

	public void CancelTimer()
	{
		if (_timerRunning)
		{
			_callGenerationId++;
			_timerRunning = false;
			CancelDelayedFx();
			if (!gameEntity.gameObject.activeSelf)
			{
				gameEntity.gameObject.SetActive(value: true);
			}
		}
	}

	private void CancelDelayedFx()
	{
		if (_delayedDisappearAudioIndex >= 0)
		{
			GTAudioOneShot.CancelDelayed(_delayedDisappearAudioIndex);
			_delayedDisappearAudioIndex = -1;
		}
		if (_delayedDisappearPoolIndex >= 0)
		{
			ObjectPools.CancelDelayedInstantiate(_delayedDisappearPoolIndex);
			_delayedDisappearPoolIndex = -1;
		}
	}

	void IDelayedExecListener.OnDelayedAction(int contextId)
	{
		if (contextId >> 2 != _callGenerationId || gameEntity == null)
		{
			return;
		}
		int num = contextId & 3;
		Options options = m_options;
		switch (num)
		{
		case 0:
			if (_delayedDisappearAudioIndex >= 0)
			{
				GTAudioOneShot.UpdateDelayed(_delayedDisappearAudioIndex, base.transform.parent, base.transform.localPosition);
			}
			if (_delayedDisappearPoolIndex >= 0)
			{
				ObjectPools.UpdateDelayedInstantiate(_delayedDisappearPoolIndex, base.transform.parent, base.transform.localPosition);
			}
			if (options.beepSound != null)
			{
				GTAudioOneShot.Play(options.beepSound, base.transform.position, options.beepVolume);
			}
			break;
		case 1:
			Disappear();
			break;
		case 2:
			Reappear();
			break;
		}
	}

	private bool IsCurrentlyInteracting()
	{
		if (!gameEntity.IsHeld() && gameEntity.snappedByActorNumber == -1)
		{
			return gameEntity.attachedToEntityId != GameEntityId.Invalid;
		}
		return true;
	}

	private void Disappear()
	{
		gameEntity.gameObject.SetActive(value: false);
	}

	private void Reappear()
	{
		_timerRunning = false;
		if (resetTarget != null)
		{
			base.transform.SetPositionAndRotation(resetTarget.position, resetTarget.rotation);
			base.transform.localScale = resetTarget.localScale;
		}
		else
		{
			base.transform.SetPositionAndRotation(initialPosition, initialRotation);
			base.transform.localScale = initialScale;
		}
		Rigidbody componentInParent = GetComponentInParent<Rigidbody>();
		if (componentInParent != null)
		{
			componentInParent.linearVelocity = Vector3.zero;
			componentInParent.angularVelocity = Vector3.zero;
			componentInParent.isKinematic = forceKinematicOnReset || initialIsKinematic;
		}
		gameEntity.gameObject.SetActive(value: true);
		Vector3 position = base.transform.position;
		Options options = m_options;
		if (options.reappearSound != null)
		{
			GTAudioOneShot.Play(options.reappearSound, position, options.reappearVolume);
		}
		if (options.pooledReappearPrefab != null)
		{
			ObjectPools.instance.Instantiate(options.pooledReappearPrefab, position);
		}
	}

	public void ReturnNow()
	{
		CancelTimer();
		Disappear();
		Reappear();
	}

	public void SetResetTarget(Transform target)
	{
		resetTarget = target;
	}

	internal void Configure(Options options)
	{
		m_options = options;
	}
}
