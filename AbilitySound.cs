using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AbilitySound
{
	public enum SoundSelectMode
	{
		Sequential,
		Random
	}

	public float volume = 1f;

	public float pitch = 1f;

	public bool loop;

	public float delay;

	public List<AudioClip> sounds;

	private AudioClip currentSound;

	public AudioSource audioSource;

	private AudioSource usedAudioSource;

	private int nextSound = -1;

	public SoundSelectMode soundSelectMode;

	public bool IsValid()
	{
		if (sounds != null)
		{
			return sounds.Count > 0;
		}
		return false;
	}

	public void UpdateNextSound()
	{
		switch (soundSelectMode)
		{
		case SoundSelectMode.Sequential:
			nextSound = (nextSound + 1) % sounds.Count;
			break;
		case SoundSelectMode.Random:
			nextSound = UnityEngine.Random.Range(0, sounds.Count);
			break;
		}
	}

	public void Play(AudioSource audioSourceIn)
	{
		usedAudioSource = ((audioSourceIn != null) ? audioSourceIn : audioSource);
		if (sounds == null || sounds.Count <= 0 || !(usedAudioSource != null))
		{
			return;
		}
		if (nextSound < 0)
		{
			UpdateNextSound();
		}
		AudioClip audioClip = sounds[nextSound];
		UpdateNextSound();
		if (audioClip != null)
		{
			usedAudioSource.clip = audioClip;
			usedAudioSource.volume = volume;
			usedAudioSource.pitch = pitch;
			usedAudioSource.loop = loop;
			if (delay <= 0f)
			{
				usedAudioSource.Play();
			}
			else
			{
				usedAudioSource.PlayDelayed(delay);
			}
			currentSound = audioClip;
		}
	}

	public void Stop()
	{
		if (usedAudioSource != null && usedAudioSource.clip == currentSound)
		{
			usedAudioSource.Stop();
			currentSound = null;
			usedAudioSource = null;
		}
	}
}
