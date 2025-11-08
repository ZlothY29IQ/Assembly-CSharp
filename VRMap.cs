using System;
using UnityEngine;
using UnityEngine.XR;

[Serializable]
public class VRMap
{
	public XRNode vrTargetNode;

	public Transform overrideTarget;

	public Transform rigTarget;

	public Vector3 trackingPositionOffset;

	public Vector3 trackingRotationOffset;

	public Transform headTransform;

	internal NetworkVector3 netSyncPos = new NetworkVector3();

	public Quaternion syncRotation;

	public float calcT;

	private InputDevice myInputDevice;

	private bool hasInputDevice;

	public Transform handholdOverrideTarget;

	public Vector3 handholdOverrideTargetOffset;

	public Vector3 syncPos
	{
		get
		{
			return netSyncPos.CurrentSyncTarget;
		}
		set
		{
			netSyncPos.SetNewSyncTarget(value);
		}
	}

	public virtual void Initialize()
	{
	}

	public void MapOther(float lerpValue)
	{
		rigTarget.GetLocalPositionAndRotation(out var localPosition, out var localRotation);
		rigTarget.SetLocalPositionAndRotation(Vector3.Lerp(localPosition, syncPos, lerpValue), Quaternion.Lerp(localRotation, syncRotation, lerpValue));
	}

	public void MapMine(float ratio, Transform playerOffsetTransform)
	{
		rigTarget.GetPositionAndRotation(out var position, out var rotation);
		if (overrideTarget != null)
		{
			overrideTarget.GetPositionAndRotation(out var position2, out var rotation2);
			rigTarget.SetPositionAndRotation(position2 + rotation * trackingPositionOffset * ratio, rotation2 * Quaternion.Euler(trackingRotationOffset));
		}
		else
		{
			if (!hasInputDevice && ConnectedControllerHandler.Instance.GetValidForXRNode(vrTargetNode))
			{
				myInputDevice = InputDevices.GetDeviceAtXRNode(vrTargetNode);
				hasInputDevice = true;
				if (vrTargetNode != XRNode.LeftHand && vrTargetNode != XRNode.RightHand)
				{
					hasInputDevice = myInputDevice.isValid;
				}
			}
			if (hasInputDevice && myInputDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out var value) && myInputDevice.TryGetFeatureValue(CommonUsages.devicePosition, out var value2))
			{
				rigTarget.SetPositionAndRotation(value2 + rotation * trackingPositionOffset * ratio + playerOffsetTransform.position, value * Quaternion.Euler(trackingRotationOffset));
				rigTarget.RotateAround(playerOffsetTransform.position, Vector3.up, playerOffsetTransform.eulerAngles.y);
			}
		}
		if (handholdOverrideTarget != null)
		{
			rigTarget.position = Vector3.MoveTowards(position, handholdOverrideTarget.position - handholdOverrideTargetOffset + rotation * trackingPositionOffset * ratio, Time.deltaTime * 2f);
		}
	}

	public Vector3 GetExtrapolatedControllerPosition()
	{
		rigTarget.GetPositionAndRotation(out var position, out var rotation);
		return position - rotation * trackingPositionOffset * rigTarget.lossyScale.x;
	}

	public virtual void MapOtherFinger(float handSync, float lerpValue)
	{
		calcT = handSync;
		LerpFinger(lerpValue, isOther: true);
	}

	public virtual void MapMyFinger(float lerpValue)
	{
	}

	public virtual void LerpFinger(float lerpValue, bool isOther)
	{
	}
}
