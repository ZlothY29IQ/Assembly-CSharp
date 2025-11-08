using GorillaExtensions;
using GorillaLocomotion;
using UnityEngine;

public class HoverboardHandle : HoldableObject
{
	[SerializeField]
	private HoverboardVisual parentVisual;

	[SerializeField]
	private Quaternion defaultHoldAngleLeft;

	[SerializeField]
	private Quaternion defaultHoldAngleRight;

	[SerializeField]
	private Vector3 defaultHoldPosLeft;

	[SerializeField]
	private Vector3 defaultHoldPosRight;

	private int noHapticsUntilFrame = -1;

	public override void OnHover(InteractionPoint pointHovered, GameObject hoveringHand)
	{
		if (GTPlayer.Instance.isHoverAllowed)
		{
			if (Time.frameCount > noHapticsUntilFrame)
			{
				GorillaTagger.Instance.StartVibration(hoveringHand == EquipmentInteractor.instance.leftHand, GorillaTagger.Instance.tapHapticStrength / 8f, GorillaTagger.Instance.tapHapticDuration * 0.5f);
			}
			noHapticsUntilFrame = Time.frameCount + 1;
		}
	}

	public override void OnGrab(InteractionPoint pointGrabbed, GameObject grabbingHand)
	{
		if (GTPlayer.Instance.isHoverAllowed)
		{
			bool flag = grabbingHand == EquipmentInteractor.instance.leftHand;
			Transform transform = (flag ? VRRig.LocalRig.leftHand.rigTarget : VRRig.LocalRig.rightHand.rigTarget);
			Quaternion localRotation;
			Vector3 localPosition;
			if (!parentVisual.IsHeld)
			{
				localRotation = (flag ? defaultHoldAngleLeft : defaultHoldAngleRight);
				localPosition = (flag ? defaultHoldPosLeft : defaultHoldPosRight);
			}
			else
			{
				localRotation = transform.InverseTransformRotation(parentVisual.transform.rotation);
				localPosition = transform.InverseTransformPoint(parentVisual.transform.position);
			}
			parentVisual.SetIsHeld(flag, localPosition, localRotation, parentVisual.boardColor);
			EquipmentInteractor.instance.UpdateHandEquipment(this, flag);
		}
	}

	public override void DropItemCleanup()
	{
		if (parentVisual.gameObject.activeSelf)
		{
			parentVisual.DropFreeBoard();
		}
		parentVisual.SetNotHeld();
	}

	public override bool OnRelease(DropZone zoneReleased, GameObject releasingHand)
	{
		if (EquipmentInteractor.instance.rightHandHeldEquipment == this && releasingHand != EquipmentInteractor.instance.rightHand)
		{
			return false;
		}
		if (EquipmentInteractor.instance.leftHandHeldEquipment == this && releasingHand != EquipmentInteractor.instance.leftHand)
		{
			return false;
		}
		EquipmentInteractor.instance.UpdateHandEquipment(null, parentVisual.IsLeftHanded);
		parentVisual.SetNotHeld();
		return true;
	}
}
