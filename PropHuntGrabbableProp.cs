using System.Collections.Generic;
using UnityEngine;

public class PropHuntGrabbableProp : HoldableObject
{
	public PropHuntHandFollower handFollower;

	public Vector3 offset;

	public List<InteractionPoint> interactionPoints;

	public override void OnHover(InteractionPoint pointHovered, GameObject hoveringHand)
	{
	}

	public override void OnGrab(InteractionPoint pointGrabbed, GameObject grabbingHand)
	{
		bool flag = grabbingHand == EquipmentInteractor.instance.leftHand;
		handFollower.SwitchHand(flag);
		EquipmentInteractor.instance.UpdateHandEquipment(this, flag);
	}

	public override void DropItemCleanup()
	{
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
		return true;
	}
}
