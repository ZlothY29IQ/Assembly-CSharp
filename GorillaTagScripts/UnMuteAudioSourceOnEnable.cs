using UnityEngine;

namespace GorillaTagScripts;

public class UnMuteAudioSourceOnEnable : MonoBehaviour
{
	public AudioSource audioSource;

	public float originalVolume;

	public void Awake()
	{
		originalVolume = audioSource.volume;
	}

	public void OnEnable()
	{
		audioSource.volume = originalVolume;
	}

	public void OnDisable()
	{
		audioSource.volume = 0f;
	}
}
