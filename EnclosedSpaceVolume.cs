using GorillaLocomotion;
using UnityEngine;

public class EnclosedSpaceVolume : GorillaTriggerBox
{
	public AudioSource audioSourceInside;

	public AudioSource audioSourceOutside;

	public float loudVolume;

	public float quietVolume;

	private void Awake()
	{
		audioSourceInside.volume = quietVolume;
		audioSourceOutside.volume = loudVolume;
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.attachedRigidbody.GetComponentInParent<GTPlayer>() != null)
		{
			audioSourceInside.volume = loudVolume;
			audioSourceOutside.volume = quietVolume;
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.attachedRigidbody.GetComponentInParent<GTPlayer>() != null)
		{
			audioSourceInside.volume = quietVolume;
			audioSourceOutside.volume = loudVolume;
		}
	}
}
