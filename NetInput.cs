using UnityEngine;
using UnityEngine.XR;

public static class NetInput
{
	private static VRRig _localPlayerVRRig;

	public static VRRig LocalPlayerVRRig
	{
		get
		{
			if (_localPlayerVRRig == null)
			{
				_localPlayerVRRig = GameObject.Find("Local VRRig").GetComponentInChildren<VRRig>();
			}
			return _localPlayerVRRig;
		}
	}

	public static NetworkedInput GetInput()
	{
		NetworkedInput result = default(NetworkedInput);
		if (LocalPlayerVRRig == null)
		{
			return result;
		}
		result.headRot_LS = LocalPlayerVRRig.head.rigTarget.localRotation;
		result.rightHandPos_LS = LocalPlayerVRRig.rightHand.rigTarget.localPosition;
		result.rightHandRot_LS = LocalPlayerVRRig.rightHand.rigTarget.localRotation;
		result.leftHandPos_LS = LocalPlayerVRRig.leftHand.rigTarget.localPosition;
		result.leftHandRot_LS = LocalPlayerVRRig.leftHand.rigTarget.localRotation;
		result.handPoseData = LocalPlayerVRRig.ReturnHandPosition();
		result.rootPosition = LocalPlayerVRRig.transform.position;
		result.rootRotation = LocalPlayerVRRig.transform.rotation;
		result.leftThumbTouch = ControllerInputPoller.PrimaryButtonTouch(XRNode.LeftHand) || ControllerInputPoller.SecondaryButtonTouch(XRNode.LeftHand);
		result.leftThumbPress = ControllerInputPoller.PrimaryButtonPress(XRNode.LeftHand) || ControllerInputPoller.SecondaryButtonPress(XRNode.LeftHand);
		result.leftIndexValue = ControllerInputPoller.TriggerFloat(XRNode.LeftHand);
		result.leftMiddleValue = ControllerInputPoller.GripFloat(XRNode.LeftHand);
		result.rightThumbTouch = ControllerInputPoller.PrimaryButtonTouch(XRNode.RightHand) || ControllerInputPoller.SecondaryButtonPress(XRNode.RightHand);
		result.rightThumbPress = ControllerInputPoller.PrimaryButtonPress(XRNode.RightHand) || ControllerInputPoller.SecondaryButtonPress(XRNode.RightHand);
		result.rightIndexValue = ControllerInputPoller.TriggerFloat(XRNode.RightHand);
		result.rightMiddleValue = ControllerInputPoller.GripFloat(XRNode.RightHand);
		result.scale = LocalPlayerVRRig.scaleFactor;
		return result;
	}
}
