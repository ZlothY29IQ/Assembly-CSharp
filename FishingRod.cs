using System;
using UnityEngine;
using UnityEngine.XR;

public class FishingRod : TransferrableObject
{
	public Transform handleTransform;

	public HingeJoint handleJoint;

	public Rigidbody handleRigidbody;

	public BoxCollider handleCollider;

	public Rigidbody bobRigidbody;

	public Collider bobCollider;

	public VerletLine line;

	public GorillaVelocityEstimator tipTracker;

	public Rigidbody tipBody;

	[NonSerialized]
	public VRRig rig;

	[Space]
	public Vector3 reelFreezeLocalPosition;

	public Transform reelFrom;

	public Transform reelTo;

	public Transform reelToSync;

	[Space]
	public float reelSpinRate = 1f;

	public float lineResizeRate = 1f;

	public float lineCastFactor = 3f;

	public float lineLengthMin = 0.1f;

	public float lineLengthMax = 8f;

	[NonSerialized]
	[Space]
	private bool _bobFloating;

	public float bobFloatForce = 8f;

	public float bobStaticDrag = 3.2f;

	public float bobDynamicDrag = 1.1f;

	[NonSerialized]
	private float _bobFloatPlaneY;

	[NonSerialized]
	[Space]
	private float _targetSegmentMin;

	[NonSerialized]
	private float _targetSegmentMax;

	[NonSerialized]
	[Space]
	private bool _manualReeling;

	[NonSerialized]
	private bool _lineResizing;

	[NonSerialized]
	private bool _lineExpanding;

	[NonSerialized]
	private bool _lineResetting;

	[NonSerialized]
	private TimeSince _sinceReset;

	[NonSerialized]
	[Space]
	private Quaternion _lastLocalRot = Quaternion.identity;

	[NonSerialized]
	private float _localRotDelta;

	[NonSerialized]
	private bool _isGrippingHandle;

	[NonSerialized]
	private Transform _grippingHand;

	private TimeSince _sinceGripLoss;

	public override void OnActivate()
	{
		base.OnActivate();
		Transform transform = base.transform;
		Vector3 force = transform.up + transform.forward * 640f;
		bobRigidbody.AddForce(force, ForceMode.Impulse);
		line.tensionScale = 0.86f;
		ReelOut();
	}

	public override void OnDeactivate()
	{
		base.OnDeactivate();
		line.tensionScale = 1f;
		ReelStop();
	}

	protected override void Start()
	{
		base.Start();
		rig = GetComponentInParent<VRRig>();
	}

	public void SetBobFloat(bool enable)
	{
		if ((bool)bobRigidbody)
		{
			_bobFloatPlaneY = bobRigidbody.position.y;
			_bobFloating = enable;
		}
	}

	private void QuickReel()
	{
		if (!_lineResizing)
		{
			bobCollider.enabled = false;
			ReelIn();
		}
	}

	public bool IsFreeHandGripping()
	{
		bool flag = InLeftHand();
		Transform transform = (flag ? rig.rightHandTransform : rig.leftHandTransform);
		float magnitude = (reelToSync.position - transform.position).magnitude;
		if (!(disableStealing = (bool)_grippingHand || magnitude <= 0.16f))
		{
			return false;
		}
		VRMapThumb vRMapThumb = (flag ? rig.rightThumb : rig.leftThumb);
		VRMapIndex vRMapIndex = (flag ? rig.rightIndex : rig.leftIndex);
		VRMapMiddle obj = (flag ? rig.rightMiddle : rig.leftMiddle);
		float calcT = vRMapThumb.calcT;
		float calcT2 = vRMapIndex.calcT;
		float calcT3 = obj.calcT;
		bool flag2 = calcT >= 0.1f && calcT2 >= 0.2f && calcT3 >= 0.2f;
		_grippingHand = (flag2 ? transform : null);
		return flag2;
	}

	public override bool OnRelease(DropZone zoneReleased, GameObject releasingHand)
	{
		if (!base.OnRelease(zoneReleased, releasingHand))
		{
			return false;
		}
		if ((bool)_grippingHand)
		{
			_grippingHand = null;
		}
		ResetLineLength(lineLengthMin * 1.32f);
		return true;
	}

	public void ReelIn()
	{
		_manualReeling = false;
		SetHandleMotorUse(useMotor: true, reelSpinRate, handleJoint, reverse: true);
		_lineResizing = true;
		_lineExpanding = false;
		float num = (float)line.segmentNumber + 0.0001f;
		line.segmentMinLength = (_targetSegmentMin = lineLengthMin / num);
		line.segmentMaxLength = (_targetSegmentMax = lineLengthMax / num);
	}

	public void ReelOut()
	{
		_manualReeling = false;
		SetHandleMotorUse(useMotor: true, reelSpinRate, handleJoint, reverse: false);
		_lineResizing = true;
		_lineExpanding = true;
		float num = (float)line.segmentNumber + 0.0001f;
		line.segmentMinLength = (_targetSegmentMin = lineLengthMin / num);
		line.segmentMaxLength = (_targetSegmentMax = lineLengthMax / num);
	}

