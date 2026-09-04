using System;
using UnityEngine;

public class ConsentHoldButton : MonoBehaviour
{
	[Serializable]
	public struct Feedback
	{
		[Tooltip("Hand-tap sound index passed to VRRig.PlayHandTapLocal when the hold completes.")]
		public int completeSoundIndex;

		public float completeSoundVolume;

		[Tooltip("Haptic strength while touching the button, as a multiplier on GorillaTagger.tapHapticStrength.")]
		public float pressHapticScale;

		[Tooltip("Haptic strength of the per-frame pulse while holding, as a multiplier on GorillaTagger.tapHapticStrength.")]
		public float holdPulseHapticScale;

		[Tooltip("Haptic strength when the hold completes, as a multiplier on GorillaTagger.tapHapticStrength.")]
		public float completeHapticScale;

		[Tooltip("Seconds after a completed hold before the button can be pressed again.")]
		public float retriggerCooldownSeconds;

		public static Feedback Default => new Feedback
		{
			completeSoundIndex = 67,
			completeSoundVolume = 0.1f,
			pressHapticScale = 0.5f,
			holdPulseHapticScale = 0.25f,
			completeHapticScale = 1f,
			retriggerCooldownSeconds = 0.25f
		};
	}

	[Tooltip("Seconds the button must be held before it triggers.")]
	[SerializeField]
	private float holdDurationSeconds = 1.5f;

	[SerializeField]
	private bool leftHandPressable;

	[SerializeField]
	private bool rightHandPressable = true;

	[Tooltip("Stretched child image scaled on X from 0 to 1 while held (pivot must be on the left edge).")]
	[SerializeField]
	private RectTransform fill;

	[SerializeField]
	private Feedback feedback = Feedback.Default;

	private bool pressing;

	private float elapsed;

	private Collider pressingCollider;

	private bool pressingHandIsLeft;

	private float cooldownUntil;

	public event Action HoldComplete;

	public void ResetHold()
	{
		pressing = false;
		elapsed = 0f;
		pressingCollider = null;
		if (fill != null)
		{
			fill.localScale = new Vector3(0f, 1f, 1f);
		}
	}

	private void OnDisable()
	{
		ResetHold();
	}

	private void OnTriggerEnter(Collider other)
	{
		if (pressing || Time.time < cooldownUntil)
		{
			return;
		}
		GorillaTriggerColliderHandIndicator componentInParent = other.GetComponentInParent<GorillaTriggerColliderHandIndicator>();
		if (!(componentInParent == null) && (!componentInParent.isLeftHand || leftHandPressable) && (componentInParent.isLeftHand || rightHandPressable))
		{
			pressing = true;
			elapsed = 0f;
			pressingCollider = other;
			pressingHandIsLeft = componentInParent.isLeftHand;
			if (GorillaTagger.Instance != null)
			{
				GorillaTagger.Instance.StartVibration(componentInParent.isLeftHand, GorillaTagger.Instance.tapHapticStrength * feedback.pressHapticScale, GorillaTagger.Instance.tapHapticDuration);
			}
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (pressing && other == pressingCollider)
		{
			ResetHold();
		}
	}

	private void Update()
	{
		if (!pressing)
		{
			return;
		}
		if (pressingCollider == null || !pressingCollider.gameObject.activeInHierarchy)
		{
			ResetHold();
			return;
		}
		elapsed += Time.deltaTime;
		float num = ((holdDurationSeconds > 0f) ? Mathf.Clamp01(elapsed / holdDurationSeconds) : 1f);
		if (fill != null)
		{
			fill.localScale = new Vector3(num, 1f, 1f);
		}
		if (GorillaTagger.Instance != null)
		{
			GorillaTagger.Instance.StartVibration(pressingHandIsLeft, GorillaTagger.Instance.tapHapticStrength * feedback.holdPulseHapticScale, Time.fixedDeltaTime);
		}
		if (num >= 1f)
		{
			bool flag = pressingHandIsLeft;
			ResetHold();
			cooldownUntil = Time.time + feedback.retriggerCooldownSeconds;
			if (GorillaTagger.Instance != null)
			{
				GorillaTagger.Instance.offlineVRRig.PlayHandTapLocal(feedback.completeSoundIndex, flag, feedback.completeSoundVolume);
				GorillaTagger.Instance.StartVibration(flag, GorillaTagger.Instance.tapHapticStrength * feedback.completeHapticScale, GorillaTagger.Instance.tapHapticDuration);
			}
			this.HoldComplete?.Invoke();
		}
	}
}
