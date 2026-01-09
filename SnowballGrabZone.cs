using UnityEngine;

public class SnowballGrabZone : HoldableObject
{
	[GorillaSoundLookup]
	public int materialIndex;

	public override void OnHover(InteractionPoint pointHovered, GameObject hoveringHand)
	{
	}

	public override void DropItemCleanup()
	{
	}

	public override void OnGrab(InteractionPoint pointGrabbed, GameObject grabbingHand)
	{
		bool flag = grabbingHand == EquipmentInteractor.instance.leftHand;
		if (!(flag ? EquipmentInteractor.instance.disableLeftGrab : EquipmentInteractor.instance.disableRightGrab))
		{
			(flag ? SnowballMaker.leftHandInstance : SnowballMaker.rightHandInstance).TryCreateSnowball(materialIndex, out var _);
		}
	}
}
