using GorillaExtensions;
using GorillaLocomotion;
using UnityEngine;

public class TransferrableObjectHoldablePart_Slide : TransferrableObjectHoldablePart
{
	[SerializeField]
	private float _maxHandSnapDistance;

	[SerializeField]
	private SnapXformToLine _snapToLine;

	private const int LEFT = 0;

	private const int RIGHT = 1;

	protected override void UpdateHeld(VRRig rig, bool isHeldLeftHand)
	{
		int num = ((!isHeldLeftHand) ? 1 : 0);
		GTPlayer instance = GTPlayer.Instance;
		if (rig.isOfflineVRRig)
		{
			Transform controllerTransform = instance.GetControllerTransform(num == 0);
			Vector3 position = controllerTransform.position;
			Vector3 snappedPoint = _snapToLine.GetSnappedPoint(position);
			if (_maxHandSnapDistance > 0f && (controllerTransform.position - snappedPoint).IsLongerThan(_maxHandSnapDistance))
			{
				OnRelease(null, isHeldLeftHand ? EquipmentInteractor.instance.leftHand : EquipmentInteractor.instance.rightHand);
				return;
			}
			controllerTransform.position = snappedPoint;
			_snapToLine.target.position = snappedPoint;
		}
		else
		{
			Vector3 vector = instance.GetHandOffset(isHeldLeftHand) * rig.scaleFactor;
			VRMap vRMap = (isHeldLeftHand ? rig.leftHand : rig.rightHand);
			_snapToLine.target.position = vRMap.GetExtrapolatedControllerPosition() - vector;
		}
	}
}
