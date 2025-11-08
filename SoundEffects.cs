using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class SoundEffects : MonoBehaviour
{
	public AudioSource source;

	[Space]
	public List<AudioClip> audioClips = new List<AudioClip>();

	public string seed = "0x1337C0D3";

	[Space]
	public bool distinct = true;

	[SerializeField]
	private float _minDelay;

	[Space]
	[SerializeField]
	private SRand _rnd;

	[NonSerialized]
	private int _lastClipIndex = -1;

	[NonSerialized]
	private double _lastClipLength = -1.0;

	[NonSerialized]
	private TimeSince _lastClipElapsedTime;

	public bool isPlaying
	{
		get
		{
			if (_lastClipIndex < 0)
			{
				return false;
			}
			if (_lastClipLength < 0.0)
			{
				return false;
			}
			return (double)_lastClipElapsedTime < _lastClipLength;
		}
	}

	public void Clear()
	{
		audioClips.Clear();
		_lastClipIndex = -1;
		_lastClipLength = -1.0;
	}

	public void Stop()
	{
		if ((bool)source)
		{
			source.GTStop();
		}
		_lastClipLength = -1.0;
	}

	public void PlayNext(float delayMin, float delayMax, float volMin, float volMax)
	{
		float delay = _rnd.NextFloat(delayMin, delayMax);
		float volume = _rnd.NextFloat(volMin, volMax);
		PlayNext(delay, volume);
	}

	public void PlayNext(float delay = 0f, float volume = 1f)
	{
		if ((bool)source && audioClips != null && audioClips.Count != 0)
		{
			if (source.isPlaying)
			{
				source.GTStop();
			}
			int num = _rnd.NextInt(audioClips.Count);
			while (distinct && _lastClipIndex == num)
			{
				num = _rnd.NextInt(audioClips.Count);
			}
			AudioClip audioClip = audioClips[num];
			_lastClipIndex = num;
			_lastClipLength = audioClip.length;
			float num2 = delay;
			if (num2 < _minDelay)
			{
				num2 = _minDelay;
			}
			if (num2 < 0.0001f)
			{
				source.GTPlayOneShot(audioClip, volume);
				_lastClipElapsedTime = 0f;
				return;
			}
			source.clip = audioClip;
			source.volume = volume;
			source.GTPlayDelayed(num2);
			_lastClipElapsedTime = 0f - num2;
		}
	}

	[Conditional("UNITY_EDITOR")]
	private void OnValidate()
	{
		if (string.IsNullOrEmpty(seed))
		{
			seed = "0x1337C0D3";
		}
		_rnd = new SRand(seed);
		if (audioClips == null)
		{
			audioClips = new List<AudioClip>();
		}
	}
}
