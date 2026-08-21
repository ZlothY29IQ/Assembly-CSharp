using System;
using GorillaExtensions;
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

	private static bool k_useFastRotation;

	[SerializeField]
	private Transform m_gtPlayerBodyTransform;

	[SerializeField]
	private GTPlayer m_gtPlayerInstance;

	[Header("Slow rotation for small angles")]
	[Tooltip("If rotating less than this distance (degrees), use the slow speed.")]
	[SerializeField]
	private float m_smallRotationAngleThreshold = 45f;

	[Tooltip("Rotation speed (rad/s) used for rotations less than the small-angle threshold.")]
	[SerializeField]
	private float m_smallRotationSpeed = 5f;

	public static float GravityStrength { get; private set; }

	public static Vector3 GravityForce { get; private set; } = Physics.gravity;

	public static Vector3 Up { get; private set; } = Vector3.up;

	public static Vector3 PhysicsUp { get; private set; } = Vector3.up;

	public static Vector3 Down { get; private set; } = Vector3.down;

	public static Vector3 PhysicsDown { get; private set; } = Vector3.down;

	public static Vector3 Forward { get; private set; } = Vector3.forward;

	public static Vector3 Right { get; private set; } = Vector3.right;

	public static Quaternion BodyRotation => k_bodyTransform.rotation;

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

	public static void RotateFromToDirection(in Vector3 currentDir, in Vector3 targetDir)
	{
		Quaternion currentRotation = k_rigidBody.rotation;
		SetRotation(Quaternion.FromToRotation(currentDir, targetDir) * currentRotation, in currentRotation);
	}

	public static void RotateBy(in Quaternion rotation)
	{
		Quaternion currentRotation = k_transform.rotation;
		SetRotation(currentRotation * rotation, in currentRotation);
	}

	public static void SetRotation(in Quaternion targetRotation)
	{
		SetRotation(in targetRotation, k_rigidBody.rotation);
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
				vector -= leftHandRef.lastHitInfo.normal * 0.0001f;
			}
		}
		if (rightHandRef.wasColliding || rightHandRef.wasSliding || flag2)
		{
			Vector3 rotatedDifference2 = GetRotatedDifference(in pivotPoint, flag2 ? currentClimber.transform.position : rightHandRef.lastPosition, in rotation);
			Vector3 lhs2 = Vector3.Normalize(rotatedDifference2);
			if (flag2 || Vector3.Dot(lhs2, rightHandRef.lastHitInfo.normal) <= 0f)
			{
				vector = rotatedDifference2;
				vector -= rightHandRef.lastHitInfo.normal * 0.0001f;
			}
		}
		k_rotationPosOffsetChange -= vector;
		Vector3 position = pivotPoint - vector;
		k_rigidBody.position = position;
		k_rigidBody.rotation = newRotation;
		k_transform.SetPositionAndRotation(position, newRotation);
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

	public static void TeleportFromTo(Transform sourceNode, Transform targetNode, bool keepVelocity, bool centre, in Vector3? offset = null)
	{
		Vector3 position = k_rigidBody.position;
		Quaternion currentRot = k_rigidBody.rotation;
		Vector3 position2 = sourceNode.InverseTransformPoint(position);
		Vector3 targetPos = targetNode.TransformPoint(position2);
		if (offset.HasValue)
		{
			targetPos += offset.Value;
		}
		Quaternion localRotation = sourceNode.InverseTransformRotation(currentRot);
		TeleportTo(in targetPos, targetNode.TransformRotation(localRotation), in currentRot, keepVelocity, centre);
	}

	public static void TeleportTo(in Vector3 targetPos, in Quaternion targetRot, bool keepVelocity, bool centre)
	{
		TeleportTo(in targetPos, in targetRot, k_rigidBody.rotation, keepVelocity, centre);
	}

	private static void TeleportTo(in Vector3 targetPos, in Quaternion targetRot, in Quaternion currentRot, bool keepVelocity, bool centre)
	{
		Vector3 position = targetPos;
		if (centre)
		{
			Vector3 position2 = k_rigidBody.position;
			Vector3 vector = k_playerInstance.mainCamera.transform.position - position2;
			position -= vector;
		}
		k_rigidBody.isKinematic = true;
		k_rigidBody.position = position;
		k_transform.position = position;
		SetRotation(in targetRot, in currentRot);
		k_rigidBody.isKinematic = false;
		if (keepVelocity)
		{
			Vector3 linearVelocity = k_rigidBody.linearVelocity;
			k_rigidBody.linearVelocity = targetRot * Quaternion.Inverse(currentRot) * linearVelocity;
		}
		else
		{
			k_rigidBody.linearVelocity = Vector3.zero;
		}
		k_playerInstance.TeleportCleanup();
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
		float num2 = num * (MathF.PI / 180f);
		if (num > m_smallRotationAngleThreshold)
		{
			k_useFastRotation = true;
		}
		float num3 = (k_useFastRotation ? speed : (m_smallRotationSpeed * Time.fixedDeltaTime));
		Vector3 targetUp;
		if (num2 <= num3)
		{
			targetUp = upDir;
			k_useFastRotation = false;
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
			targetUp = Vector3.RotateTowards(Up, target, num3, 0f);
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
		if (!IgnoreGravityForce && !k_playerInstance.isClimbing && k_playerInstance.GravityOverrideCount <= 0)
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
}
