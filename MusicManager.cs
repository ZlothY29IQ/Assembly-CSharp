using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
	[OnEnterPlay_SetNull]
	public static volatile MusicManager Instance;

	private HashSet<MusicSource> activeSources = new HashSet<MusicSource>();

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			Object.Destroy(this);
		}
	}

	public void RegisterMusicSource(MusicSource musicSource)
	{
		if (!activeSources.Contains(musicSource))
		{
			activeSources.Add(musicSource);
		}
	}

	public void UnregisterMusicSource(MusicSource musicSource)
	{
		if (activeSources.Contains(musicSource))
		{
			activeSources.Remove(musicSource);
			musicSource.UnsetVolumeOverride();
		}
	}

	public void FadeOutMusic(float duration = 3f)
	{
		StopAllCoroutines();
		if (duration > 0f)
		{
			StartCoroutine(FadeOutVolumeCoroutine(duration));
			return;
		}
		foreach (MusicSource activeSource in activeSources)
		{
			activeSource.SetVolumeOverride(0f);
		}
	}

	public void FadeInMusic(float duration = 3f)
	{
		StopAllCoroutines();
		if (duration > 0f)
		{
			StartCoroutine(FadeInVolumeCoroutine(duration));
			return;
		}
		foreach (MusicSource activeSource in activeSources)
		{
			activeSource.UnsetVolumeOverride();
		}
	}

	private IEnumerator FadeInVolumeCoroutine(float duration)
	{
		bool complete = false;
		while (!complete)
		{
			complete = true;
			float deltaTime = Time.deltaTime;
			foreach (MusicSource activeSource in activeSources)
			{
				float num = activeSource.DefaultVolume / duration;
				float volumeOverride = Mathf.MoveTowards(activeSource.AudioSource.volume, activeSource.DefaultVolume, num * deltaTime);
				activeSource.SetVolumeOverride(volumeOverride);
				if (activeSource.AudioSource.volume != activeSource.DefaultVolume)
				{
					complete = false;
				}
			}
			yield return null;
		}
		foreach (MusicSource activeSource2 in activeSources)
		{
			activeSource2.UnsetVolumeOverride();
		}
	}

	private IEnumerator FadeOutVolumeCoroutine(float duration)
	{
		bool complete = false;
		while (!complete)
		{
			complete = true;
			float deltaTime = Time.deltaTime;
			foreach (MusicSource activeSource in activeSources)
			{
				float volumeOverride = Mathf.MoveTowards(maxDelta: activeSource.DefaultVolume / duration * deltaTime, current: activeSource.AudioSource.volume, target: 0f);
				activeSource.SetVolumeOverride(volumeOverride);
				if (activeSource.AudioSource.volume != 0f)
				{
					complete = false;
				}
			}
			yield return null;
		}
	}

	public static void StopAllMusic()
	{
		StopAllMusic(null);
	}

	public static void StopAllMusic(AudioClip clip)
	{
		if (Instance == null)
		{
			return;
		}
		Instance.StopAllCoroutines();
		foreach (MusicSource activeSource in Instance.activeSources)
		{
			activeSource.UnsetVolumeOverride();
			activeSource.AudioSource.Stop();
			if (clip != null)
			{
				activeSource.AudioSource.PlayOneShot(clip);
			}
		}
	}
}
