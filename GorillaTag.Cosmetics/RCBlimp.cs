using GorillaLocomotion;
using GorillaLocomotion.Climbing;
using Photon.Pun;
using UnityEngine;
using UnityEngine.XR;

namespace GorillaTag.Cosmetics;

public class RCBlimp : RCVehicle
{
	[SerializeField]
	private float maxAscendSpeed = 6f;

	[SerializeField]
	private float ascendAccelTime = 3f;

	[SerializeField]
	private float gravityCompensation = 0.9f;

	[SerializeField]
	private float crashedGravityCompensation = 0.5f;

	[SerializeField]
	private float maxTurnRate = 90f;

	[SerializeField]
	private float turnAccelTime = 0.75f;

	[SerializeField]
	private float maxHorizontalSpeed = 6f;

	[SerializeField]
	private float horizontalAccelTime = 2f;

	[SerializeField]
	private float maxHorizontalTiltAngle = 45f;

	[SerializeField]
	private float horizontalTiltTime = 2f;

	[SerializeField]
	private Vector2 motorSoundVolumeMinMax = new Vector2(0.1f, 0.8f);

	[SerializeField]
	private float deflateSoundVolume = 0.1f;

	[SerializeField]
	private Collider crashCollider;

	[SerializeField]
	private Transform leftPropeller;

	[SerializeField]
	private Transform rightPropeller;

	[SerializeField]
	private SkinnedMeshRenderer blimpMesh;

	[SerializeField]
	private AudioSource audioSource;

	[SerializeField]
	private AudioClip motorSound;

	[SerializeField]
	private AudioClip deflateSound;

	private float turnRate;

	private float turnAngle;

	private float tiltAngle;

	private float ascendAccel;

	private float turnAccel;

	private float tiltAccel;

	private float horizontalAccel;

	private float leftPropellerAngle;

	private float rightPropellerAngle;

	private float leftPropellerSpinRate;

	private float rightPropellerSpinRate;

	private float blimpDeflateBlendWeight;

	private float deflateRate = Mathf.Exp(1f);

	private const float propellerIdleAcc = 1f;

	private const float propellerIdleSpinRate = 0.6f;

	private const float propellerMaxAcc = 6.6666665f;

	private const float propellerMaxSpinRate = 5f;

	private float motorVolumeRampTime = 1f;

	private float motorLevel;

	protected override void AuthorityBeginDocked()
	{
		base.AuthorityBeginDocked();
		turnRate = 0f;
		turnAngle = Vector3.SignedAngle(Vector3.forward, Vector3.ProjectOnPlane(base.transform.forward, Vector3.up), Vector3.up);
		motorLevel = 0f;
		if (connectedRemote == null)
		{
			base.gameObject.SetActive(value: false);
		}
	}

	protected override void Awake()
	{
		base.Awake();
		ascendAccel = maxAscendSpeed / ascendAccelTime;
		turnAccel = maxTurnRate / turnAccelTime;
		horizontalAccel = maxHorizontalSpeed / horizontalAccelTime;
		tiltAccel = maxHorizontalTiltAngle / horizontalTiltTime;
	}

	protected override void OnDisable()
	{
		base.OnDisable();
		audioSource.GTStop();
	}

	protected override void AuthorityUpdate(float dt)
	{
		base.AuthorityUpdate(dt);
		motorLevel = 0f;
		if (localState == State.Mobilized)
		{
			motorLevel = Mathf.Max(Mathf.Max(Mathf.Abs(activeInput.joystick.y), Mathf.Abs(activeInput.joystick.x)), activeInput.trigger);
		}
		if (networkSync != null)
		{
			networkSync.syncedState.dataA = (byte)Mathf.Clamp(Mathf.FloorToInt(motorLevel * 255f), 0, 255);
		}
	}

