using UnityEngine;
using UnityEngine.Events;

public class TransferrableObjectHoldablePart : HoldableObject, ITickSystemTick
{
	[SerializeField]
	protected TransferrableObject transferrableParentObject;

	[SerializeField]
	private TransferrableObject.ItemStates heldBit = TransferrableObject.ItemStates.Part0Held;

	private bool isHeld;

	protected bool isHeldLeftHand;

	public UnityEvent onGrab;

	public UnityEvent onRelease;

	public UnityEvent onDrop;

	public bool TickRunning { get; set; }

	private void OnEnable()
	{
		TickSystem<object>.AddTickCallback(this);
	}

	private void OnDisable()
	{
		TickSystem<object>.RemoveTickCallback(this);
	}

	public void Tick()
	{
		VRRig rig;
		if (!transferrableParentObject.IsLocalObject())
		{
			rig = transferrableParentObject.myOnlineRig;
			isHeld = (transferrableParentObject.itemState & heldBit) != 0;
			TransferrableObject.PositionState currentState = transferrableParentObject.currentState;
			if (currentState == TransferrableObject.PositionState.OnRightArm || currentState == TransferrableObject.PositionState.InRightHand)
			{
				isHeldLeftHand = isHeld;
			}
			else
			{
				isHeldLeftHand = false;
			}
		}
		else
		{
			rig = VRRig.LocalRig;
		}
		if (isHeld)
		{
			if (transferrableParentObject.InHand())
			{
				UpdateHeld(rig, isHeldLeftHand);
			}
			else if (transferrableParentObject.IsLocalObject())
			{
				OnRelease(null, isHeldLeftHand ? EquipmentInteractor.instance.leftHand : EquipmentInteractor.instance.rightHand);
			}
		}
	}

	protected virtual void UpdateHeld(VRRig rig, bool isHeldLeftHand)
	{
	}

	public override void OnHover(InteractionPoint pointHovered, GameObject hoveringHand)
	{
	}

	public override void OnGrab(InteractionPoint pointGrabbed, GameObject grabbingHand)
	{
		if (!transferrableParentObject.ownerRig || transferrableParentObject.ownerRig.isLocal)
		{
			isHeld = true;
			isHeldLeftHand = grabbingHand == EquipmentInteractor.instance.leftHand;
			transferrableParentObject.itemState |= heldBit;
			EquipmentInteractor.instance.UpdateHandEquipment(this, isHeldLeftHand);
			onGrab?.Invoke();
		}
	}

	public override void DropItemCleanup()
	{
		isHeld = false;
		isHeldLeftHand = false;
		transferrableParentObject.itemState &= ~heldBit;
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
		EquipmentInteractor.instance.UpdateHandEquipment(null, isHeldLeftHand);
		isHeld = false;
		isHeldLeftHand = false;
		transferrableParentObject.itemState &= ~heldBit;
		onRelease?.Invoke();
		return true;
	}
}
