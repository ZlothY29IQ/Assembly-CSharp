using GorillaLocomotion;
using UnityEngine;

public class DampenVolumeSpace : MonoBehaviour
{
	public AudioSource audioSource;

	public float setVolume;

	private void Awake()
	{
		if (audioSource == null)
		{
			base.enabled = false;
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		GTPlayer componentInParent = other.GetComponentInParent<GTPlayer>();
		if ((object)componentInParent != null && componentInParent == GTPlayer.Instance)
		{
			audioSource.volume = setVolume;
		}
	}
}