	public void ReelStop()
	{
		if (_manualReeling)
		{
			_localRotDelta = 0f;
		}
		else
		{
			SetHandleMotorUse(useMotor: false, 0f, handleJoint, reverse: false);
		}
		bobCollider.enabled = true;
		if ((bool)line)
		{
			line.resizeScale = 1f;
		}
		_lineResizing = false;
		_lineExpanding = false;
	}

	private static void SetHandleMotorUse(bool useMotor, float spinRate, HingeJoint handleJoint, bool reverse)
	{
		JointMotor motor = handleJoint.motor;
		motor.force = (useMotor ? 1f : 0f) * spinRate;
		motor.targetVelocity = 16384f * (reverse ? (-1f) : 1f);
		handleJoint.motor = motor;
	}

	public override void TriggeredLateUpdate()
	{
		base.TriggeredLateUpdate();
		_manualReeling = (_isGrippingHandle = IsFreeHandGripping());
		if ((bool)ControllerInputPoller.instance && ControllerInputPoller.PrimaryButtonPress(InLeftHand() ? XRNode.LeftHand : XRNode.RightHand))
		{
			QuickReel();
		}
		if (_lineResetting && _sinceReset.HasElapsed(line.resizeSpeed))
		{
			bobCollider.enabled = true;
			_lineResetting = false;
		}
		handleTransform.localPosition = reelFreezeLocalPosition;
	}

	private void ResetLineLength(float length)
	{
		if ((bool)line)
		{
			_lineResetting = true;
			bobCollider.enabled = false;
			line.ForceTotalLength(length);
			_sinceReset = TimeSince.Now();
		}
	}

	private void FixedUpdate()
	{
		Transform transform = base.transform;
		handleRigidbody.useGravity = !_manualReeling;
		if (_bobFloating && (bool)bobRigidbody)
		{
			float y = bobRigidbody.position.y;
			float num = bobFloatForce * bobRigidbody.mass;
			float num2 = num * Mathf.Clamp01(_bobFloatPlaneY - y);
			num += num2;
			if (y <= _bobFloatPlaneY)
			{
				bobRigidbody.AddForce(0f, num, 0f);
			}
		}
		if (_manualReeling)
		{
			if (_isGrippingHandle && (bool)_grippingHand)
			{
				reelTo.position = _grippingHand.position;
			}
			Vector3 toDirection = reelFrom.InverseTransformPoint(reelTo.position);
			toDirection.x = 0f;
			toDirection.Normalize();
			toDirection *= 2f;
			Quaternion quaternion = Quaternion.FromToRotation(Vector3.forward, toDirection);
			quaternion = (InRightHand() ? quaternion : Quaternion.Inverse(quaternion));
			_localRotDelta = GetSignedDeltaYZ(ref _lastLocalRot, ref quaternion);
			_lastLocalRot = quaternion;
			Quaternion rot = transform.rotation * quaternion;
			handleRigidbody.MoveRotation(rot);
		}
		else
		{
			reelTo.localPosition = transform.InverseTransformPoint(reelToSync.position);
		}
		if (!line)
		{
			return;
		}
		if (_manualReeling)
		{
			_lineResizing = Mathf.Abs(_localRotDelta) >= 0.001f;
			_lineExpanding = Mathf.Sign(_localRotDelta) >= 0f;
		}
		if (!_lineResizing)
		{
			return;
		}
		float num3 = (_manualReeling ? (Mathf.Abs(_localRotDelta) * 0.66f * Time.fixedDeltaTime) : (lineResizeRate * lineCastFactor));
		line.resizeScale = lineCastFactor;
		float num4 = num3 * Time.fixedDeltaTime;
		float num5 = line.segmentTargetLength;
		if (_manualReeling)
		{
			float num6 = 1f / ((float)line.segmentNumber + 0.0001f);
			float num7 = lineLengthMin * num6;
			float num8 = lineLengthMax * num6;
			num4 *= (_lineExpanding ? 1f : (-1f));
			num4 *= (InRightHand() ? (-1f) : 1f);
			float num9 = num5 + num4;
			if (num9 > num7 && num9 < num8)
			{
				num5 += num4;
			}
		}
		else if (_lineExpanding)
		{
			if (num5 < _targetSegmentMax)
			{
				num5 += num4;
			}
			else
			{
				_lineResizing = false;
			}
		}
		else if (num5 > _targetSegmentMin)
		{
			num5 -= num4;
		}
		else
		{
			_lineResizing = false;
		}
		if (_lineResizing)
		{
			line.segmentTargetLength = num5;
		}
		else
		{
			ReelStop();
		}
	}

	private static float GetSignedDeltaYZ(ref Quaternion a, ref Quaternion b)
	{
		Vector3 forward = Vector3.forward;
		Vector3 vector = a * forward;
		Vector3 vector2 = b * forward;
		float current = Mathf.Atan2(vector.y, vector.z) * 57.29578f;
		float target = Mathf.Atan2(vector2.y, vector2.z) * 57.29578f;
		return Mathf.DeltaAngle(current, target);
	}
}
