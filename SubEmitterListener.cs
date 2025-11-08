using System;
using UnityEngine;
using UnityEngine.Events;

public class SubEmitterListener : MonoBehaviour
{
	public ParticleSystem target;

	public ParticleSystem subEmitter;

	public int subEmitterIndex;

	public UnityEvent onSubEmit;

	public float intervalScale = 1f;

	public float interval;

	[NonSerialized]
	private bool _canListen;

	[NonSerialized]
	private bool _listening;

	[NonSerialized]
	private bool _listenOnce;

	[NonSerialized]
	private TimeSince _sinceLastEmit;

	private void OnEnable()
	{
		if (target == null)
		{
			Disable();
			return;
		}
		ParticleSystem.SubEmittersModule subEmitters = target.subEmitters;
		if (subEmitterIndex < 0)
		{
			subEmitterIndex = 0;
		}
		_canListen = subEmitters.subEmittersCount > 0 && subEmitterIndex <= subEmitters.subEmittersCount - 1;
		if (!_canListen)
		{
			Disable();
			return;
		}
		subEmitter = target.subEmitters.GetSubEmitterSystem(subEmitterIndex);
		ParticleSystem.MainModule main = subEmitter.main;
		interval = main.startLifetime.constantMax * main.startLifetimeMultiplier;
	}

	private void OnDisable()
	{
		_listenOnce = false;
		_listening = false;
	}

	public void ListenStart()
	{
		if (!_listening && _canListen)
		{
			Enable();
			_listening = true;
		}
	}

	public void ListenStop()
	{
		Disable();
	}

	public void ListenOnce()
	{
		if (!_listening)
		{
			Enable();
			if (_canListen)
			{
				Enable();
				_listenOnce = true;
				_listening = true;
			}
		}
	}

	private void Update()
	{
		if (_canListen && _listening && subEmitter.particleCount > 0 && (float)_sinceLastEmit >= interval * intervalScale)
		{
			_sinceLastEmit = 0f;
			OnSubEmit();
			if (_listenOnce)
			{
				Disable();
			}
		}
	}

	protected virtual void OnSubEmit()
	{
		onSubEmit?.Invoke();
	}

	public void Enable()
	{
		if (!base.enabled)
		{
			base.enabled = true;
		}
	}

	public void Disable()
	{
		if (base.enabled)
		{
			base.enabled = false;
		}
	}
}
