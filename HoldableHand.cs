using GorillaGameModes;
using GorillaLocomotion;
using UnityEngine;

public class HoldableHand : HoldableObject, IGorillaSliceableSimple
{
	[SerializeField]
	private VRRig myPlayer;

	[SerializeField]
	private bool isBody;

	[SerializeField]
	private bool isLeftHand;

	public InteractionPoint interactionPoint;

	public VRRig Rig => myPlayer;

	private void Start()
	{
		if (myPlayer.isOfflineVRRig)
		{
			base.gameObject.SetActive(value: false);
		}
		if (interactionPoint == null)
		{
			interactionPoint = GetComponent<InteractionPoint>();
		}
	}

	public void OnEnable()
	{
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.LateUpdate);
	}

	public void OnDisable()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.LateUpdate);
	}

	public void SliceUpdate()
	{
		interactionPoint.enabled = GameMode.ActiveGameMode is GorillaGuardianManager;
	}

	public override void OnGrab(InteractionPoint pointGrabbed, GameObject grabbingHand)
	{
		if (GameMode.ActiveGameMode is GorillaGuardianManager gorillaGuardianManager && !myPlayer.creator.IsLocal && gorillaGuardianManager.IsPlayerGuardian(NetworkSystem.Instance.LocalPlayer))
		{
			bool flag = grabbingHand == EquipmentInteractor.instance.leftHand;
			myPlayer.netView.SendRPC("GrabbedByPlayer", myPlayer.Creator, isBody, isLeftHand, flag);
			myPlayer.ApplyLocalGrabOverride(isBody, isLeftHand, grabbingHand.transform);
			EquipmentInteractor.instance.UpdateHandEquipment(this, flag);
			ClearOtherGrabs(flag);
		}
	}

	public override bool OnRelease(DropZone zoneReleased, GameObject releasingHand)
	{
		if (!base.OnRelease(zoneReleased, releasingHand))
		{
			return false;
		}
		if (GameMode.ActiveGameMode is GorillaGuardianManager gorillaGuardianManager && !myPlayer.creator.IsLocal)
		{
			bool forLeftHand = releasingHand == EquipmentInteractor.instance.leftHand;
			Vector3 vector = Vector3.zero;
			if (gorillaGuardianManager.IsPlayerGuardian(NetworkSystem.Instance.LocalPlayer))
			{
				vector = GTPlayer.Instance.GetHandVelocityTracker(forLeftHand).GetAverageVelocity(worldSpace: true);
			}
			vector = Vector3.ClampMagnitude(vector, 20f);
			myPlayer.netView.SendRPC("DroppedByPlayer", myPlayer.Creator, vector);
			myPlayer.ClearLocalGrabOverride();
			myPlayer.ApplyLocalTrajectoryOverride(vector);
			EquipmentInteractor.instance.UpdateHandEquipment(null, forLeftHand);
		}
		return true;
	}

	public override void OnHover(InteractionPoint pointHovered, GameObject hoveringHand)
	{
	}

	public override void DropItemCleanup()
	{
		myPlayer.ClearLocalGrabOverride();
	}

	private void ClearOtherGrabs(bool grabbedLeft)
	{
		IHoldableObject holdableObject = (grabbedLeft ? EquipmentInteractor.instance.rightHandHeldEquipment : EquipmentInteractor.instance.leftHandHeldEquipment);
		if (isBody)
		{
			if (holdableObject == myPlayer.leftHolds || holdableObject == myPlayer.rightHolds)
			{
				EquipmentInteractor.instance.UpdateHandEquipment(null, !grabbedLeft);
			}
		}
		else if (isLeftHand)
		{
			if (holdableObject == myPlayer.rightHolds || holdableObject == myPlayer.bodyHolds)
			{
				EquipmentInteractor.instance.UpdateHandEquipment(null, !grabbedLeft);
			}
		}
		else if (holdableObject == myPlayer.leftHolds || holdableObject == myPlayer.bodyHolds)
		{
			EquipmentInteractor.instance.UpdateHandEquipment(null, !grabbedLeft);
		}
	}
}