	protected override void RemoteUpdate(float dt)
	{
		base.RemoteUpdate(dt);
		if (localState == State.Mobilized && networkSync != null)
		{
			motorLevel = Mathf.Clamp01((float)(int)networkSync.syncedState.dataA / 255f);
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
				audioSource.volume = deflateSoundVolume;
				if (deflateSound != null)
				{
					audioSource.GTPlayOneShot(deflateSound);
				}
				leftPropellerSpinRate = 0f;
				rightPropellerSpinRate = 0f;
				leftPropellerAngle = 0f;
				rightPropellerAngle = 0f;
				leftPropeller.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, -90f));
				rightPropeller.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, 90f));
				crashCollider.enabled = true;
			}
			blimpDeflateBlendWeight = Mathf.Lerp(1f, blimpDeflateBlendWeight, Mathf.Exp((0f - deflateRate) * dt));
			blimpMesh.SetBlendShapeWeight(0, blimpDeflateBlendWeight * 100f);
			break;
		case State.DockedLeft:
		case State.DockedRight:
			if (localStatePrev != State.DockedLeft && localStatePrev != State.DockedRight)
			{
				audioSource.GTStop();
				blimpDeflateBlendWeight = 0f;
				blimpMesh.SetBlendShapeWeight(0, 0f);
				crashCollider.enabled = false;
			}
			leftPropellerSpinRate = Mathf.MoveTowards(leftPropellerSpinRate, 0.6f, 6.6666665f * dt);
			rightPropellerSpinRate = Mathf.MoveTowards(rightPropellerSpinRate, 0.6f, 6.6666665f * dt);
			leftPropellerAngle += leftPropellerSpinRate * 360f * dt;
			rightPropellerAngle += rightPropellerSpinRate * 360f * dt;
			leftPropeller.transform.localRotation = Quaternion.Euler(new Vector3(leftPropellerAngle, 0f, -90f));
			rightPropeller.transform.localRotation = Quaternion.Euler(new Vector3(rightPropellerAngle, 0f, 90f));
			break;
		case State.Mobilized:
		{
			if (localStatePrev != State.Mobilized)
			{
				audioSource.loop = true;
				audioSource.clip = motorSound;
				audioSource.volume = 0f;
				audioSource.GTPlay();
				blimpDeflateBlendWeight = 0f;
				blimpMesh.SetBlendShapeWeight(0, 0f);
				crashCollider.enabled = false;
			}
			float target = Mathf.Lerp(motorSoundVolumeMinMax.x, motorSoundVolumeMinMax.y, motorLevel);
			audioSource.volume = Mathf.MoveTowards(audioSource.volume, target, motorSoundVolumeMinMax.y / motorVolumeRampTime * dt);
			blimpDeflateBlendWeight = 0f;
			float num = activeInput.joystick.y * 5f;
			float num2 = activeInput.joystick.x * 5f;
			float target2 = Mathf.Clamp(num2 + num + 0.6f, -5f, 5f);
			float target3 = Mathf.Clamp(0f - num2 + num + 0.6f, -5f, 5f);
			leftPropellerSpinRate = Mathf.MoveTowards(leftPropellerSpinRate, target2, 6.6666665f * dt);
			rightPropellerSpinRate = Mathf.MoveTowards(rightPropellerSpinRate, target3, 6.6666665f * dt);
			leftPropellerAngle += leftPropellerSpinRate * 360f * dt;
			rightPropellerAngle += rightPropellerSpinRate * 360f * dt;
			leftPropeller.transform.localRotation = Quaternion.Euler(new Vector3(leftPropellerAngle, 0f, -90f));
			rightPropeller.transform.localRotation = Quaternion.Euler(new Vector3(rightPropellerAngle, 0f, 90f));
			break;
		}
		case State.Disabled:
			break;
		}
	}

	private void FixedUpdate()
	{
		if (!base.HasLocalAuthority)
		{
			return;
		}
		float fixedDeltaTime = Time.fixedDeltaTime;
		float x = base.transform.lossyScale.x;
		if (localState == State.Mobilized)
		{
			float num = maxAscendSpeed * x;
			float num2 = maxHorizontalSpeed * x;
			float num3 = ascendAccel * x;
			Vector3 linearVelocity = rb.linearVelocity;
			Vector3 normalized = new Vector3(base.transform.forward.x, 0f, base.transform.forward.z).normalized;
			turnAngle = Vector3.SignedAngle(Vector3.forward, normalized, Vector3.up);
			tiltAngle = Vector3.SignedAngle(normalized, base.transform.forward, base.transform.right);
			float target = activeInput.joystick.x * maxTurnRate;
			turnRate = Mathf.MoveTowards(turnRate, target, turnAccel * fixedDeltaTime);
			turnAngle += turnRate * fixedDeltaTime;
			float value = Vector3.Dot(normalized, linearVelocity);
			float t = Mathf.InverseLerp(0f - num2, num2, value);
			float target2 = Mathf.Lerp(0f - maxHorizontalTiltAngle, maxHorizontalTiltAngle, t);
			tiltAngle = Mathf.MoveTowards(tiltAngle, target2, tiltAccel * fixedDeltaTime);
			base.transform.rotation = Quaternion.Euler(new Vector3(tiltAngle, turnAngle, 0f));
			Vector3 vector = new Vector3(linearVelocity.x, 0f, linearVelocity.z);
			Vector3 vector2 = Vector3.Lerp(normalized * activeInput.joystick.y * num2, vector, Mathf.Exp((0f - horizontalAccelTime) * fixedDeltaTime));
			rb.AddForce((vector2 - vector) * rb.mass, ForceMode.Impulse);
			float num4 = activeInput.trigger * num;
			if (num4 > 0.01f && linearVelocity.y < num4)
			{
				rb.AddForce(Vector3.up * num3 * rb.mass, ForceMode.Force);
			}
			if (rb.useGravity)
			{
				RCVehicle.AddScaledGravityCompensationForce(rb, x, gravityCompensation);
			}
		}
		else if (localState == State.Crashed && rb.useGravity)
		{
			RCVehicle.AddScaledGravityCompensationForce(rb, x, crashedGravityCompensation);
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		bool flag = other.gameObject.IsOnLayer(UnityLayer.GorillaThrowable);
		bool flag2 = other.gameObject.IsOnLayer(UnityLayer.GorillaHand);
		if (!other.isTrigger && base.HasLocalAuthority && localState == State.Mobilized)
		{
			AuthorityBeginCrash();
		}
		else
		{
			if (!(flag || flag2) || localState != State.Mobilized)
			{
				return;
			}
			Vector3 vector = Vector3.zero;
			if (flag2)
			{
				GorillaHandClimber component = other.gameObject.GetComponent<GorillaHandClimber>();
				if (component != null)
				{
					vector = GTPlayer.Instance.GetHandVelocityTracker(component.xrNode == XRNode.LeftHand).GetAverageVelocity(worldSpace: true);
				}
			}
			else if (other.attachedRigidbody != null)
			{
				vector = other.attachedRigidbody.linearVelocity;
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
}
