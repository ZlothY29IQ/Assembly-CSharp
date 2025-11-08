using System;
using UnityEngine;
using UnityEngine.XR;

[Serializable]
public class VRMapThumb : VRMap
{
	public InputFeatureUsage inputAxis;

	public bool primaryButtonTouch;

	public bool primaryButtonPress;

	public bool secondaryButtonTouch;

	public bool secondaryButtonPress;

	public Transform fingerBone1;

	public Transform fingerBone2;

	public Vector3 closedAngle1;

	public Vector3 closedAngle2;

	public Vector3 startingAngle1;

	public Vector3 startingAngle2;

	public Quaternion closedAngle1Quat;

	public Quaternion closedAngle2Quat;

	public Quaternion startingAngle1Quat;

	public Quaternion startingAngle2Quat;

	public Quaternion[] angle1Table;

	public Quaternion[] angle2Table;

	private float currentAngle1;

	private float currentAngle2;

	private int lastAngle1;

	private int lastAngle2;

	private InputDevice tempDevice;

	private int myTempInt;

	public override void Initialize()
	{
		closedAngle1Quat = Quaternion.Euler(closedAngle1);
		closedAngle2Quat = Quaternion.Euler(closedAngle2);
		startingAngle1Quat = Quaternion.Euler(startingAngle1);
		startingAngle2Quat = Quaternion.Euler(startingAngle2);
	}

	public override void MapMyFinger(float lerpValue)
	{
		calcT = 0f;
		if (vrTargetNode == XRNode.LeftHand)
		{
			primaryButtonPress = ControllerInputPoller.instance.leftControllerPrimaryButton;
			primaryButtonTouch = ControllerInputPoller.instance.leftControllerPrimaryButtonTouch;
			secondaryButtonPress = ControllerInputPoller.instance.leftControllerSecondaryButton;
			secondaryButtonTouch = ControllerInputPoller.instance.leftControllerSecondaryButtonTouch;
		}
		else
		{
			primaryButtonPress = ControllerInputPoller.instance.rightControllerPrimaryButton;
			primaryButtonTouch = ControllerInputPoller.instance.rightControllerPrimaryButtonTouch;
			secondaryButtonPress = ControllerInputPoller.instance.rightControllerSecondaryButton;
			secondaryButtonTouch = ControllerInputPoller.instance.rightControllerSecondaryButtonTouch;
		}
		if (primaryButtonPress || secondaryButtonPress)
		{
			calcT = 1f;
		}
		else if (primaryButtonTouch || secondaryButtonTouch)
		{
			calcT = 0.1f;
		}
		LerpFinger(lerpValue, isOther: false);
	}

	public override void LerpFinger(float lerpValue, bool isOther)
	{
		if (isOther)
		{
			currentAngle1 = Mathf.Lerp(currentAngle1, calcT, lerpValue);
			currentAngle2 = Mathf.Lerp(currentAngle2, calcT, lerpValue);
			myTempInt = (int)(currentAngle1 * 10.1f);
			if (myTempInt != lastAngle1)
			{
				lastAngle1 = myTempInt;
				fingerBone1.localRotation = angle1Table[lastAngle1];
			}
			myTempInt = (int)(currentAngle2 * 10.1f);
			if (myTempInt != lastAngle2)
			{
				lastAngle2 = myTempInt;
				fingerBone2.localRotation = angle2Table[lastAngle2];
			}
		}
		else
		{
			fingerBone1.localRotation = Quaternion.Lerp(fingerBone1.localRotation, Quaternion.Lerp(startingAngle1Quat, closedAngle1Quat, calcT), lerpValue);
			fingerBone2.localRotation = Quaternion.Lerp(fingerBone2.localRotation, Quaternion.Lerp(startingAngle2Quat, closedAngle2Quat, calcT), lerpValue);
		}
	}
}
