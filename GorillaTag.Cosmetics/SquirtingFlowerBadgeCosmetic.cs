using GorillaTag.CosmeticSystem;
using UnityEngine;

namespace GorillaTag.Cosmetics;

public class SquirtingFlowerBadgeCosmetic : MonoBehaviour, ISpawnable, IFingerFlexListener
{
	[SerializeField]
	private ParticleSystem particlesToPlay;

	[SerializeField]
	private GameObject objectToEnable;

	[SerializeField]
	private AudioClip audioToPlay;

	[SerializeField]
	private AudioSource audioSource;

	[SerializeField]
	private float coolDownTimer = 2f;

	[SerializeField]
	private bool leftHand;

	private float triggeredTime;

	private bool restartTimer;

	private bool buttonReleased = true;

	public VRRig MyRig { get; private set; }

	public bool IsSpawned { get; set; }

	public ECosmeticSelectSide CosmeticSelectedSide { get; set; }

	public void OnSpawn(VRRig rig)
	{
		MyRig = rig;
	}

	public void OnDespawn()
	{
	}

	private void Update()
	{
		if (!restartTimer && Time.time - triggeredTime >= coolDownTimer)
		{
			restartTimer = true;
		}
	}

	private void OnPlayEffectLocal()
	{
		if (particlesToPlay != null)
		{
			particlesToPlay.Play();
		}
		if (objectToEnable != null)
		{
			objectToEnable.SetActive(value: true);
		}
		if (audioSource != null && audioToPlay != null)
		{
			audioSource.GTPlayOneShot(audioToPlay);
		}
		restartTimer = false;
		triggeredTime = Time.time;
	}

	public void OnButtonPressed(bool isLeftHand, float value)
	{
		if (FingerFlexValidation(isLeftHand) && restartTimer && buttonReleased)
		{
			OnPlayEffectLocal();
			buttonReleased = false;
		}
	}

	public void OnButtonReleased(bool isLeftHand, float value)
	{
		if (FingerFlexValidation(isLeftHand))
		{
			buttonReleased = true;
		}
	}

	public void OnButtonPressStayed(bool isLeftHand, float value)
	{
	}

	public bool FingerFlexValidation(bool isLeftHand)
	{
		if (leftHand && !isLeftHand)
		{
			return false;
		}
		if (!leftHand && isLeftHand)
		{
			return false;
		}
		return true;
	}
}
