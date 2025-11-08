using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class HeldButton : MonoBehaviour
{
	public Material pressedMaterial;

	public Material unpressedMaterial;

	public MeshRenderer buttonRenderer;

	private bool isOn;

	public float debounceTime = 0.25f;

	public bool leftHandPressable;

	public bool rightHandPressable = true;

	public float pressDuration = 0.5f;

	public UnityEvent onStartPressingButton;

	public UnityEvent onStopPressingButton;

	public UnityEvent onPressButton;

	[TextArea]
	public string offText;

	[TextArea]
	public string onText;

	public Text myText;

	private float touchTime;

	private float releaseTime;

	private bool pendingPress;

	private Collider pendingPressCollider;

	private GorillaTriggerColliderHandIndicator pressingHand;

	private void OnTriggerEnter(Collider other)
	{
		if (base.enabled)
		{
			GorillaTriggerColliderHandIndicator componentInParent = other.GetComponentInParent<GorillaTriggerColliderHandIndicator>();
			if (!(componentInParent == null) && (!componentInParent.isLeftHand || leftHandPressable) && (componentInParent.isLeftHand || rightHandPressable) && (!pendingPress || other != pendingPressCollider))
			{
				onStartPressingButton?.Invoke();
				touchTime = Time.time;
				pendingPressCollider = other;
				pressingHand = componentInParent;
				pendingPress = true;
				SetOn(inOn: true);
				GorillaTagger.Instance.StartVibration(componentInParent.isLeftHand, GorillaTagger.Instance.tapHapticStrength / 2f, GorillaTagger.Instance.tapHapticDuration);
			}
		}
	}

	private void LateUpdate()
	{
		if (!pendingPress)
		{
			return;
		}
		if (touchTime < releaseTime && releaseTime + debounceTime < Time.time)
		{
			onStopPressingButton?.Invoke();
			pendingPress = false;
			pendingPressCollider = null;
			pressingHand = null;
			SetOn(inOn: false);
		}
		else if (touchTime + pressDuration < Time.time)
		{
			onPressButton.Invoke();
			if (pressingHand != null)
			{
				GorillaTagger.Instance.offlineVRRig.PlayHandTapLocal(67, pressingHand.isLeftHand, 0.1f);
				GorillaTagger.Instance.StartVibration(pressingHand.isLeftHand, GorillaTagger.Instance.tapHapticStrength, GorillaTagger.Instance.tapHapticDuration);
			}
			onStopPressingButton?.Invoke();
			pendingPress = false;
			pendingPressCollider = null;
			pressingHand = null;
			releaseTime = Time.time;
			SetOn(inOn: false);
		}
		else if (touchTime > releaseTime && pressingHand != null)
		{
			GorillaTagger.Instance.StartVibration(pressingHand.isLeftHand, GorillaTagger.Instance.tapHapticStrength / 4f, Time.fixedDeltaTime);
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (pendingPress && pendingPressCollider == other)
		{
			releaseTime = Time.time;
			onStopPressingButton?.Invoke();
		}
	}

	public void SetOn(bool inOn)
	{
		if (inOn == isOn)
		{
			return;
		}
		isOn = inOn;
		if (isOn)
		{
			buttonRenderer.material = pressedMaterial;
			if (myText != null)
			{
				myText.text = onText;
			}
		}
		else
		{
			buttonRenderer.material = unpressedMaterial;
			if (myText != null)
			{
				myText.text = offText;
			}
		}
	}
}
