using System;
using System.Collections.Generic;
using GorillaLocomotion;
using GorillaLocomotion.Climbing;
using Photon.Pun;
using UnityEngine;
using UnityEngine.XR;

namespace GorillaTag.Cosmetics;

public class RCPlane : RCVehicle
{
	public Vector2 pitchVelocityTargetMinMax = new Vector2(-180f, 180f);

	public Vector2 pitchVelocityRampTimeMinMax = new Vector2(-0.75f, 0.75f);

	public float rollVelocityTarget = 180f;

	public float rollVelocityRampTime = 0.75f;

	public float thrustVelocityTarget = 15f;

	public float thrustAccelTime = 2f;

	[SerializeField]
	private float pitchVelocityFollowRateAngle = 60f;

	[SerializeField]
	private float pitchVelocityFollowRateMagnitude = 5f;

	[SerializeField]
	private float maxDrag = 0.1f;

	[SerializeField]
	private Vector2 liftVsSpeedInput = new Vector2(0f, 4f);

	[SerializeField]
	private Vector2 liftVsSpeedOutput = new Vector2(0.5f, 1f);

	[SerializeField]
	private AnimationCurve liftVsAttackCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	[SerializeField]
	private AnimationCurve dragVsAttackCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	[SerializeField]
	private Vector2 gravityCompensationRange = new Vector2(0.5f, 1f);

	[SerializeField]
	private List<Collider> nonCrashColliders = new List<Collider>();

	[SerializeField]
	private Transform propeller;

	[SerializeField]
	private Transform leftAileronUpper;

	[SerializeField]
	private Transform leftAileronLower;

	[SerializeField]
	private Transform rightAileronUpper;

	[SerializeField]
	private Transform rightAileronLower;

	[SerializeField]
	private AudioSource audioSource;

	[SerializeField]
	private AudioClip motorSound;

	[SerializeField]
	private AudioClip crashSound;

	[SerializeField]
	private Vector2 motorSoundVolumeMinMax = new Vector2(0.02f, 0.1f);

	[SerializeField]
	private float crashSoundVolume = 0.12f;

	private float motorVolumeRampTime = 1f;

	private float propellerAngle;

	private float propellerSpinRate;

	private const float propellerIdleAcc = 1f;

	private const float propellerIdleSpinRate = 0.6f;

	private const float propellerMaxAcc = 6.6666665f;

	private const float propellerMaxSpinRate = 5f;

	public float initialSpeed = 3f;

	private float pitch;

	private float pitchVel;

	private Vector2 pitchAccelMinMax;

	private float roll;

	private float rollVel;

	private float rollAccel;

	private float thrustAccel;

	private float motorLevel;

	private float leftAileronLevel;

	private float rightAileronLevel;

	private Vector2 aileronAngularRange = new Vector2(-30f, 45f);

	private float aileronAngularAcc = 120f;

	private float leftAileronAngle;

	private float rightAileronAngle;

	protected override void Awake()
	{
		base.Awake();
		pitchAccelMinMax.x = pitchVelocityTargetMinMax.x / pitchVelocityRampTimeMinMax.x;
		pitchAccelMinMax.y = pitchVelocityTargetMinMax.y / pitchVelocityRampTimeMinMax.y;
		rollAccel = rollVelocityTarget / rollVelocityRampTime;
		thrustAccel = thrustVelocityTarget / thrustAccelTime;
	}

	protected override void AuthorityBeginMobilization()
	{
		base.AuthorityBeginMobilization();
		float x = base.transform.lossyScale.x;
		rb.linearVelocity = base.transform.forward * initialSpeed * x;
	}

	protected override void AuthorityUpdate(float dt)
	{
		base.AuthorityUpdate(dt);
		motorLevel = 0f;
		if (localState == State.Mobilized)
		{
			motorLevel = activeInput.trigger;
		}
		leftAileronLevel = 0f;
		rightAileronLevel = 0f;
		float magnitude = activeInput.joystick.magnitude;
		if (magnitude > 0.01f)
		{
			float num = Mathf.Abs(activeInput.joystick.x) / magnitude;
			float num2 = Mathf.Abs(activeInput.joystick.y) / magnitude;
			leftAileronLevel = Mathf.Clamp(num * activeInput.joystick.x + num2 * (0f - activeInput.joystick.y), -1f, 1f);
			rightAileronLevel = Mathf.Clamp(num * activeInput.joystick.x + num2 * activeInput.joystick.y, -1f, 1f);
		}
		if (networkSync != null)
		{
			networkSync.syncedState.dataA = (byte)Mathf.Clamp(Mathf.FloorToInt(motorLevel * 255f), 0, 255);
			networkSync.syncedState.dataB = (byte)Mathf.Clamp(Mathf.FloorToInt(leftAileronLevel * 126f), -126, 126);
			networkSync.syncedState.dataC = (byte)Mathf.Clamp(Mathf.FloorToInt(rightAileronLevel * 126f), -126, 126);
		}
	}

