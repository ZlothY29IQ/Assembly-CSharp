using UnityEngine;
using UnityEngine.Events;

public class SimpleButton : MonoBehaviour, IClickable
{
	protected GameObject activator;

	private float pressTime;

	[SerializeField]
	private float coolDown = 0.1f;

	[SerializeField]
	private int audioCLipIndex = 67;

	[SerializeField]
	private UnityEvent Press;

	[SerializeField]
	private UnityEvent Release;

	protected void OnTriggerEnter(Collider collider)
	{
		if (activator != null || Time.time - pressTime < coolDown)
		{
			return;
		}
		GorillaTriggerColliderHandIndicator componentInParent = collider.gameObject.GetComponentInParent<GorillaTriggerColliderHandIndicator>();
		if ((bool)componentInParent)
		{
			activator = collider.gameObject;
			pressTime = Time.time;
			if (audioCLipIndex > 0)
			{
				GorillaTagger.Instance.offlineVRRig.PlayHandTapLocal(audioCLipIndex, componentInParent.isLeftHand, 0.05f);
				GorillaTagger.Instance.StartVibration(componentInParent.isLeftHand, GorillaTagger.Instance.tapHapticStrength / 2f, GorillaTagger.Instance.tapHapticDuration);
			}
			DoPress(componentInParent.isLeftHand);
		}
	}

	protected void OnTriggerExit(Collider collider)
	{
		if (activator == collider.gameObject)
		{
			activator = null;
			pressTime = Time.time;
			Release?.Invoke();
		}
	}

	private void DoPress(bool isLeft)
	{
		Press?.Invoke();
		handlePress(isLeft);
	}

	protected virtual void handlePress(bool isLeft)
	{
	}

	public void Click(bool leftHand = false)
	{
		DoPress(isLeft: false);
	}
}
