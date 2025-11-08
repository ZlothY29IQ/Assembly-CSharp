using GorillaExtensions;
using GorillaLocomotion;
using UnityEngine;
using UnityEngine.Events;

namespace GorillaTag.Cosmetics;

public class TransferrableObjectHoldablePart_Pin : TransferrableObjectHoldablePart
{
	[SerializeField]
	private float breakStrengthThreshold = 0.8f;

	[SerializeField]
	private float maxHandSnapDistance = 0.5f;

	[SerializeField]
	private Transform pin;

	public UnityEvent OnBreak;

	public UnityEvent OnBreakLocal;

	public UnityEvent OnEnableHoldable;

	protected void OnEnable()
	{
		OnEnableHoldable?.Invoke();
	}

	protected override void UpdateHeld(VRRig rig, bool isHeldLeftHand)
	{
		if (!rig.isOfflineVRRig)
		{
			return;
		}
		Transform controllerTransform = GTPlayer.Instance.GetControllerTransform(isHeldLeftHand);
		if (GTPlayer.Instance.GetInteractPointVelocityTracker(isHeldLeftHand).GetAverageVelocity(worldSpace: true).magnitude > breakStrengthThreshold || (controllerTransform.position - pin.transform.position).IsLongerThan(maxHandSnapDistance))
		{
			OnRelease(null, isHeldLeftHand ? EquipmentInteractor.instance.leftHand : EquipmentInteractor.instance.rightHand);
			OnBreak?.Invoke();
			if ((bool)transferrableParentObject && transferrableParentObject.IsMyItem())
			{
				OnBreakLocal?.Invoke();
			}
		}
		else
		{
			controllerTransform.position = pin.position;
		}
	}
}
