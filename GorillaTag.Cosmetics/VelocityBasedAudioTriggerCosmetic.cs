using GorillaLocomotion.Climbing;
using UnityEngine;

namespace GorillaTag.Cosmetics;

public class VelocityBasedAudioTriggerCosmetic : MonoBehaviour
{
	[SerializeField]
	private GorillaVelocityTracker velocityTracker;

	[SerializeField]
	private AudioSource audioSource;

	[SerializeField]
	private AudioClip audioClip;

	[SerializeField]
	private SoundBankPlayer soundBank;

	[Tooltip(" Minimum velocity to trigger audio")]
	[SerializeField]
	private float minVelocityThreshold = 0.5f;

	[SerializeField]
	private float maxVelocity = 2f;

	[SerializeField]
	private float minOutputVolume;

	[SerializeField]
	private float maxOutputVolume = 1f;

	private void Awake()
	{
		if (audioClip != null)
		{
			audioSource.clip = audioClip;
		}
		if (soundBank != null && audioSource != null)
		{
			soundBank.audioSource = audioSource;
		}
	}

	private void Update()
	{
		Vector3 averageVelocity = velocityTracker.GetAverageVelocity(worldSpace: true);
		if (averageVelocity.magnitude < minVelocityThreshold)
		{
			return;
		}
		float t = Mathf.InverseLerp(minVelocityThreshold, maxVelocity, averageVelocity.magnitude);
		float num = Mathf.Lerp(minOutputVolume, maxOutputVolume, t);
		audioSource.volume = num;
		if (audioSource != null && !audioSource.isPlaying && audioClip != null)
		{
			audioSource.clip = audioClip;
			if (audioSource.isActiveAndEnabled)
			{
				audioSource.GTPlay();
			}
		}
		else if (soundBank != null && soundBank.soundBank != null && !soundBank.isPlaying)
		{
			soundBank.Play(num);
		}
	}
}