	protected override void RemoteUpdate(float dt)
	{
		base.RemoteUpdate(dt);
		if (networkSync != null)
		{
			motorLevel = Mathf.Clamp01((float)(int)networkSync.syncedState.dataA / 255f);
			leftAileronLevel = Mathf.Clamp((float)(int)networkSync.syncedState.dataB / 126f, -1f, 1f);
			rightAileronLevel = Mathf.Clamp((float)(int)networkSync.syncedState.dataC / 126f, -1f, 1f);
		}
	}

	protected override void SharedUpdate(float dt)
	{
		base.SharedUpdate(dt);
		switch (localState)
		{
		case State.Crashed:
			if (localStatePrev != State.Crashed)
			{
				audioSource.GTStop();
				audioSource.clip = null;
				audioSource.loop = false;
				audioSource.volume = crashSoundVolume;
				if (crashSound != null)
				{
					audioSource.GTPlayOneShot(crashSound);
				}
			}
			propellerSpinRate = Mathf.MoveTowards(propellerSpinRate, 0f, 13.333333f * dt);
			propellerAngle += propellerSpinRate * 360f * dt;
			propeller.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, propellerAngle));
			break;
		case State.DockedLeft:
		case State.DockedRight:
			propellerSpinRate = Mathf.MoveTowards(propellerSpinRate, 0.6f, 6.6666665f * dt);
			propellerAngle += propellerSpinRate * 360f * dt;
			propeller.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, propellerAngle));
			break;
		case State.Mobilized:
		{
			if (localStatePrev != State.Mobilized)
			{
				audioSource.loop = true;
				audioSource.clip = motorSound;
				audioSource.volume = 0f;
				audioSource.GTPlay();
			}
			float target = Mathf.Lerp(motorSoundVolumeMinMax.x, motorSoundVolumeMinMax.y, motorLevel);
			audioSource.volume = Mathf.MoveTowards(audioSource.volume, target, motorSoundVolumeMinMax.y / motorVolumeRampTime * dt);
			propellerSpinRate = Mathf.MoveTowards(propellerSpinRate, 5f, 6.6666665f * dt);
			propellerAngle += propellerSpinRate * 360f * dt;
			propeller.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, propellerAngle));
			break;
		}
		}
		float target2 = Mathf.Lerp(aileronAngularRange.x, aileronAngularRange.y, Mathf.InverseLerp(-1f, 1f, leftAileronLevel));
		float target3 = Mathf.Lerp(aileronAngularRange.x, aileronAngularRange.y, Mathf.InverseLerp(-1f, 1f, rightAileronLevel));
		leftAileronAngle = Mathf.MoveTowards(leftAileronAngle, target2, aileronAngularAcc * Time.deltaTime);
		rightAileronAngle = Mathf.MoveTowards(rightAileronAngle, target3, aileronAngularAcc * Time.deltaTime);
		Quaternion localRotation = Quaternion.Euler(0f, -90f, 90f + leftAileronAngle);
		Quaternion localRotation2 = Quaternion.Euler(0f, 90f, -90f + rightAileronAngle);
		leftAileronLower.localRotation = localRotation;
		leftAileronUpper.localRotation = localRotation;
		rightAileronLower.localRotation = localRotation2;
		rightAileronUpper.localRotation = localRotation2;
	}

	private void FixedUpdate()
	{
		if (base.HasLocalAuthority && localState == State.Mobilized)
		{
			float x = base.transform.lossyScale.x;
			float num = thrustVelocityTarget * x;
			float num2 = thrustAccel * x;
			float fixedDeltaTime = Time.fixedDeltaTime;
			pitch = NormalizeAngle180(pitch);
			roll = NormalizeAngle180(roll);
			float num3 = pitch;
			float num4 = roll;
			if (activeInput.joystick.y >= 0f)
			{
				float target = activeInput.joystick.y * pitchVelocityTargetMinMax.y;
				pitchVel = Mathf.MoveTowards(pitchVel, target, pitchAccelMinMax.y * fixedDeltaTime);
				pitch += pitchVel * fixedDeltaTime;
			}
			else
			{
				float target2 = (0f - activeInput.joystick.y) * pitchVelocityTargetMinMax.x;
				pitchVel = Mathf.MoveTowards(pitchVel, target2, pitchAccelMinMax.x * fixedDeltaTime);
				pitch += pitchVel * fixedDeltaTime;
			}
			float target3 = (0f - activeInput.joystick.x) * rollVelocityTarget;
			rollVel = Mathf.MoveTowards(rollVel, target3, rollAccel * fixedDeltaTime);
			roll += rollVel * fixedDeltaTime;
			Quaternion quaternion = Quaternion.Euler(new Vector3(pitch - num3, 0f, roll - num4));
			base.transform.rotation = base.transform.rotation * quaternion;
			rb.angularVelocity = Vector3.zero;
			Vector3 linearVelocity = rb.linearVelocity;
			float magnitude = linearVelocity.magnitude;
			float num5 = Mathf.Max(Vector3.Dot(base.transform.forward, linearVelocity), 0f);
			float num6 = activeInput.trigger * num;
			float num7 = 0.1f * x;
			if (num6 > num7 && num6 > num5)
			{
				float num8 = Mathf.MoveTowards(num5, num6, num2 * fixedDeltaTime);
				rb.AddForce(base.transform.forward * (num8 - num5) * rb.mass, ForceMode.Impulse);
			}
			float b = 0.01f * x;
			float time = Vector3.Dot(linearVelocity / Mathf.Max(magnitude, b), base.transform.forward);
			float num9 = liftVsAttackCurve.Evaluate(time);
			float num10 = Mathf.Lerp(liftVsSpeedOutput.x, liftVsSpeedOutput.y, Mathf.InverseLerp(liftVsSpeedInput.x, liftVsSpeedInput.y, magnitude / x));
			float num11 = num9 * num10;
			Vector3 vector = Vector3.RotateTowards(linearVelocity, base.transform.forward * magnitude, pitchVelocityFollowRateAngle * (MathF.PI / 180f) * fixedDeltaTime, pitchVelocityFollowRateMagnitude * fixedDeltaTime) - linearVelocity;
			rb.AddForce(vector * num11 * rb.mass, ForceMode.Impulse);
			float time2 = Vector3.Dot(linearVelocity.normalized, base.transform.up);
			float num12 = dragVsAttackCurve.Evaluate(time2);
			rb.AddForce(-linearVelocity * maxDrag * num12 * rb.mass, ForceMode.Force);
			if (rb.useGravity)
			{
				float gravityCompensation = Mathf.Lerp(gravityCompensationRange.x, gravityCompensationRange.y, Mathf.InverseLerp(0f, num, num5 / x));
				RCVehicle.AddScaledGravityCompensationForce(rb, x, gravityCompensation);
			}
		}
	}

	private void OnCollisionEnter(Collision collision)
	{
		if (base.HasLocalAuthority && localState == State.Mobilized)
		{
			for (int i = 0; i < collision.contactCount; i++)
			{
				ContactPoint contact = collision.GetContact(i);
				if (!nonCrashColliders.Contains(contact.thisCollider))
				{
					AuthorityBeginCrash();
				}
			}
			return;
		}
		bool flag = collision.collider.gameObject.IsOnLayer(UnityLayer.GorillaThrowable);
		bool flag2 = collision.collider.gameObject.IsOnLayer(UnityLayer.GorillaHand);
		if (!(flag || flag2) || localState != State.Mobilized)
		{
			return;
		}
		Vector3 vector = Vector3.zero;
		if (flag2)
		{
			GorillaHandClimber component = collision.collider.gameObject.GetComponent<GorillaHandClimber>();
			if (component != null)
			{
				vector = GTPlayer.Instance.GetHandVelocityTracker(component.xrNode == XRNode.LeftHand).GetAverageVelocity(worldSpace: true);
			}
		}
		else if (collision.rigidbody != null)
		{
			vector = collision.rigidbody.linearVelocity;
		}
		if (flag || vector.sqrMagnitude > 0.01f)
		{
			if (base.HasLocalAuthority)
			{
				AuthorityApplyImpact(vector, flag);
			}
			else if (networkSync != null)
			{
				networkSync.photonView.RPC("HitRCVehicleRPC", RpcTarget.Others, vector, flag);
			}
		}
	}
}
