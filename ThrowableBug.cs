using System;
using GorillaExtensions;
using UnityEngine;
using UnityEngine.Serialization;

public class ThrowableBug : TransferrableObject, ITickSystemTick
{
	public enum BugName
	{
		NONE,
		DougTheBug,
		MattTheBat
	}

	private enum AudioState
	{
		JustGrabbed,
		ContinuallyGrabbed,
		JustReleased,
		NotHeld
	}

	public ThrowableBugReliableState reliableState;

	public float slowingDownProgress;

	public float startingSpeed;

	public int raycastFramePeriod = 5;

	private int raycastFrameCounter;

	public float bobingSpeed = 1f;

	public float bobMagnintude = 0.1f;

	public bool shouldRandomizeFrequency;

	public float minRandFrequency = 0.008f;

	public float maxRandFrequency = 1f;

	public float bobingFrequency = 1f;

	public float bobingState;

	public float thrownYVelocity;

	public float collisionHitRadius;

	public LayerMask collisionCheckMask;

	public Vector3 thrownVeloicity;

	public Vector3 targetVelocity;

	public Quaternion bugRotationalVelocity;

	private RaycastHit[] rayCastNonAllocColliders;

	private RaycastHit[] rayCastNonAllocColliders2;

	public VRRig followingRig;

	public bool isTooHighTravelingDown;

	public float descentSlerp;

	public float ascentSlerp;

	public float maxNaturalSpeed;

	public float slowdownAcceleration;

	public float maximumHeightOffOfTheGroundBeforeStartingDescent = 5f;

	public float minimumHeightOffOfTheGroundBeforeStoppingDescent = 3f;

	public float descentRate = 0.2f;

	public float descentSlerpRate = 0.2f;

	public float minimumHeightOffOfTheGroundBeforeStartingAscent = 0.5f;

	public float maximumHeightOffOfTheGroundBeforeStoppingAscent = 0.75f;

	public float ascentRate = 0.4f;

	public float ascentSlerpRate = 1f;

	private bool isTooLowTravelingUp;

	public Animator animator;

	[FormerlySerializedAs("grabBugAudioSource")]
	public AudioClip grabBugAudioClip;

	[FormerlySerializedAs("releaseBugAudioSource")]
	public AudioClip releaseBugAudioClip;

	[FormerlySerializedAs("flyingBugAudioSource")]
	public AudioClip flyingBugAudioClip;

	[SerializeField]
	private AudioSource audioSource;

	public GTZone startZone;

	private GTZone currentZone;

	private float bobbingDefaultFrequency = 1f;

	public int updateMultiplier;

	private AudioState currentAudioState;

	private float speedMultiplier = 1f;

	private GorillaVelocityEstimator velocityEstimator;

	[SerializeField]
	private BugName bugName;

	private Transform lockedTarget;

	private bool locked;

	private static readonly int _g_IsHeld = Animator.StringToHash("isHeld");

	public bool TickRunning { get; set; }

	protected override void Start()
	{
		base.Start();
		float f = UnityEngine.Random.Range(0f, MathF.PI * 2f);
		targetVelocity = new Vector3(Mathf.Sin(f) * maxNaturalSpeed, 0f, Mathf.Cos(f) * maxNaturalSpeed);
		currentState = PositionState.Dropped;
		rayCastNonAllocColliders = new RaycastHit[5];
		rayCastNonAllocColliders2 = new RaycastHit[5];
		velocityEstimator = GetComponent<GorillaVelocityEstimator>();
		currentZone = startZone;
	}

	internal override void OnEnable()
	{
		base.OnEnable();
		ThrowableBugBeacon.OnCall += ThrowableBugBeacon_OnCall;
		ThrowableBugBeacon.OnDismiss += ThrowableBugBeacon_OnDismiss;
		ThrowableBugBeacon.OnLock += ThrowableBugBeacon_OnLock;
		ThrowableBugBeacon.OnUnlock += ThrowableBugBeacon_OnUnlock;
		ThrowableBugBeacon.OnChangeSpeedMultiplier += ThrowableBugBeacon_OnChangeSpeedMultiplier;
		TickSystem<object>.AddTickCallback(this);
	}

	internal override void OnDisable()
	{
		base.OnDisable();
		ThrowableBugBeacon.OnCall -= ThrowableBugBeacon_OnCall;
		ThrowableBugBeacon.OnDismiss -= ThrowableBugBeacon_OnDismiss;
		ThrowableBugBeacon.OnLock -= ThrowableBugBeacon_OnLock;
		ThrowableBugBeacon.OnUnlock -= ThrowableBugBeacon_OnUnlock;
		ThrowableBugBeacon.OnChangeSpeedMultiplier -= ThrowableBugBeacon_OnChangeSpeedMultiplier;
		TickSystem<object>.RemoveTickCallback(this);
	}

	private bool isValid(ThrowableBugBeacon tbb)
	{
		if (tbb.BugName == bugName)
		{
			if (!(tbb.Range <= 0f))
			{
				return Vector3.Distance(tbb.transform.position, base.transform.position) <= tbb.Range;
			}
			return true;
		}
		return false;
	}

