using GorillaLocomotion;
using UnityEngine;

public class ManipulatableObject : HoldableObject
{
	protected bool isHeld;

	protected GameObject holdingHand;

	protected virtual void OnStartManipulation(GameObject grabbingHand)
	{
	}

	protected virtual void OnStopManipulation(GameObject releasingHand, Vector3 releaseVelocity)
	{
	}

	protected virtual bool ShouldHandDetach(GameObject hand)
	{
		return false;
	}

	protected virtual void OnHeldUpdate(GameObject hand)
	{
	}

	protected virtual void OnReleasedUpdate()
	{
	}

	public virtual void LateUpdate()
	{
		if (isHeld)
		{
			if (holdingHand == null)
			{
				EquipmentInteractor.instance.ForceDropManipulatableObject(this);
				return;
			}
			OnHeldUpdate(holdingHand);
			if (ShouldHandDetach(holdingHand))
			{
				EquipmentInteractor.instance.ForceDropManipulatableObject(this);
			}
		}
		else
		{
			OnReleasedUpdate();
		}
	}

	public override void OnHover(InteractionPoint pointHovered, GameObject hoveringHand)
	{
	}

	public override void OnGrab(InteractionPoint pointGrabbed, GameObject grabbingHand)
	{
		bool forLeftHand = grabbingHand == EquipmentInteractor.instance.leftHand;
		EquipmentInteractor.instance.UpdateHandEquipment(this, forLeftHand);
		isHeld = true;
		holdingHand = grabbingHand;
		OnStartManipulation(holdingHand);
	}

	public override bool OnRelease(DropZone zoneReleased, GameObject releasingHand)
	{
		if (!base.OnRelease(zoneReleased, releasingHand))
		{
			return false;
		}
		bool flag = releasingHand == EquipmentInteractor.instance.leftHand;
		Vector3 averageVelocity = GTPlayer.Instance.GetHandVelocityTracker(flag).GetAverageVelocity(worldSpace: true);
		if (flag)
		{
			EquipmentInteractor.instance.leftHandHeldEquipment = null;
		}
		else
		{
			EquipmentInteractor.instance.rightHandHeldEquipment = null;
		}
		isHeld = false;
		holdingHand = null;
		OnStopManipulation(releasingHand, averageVelocity);
		return true;
	}

	public override void DropItemCleanup()
	{
	}
}
