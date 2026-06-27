using UnityEngine;

public abstract class HoldableObject : MonoBehaviour, IHoldableObject
{
	public virtual bool TwoHanded => false;

	protected void OnDestroy()
	{
		if (EquipmentInteractor.hasInstance)
		{
			EquipmentInteractor.instance.ForceDropEquipment(this);
		}
	}

	public abstract void OnHover(InteractionPoint pointHovered, GameObject hoveringHand);

	public abstract void OnGrab(InteractionPoint pointGrabbed, GameObject grabbingHand);

	public abstract void DropItemCleanup();

	public virtual bool OnRelease(DropZone zoneReleased, GameObject releasingHand)
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
