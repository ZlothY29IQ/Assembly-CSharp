using System;
using System.Collections;
using System.Collections.Generic;
using GorillaLocomotion;
using UnityEngine;

public class MusicFadeArea : MonoBehaviour
{
	[Serializable]
	public struct AudioSourceEntry
	{
		public AudioSource audioSource;

		public float maxVolume;
	}

	[SerializeField]
	private List<AudioSourceEntry> sourcesToFadeIn = new List<AudioSourceEntry>();

	[SerializeField]
	private float fadeDuration = 3f;

	private float fadeProgress;

	private Coroutine fadeCoroutine;

	private void Awake()
	{
		for (int i = 0; i < sourcesToFadeIn.Count; i++)
		{
			sourcesToFadeIn[i].audioSource.Stop();
			sourcesToFadeIn[i].audioSource.volume = 0f;
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other == GTPlayer.Instance.headCollider)
		{
			MusicManager.Instance.FadeOutMusic(fadeDuration);
			if (fadeCoroutine != null)
			{
				StopCoroutine(fadeCoroutine);
			}
			if (sourcesToFadeIn.Count > 0)
			{
				fadeCoroutine = StartCoroutine(FadeInSources());
			}
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (other == GTPlayer.Instance.headCollider)
		{
			MusicManager.Instance.FadeInMusic(fadeDuration);
			if (fadeCoroutine != null)
			{
				StopCoroutine(fadeCoroutine);
			}
			if (sourcesToFadeIn.Count > 0)
			{
				fadeCoroutine = StartCoroutine(FadeOutSources());
			}
		}
	}

	private IEnumerator FadeInSources()
	{
		for (int i = 0; i < sourcesToFadeIn.Count; i++)
		{
			sourcesToFadeIn[i].audioSource.Play();
			sourcesToFadeIn[i].audioSource.volume = sourcesToFadeIn[i].maxVolume * fadeProgress;
		}
		while (fadeProgress < 1f)
		{
			for (int j = 0; j < sourcesToFadeIn.Count; j++)
			{
				sourcesToFadeIn[j].audioSource.volume = sourcesToFadeIn[j].maxVolume * fadeProgress;
			}
			yield return null;
			fadeProgress = Mathf.MoveTowards(fadeProgress, 1f, Time.deltaTime / fadeDuration);
		}
		for (int k = 0; k < sourcesToFadeIn.Count; k++)
		{
			sourcesToFadeIn[k].audioSource.volume = sourcesToFadeIn[k].maxVolume;
		}
	}

	private IEnumerator FadeOutSources()
	{
		for (int i = 0; i < sourcesToFadeIn.Count; i++)
		{
			sourcesToFadeIn[i].audioSource.volume = sourcesToFadeIn[i].maxVolume * fadeProgress;
		}
		while (fadeProgress > 0f)
		{
			for (int j = 0; j < sourcesToFadeIn.Count; j++)
			{
				sourcesToFadeIn[j].audioSource.volume = sourcesToFadeIn[j].maxVolume * fadeProgress;
			}
			yield return null;
			fadeProgress = Mathf.MoveTowards(fadeProgress, 0f, Time.deltaTime / fadeDuration);
		}
		for (int k = 0; k < sourcesToFadeIn.Count; k++)
		{
			sourcesToFadeIn[k].audioSource.Stop();
			sourcesToFadeIn[k].audioSource.volume = 0f;
		}
	}
}
