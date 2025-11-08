using GorillaTag;
using GorillaTag.CosmeticSystem;
using UnityEngine;

public class HypnoRing : MonoBehaviour, ISpawnable
{
	[SerializeField]
	private bool attachedToLeftHand;

	private VRRig myRig;

	[SerializeField]
	private float rotationSpeed;

	[SerializeField]
	private AudioSource audioSource;

	[SerializeField]
	private float maxVolume = 1f;

	[SerializeField]
	private float fadeInDuration;

	[SerializeField]
	private float fadeOutDuration;

	private float currentVolume;

	bool ISpawnable.IsSpawned { get; set; }

	ECosmeticSelectSide ISpawnable.CosmeticSelectedSide { get; set; }

	void ISpawnable.OnDespawn()
	{
	}

	void ISpawnable.OnSpawn(VRRig rig)
	{
		myRig = rig;
	}

	private void Update()
	{
		if ((attachedToLeftHand ? myRig.leftIndex.calcT : myRig.rightIndex.calcT) > 0.5f)
		{
			base.transform.localRotation *= Quaternion.AngleAxis(Time.deltaTime * rotationSpeed, Vector3.up);
			currentVolume = Mathf.MoveTowards(currentVolume, maxVolume, Time.deltaTime / fadeInDuration);
			audioSource.volume = currentVolume;
			if (!audioSource.isPlaying)
			{
				audioSource.GTPlay();
			}
			return;
		}
		currentVolume = Mathf.MoveTowards(currentVolume, 0f, Time.deltaTime / fadeOutDuration);
		if (audioSource.isPlaying)
		{
			if (currentVolume == 0f)
			{
				audioSource.GTStop();
			}
			else
			{
				audioSource.volume = currentVolume;
			}
		}
	}
}
