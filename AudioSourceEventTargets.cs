using UnityEngine;

public class AudioSourceEventTargets : MonoBehaviour
{
	private AudioSource audioSource;

	private float fadeVolume;

	private float fadeSpeed;

	private void Awake()
	{
		audioSource = GetComponent<AudioSource>();
		fadeVolume = audioSource.volume;
		base.enabled = false;
	}

	public void SetFadeSpeed(float arg)
	{
		fadeSpeed = Mathf.Max(arg, 0.01f);
	}

	public void StartFade(float arg)
	{
		fadeVolume = Mathf.Clamp01(arg);
		base.enabled = true;
	}

	public void Update()
	{
		if (audioSource.volume != fadeVolume)
		{
			audioSource.volume = Mathf.MoveTowards(audioSource.volume, fadeVolume, fadeSpeed * Time.deltaTime);
		}
		base.enabled = audioSource.volume != fadeVolume;
	}
}
