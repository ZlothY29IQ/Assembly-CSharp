using System;
using GorillaLocomotion.Climbing;
using GorillaTag.Gravity;
using UnityEngine;
using UnityEngine.XR;

namespace GorillaLocomotion;

public class GTPlayerTransform : MonkeGravityController
{
	private static Vector3 k_rotationPosOffsetChange = Vector3.zero;

	private static Transform k_transform;

	private static Rigidbody k_rigidBody;

	private static Transform k_bodyTransform;

	private static GTPlayer k_playerInstance;

	private static int k_rotationOverrideFrameTime;

	[SerializeField]
	private Transform m_gtPlayerBodyTransform;

	[SerializeField]
	private GTPlayer m_gtPlayerInstance;

	public static float GravityStrength { get; private set; }

	public static Vector3 GravityForce { get; private set; } = Physics.gravity;

	public static Vector3 Up { get; private set; } = Vector3.up;

	public static Vector3 PhysicsUp { get; private set; } = Vector3.up;

	public static Vector3 Down { get; private set; } = Vector3.down;

	public static Vector3 PhysicsDown { get; private set; } = Vector3.down;

	public static Vector3 Forward { get; private set; } = Vector3.forward;

	public static Vector3 Right { get; private set; } = Vector3.right;

	public static Quaternion BodyRotation => k_bodyTransform.rotation;

	public static bool UseNetRotation => true;

	public static bool IgnoreGravityRotation { get; set; } = false;

	public static bool IgnoreGravityForce { get; set; } = false;

	public static Vector3 RotationPosOffsetChange => k_rotationPosOffsetChange;

	public static GTPlayerTransform Instance { get; private set; }

	public override float Scale => VRRig.LocalRig.scaleFactor;

	public static void RotateToUp(in Vector3 targetUp)
	{
		if (targetUp == Up)
		{
			Instance.ClearRotationRecovery();
		}
		else
		{
			RotateFromToDirection(Up, in targetUp);
		}
	}

	public static void RotateToForward(in Vector3 targetForward)
	{
		if (!(targetForward == Forward))
		{
			RotateFromToDirection(Forward, in targetForward);
		}
	}

	public static void RotateFromToDirection(in Vector3 currentDir, in Vector3 targetDir)
	{
		Quaternion currentRotation = k_transform.rotation;
		SetRotation(Quaternion.FromToRotation(currentDir, targetDir) * currentRotation, in currentRotation);
	}

	public static void RotateBy(in Quaternion rotation)
	{
		Quaternion currentRotation = k_transform.rotation;
		SetRotation(currentRotation * rotation, in currentRotation);
	}

	public static void SetRotation(in Quaternion targetRotation)
	{
		SetRotation(in targetRotation, k_transform.rotation);
	}

	private static void SetRotation(in Quaternion newRotation, in Quaternion currentRotation)
	{
		ref readonly GTPlayer.HandState leftHandRef = ref k_playerInstance.LeftHandRef;
		ref readonly GTPlayer.HandState rightHandRef = ref k_playerInstance.RightHandRef;
		Vector3 pivotPoint = k_rigidBody.position;
		Quaternion rotation = newRotation * Quaternion.Inverse(currentRotation);
		Vector3 vector = GetRotatedDifference(in pivotPoint, k_bodyTransform.position, in rotation);
		bool flag = false;
		bool flag2 = false;
		GorillaHandClimber currentClimber = k_playerInstance.CurrentClimber;
		if (k_playerInstance.isClimbing)
		{
			flag = currentClimber.xrNode == XRNode.LeftHand;
			flag2 = currentClimber.xrNode == XRNode.RightHand;
		}
		if (leftHandRef.wasColliding || leftHandRef.wasSliding || flag)
		{
			Vector3 rotatedDifference = GetRotatedDifference(in pivotPoint, flag ? currentClimber.transform.position : leftHandRef.lastPosition, in rotation);
			Vector3 lhs = Vector3.Normalize(rotatedDifference);
			if (flag || Vector3.Dot(lhs, leftHandRef.lastHitInfo.normal) <= 0f)
			{
				vector = rotatedDifference;
			}
		}
		if (rightHandRef.wasColliding || rightHandRef.wasSliding || flag2)
		{
			Vector3 rotatedDifference2 = GetRotatedDifference(in pivotPoint, flag2 ? currentClimber.transform.position : rightHandRef.lastPosition, in rotation);
			Vector3 lhs2 = Vector3.Normalize(rotatedDifference2);
			if (flag2 || Vector3.Dot(lhs2, rightHandRef.lastHitInfo.normal) <= 0f)
			{
				vector = rotatedDifference2;
			}
		}
		k_rotationPosOffsetChange -= vector;
		k_rigidBody.position = pivotPoint - vector;
		k_rigidBody.rotation = newRotation;
		Up = newRotation * Vector3.up;
		Down = Up * -1f;
		Forward = newRotation * Vector3.forward;
		Right = newRotation * Vector3.right;
	}

