using System.Collections.Generic;
using GorillaLocomotion;
using GorillaLocomotion.Climbing;
using Photon.Pun;
using UnityEngine;
using UnityEngine.XR;

namespace GorillaTag.Cosmetics;

public class RCDragon : RCVehicle
{
	[SerializeField]
	private float maxAscendSpeed = 6f;

	[SerializeField]
	private float ascendAccelTime = 3f;

	[SerializeField]
	private float ascendWhileFlyingAccelBoost;

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
	private float crashSoundVolume = 0.1f;

	[SerializeField]
	private float breathFireVolume = 0.5f;

	[SerializeField]
	private float wingFlapVolume = 0.1f;

	[SerializeField]
	private Animation animation;

	[SerializeField]
	private string wingFlapAnimName;

	[SerializeField]
	private float wingFlapAnimSpeed = 1f;

	[SerializeField]
	private string dockedAnimName;

	[SerializeField]
	private string idleAnimName;

	[SerializeField]
	private string crashAnimName;

	[SerializeField]
	private float crashAnimSpeed = 1f;

	[SerializeField]
	private string mouthClosedAnimName;

	[SerializeField]
	private string mouthBreathFireAnimName;

	private bool shouldFlap;

	private bool isFlapping;

	private float nextFlapEventAnimTime;

	[SerializeField]
	private float flapAnimEventTime = 0.25f;

	[SerializeField]
	private GameObject fireBreath;

	[SerializeField]
	private float fireBreathDuration;

	private float fireBreathTimeRemaining;

	[SerializeField]
	private Collider crashCollider;

	[SerializeField]
	private AudioSource audioSource;

	[SerializeField]
	private List<AudioClip> breathFireSound;

	[SerializeField]
	private List<AudioClip> wingFlapSound;

	[SerializeField]
	private AudioClip crashSound;

	private float turnRate;

	private float turnAngle;

	private float tiltAngle;

	private float ascendAccel;

	private float turnAccel;

	private float tiltAccel;

