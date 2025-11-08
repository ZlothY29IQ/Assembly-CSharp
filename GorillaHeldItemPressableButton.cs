using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;

public class GorillaHeldItemPressableButton : MonoBehaviour, IDelayedExecListener
{
	public int pressButtonSoundIndex = 67;

	public bool isOn;

	public float delayBetweenSuccessfulPresses = 0.25f;

	private float touchTime;

	public HeldItemButtonMode mode;

	public List<TransferrableObject> acceptedHoldables;

	private List<Type> acceptedTypes;

	public bool acceptAnyHoldableThatMatchesType = true;

	public HeldItemButtonConsumeMode consumeItem;

	[Space]
	public UnityEvent<TransferrableObject> onPressButton;

	[Space]
	public UnityEvent<TransferrableObject> onReleaseButton;

	public event Action<GorillaHeldItemPressableButton, TransferrableObject, bool> onPressed;

	public event Action<GorillaHeldItemPressableButton, TransferrableObject, bool> onReleased;

	private void Start()
	{
		if (!acceptAnyHoldableThatMatchesType)
		{
			return;
		}
		acceptedTypes = new List<Type>();
		foreach (TransferrableObject acceptedHoldable in acceptedHoldables)
		{
			acceptedTypes.Add(acceptedHoldable.GetType());
		}
	}

	protected void OnTriggerEnter(Collider collider)
	{
		if (!base.enabled || !(touchTime + delayBetweenSuccessfulPresses < Time.time))
		{
			return;
		}
		TransferrableObject componentInParent = collider.GetComponentInParent<TransferrableObject>();
		if (componentInParent == null)
		{
			componentInParent = collider.transform.parent.GetComponentInParent<TransferrableObject>();
		}
		if (componentInParent == null || !componentInParent.InHand())
		{
			return;
		}
		if (acceptAnyHoldableThatMatchesType)
		{
			if (!acceptedTypes.Contains(componentInParent.GetType()))
			{
				return;
			}
		}
		else if (!acceptedHoldables.Contains(componentInParent))
		{
			return;
		}
		touchTime = Time.time;
		switch (mode)
		{
		case HeldItemButtonMode.OneShot:
			onPressButton?.Invoke(componentInParent);
			this.onPressed?.Invoke(this, componentInParent, componentInParent.InLeftHand());
			ButtonActivation(componentInParent);
			ButtonActivationWithHand(componentInParent, componentInParent.InLeftHand());
			break;
		case HeldItemButtonMode.ResetAfterDelay:
			isOn = true;
			onPressButton?.Invoke(componentInParent);
			this.onPressed?.Invoke(this, componentInParent, componentInParent.InLeftHand());
			ButtonActivation(componentInParent);
			ButtonActivationWithHand(componentInParent, componentInParent.InLeftHand());
			GTDelayedExec.Add(this, delayBetweenSuccessfulPresses, 0);
			break;
		case HeldItemButtonMode.Toggle:
			isOn = !isOn;
			if (isOn)
			{
				onPressButton?.Invoke(componentInParent);
				this.onPressed?.Invoke(this, componentInParent, componentInParent.InLeftHand());
				ButtonActivation(componentInParent);
				ButtonActivationWithHand(componentInParent, componentInParent.InLeftHand());
			}
			else
			{
				onReleaseButton?.Invoke(componentInParent);
				this.onReleased?.Invoke(this, componentInParent, componentInParent.InLeftHand());
			}
			break;
		}
		GorillaTagger.Instance.offlineVRRig.PlayHandTapLocal(pressButtonSoundIndex, componentInParent.InLeftHand(), 0.05f);
		GorillaTagger.Instance.StartVibration(componentInParent.InLeftHand(), GorillaTagger.Instance.tapHapticStrength / 2f, GorillaTagger.Instance.tapHapticDuration);
		if (NetworkSystem.Instance.InRoom && GorillaTagger.Instance.myVRRig != null)
		{
			GorillaTagger.Instance.myVRRig.SendRPC("RPC_PlayHandTap", RpcTarget.Others, 67, componentInParent.InLeftHand(), 0.05f);
		}
		switch (consumeItem)
		{
		case HeldItemButtonConsumeMode.Destroy:
			componentInParent.OnMyCreatorLeft();
			break;
		case HeldItemButtonConsumeMode.Disable:
			componentInParent.gameObject.SetActive(value: false);
			break;
		case HeldItemButtonConsumeMode.None:
			break;
		}
	}

	public virtual void ButtonActivation(TransferrableObject holdable)
	{
	}

	public virtual void ButtonActivationWithHand(TransferrableObject holdable, bool isLeftHand)
	{
	}

	public virtual void ResetState()
	{
		isOn = false;
		onReleaseButton?.Invoke(null);
		this.onReleased?.Invoke(this, null, arg3: false);
	}

	public void OnDelayedAction(int contextIndex)
	{
		ResetState();
	}
}