	private static Vector3 GetRotatedDifference(in Vector3 pivotPoint, in Vector3 worldPoint, in Quaternion rotation)
	{
		Vector3 vector = worldPoint - pivotPoint;
		return rotation * vector - vector;
	}

	public static void ApplyRotationOverride(in Quaternion rotation, int frameTime)
	{
		SetRotation(in rotation);
		k_rotationOverrideFrameTime = frameTime;
	}

	public static void ResetRotationPositionOffset()
	{
		k_rotationPosOffsetChange = Vector3.zero;
	}

	public static void EnableNetworkRotations()
	{
	}

	public static void DisableNetworkRotations()
	{
	}

	protected override void Awake()
	{
		base.Awake();
		if (!base.Register)
		{
			Debug.LogError("GTPlayerTransform: failed to load required references", base.gameObject);
		}
		Instance = this;
		k_transform = m_targetTransform;
		k_rigidBody = m_targetRigidBody;
		k_bodyTransform = m_gtPlayerBodyTransform;
		k_playerInstance = m_gtPlayerInstance;
		GravityStrength = Physics.gravity.magnitude * -1f;
		GravityForce = Physics.gravity;
		Up = k_transform.up;
		Forward = k_transform.forward;
		Right = k_transform.right;
		Down = Up * -1f;
		m_globalGravityIntent = false;
	}

	public override void ApplyGravityUpRotation(in Vector3 upDir, float speed)
	{
		if (IgnoreGravityRotation || k_rotationOverrideFrameTime >= Time.frameCount - 1)
		{
			return;
		}
		if (base.InstantRotation)
		{
			RotateToUp(in upDir);
			return;
		}
		float num = Vector3.Angle(Up, upDir);
		Vector3 targetUp;
		if (num * (MathF.PI / 180f) <= speed)
		{
			targetUp = upDir;
		}
		else
		{
			Vector3 target = upDir;
			if (Mathf.Approximately(num, 180f))
			{
				switch (m_preferredRotationDirection)
				{
				case RotationDirection.Left:
					target = k_bodyTransform.right * -1f;
					break;
				case RotationDirection.Right:
					target = k_bodyTransform.right;
					break;
				case RotationDirection.Forward:
					target = k_bodyTransform.forward;
					break;
				case RotationDirection.Backward:
					target = k_bodyTransform.forward * -1f;
					break;
				}
			}
			targetUp = Vector3.RotateTowards(Up, target, speed, 0f);
		}
		RotateToUp(in targetUp);
	}

	public override void ApplyGravityForce(in Vector3 force, ForceMode forceType = ForceMode.Acceleration)
	{
		GravityForce = force;
		GravityStrength = GravityForce.magnitude * -1f;
		if (!IgnoreGravityForce && !k_playerInstance.isClimbing && k_playerInstance.GravityOverrideCount <= 0)
		{
			base.ApplyGravityForce(force * k_playerInstance.scale, forceType);
		}
	}

	public override Vector3 GetWorldPoint()
	{
		return k_bodyTransform.position;
	}

	public override void CallBack()
	{
		base.CallBack();
		PhysicsUp = base.GravityUp;
		PhysicsDown = base.GravityDown;
		if (base.GravityZonesCount <= 0 && Up != PhysicsUp)
		{
			ApplyGravityUpRotation(PhysicsUp, MonkeGravityManager.DefaultGravityInfo.rotationSpeed * Time.fixedDeltaTime);
		}
	}
}
