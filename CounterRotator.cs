using GorillaLocomotion;
using GorillaTag.Gravity;
using UnityEngine;

public class CounterRotator : MonoBehaviour
{
	public enum PlayerMovementMode
	{
		None,
		Velocity,
		Gravity
	}

	[SerializeField]
	private GameObject stabilizedObject;

	[SerializeField]
	private ChangingBasicGravityZone gravityCompensator;

	[Tooltip("How the stabilized object's correction is passed on to the local player.")]
	[SerializeField]
	private PlayerMovementMode playerMovementMode = PlayerMovementMode.Gravity;

	[Header("Gravity mode")]
	[Tooltip("How far gravity tilts per m/s of correction movement. 1 = 45 degrees at 1 m/s.")]
	[SerializeField]
	private float gravityTiltPerSpeed = 0.1f;

	[Tooltip("Time constant for smoothing the gravity tilt, in seconds.")]
	[SerializeField]
	private float gravitySmoothingTime = 0.2f;

	[Tooltip("How long without any correction movement before gravity returns to downwards, in seconds.")]
	[SerializeField]
	private float gravityReturnDelay = 0.5f;

	private Vector3 startingPosition;

	private Quaternion startingRotation;

	private Vector3 pendingPlayerMovement;

	private Vector3 appliedCarryVelocity;

	private Vector3 targetGravityTilt;

	private Vector3 smoothedGravityTilt;

	private float lastPlayerMovementTime = float.NegativeInfinity;

	private bool isTiltingGravity;

	private void Start()
	{
		startingPosition = stabilizedObject.transform.position;
		startingRotation = stabilizedObject.transform.rotation;
	}

	private void OnDisable()
	{
		pendingPlayerMovement = Vector3.zero;
		appliedCarryVelocity = Vector3.zero;
		targetGravityTilt = Vector3.zero;
		smoothedGravityTilt = Vector3.zero;
		StopTiltingGravity();
	}

	private void FixedUpdate()
	{
		if (GTPlayer.hasInstance && (playerMovementMode == PlayerMovementMode.Velocity || !(appliedCarryVelocity == Vector3.zero)))
		{
			GTPlayer instance = GTPlayer.Instance;
			Vector3 linearVelocity = instance.playerRigidBody.linearVelocity;
			linearVelocity.x = SubtractTowardsZero(linearVelocity.x, appliedCarryVelocity.x);
			linearVelocity.y = SubtractTowardsZero(linearVelocity.y, appliedCarryVelocity.y);
			linearVelocity.z = SubtractTowardsZero(linearVelocity.z, appliedCarryVelocity.z);
			appliedCarryVelocity = pendingPlayerMovement / Time.fixedDeltaTime;
			pendingPlayerMovement = Vector3.zero;
			instance.SetVelocity(linearVelocity + appliedCarryVelocity);
		}
	}

	private static float SubtractTowardsZero(float value, float amount)
	{
		if (value > 0f && amount > 0f)
		{
			return Mathf.Max(value - amount, 0f);
		}
		if (value < 0f && amount < 0f)
		{
			return Mathf.Min(value - amount, 0f);
		}
		return value;
	}

	private void LateUpdate()
	{
		bool num = playerMovementMode != PlayerMovementMode.None && GTPlayer.hasInstance;
		Vector3 position = Vector3.zero;
		if (num)
		{
			position = stabilizedObject.transform.InverseTransformPoint(GTPlayer.Instance.bodyCollider.transform.position);
		}
		Quaternion quaternion = startingRotation * Quaternion.Inverse(stabilizedObject.transform.rotation);
		base.transform.rotation = quaternion * base.transform.rotation;
		Vector3 vector = startingPosition - stabilizedObject.transform.position;
		base.transform.position += vector;
		Vector3 vector2 = Vector3.zero;
		if (num)
		{
			vector2 = stabilizedObject.transform.TransformPoint(position) - GTPlayer.Instance.bodyCollider.transform.position;
		}
		if (playerMovementMode == PlayerMovementMode.Velocity)
		{
			pendingPlayerMovement += vector2;
		}
		UpdateGravityTilt(vector2);
		if (gravityCompensator != null)
		{
			gravityCompensator.SetGravityDirection(-base.transform.up);
		}
	}

	private void UpdateGravityTilt(Vector3 playerMovement)
	{
		float deltaTime = Time.deltaTime;
		if (deltaTime <= 0f)
		{
			return;
		}
		if (playerMovementMode == PlayerMovementMode.Gravity && playerMovement.sqrMagnitude > 1E-10f)
		{
			lastPlayerMovementTime = Time.time;
			targetGravityTilt = playerMovement / deltaTime;
		}
		else if (playerMovementMode != PlayerMovementMode.Gravity || Time.time - lastPlayerMovementTime > gravityReturnDelay)
		{
			targetGravityTilt = Vector3.zero;
		}
		float t = ((gravitySmoothingTime > 0f) ? (1f - Mathf.Exp((0f - deltaTime) / gravitySmoothingTime)) : 1f);
		smoothedGravityTilt = Vector3.Lerp(smoothedGravityTilt, targetGravityTilt, t);
		if (!GTPlayer.hasInstance)
		{
			return;
		}
		if (targetGravityTilt == Vector3.zero && smoothedGravityTilt.sqrMagnitude < 1E-06f)
		{
			smoothedGravityTilt = Vector3.zero;
			StopTiltingGravity();
			return;
		}
		Vector3 personalGravityDirection = Vector3.up - smoothedGravityTilt * gravityTiltPerSpeed;
		if (personalGravityDirection.sqrMagnitude > 1E-06f)
		{
			GTPlayerTransform.Instance.SetPersonalGravityDirection(personalGravityDirection);
			isTiltingGravity = true;
		}
	}

	private void StopTiltingGravity()
	{
		if (isTiltingGravity && GTPlayer.hasInstance)
		{
			GTPlayerTransform.Instance.SetPersonalGravityDirection(Vector3.up);
		}
		isTiltingGravity = false;
	}
}
