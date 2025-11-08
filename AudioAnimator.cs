using System;
using UnityEngine;

public class AudioAnimator : MonoBehaviour
{
	[Serializable]
	private struct AudioTarget
	{
		public AudioSource audioSource;

		public AnimationCurve pitchCurve;

		public AnimationCurve volumeCurve;

		[NonSerialized]
		public float baseVolume;

		public float riseSmoothing;

		public float lowerSmoothing;
	}

	private bool didInitBaseVolume;

	[SerializeField]
	private AudioTarget[] targets;

	private void Start()
	{
		if (!didInitBaseVolume)
		{
			InitBaseVolume();
		}
	}

	private void InitBaseVolume()
	{
		for (int i = 0; i < targets.Length; i++)
		{
			targets[i].baseVolume = targets[i].audioSource.volume;
		}
		didInitBaseVolume = true;
	}

	public void UpdateValue(float value, bool ignoreSmoothing = false)
	{
		UpdatePitchAndVolume(value, value, ignoreSmoothing);
	}

	public void UpdatePitchAndVolume(float pitchValue, float volumeValue, bool ignoreSmoothing = false)
	{
		if (!didInitBaseVolume)
		{
			InitBaseVolume();
		}
		for (int i = 0; i < targets.Length; i++)
		{
			AudioTarget audioTarget = targets[i];
			float p = audioTarget.pitchCurve.Evaluate(pitchValue);
			float pitch = Mathf.Pow(1.05946f, p);
			audioTarget.audioSource.pitch = pitch;
			float num = audioTarget.volumeCurve.Evaluate(volumeValue);
			float volume = audioTarget.audioSource.volume;
			float num2 = audioTarget.baseVolume * num;
			if (ignoreSmoothing)
			{
				audioTarget.audioSource.volume = num2;
			}
			else if (volume > num2)
			{
				audioTarget.audioSource.volume = Mathf.MoveTowards(audioTarget.audioSource.volume, audioTarget.baseVolume * num, (1f - audioTarget.lowerSmoothing) * audioTarget.baseVolume * Time.deltaTime * 90f);
			}
			else
			{
				audioTarget.audioSource.volume = Mathf.MoveTowards(audioTarget.audioSource.volume, audioTarget.baseVolume * num, (1f - audioTarget.riseSmoothing) * audioTarget.baseVolume * Time.deltaTime * 90f);
			}
		}
	}
}