	private void ThrowableBugBeacon_OnCall(ThrowableBugBeacon tbb)
	{
		if (isValid(tbb))
		{
			reliableState.travelingDirection = tbb.transform.position - base.transform.position;
		}
	}

	private void ThrowableBugBeacon_OnLock(ThrowableBugBeacon tbb)
	{
		if (isValid(tbb))
		{
			reliableState.travelingDirection = tbb.transform.position - base.transform.position;
			lockedTarget = tbb.transform;
			locked = true;
		}
	}

	private void ThrowableBugBeacon_OnDismiss(ThrowableBugBeacon tbb)
	{
		if (isValid(tbb))
		{
			reliableState.travelingDirection = base.transform.position - tbb.transform.position;
			locked = false;
		}
	}

	private void ThrowableBugBeacon_OnUnlock(ThrowableBugBeacon tbb)
	{
		if (isValid(tbb))
		{
			locked = false;
		}
	}

	private void ThrowableBugBeacon_OnChangeSpeedMultiplier(ThrowableBugBeacon tbb, float f)
	{
		if (isValid(tbb))
		{
			speedMultiplier = f;
		}
	}

	public override bool ShouldBeKinematic()
	{
		return true;
	}

	protected override void LateUpdateShared()
	{
		base.LateUpdateShared();
		raycastFrameCounter = (raycastFrameCounter + 1) % raycastFramePeriod;
		bool flag = currentState == PositionState.InLeftHand || currentState == PositionState.InRightHand;
		if (animator.enabled)
		{
			animator.SetBool(_g_IsHeld, flag);
		}
		animator.enabled = GorillaTagger.Instance.offlineVRRig.zoneEntity.currentZone == currentZone;
		if (!audioSource)
		{
			return;
		}
		switch (currentAudioState)
		{
		case AudioState.NotHeld:
			if (!flag)
			{
				if ((bool)flyingBugAudioClip && !audioSource.isPlaying)
				{
					audioSource.clip = flyingBugAudioClip;
					audioSource.time = 0f;
					if (audioSource.isActiveAndEnabled)
					{
						audioSource.GTPlay();
					}
				}
			}
			else
			{
				currentAudioState = AudioState.JustGrabbed;
			}
			break;
		case AudioState.JustGrabbed:
			if (flag)
			{
				if ((bool)grabBugAudioClip && audioSource.clip != grabBugAudioClip)
				{
					audioSource.clip = grabBugAudioClip;
					audioSource.time = 0f;
					if (audioSource.isActiveAndEnabled)
					{
						audioSource.GTPlay();
					}
				}
				else if (!audioSource.isPlaying)
				{
					currentAudioState = AudioState.ContinuallyGrabbed;
				}
			}
			else
			{
				currentAudioState = AudioState.JustReleased;
			}
			break;
		case AudioState.ContinuallyGrabbed:
			if (!flag)
			{
				currentAudioState = AudioState.JustReleased;
			}
			break;
		case AudioState.JustReleased:
			if (!flag)
			{
				if ((bool)releaseBugAudioClip && audioSource.clip != releaseBugAudioClip)
				{
					audioSource.clip = releaseBugAudioClip;
					audioSource.time = 0f;
					if (audioSource.isActiveAndEnabled)
					{
						audioSource.GTPlay();
					}
				}
				else if (!audioSource.isPlaying)
				{
					currentAudioState = AudioState.NotHeld;
				}
			}
			else
			{
				currentAudioState = AudioState.JustGrabbed;
			}
			break;
		}
	}

