using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class GumBubble : LerpComponent
{
	public Transform target;

	public Vector3 targetScale = Vector3.one;

	[SerializeField]
	private AnimationCurve _lerpCurve;

	public AudioSource audioSource;

	[SerializeField]
	private AudioClip _sfxInflate;

	[SerializeField]
	private AudioClip _sfxPop;

	[SerializeField]
	private float _delayInflate = 1.16f;

	[FormerlySerializedAs("_popDelay")]
	[SerializeField]
	private float _delayPop = 0.5f;

	[SerializeField]
	private bool _animating;

	public UnityEvent onPop;

	public UnityEvent onInflate;

	[NonSerialized]
	private bool _done;

	[NonSerialized]
	private TimeSince _sinceInflate;

	private void Awake()
	{
		base.enabled = false;
		base.gameObject.SetActive(value: false);
	}

	public void InflateDelayed()
	{
		InflateDelayed(_delayInflate);
	}

	public void InflateDelayed(float delay)
	{
		if (delay < 0f)
		{
			delay = 0f;
		}
		Invoke("Inflate", delay);
	}

	public void Inflate()
	{
		base.gameObject.SetActive(value: true);
		base.enabled = true;
		if (!_animating)
		{
			_animating = true;
			_sinceInflate = 0f;
			if (audioSource != null && _sfxInflate != null)
			{
				audioSource.GTPlayOneShot(_sfxInflate);
			}
			onInflate?.Invoke();
		}
	}

	public void Pop()
	{
		_lerp = 0f;
		RenderLerp();
		if (audioSource != null && _sfxPop != null)
		{
			audioSource.GTPlayOneShot(_sfxPop);
		}
		onPop?.Invoke();
		_done = false;
		_animating = false;
		base.enabled = false;
		base.gameObject.SetActive(value: false);
	}

	private void Update()
	{
		float t = Mathf.Clamp01((float)_sinceInflate / _lerpLength);
		_lerp = Mathf.Lerp(0f, 1f, t);
		if (_lerp <= 1f && !_done)
		{
			RenderLerp();
			if (Mathf.Approximately(_lerp, 1f))
			{
				_done = true;
			}
		}
		float num = _lerpLength + _delayPop;
		if ((float)_sinceInflate >= num)
		{
			Pop();
		}
	}

	protected override void OnLerp(float t)
	{
		if ((bool)target)
		{
			if (_lerpCurve == null)
			{
				GTDev.LogError("[GumBubble] Missing lerp curve", this);
			}
			else
			{
				target.localScale = targetScale * _lerpCurve.Evaluate(t);
			}
		}
	}
}
