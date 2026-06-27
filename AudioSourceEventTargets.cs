using UnityEngine;

public class AudioSourceEventTargets : MonoBehaviour
{
	private AudioSource audioSource;

	private float fadeVolume;

	private float fadeSpeed;

	[Header("Change Value To Trigger Play (false to true and true to false both work, but value must change the frame you want it played)")]
	public bool ExternalTriggerPlay;

	private bool lastExternalTriggerPlayMatched = true;

	private bool lastValueWhenPlayed;

	[Header("Change Value To Trigger Stop (false to true and true to false both work, but value must change the frame you want it stopped)")]
	public bool ExternalTriggerStop;

	private bool lastExternalTriggerStopMatched = true;

	private bool lastValueWhenStopped;

	private void Awake()
	{
		audioSource = GetComponent<AudioSource>();
		fadeVolume = audioSource.volume;
	}

	public void SetFadeSpeed(float arg)
	{
		fadeSpeed = Mathf.Max(arg, 0.01f);
	}

	public void StartFade(float arg)
	{
		fadeVolume = Mathf.Clamp01(arg);
	}

	public void Update()
	{
		if (audioSource.volume != fadeVolume)
		{
			audioSource.volume = Mathf.MoveTowards(audioSource.volume, fadeVolume, fadeSpeed * Time.deltaTime);
		}
		if (lastValueWhenPlayed != ExternalTriggerPlay)
		{
			if (!lastExternalTriggerPlayMatched)
			{
				audioSource.Play();
				lastValueWhenPlayed = ExternalTriggerPlay;
				lastExternalTriggerPlayMatched = true;
			}
			else
			{
				ExternalTriggerPlay = lastValueWhenPlayed;
				lastExternalTriggerPlayMatched = false;
			}
		}
		else
		{
			lastExternalTriggerPlayMatched = true;
		}
		if (lastValueWhenStopped != ExternalTriggerStop)
		{
			if (!lastExternalTriggerStopMatched)
			{
				audioSource.Stop();
				lastValueWhenStopped = ExternalTriggerStop;
				lastExternalTriggerStopMatched = true;
			}
			else
			{
				ExternalTriggerStop = lastValueWhenStopped;
				lastExternalTriggerStopMatched = false;
			}
		}
		else
		{
			lastExternalTriggerStopMatched = true;
		}
	}
}
