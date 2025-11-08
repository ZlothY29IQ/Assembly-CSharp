using UnityEngine;

public class RandomAudioStart : MonoBehaviour, IBuildValidation
{
	public AudioSource audioSource;

	public bool BuildValidationCheck()
	{
		if (audioSource == null)
		{
			Debug.LogError("audio source is missing for RandomAudioStart, it won't work correctly", base.gameObject);
			return false;
		}
		return true;
	}

	private void OnEnable()
	{
		audioSource.time = Random.value * audioSource.clip.length;
	}

	[ContextMenu("Assign Audio Source")]
	public void AssignAudioSource()
	{
		audioSource = GetComponent<AudioSource>();
	}
}
