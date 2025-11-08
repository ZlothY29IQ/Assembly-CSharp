using System;
using GorillaExtensions;
using GorillaLocomotion;
using UnityEngine;

public class FreeHoverboardHandle : HoldableObject
{
	[SerializeField]
	private FreeHoverboardInstance parentFreeBoard;

	private bool hasParentBoard;

	[SerializeField]
	private Vector3 defaultHoldPosLeft;

	[SerializeField]
	private Vector3 defaultHoldPosRight;

	[SerializeField]
	private Quaternion defaultHoldAngleLeft;

	[SerializeField]
	private Quaternion defaultHoldAngleRight;

	private int noHapticsUntilFrame = -1;

	private void Awake()
	{
		hasParentBoard = parentFreeBoard != null;
	}

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
			if (hasParentBoard)
			{
				FreeHoverboardManager.instance.SendGrabBoardRPC(parentFreeBoard);
				Transform obj = (flag ? VRRig.LocalRig.leftHand.rigTarget : VRRig.LocalRig.rightHand.rigTarget);
				Quaternion rot = obj.InverseTransformRotation(base.transform.rotation);
				Vector3 pos = obj.InverseTransformPoint(base.transform.position);
				GTPlayer.Instance.GrabPersonalHoverboard(flag, pos, rot, parentFreeBoard.boardColor);
			}
			else
			{
				Quaternion rot2 = (flag ? defaultHoldAngleLeft : defaultHoldAngleRight);
				Vector3 pos2 = (flag ? defaultHoldPosLeft : defaultHoldPosRight);
				GTPlayer.Instance.GrabPersonalHoverboard(flag, pos2, rot2, VRRig.LocalRig.playerColor);
			}
		}
	}

	public override void DropItemCleanup()
	{
	}

	public override bool OnRelease(DropZone zoneReleased, GameObject releasingHand)
	{
		throw new NotImplementedException();
	}
}
