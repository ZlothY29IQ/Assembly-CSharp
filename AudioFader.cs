using UnityEngine;

public class AudioFader : MonoBehaviour
{
	[SerializeField]
	private AudioSource audioToFade;

	[SerializeField]
	private AudioSource outro;

	[SerializeField]
	private float fadeInDuration = 0.3f;

	[SerializeField]
	private float fadeOutDuration = 0.3f;

	[SerializeField]
	private float maxVolume = 1f;

	private float currentVolume;

	private float targetVolume;

	private float currentFadeSpeed;

	private float fadeInSpeed;

	private float fadeOutSpeed;

	private void Start()
	{
		fadeInSpeed = maxVolume / fadeInDuration;
		fadeOutSpeed = maxVolume / fadeOutDuration;
	}

	public void FadeIn()
	{
		targetVolume = maxVolume;
		if (fadeInDuration > 0f)
		{
			base.enabled = true;
			currentFadeSpeed = fadeInSpeed;
		}
		else
		{
			currentVolume = maxVolume;
		}
		audioToFade.volume = currentVolume;
		if (!audioToFade.isPlaying)
		{
			audioToFade.GTPlay();
		}
	}

	public void FadeOut()
	{
		targetVolume = 0f;
		if (fadeOutDuration > 0f)
		{
			base.enabled = true;
			currentFadeSpeed = fadeOutSpeed;
		}
		else
		{
			currentVolume = 0f;
			if (audioToFade.isPlaying)
			{
				audioToFade.Stop();
			}
		}
		if (outro != null && currentVolume > 0f)
		{
			outro.volume = currentVolume;
			outro.GTPlay();
		}
	}

	private void Update()
	{
		currentVolume = Mathf.MoveTowards(currentVolume, targetVolume, currentFadeSpeed * Time.deltaTime);
		audioToFade.volume = currentVolume;
		if (currentVolume == targetVolume)
		{
			base.enabled = false;
			if (currentVolume == 0f && audioToFade.isPlaying)
			{
				audioToFade.Stop();
			}
		}
	}
}
