using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.XR;
using Valve.VR;

public class GorillaFireballControllerManager : MonoBehaviour
{
	public InputDevice leftHand;

	public InputDevice rightHand;

	public bool hasInitialized;

	public float leftHandLastState;

	public float rightHandLastState;

	public float throwingThreshold = 0.9f;

	private void Update()
	{
		if (!hasInitialized)
		{
			hasInitialized = true;
			List<InputDevice> list = new List<InputDevice>();
			List<InputDevice> list2 = new List<InputDevice>();
			InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, list);
			InputDevices.GetDevicesAtXRNode(XRNode.RightHand, list2);
			if (list.Count == 1)
			{
				leftHand = list[0];
			}
			if (list2.Count == 1)
			{
				rightHand = list2[0];
			}
		}
		float axis = SteamVR_Actions.gorillaTag_LeftTriggerFloat.GetAxis(SteamVR_Input_Sources.LeftHand);
		if (leftHandLastState <= throwingThreshold && axis > throwingThreshold)
		{
			CreateFireball(isLeftHand: true);
		}
		else if (leftHandLastState >= throwingThreshold && axis < throwingThreshold)
		{
			TryThrowFireball(isLeftHand: true);
		}
		leftHandLastState = axis;
		axis = SteamVR_Actions.gorillaTag_RightTriggerFloat.GetAxis(SteamVR_Input_Sources.RightHand);
		if (rightHandLastState <= throwingThreshold && axis > throwingThreshold)
		{
			CreateFireball(isLeftHand: false);
		}
		else if (rightHandLastState >= throwingThreshold && axis < throwingThreshold)
		{
			TryThrowFireball(isLeftHand: false);
		}
		rightHandLastState = axis;
	}

	public void TryThrowFireball(bool isLeftHand)
	{
		if (isLeftHand && GorillaPlaySpace.Instance.myVRRig.leftHandTransform.GetComponentInChildren<GorillaFireball>() != null)
		{
			GorillaPlaySpace.Instance.myVRRig.leftHandTransform.GetComponentInChildren<GorillaFireball>().ThrowThisThingo();
		}
		else if (!isLeftHand && GorillaPlaySpace.Instance.myVRRig.rightHandTransform.GetComponentInChildren<GorillaFireball>() != null)
		{
			GorillaPlaySpace.Instance.myVRRig.rightHandTransform.GetComponentInChildren<GorillaFireball>().ThrowThisThingo();
		}
	}

	public void CreateFireball(bool isLeftHand)
	{
		object[] array = new object[1];
		Vector3 position;
		if (isLeftHand)
		{
			array[0] = true;
			position = GorillaPlaySpace.Instance.myVRRig.leftHandTransform.position;
		}
		else
		{
			array[0] = false;
			position = GorillaPlaySpace.Instance.myVRRig.rightHandTransform.position;
		}
		PhotonNetwork.Instantiate("GorillaPrefabs/GorillaFireball", position, Quaternion.identity, 0, array);
	}
}
