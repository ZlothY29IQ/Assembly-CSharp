using UnityEngine;

public class PlayAudioOnEnable : MonoBehaviour
{
	[SerializeField]
	private AudioSource audioSource;

	[SerializeField]
	private AudioClip[] audioClips;

	private void OnEnable()
	{
		audioSource.clip = audioClips[Random.Range(0, audioClips.Length)];
		audioSource.GTPlay();
	}
}