	protected override void LateUpdateLocal()
	{
		base.LateUpdateLocal();
		if (!reliableState || (currentState & PositionState.Dropped) == 0)
		{
			return;
		}
		if (locked && Vector3.Distance(lockedTarget.position, base.transform.position) > 0.1f)
		{
			reliableState.travelingDirection = lockedTarget.position - base.transform.position;
		}
		if (slowingDownProgress < 1f)
		{
			slowingDownProgress += slowdownAcceleration * Time.deltaTime;
			reliableState.travelingDirection = Vector3.Slerp(thrownVeloicity, targetVelocity, Mathf.SmoothStep(0f, 1f, slowingDownProgress));
		}
		else
		{
			reliableState.travelingDirection = reliableState.travelingDirection.normalized * maxNaturalSpeed;
		}
		bobingFrequency = (shouldRandomizeFrequency ? RandomizeBobingFrequency() : bobbingDefaultFrequency);
		float num = bobingState + bobingSpeed * Time.deltaTime;
		float num2 = Mathf.Sin(num / bobingFrequency) - Mathf.Sin(bobingState / bobingFrequency);
		Vector3 vector = Vector3.up * (num2 * bobMagnintude);
		bobingState = num;
		if (bobingState > MathF.PI * 2f)
		{
			bobingState -= MathF.PI * 2f;
		}
		vector += reliableState.travelingDirection * Time.deltaTime;
		float maxDistance = (isTooHighTravelingDown ? minimumHeightOffOfTheGroundBeforeStoppingDescent : maximumHeightOffOfTheGroundBeforeStartingDescent);
		float num3 = (isTooLowTravelingUp ? maximumHeightOffOfTheGroundBeforeStoppingAscent : minimumHeightOffOfTheGroundBeforeStartingAscent);
		if (raycastFrameCounter == 0)
		{
			if (Physics.RaycastNonAlloc(base.transform.position, Vector3.down, rayCastNonAllocColliders2, maxDistance, collisionCheckMask) > 0)
			{
				isTooHighTravelingDown = false;
				if (descentSlerp > 0f)
				{
					descentSlerp = Mathf.Clamp01(descentSlerp - descentSlerpRate * Time.deltaTime);
				}
				RaycastHit raycastHit = rayCastNonAllocColliders2[0];
				isTooLowTravelingUp = raycastHit.distance < num3;
				if (isTooLowTravelingUp)
				{
					if (ascentSlerp < 1f)
					{
						ascentSlerp = Mathf.Clamp01(ascentSlerp + ascentSlerpRate * Time.deltaTime);
					}
				}
				else if (ascentSlerp > 0f)
				{
					ascentSlerp = Mathf.Clamp01(ascentSlerp - ascentSlerpRate * Time.deltaTime);
				}
			}
			else
			{
				isTooHighTravelingDown = true;
				if (descentSlerp < 1f)
				{
					descentSlerp = Mathf.Clamp01(descentSlerp + descentSlerpRate * Time.deltaTime);
				}
			}
		}
		vector += Time.deltaTime * Mathf.SmoothStep(0f, 1f, descentSlerp) * descentRate * Vector3.down;
		vector += Time.deltaTime * Mathf.SmoothStep(0f, 1f, ascentSlerp) * ascentRate * Vector3.up;
		Quaternion.FromToRotation(base.transform.rotation * Vector3.up, Quaternion.identity * Vector3.up).ToAngleAxis(out var angle, out var axis);
		Quaternion quaternion = Quaternion.AngleAxis(angle * 0.02f, axis);
		Quaternion.FromToRotation(base.transform.rotation * Vector3.forward, reliableState.travelingDirection.normalized).ToAngleAxis(out var angle2, out var axis2);
		Quaternion quaternion2 = Quaternion.AngleAxis(angle2 * 0.005f, axis2);
		quaternion = quaternion2 * quaternion;
		vector = quaternion * quaternion * quaternion * quaternion * vector;
		vector *= speedMultiplier;
		speedMultiplier = Mathf.MoveTowards(speedMultiplier, 1f, Time.deltaTime);
		if (raycastFrameCounter == 0)
		{
			if (Physics.SphereCastNonAlloc(base.transform.position, collisionHitRadius, vector.normalized, rayCastNonAllocColliders, vector.magnitude, collisionCheckMask) > 0)
			{
				Vector3 normal = rayCastNonAllocColliders[0].normal;
				reliableState.travelingDirection = Vector3.Reflect(reliableState.travelingDirection, normal).x0z();
				base.transform.position += Vector3.Reflect(vector, normal);
				thrownVeloicity = Vector3.Reflect(thrownVeloicity, normal);
				targetVelocity = Vector3.Reflect(targetVelocity, normal).x0z();
			}
			else
			{
				base.transform.position += vector;
			}
		}
		else
		{
			base.transform.position += vector;
		}
		bugRotationalVelocity = quaternion * bugRotationalVelocity;
		bugRotationalVelocity.ToAngleAxis(out var angle3, out var axis3);
		bugRotationalVelocity = Quaternion.AngleAxis(angle3 * 0.9f, axis3);
		base.transform.rotation = bugRotationalVelocity * base.transform.rotation;
	}

	private float RandomizeBobingFrequency()
	{
		return UnityEngine.Random.Range(minRandFrequency, maxRandFrequency);
	}

	public override bool OnRelease(DropZone zoneReleased, GameObject releasingHand)
	{
		if (!base.OnRelease(zoneReleased, releasingHand))
		{
			return false;
		}
		slowingDownProgress = 0f;
		Vector3 travelingDirection = (thrownVeloicity = velocityEstimator.linearVelocity);
		reliableState.travelingDirection = travelingDirection;
		bugRotationalVelocity = Quaternion.Euler(velocityEstimator.angularVelocity);
		startingSpeed = travelingDirection.magnitude;
		Vector3 normalized = reliableState.travelingDirection.x0z().normalized;
		targetVelocity = normalized * maxNaturalSpeed;
		return true;
	}

	public void OnCollisionEnter(Collision collision)
	{
		reliableState.travelingDirection *= -1f;
	}

	public void Tick()
	{
		if (updateMultiplier > 0)
		{
			for (int i = 0; i < updateMultiplier; i++)
			{
				LateUpdateLocal();
			}
		}
	}
}