	private float horizontalAccel;

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
		shouldFlap = false;
		isFlapping = false;
		StopBreathFire();
		if (animation != null)
		{
			animation[wingFlapAnimName].speed = wingFlapAnimSpeed;
			animation[crashAnimName].speed = crashAnimSpeed;
			animation[mouthClosedAnimName].layer = 1;
			animation[mouthBreathFireAnimName].layer = 1;
		}
		nextFlapEventAnimTime = flapAnimEventTime;
	}

	protected override void OnDisable()
	{
		base.OnDisable();
		audioSource.GTStop();
	}

	public void StartBreathFire()
	{
		if (!string.IsNullOrEmpty(mouthBreathFireAnimName))
		{
			animation.CrossFade(mouthBreathFireAnimName, 0.1f);
		}
		if (fireBreath != null)
		{
			fireBreath.SetActive(value: true);
		}
		PlayRandomSound(breathFireSound, breathFireVolume);
		fireBreathTimeRemaining = fireBreathDuration;
	}

	public void StopBreathFire()
	{
		if (!string.IsNullOrEmpty(mouthClosedAnimName))
		{
			animation.CrossFade(mouthClosedAnimName, 0.1f);
		}
		if (fireBreath != null)
		{
			fireBreath.SetActive(value: false);
		}
		fireBreathTimeRemaining = -1f;
	}

	public bool IsBreathingFire()
	{
		return fireBreathTimeRemaining >= 0f;
	}

	private void PlayRandomSound(List<AudioClip> clips, float volume)
	{
		if (clips != null && clips.Count != 0)
		{
			PlaySound(clips[Random.Range(0, clips.Count)], volume);
		}
	}

	private void PlaySound(AudioClip clip, float volume)
	{
		if (!(audioSource == null) && !(clip == null))
		{
			audioSource.GTStop();
			audioSource.clip = null;
			audioSource.loop = false;
			audioSource.volume = volume;
			audioSource.GTPlayOneShot(clip);
		}
	}

	protected override void AuthorityUpdate(float dt)
	{
		base.AuthorityUpdate(dt);
		motorLevel = 0f;
		if (localState == State.Mobilized)
		{
			motorLevel = Mathf.Max(Mathf.Max(Mathf.Abs(activeInput.joystick.y), Mathf.Abs(activeInput.joystick.x)), activeInput.trigger);
			if (!IsBreathingFire() && activeInput.buttons > 0)
			{
				StartBreathFire();
			}
		}
		if (networkSync != null)
		{
			networkSync.syncedState.dataA = (byte)Mathf.Clamp(Mathf.FloorToInt(motorLevel * 255f), 0, 255);
			networkSync.syncedState.dataB = activeInput.buttons;
			networkSync.syncedState.dataC = (byte)(shouldFlap ? 1u : 0u);
		}
	}

	protected override void RemoteUpdate(float dt)
	{
		base.RemoteUpdate(dt);
		if (localState == State.Mobilized && networkSync != null)
		{
			motorLevel = Mathf.Clamp01((float)(int)networkSync.syncedState.dataA / 255f);
			if (!IsBreathingFire() && networkSync.syncedState.dataB > 0)
			{
				StartBreathFire();
			}
			shouldFlap = networkSync.syncedState.dataC > 0;
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
				PlaySound(crashSound, crashSoundVolume);
				if (crashCollider != null)
				{
					crashCollider.enabled = true;
				}
				if (animation != null)
				{
					animation.CrossFade(crashAnimName, 0.05f);
				}
				if (IsBreathingFire())
				{
					StopBreathFire();
				}
			}
			break;
		case State.DockedLeft:
		case State.DockedRight:
			if (localStatePrev != State.DockedLeft && localStatePrev != State.DockedRight)
			{
				audioSource.GTStop();
				if (crashCollider != null)
				{
					crashCollider.enabled = false;
				}
				if (animation != null)
				{
					animation.Play(dockedAnimName);
				}
				if (IsBreathingFire())
				{
					StopBreathFire();
				}
			}
			break;
		case State.Mobilized:
		{
			if (localStatePrev != State.Mobilized && crashCollider != null)
			{
				crashCollider.enabled = false;
			}
			if (animation != null)
			{
				if (!isFlapping && shouldFlap)
				{
					animation.CrossFade(wingFlapAnimName, 0.1f);
					nextFlapEventAnimTime = flapAnimEventTime;
				}
				else if (isFlapping && !shouldFlap)
				{
					animation.CrossFade(idleAnimName, 0.15f);
				}
				isFlapping = shouldFlap;
				if (isFlapping && !IsBreathingFire())
				{
					AnimationState animationState = animation[wingFlapAnimName];
					if (animationState.normalizedTime * animationState.length > nextFlapEventAnimTime)
					{
						PlayRandomSound(wingFlapSound, wingFlapVolume);
						nextFlapEventAnimTime = (Mathf.Floor(animationState.normalizedTime) + 1f) * animationState.length + flapAnimEventTime;
					}
				}
			}
			GTTime.TimeAsDouble();
			if (IsBreathingFire())
			{
				fireBreathTimeRemaining -= dt;
				if (fireBreathTimeRemaining <= 0f)
				{
					StopBreathFire();
				}
			}
			float target = Mathf.Lerp(motorSoundVolumeMinMax.x, motorSoundVolumeMinMax.y, motorLevel);
			audioSource.volume = Mathf.MoveTowards(audioSource.volume, target, motorSoundVolumeMinMax.y / motorVolumeRampTime * dt);
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
		float x = base.transform.lossyScale.x;
		float fixedDeltaTime = Time.fixedDeltaTime;
		shouldFlap = false;
		if (localState == State.Mobilized)
		{
			float num = maxAscendSpeed * x;
			float num2 = maxHorizontalSpeed * x;
			float num3 = ascendAccel * x;
			float num4 = ascendWhileFlyingAccelBoost * x;
			float num5 = 0.5f * x;
			float num6 = 45f;
			Vector3 linearVelocity = rb.linearVelocity;
			Vector3 normalized = new Vector3(base.transform.forward.x, 0f, base.transform.forward.z).normalized;
			turnAngle = Vector3.SignedAngle(Vector3.forward, normalized, Vector3.up);
			tiltAngle = Vector3.SignedAngle(normalized, base.transform.forward, base.transform.right);
			float target = activeInput.joystick.x * maxTurnRate;
			turnRate = Mathf.MoveTowards(turnRate, target, turnAccel * fixedDeltaTime);
			turnAngle += turnRate * fixedDeltaTime;
			float num7 = Vector3.Dot(normalized, linearVelocity);
			float t = Mathf.InverseLerp(0f - num2, num2, num7);
			float target2 = Mathf.Lerp(0f - maxHorizontalTiltAngle, maxHorizontalTiltAngle, t);
			tiltAngle = Mathf.MoveTowards(tiltAngle, target2, tiltAccel * fixedDeltaTime);
			base.transform.rotation = Quaternion.Euler(new Vector3(tiltAngle, turnAngle, 0f));
			Vector3 vector = new Vector3(linearVelocity.x, 0f, linearVelocity.z);
			Vector3 vector2 = Vector3.Lerp(normalized * activeInput.joystick.y * num2, vector, Mathf.Exp((0f - horizontalAccelTime) * fixedDeltaTime));
			rb.AddForce((vector2 - vector) * rb.mass, ForceMode.Impulse);
			float num8 = activeInput.trigger * num;
			if (num8 > 0.01f && linearVelocity.y < num8)
			{
				rb.AddForce(Vector3.up * num3 * rb.mass, ForceMode.Force);
			}
			bool flag = Mathf.Abs(num7) > num5;
			bool flag2 = Mathf.Abs(turnRate) > num6;
			if (flag || flag2)
			{
				rb.AddForce(Vector3.up * num4 * rb.mass, ForceMode.Force);
			}
			shouldFlap = num8 > 0.01f || flag || flag2;
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
