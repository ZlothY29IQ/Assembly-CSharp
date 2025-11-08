using UnityEngine;

namespace GorillaTag.Cosmetics;

public class HandControlledCosmetic : MonoBehaviour, ITickSystemTick
{
	public enum RotationControl
	{
		Angle,
		Translation
	}

	[SerializeField]
	private HandControlledSettingsSO activeSettings;

	[SerializeField]
	private HandControlledSettingsSO inactiveSettings;

	[SerializeField]
	private Vector3 handPositionOffset;

	[SerializeField]
	private Quaternion rightHandRotation;

	[SerializeField]
	private Quaternion leftHandRotation;

	private Quaternion handRotationOffset;

	[SerializeField]
	private BezierCurve controlIndicatorCurve;

	[SerializeField]
	private Transform debugRelativePositionTransform1;

	[SerializeField]
	private Transform debugRelativePositionTransform2;

	private VRRig myRig;

	private Transform controllingHand;

	private Vector3 startHandRelativePosition;

	private Vector3 lowAngleLimits;

	private Vector3 highAngleLimits;

	private Vector3 localEuler;

	private Quaternion startHandInverseRotation;

	private Quaternion initialRotation;

	private bool isActive;

	public bool TickRunning { get; set; }

	public void Awake()
	{
		myRig = GetComponentInParent<VRRig>();
		initialRotation = base.transform.localRotation;
		base.enabled = false;
		if (debugRelativePositionTransform1 != null)
		{
			Object.Destroy(debugRelativePositionTransform1.gameObject);
		}
		if (debugRelativePositionTransform2 != null)
		{
			Object.Destroy(debugRelativePositionTransform2.gameObject);
		}
	}

	private void SetControlIndicatorPoints()
	{
		if (myRig.isOfflineVRRig && controllingHand != null && controlIndicatorCurve != null && controlIndicatorCurve.points != null)
		{
			controlIndicatorCurve.points[0] = controllingHand.position;
			controlIndicatorCurve.points[1] = controlIndicatorCurve.points[0] + myRig.scaleFactor * controllingHand.up;
			controlIndicatorCurve.points[2] = base.transform.position;
		}
	}

	private Vector3 GetRelativeHandPosition()
	{
		return controllingHand.TransformPoint(handPositionOffset) - myRig.bodyTransform.position;
	}

	public void StartControl(bool leftHand, float flexValue)
	{
		if (base.enabled && base.gameObject.activeInHierarchy)
		{
			lowAngleLimits = activeSettings.angleLimits;
			highAngleLimits = 360f * Vector3.one - lowAngleLimits;
			handRotationOffset = (leftHand ? leftHandRotation : rightHandRotation);
			controllingHand = (leftHand ? myRig.leftHand.rigTarget.transform : myRig.rightHand.rigTarget.transform);
			startHandRelativePosition = GetRelativeHandPosition();
			startHandInverseRotation = Quaternion.Inverse(controllingHand.rotation * handRotationOffset);
			isActive = true;
			SetControlIndicatorPoints();
			TickSystem<object>.AddTickCallback(this);
		}
	}

	public void StopControl()
	{
		localEuler = base.transform.localRotation.eulerAngles;
		isActive = false;
		SetControlIndicatorPoints();
	}

	public void OnEnable()
	{
	}

	public void OnDisable()
	{
		base.transform.localRotation = initialRotation;
		StopControl();
		TickSystem<object>.RemoveTickCallback(this);
	}

	private float ReverseClampDegrees(float value, float low, float high)
	{
		value = Mathf.Repeat(value, 360f);
		if (value <= low || value >= high)
		{
			return value;
		}
		if (!(value < 180f))
		{
			return high;
		}
		return low;
	}

	public void Tick()
	{
		if (isActive)
		{
			switch (activeSettings.rotationControl)
			{
			case RotationControl.Angle:
			{
				Quaternion quaternion = controllingHand.rotation * handRotationOffset;
				Quaternion quaternion2 = startHandInverseRotation * quaternion;
				localEuler += activeSettings.inputSensitivity * quaternion2.eulerAngles;
				float t = 1f - Mathf.Exp((0f - activeSettings.inputDecaySpeed) * Time.deltaTime);
				startHandInverseRotation = Quaternion.Slerp(startHandInverseRotation, Quaternion.Inverse(quaternion), t);
				break;
			}
			case RotationControl.Translation:
			{
				Vector3 relativeHandPosition = GetRelativeHandPosition();
				float num = Vector3.SignedAngle(to: new Vector3(relativeHandPosition.x, 0f, relativeHandPosition.z), from: new Vector3(startHandRelativePosition.x, 0f, startHandRelativePosition.z), axis: Vector3.up);
				float num2 = 50f * (startHandRelativePosition.y - relativeHandPosition.y) / myRig.scaleFactor;
				float time = Vector3.Distance(startHandRelativePosition, relativeHandPosition) / myRig.scaleFactor;
				localEuler += Time.deltaTime * new Vector3(activeSettings.verticalSensitivity.Evaluate(time) * num2, activeSettings.horizontalSensitivity.Evaluate(time) * num, 0f);
				startHandRelativePosition = Vector3.MoveTowards(startHandRelativePosition, relativeHandPosition, Time.deltaTime * activeSettings.inputDecayCurve.Evaluate(time));
				break;
			}
			}
			for (int i = 0; i < 3; i++)
			{
				localEuler[i] = ReverseClampDegrees(localEuler[i], lowAngleLimits[i], highAngleLimits[i]);
			}
			base.transform.localRotation = Quaternion.Slerp(base.transform.localRotation, Quaternion.Euler(localEuler), 1f - Mathf.Exp((0f - activeSettings.rotationSpeed) * Time.deltaTime));
		}
		else
		{
			Quaternion localRotation = Quaternion.Slerp(base.transform.localRotation, initialRotation, 1f - Mathf.Exp((0f - inactiveSettings.rotationSpeed) * Time.deltaTime));
			base.transform.localRotation = localRotation;
			localEuler = localRotation.eulerAngles;
		}
	}
}
