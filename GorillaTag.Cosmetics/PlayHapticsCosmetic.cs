using GorillaLocomotion;
using UnityEngine;

namespace GorillaTag.Cosmetics;

public class PlayHapticsCosmetic : MonoBehaviour
{
	[SerializeField]
	private float hapticDuration;

	[SerializeField]
	private float hapticStrength;

	[SerializeField]
	private float minHapticStrengthThreshold;

	[SerializeField]
	private float maxHapticStrengthThreshold;

	[Tooltip("Only check this box if you are not setting the left/hand right from the subscriber")]
	[SerializeField]
	private bool leftHand;

	private TransferrableObject parentTransferable;

	private void Awake()
	{
		parentTransferable = GetComponentInParent<TransferrableObject>();
	}

	public void PlayHaptics()
	{
		GorillaTagger.Instance.StartVibration(leftHand, hapticStrength, hapticDuration);
	}

	public void PlayHapticsTransferableObject()
	{
		if (parentTransferable != null && parentTransferable.IsMyItem())
		{
			bool forLeftController = parentTransferable.InLeftHand();
			GorillaTagger.Instance.StartVibration(forLeftController, hapticStrength, hapticDuration);
		}
	}

	public void PlayHaptics(bool isLeftHand)
	{
		GorillaTagger.Instance.StartVibration(isLeftHand, hapticStrength, hapticDuration);
	}

	public void PlayHapticsBothHands(bool isLeftHand)
	{
		PlayHaptics(isLeftHand: false);
		PlayHaptics(isLeftHand: true);
	}

	public void PlayHaptics(bool isLeftHand, float value)
	{
		GorillaTagger.Instance.StartVibration(isLeftHand, hapticStrength, hapticDuration);
	}

	public void PlayHapticsBothHands(bool isLeftHand, float value)
	{
		PlayHaptics(isLeftHand: false, value);
		PlayHaptics(isLeftHand: true, value);
	}

	public void PlayHaptics(bool isLeftHand, Collider other)
	{
		GorillaTagger.Instance.StartVibration(isLeftHand, hapticStrength, hapticDuration);
	}

	public void PlayHapticsBothHands(bool isLeftHand, Collider other)
	{
		PlayHaptics(isLeftHand: false, other);
		PlayHaptics(isLeftHand: true, other);
	}

	public void PlayHaptics(bool isLeftHand, Collision other)
	{
		GorillaTagger.Instance.StartVibration(isLeftHand, hapticStrength, hapticDuration);
	}

	public void PlayHapticsBothHands(bool isLeftHand, Collision other)
	{
		PlayHaptics(isLeftHand: false, other);
		PlayHaptics(isLeftHand: true, other);
	}

	public void PlayHapticsByButtonValue(bool isLeftHand, float strength)
	{
		float amplitude = Mathf.InverseLerp(minHapticStrengthThreshold, maxHapticStrengthThreshold, strength);
		GorillaTagger.Instance.StartVibration(isLeftHand, amplitude, hapticDuration);
	}

	public void PlayHapticsByButtonValueBothHands(bool isLeftHand, float strength)
	{
		PlayHapticsByButtonValue(isLeftHand: false, strength);
		PlayHapticsByButtonValue(isLeftHand: true, strength);
	}

	public void PlayHapticsByVelocity(bool isLeftHand, float velocity)
	{
		float magnitude = GTPlayer.Instance.GetInteractPointVelocityTracker(isLeftHand).GetAverageVelocity(worldSpace: true).magnitude;
		magnitude = Mathf.InverseLerp(minHapticStrengthThreshold, maxHapticStrengthThreshold, magnitude);
		GorillaTagger.Instance.StartVibration(isLeftHand, magnitude, hapticDuration);
	}

	public void PlayHapticsByVelocityBothHands(bool isLeftHand, float velocity)
	{
		PlayHapticsByVelocity(isLeftHand: false, velocity);
		PlayHapticsByVelocity(isLeftHand: true, velocity);
	}
}
