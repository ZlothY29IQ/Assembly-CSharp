using Fusion;
using Photon.Pun;
using UnityEngine;

[NetworkBehaviourWeaved(0)]
public abstract class NetworkHoldableObject : NetworkComponent, IHoldableObject
{
	public virtual bool TwoHanded => false;

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

	public override void ReadDataFusion()
	{
	}

	public override void WriteDataFusion()
	{
	}

	protected override void ReadDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
	}

	protected override void WriteDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
	}

	[WeaverGenerated]
	public override void CopyBackingFieldsToState(bool P_0)
	{
		base.CopyBackingFieldsToState(P_0);
	}

	[WeaverGenerated]
	public override void CopyStateToBackingFields()
	{
		base.CopyStateToBackingFields();
	}
}
