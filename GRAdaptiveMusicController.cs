using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GRAdaptiveMusicController : MonoBehaviour
{
	[Serializable]
	public class SingleTrack
	{
		public AudioClip IntroClip;

		public AudioClip LoopedClip;
	}

	private static double BAR_DURATION = 1.6551724137931034;

	public List<SingleTrack> Tracks;

	public SingleTrack CurrentTrack;

	[SerializeField]
	private int trackIndex;

	public List<AudioSource> AudioSources;

	public Transform RepositionAudioSourcePoint;

	public float AdjustedSourceVolume = 0.035f;

	private int currentAudioSourceIndex;

	private float cachedSourceVolume = 0.1f;

	private Vector3 cachedSourcePosition = Vector3.zero;

	[SerializeField]
	private SynchedMusicController synchedMusicController;

	private Coroutine finishCoroutine;

	private int NextAudioSourceIndex => (currentAudioSourceIndex + 1) % AudioSources.Count;

	private void Start()
	{
		cachedSourcePosition = AudioSources[0].transform.position;
	}

	private void PlayCurrentTrack()
	{
		if (trackIndex >= 0 && trackIndex < Tracks.Count)
		{
			SingleTrack singleTrack = Tracks[trackIndex];
			AudioSource currentAudioSource = GetCurrentAudioSource();
			currentAudioSource.clip = singleTrack.IntroClip;
			currentAudioSource.Play();
			AudioSource nextAudioSource = GetNextAudioSource();
			nextAudioSource.clip = singleTrack.LoopedClip;
			nextAudioSource.loop = true;
			double num = AudioSettings.dspTime + (double)singleTrack.IntroClip.length;
			currentAudioSource.SetScheduledEndTime(num);
			nextAudioSource.PlayScheduled(num);
			currentAudioSourceIndex = NextAudioSourceIndex;
			CurrentTrack = singleTrack;
		}
	}

	[ContextMenu("Transition Next Track")]
	public void TransitionToNextTrack()
	{
		GoToTrack(trackIndex + 1);
	}

	public void TransitionToLastTrack()
	{
		GoToTrack(Tracks.Count - 1);
	}

	public void GoToTrack(int nextIndex, bool force = false)
	{
		if (force || (nextIndex >= 0 && nextIndex < Tracks.Count && trackIndex != nextIndex))
		{
			Debug.Log($"GRAdaptiveMusicController - Going to track {nextIndex}.");
			SingleTrack singleTrack = Tracks[nextIndex];
			AudioSource currentAudioSource = GetCurrentAudioSource();
			AudioSource nextAudioSource = GetNextAudioSource();
			double num = (double)currentAudioSource.timeSamples / (double)currentAudioSource.clip.frequency % BAR_DURATION;
			double num2 = AudioSettings.dspTime + (BAR_DURATION - num);
			nextAudioSource.Stop();
			nextAudioSource.clip = singleTrack.IntroClip;
			nextAudioSource.loop = false;
			currentAudioSource.SetScheduledEndTime(num2);
			nextAudioSource.PlayScheduled(num2);
			currentAudioSourceIndex = NextAudioSourceIndex;
			if (singleTrack.LoopedClip != null)
			{
				currentAudioSource = nextAudioSource;
				nextAudioSource = GetNextAudioSource();
				nextAudioSource.clip = singleTrack.LoopedClip;
				nextAudioSource.loop = true;
				double num3 = num2 + (double)singleTrack.IntroClip.length;
				currentAudioSource.SetScheduledEndTime(num3);
				nextAudioSource.PlayScheduled(num3);
				currentAudioSourceIndex = NextAudioSourceIndex;
			}
			else
			{
				Finish(singleTrack.IntroClip.length + 1f);
			}
			trackIndex = nextIndex;
			CurrentTrack = singleTrack;
		}
	}

	[ContextMenu("Restart")]
	public void Restart()
	{
		Debug.Log("Restarting AdaptiveMusicController.");
		cachedSourceVolume = AudioSources[0].volume;
		synchedMusicController.enabled = false;
		StopAllAudioSources();
		UpdateAudioSourcesVolume(AdjustedSourceVolume);
		if (RepositionAudioSourcePoint != null)
		{
			UpdateAudioSourcesPosition(RepositionAudioSourcePoint.position);
		}
		trackIndex = 0;
		currentAudioSourceIndex = 0;
		PlayCurrentTrack();
	}

	public void RestartAt(int index)
	{
		Debug.Log($"Restarting AdaptiveMusicController at index {index}.");
		cachedSourceVolume = AudioSources[0].volume;
		synchedMusicController.enabled = false;
		StopAllAudioSources();
		UpdateAudioSourcesVolume(AdjustedSourceVolume);
		if (RepositionAudioSourcePoint != null)
		{
			UpdateAudioSourcesPosition(RepositionAudioSourcePoint.position);
		}
		trackIndex = index;
		currentAudioSourceIndex = 0;
		GoToTrack(trackIndex, force: true);
	}

	private AudioSource GetCurrentAudioSource()
	{
		return AudioSources[currentAudioSourceIndex];
	}

	private AudioSource GetNextAudioSource()
	{
		return AudioSources[NextAudioSourceIndex];
	}

	private void StopAllAudioSources()
	{
		for (int i = 0; i < AudioSources.Count; i++)
		{
			AudioSources[i].Stop();
		}
	}

	private void UpdateAudioSourcesVolume(float volume)
	{
		for (int i = 0; i < AudioSources.Count; i++)
		{
			AudioSources[i].mute = false;
			AudioSources[i].volume = volume;
		}
	}

	private void UpdateAudioSourcesPosition(Vector3 position)
	{
		for (int i = 0; i < AudioSources.Count; i++)
		{
			AudioSources[i].transform.position = position;
		}
	}

	private void Finish(float delay)
	{
		if (finishCoroutine == null)
		{
			finishCoroutine = StartCoroutine(TryFinish(delay));
		}
	}

	private IEnumerator TryFinish(float delay)
	{
		yield return new WaitForSeconds(delay);
		StopAllAudioSources();
		UpdateAudioSourcesVolume(cachedSourceVolume);
		if (RepositionAudioSourcePoint != null)
		{
			UpdateAudioSourcesPosition(cachedSourcePosition);
		}
		synchedMusicController.enabled = true;
	}
}
