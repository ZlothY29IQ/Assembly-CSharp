using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;

public class GorillaPressableReleaseButton : GorillaPressableButton
{
	public UnityEvent onReleaseButton;

	private Collider touchingCollider;

	private new void OnTriggerEnter(Collider other)
	{
		if (!base.enabled || !(touchTime + debounceTime < Time.time) || (bool)touchingCollider)
		{
			return;
		}
		GorillaTriggerColliderHandIndicator component = other.GetComponent<GorillaTriggerColliderHandIndicator>();
		if (!(component == null))
		{
			touchTime = Time.time;
			touchingCollider = other;
			onPressButton?.Invoke();
			ButtonActivation();
			ButtonActivationWithHand(component.isLeftHand);
			GorillaTagger.Instance.offlineVRRig.PlayHandTapLocal(pressButtonSoundIndex, component.isLeftHand, 0.05f);
			GorillaTagger.Instance.StartVibration(component.isLeftHand, GorillaTagger.Instance.tapHapticStrength / 2f, GorillaTagger.Instance.tapHapticDuration);
			if (NetworkSystem.Instance.InRoom && GorillaTagger.Instance.myVRRig != null)
			{
				GorillaTagger.Instance.myVRRig.SendRPC("RPC_PlayHandTap", RpcTarget.Others, 67, component.isLeftHand, 0.05f);
			}
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (!base.enabled || other != touchingCollider)
		{
			return;
		}
		touchingCollider = null;
		GorillaTriggerColliderHandIndicator component = other.GetComponent<GorillaTriggerColliderHandIndicator>();
		if (!(component == null))
		{
			onReleaseButton?.Invoke();
			ButtonDeactivation();
			ButtonDeactivationWithHand(component.isLeftHand);
			GorillaTagger.Instance.offlineVRRig.PlayHandTapLocal(pressButtonSoundIndex, component.isLeftHand, 0.05f);
			GorillaTagger.Instance.StartVibration(component.isLeftHand, GorillaTagger.Instance.tapHapticStrength / 2f, GorillaTagger.Instance.tapHapticDuration);
			if (NetworkSystem.Instance.InRoom && GorillaTagger.Instance.myVRRig != null)
			{
				GorillaTagger.Instance.myVRRig.SendRPC("RPC_PlayHandTap", RpcTarget.Others, 67, component.isLeftHand, 0.05f);
			}
		}
	}

	public override void ResetState()
	{
		base.ResetState();
		touchingCollider = null;
	}

	public virtual void ButtonDeactivation()
	{
	}

	public virtual void ButtonDeactivationWithHand(bool isLeftHand)
	{
	}
}
