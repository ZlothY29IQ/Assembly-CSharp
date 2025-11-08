using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioLooper : MonoBehaviour
{
	private AudioSource audioSource;

	[SerializeField]
	private AudioClip loopClip;

	[SerializeField]
	private AudioClip[] interjectionClips;

	[SerializeField]
	private float interjectionLikelyhood = 0.5f;

	protected virtual void Awake()
	{
		audioSource = GetComponent<AudioSource>();
	}

	private void Update()
	{
		if (!audioSource.isPlaying)
		{
			if (audioSource.clip == loopClip && interjectionClips.Length != 0 && Random.value < interjectionLikelyhood)
			{
				audioSource.clip = interjectionClips[Random.Range(0, interjectionClips.Length)];
			}
			else
			{
				audioSource.clip = loopClip;
			}
			audioSource.GTPlay();
		}
	}
}
