using UnityEngine;

public class AnimationEventListener : MonoBehaviour
{
	[Tooltip("Set this if calling ActivateObject, DeactivateObject, or ToggleObject")]
	[SerializeField]
	private GameObject targetObject;

	[Tooltip("Set this if calling PlayParticles or StopParticles")]
	[SerializeField]
	private ParticleSystem particles;

	[Tooltip("Set this if calling PlaySoundAtIndex or StopAudio")]
	[SerializeField]
	private AudioSource audioSource;

	[Tooltip("Set this if calling PlaySoundAtIndex")]
	[SerializeField]
	private AudioClip[] audioClips;

	public void PlaySoundAtIndex(int index)
	{
		if (audioClips.Length > index && index >= 0 && !(audioSource == null) && !(audioClips[index] == null))
		{
			audioSource.GTPlayOneShot(audioClips[index]);
		}
	}

	public void StopAudio()
	{
		if (!(audioSource == null) && audioSource.isPlaying)
		{
			audioSource.Stop();
		}
	}

	public void ActivateObject()
	{
		if (targetObject != null)
		{
			targetObject.SetActive(value: true);
		}
	}

	public void DeactivateObject()
	{
		if (targetObject != null)
		{
			targetObject.SetActive(value: false);
		}
	}

	public void ToggleObject()
	{
		if (targetObject != null)
		{
			targetObject.SetActive(!targetObject.activeSelf);
		}
	}

	public void PlayParticles()
	{
		if (particles != null && !particles.isPlaying)
		{
			particles.Play();
		}
	}

	public void StopParticles()
	{
		if (particles != null && particles.isPlaying)
		{
			particles.Stop();
		}
	}
}
