using GorillaExtensions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace GorillaTag.Cosmetics;

public class ProjectileShooterCosmetic : MonoBehaviour, ITickSystemTick
{
	private enum ShootActivator
	{
		ButtonReleased,
		ButtonPressed,
		ButtonStayed,
		VelocityEstimatorThreshold,
		ButtonReleasedFullCharge
	}

	private enum ShootDirection
	{
		LaunchTransformRotation,
		LineFromRigToLaunchTransform
	}

	private const string CHARGE_STR = "allowCharging";

	private const string CHARGE_MSG = "only enabled when allowCharging is true.";

	private const string HAPTICS_STR = "enableHaptics";

	private const string MOVE_STR = "IsMovementShoot";

	[SerializeField]
	private HashWrapper projectilePrefab;

	[SerializeField]
	private HashWrapper projectileTrailPrefab;

	[FormerlySerializedAs("launchActivatorType")]
	[SerializeField]
	private ShootActivator shootActivatorType;

	[FormerlySerializedAs("launchDirectionType")]
	[SerializeField]
	private ShootDirection shootDirectionType;

	[SerializeField]
	private Vector3 offsetRigPosition;

	[FormerlySerializedAs("launchTransform")]
	[SerializeField]
	private Transform shootFromTransform;

	[SerializeField]
	private bool drawShootVector;

	[FormerlySerializedAs("cooldown")]
	[SerializeField]
	private float cooldownSeconds;

	[Space]
	[SerializeField]
	private bool enableHaptics = true;

	[FormerlySerializedAs("hapticsIntensity")]
	[SerializeField]
	private float shootHapticsIntensity = 0.5f;

	[FormerlySerializedAs("hapticsDuration")]
	[SerializeField]
	private float shootHapticsDuration = 0.2f;

	[SerializeField]
	[Tooltip("only enabled when allowCharging is true.")]
	private float chargeHapticsIntensity = 0.3f;

	[SerializeField]
	[Tooltip("only enabled when allowCharging is true.")]
	private float maxChargeHapticsIntensity = 0.3f;

	[SerializeField]
	private bool hapticsBothHands;

	[Space]
	[SerializeField]
	private GorillaVelocityEstimator velocityEstimator;

	[SerializeField]
	private float velocityEstimatorStartGestureSpeed = 0.5f;

	[SerializeField]
	private float velocityEstimatorStopGestureSpeed = 0.2f;

	[SerializeField]
	private float velocityEstimatorMinRigDotProduct = 0.5f;

	[SerializeField]
	private bool logVelocityEstimatorSpeed;

	[FormerlySerializedAs("launchMinSpeed")]
	[SerializeField]
	[Tooltip("only enabled when allowCharging is true.")]
	private float shootMinSpeed;

	[FormerlySerializedAs("launchMaxSpeed")]
	[SerializeField]
	private float shootMaxSpeed;

	[SerializeField]
	private bool allowCharging;

	[SerializeField]
	private float maxChargeSeconds = 2f;

	[SerializeField]
	private float snapToMaxChargeAt = 9999999f;

	[SerializeField]
	private float chargeDecaySpeed = 9999999f;

	[SerializeField]
	private bool runChargeCancelledEventOnShoot;

	[SerializeField]
	private AnimationCurve chargeRateCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	[SerializeField]
	private AnimationCurve chargeToShotSpeedCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	[FormerlySerializedAs("onReadyToShoot")]
	public UnityEvent onCooldownFinished;

	public ContinuousPropertyArray continuousChargingProperties;

	public UnityEvent<float> whileCharging;

	public UnityEvent onMaxCharge;

	public UnityEvent onChargeCancelled;

	[FormerlySerializedAs("onLaunchProjectileShared")]
	public UnityEvent<float> onShoot;

	[FormerlySerializedAs("onOwnerLaunchProjectile")]
	public UnityEvent<float> onShootLocal;

	[SerializeField]
	private int numberOfProgressSteps;

	public UnityEvent<int> onMovedToNextStep;

	public UnityEvent<int> onReachedLastProgressStep;

	private int currentStep = -1;

	private int lastStep = -1;

	private bool isPressed;

	private bool velocityEstimatorThresholdMet;

	private float cooldownRemaining;

	private float chargeTime;

	private TransferrableObject transferrableObject;

	private VRRig rig;

	private bool isLocal;

	private Transform debugShootDirection;

	public bool shootingAllowed { get; set; } = true;

	private bool IsCoolingDown => cooldownRemaining > 0f;

	public bool TickRunning { get; set; }

	private bool IsMovementShoot()
	{
		return shootActivatorType == ShootActivator.VelocityEstimatorThreshold;
	}

	private bool IsRigDirection()
	{
		return shootDirectionType == ShootDirection.LineFromRigToLaunchTransform;
	}

	private void Awake()
	{
		transferrableObject = GetComponent<TransferrableObject>();
		rig = ((transferrableObject == null) ? GetComponentInParent<VRRig>() : transferrableObject.ownerRig);
		onMovedToNextStep?.Invoke(currentStep);
		isLocal = (transferrableObject != null && transferrableObject.IsMyItem()) || (rig != null && rig == GorillaTagger.Instance.offlineVRRig);
	}

	public void Tick()
	{
		if (IsCoolingDown)
		{
			cooldownRemaining -= Time.deltaTime;
			if (cooldownRemaining <= 0f)
			{
				cooldownRemaining = 0f;
				onCooldownFinished?.Invoke();
				if (isPressed)
				{
					SetPressState(pressed: true);
				}
				if (!allowCharging && shootActivatorType != ShootActivator.VelocityEstimatorThreshold)
				{
					TickSystem<object>.RemoveTickCallback(this);
				}
			}
		}
		if (IsCoolingDown || !allowCharging)
		{
			return;
		}
		if (isPressed)
		{
			if (chargeTime < maxChargeSeconds)
			{
				chargeTime += Time.deltaTime;
				if (chargeTime >= maxChargeSeconds || chargeTime >= snapToMaxChargeAt)
				{
					chargeTime = maxChargeSeconds;
					onMaxCharge?.Invoke();
				}
			}
			float chargeFrac = GetChargeFrac();
			continuousChargingProperties?.ApplyAll(chargeFrac);
			whileCharging?.Invoke(chargeFrac);
			TryRunHaptics((chargeFrac >= 1f) ? maxChargeHapticsIntensity : (chargeFrac * chargeHapticsIntensity), Time.deltaTime);
			lastStep = currentStep;
			currentStep = Mathf.Clamp(Mathf.FloorToInt(chargeFrac * (float)numberOfProgressSteps), 0, numberOfProgressSteps - 1);
			if (currentStep >= 0 && currentStep != lastStep)
			{
				onMovedToNextStep?.Invoke(currentStep);
				if (currentStep == numberOfProgressSteps - 1)
				{
					onReachedLastProgressStep?.Invoke(currentStep);
				}
			}
			if (shootActivatorType == ShootActivator.VelocityEstimatorThreshold)
			{
				Vector3 linearVelocity = velocityEstimator.linearVelocity;
				float magnitude = linearVelocity.magnitude;
				float num = Vector3.Dot(linearVelocity / magnitude, GetVectorFromBodyToLaunchPosition().normalized);
				magnitude *= Mathf.Ceil(num - velocityEstimatorMinRigDotProduct);
				if (magnitude >= velocityEstimatorStartGestureSpeed)
				{
					velocityEstimatorThresholdMet = true;
				}
				else if (velocityEstimatorThresholdMet && magnitude < velocityEstimatorStopGestureSpeed)
				{
					TryShoot();
				}
			}
		}
		else if (chargeTime > 0f)
		{
			chargeTime -= Time.deltaTime * chargeDecaySpeed;
			if (chargeTime <= 0f)
			{
				chargeTime = 0f;
				TickSystem<object>.RemoveTickCallback(this);
				continuousChargingProperties?.ApplyAll(0f);
				whileCharging?.Invoke(0f);
			}
			else
			{
				float chargeFrac2 = GetChargeFrac();
				continuousChargingProperties?.ApplyAll(chargeFrac2);
				whileCharging?.Invoke(chargeFrac2);
			}
		}
	}

	private Vector3 GetVectorFromBodyToLaunchPosition()
	{
		return shootFromTransform.position - rig.bodyTransform.TransformPoint(offsetRigPosition);
	}

	private void GetShootPositionAndRotation(out Vector3 position, out Quaternion rotation)
	{
		ShootDirection shootDirection = shootDirectionType;
		if (shootDirection != ShootDirection.LaunchTransformRotation && shootDirection == ShootDirection.LineFromRigToLaunchTransform)
		{
			position = shootFromTransform.position;
			rotation = Quaternion.LookRotation(position - rig.bodyTransform.TransformPoint(offsetRigPosition));
		}
		else
		{
			shootFromTransform.GetPositionAndRotation(out position, out rotation);
		}
	}

	private void Shoot()
	{
		float chargeFrac = GetChargeFrac();
		float num = Mathf.Lerp(shootMinSpeed, shootMaxSpeed, chargeToShotSpeedCurve.Evaluate(chargeFrac));
		GameObject gameObject = ObjectPools.instance.Instantiate(projectilePrefab);
		gameObject.transform.localScale = Vector3.one * rig.scaleFactor;
		IProjectile component = gameObject.GetComponent<IProjectile>();
		if (component != null)
		{
			GetShootPositionAndRotation(out var position, out var rotation);
			Vector3 velocity = rotation * Vector3.forward * (num * rig.scaleFactor);
			component.Launch(position, rotation, velocity, chargeFrac, rig, currentStep);
			if ((int)projectileTrailPrefab != -1)
			{
				AttachTrail(projectileTrailPrefab, gameObject, position, blueTeam: false, orangeTeam: false);
			}
		}
		onShoot?.Invoke(chargeFrac);
		continuousChargingProperties.ApplyAll(0f);
		whileCharging?.Invoke(0f);
		if (isLocal)
		{
			onShootLocal?.Invoke(chargeFrac);
		}
		if (allowCharging && runChargeCancelledEventOnShoot)
		{
			onChargeCancelled?.Invoke();
		}
		TryRunHaptics(chargeFrac * shootHapticsIntensity, shootHapticsDuration);
		SetPressState(pressed: false);
		cooldownRemaining = cooldownSeconds;
		chargeTime = 0f;
		currentStep = -1;
		TickSystem<object>.AddTickCallback(this);
	}

	private bool TryShoot()
	{
		if ((!IsCoolingDown && shootingAllowed && shootActivatorType != ShootActivator.ButtonReleasedFullCharge) || (shootActivatorType == ShootActivator.ButtonReleasedFullCharge && chargeTime >= maxChargeSeconds))
		{
			Shoot();
			return true;
		}
		return false;
	}

	private void TryRunHaptics(float intensity, float duration)
	{
		if (enableHaptics && isLocal && !(intensity <= 0f))
		{
			bool flag = transferrableObject != null && transferrableObject.InLeftHand();
			GorillaTagger.Instance.StartVibration(flag, intensity, duration);
			if (hapticsBothHands)
			{
				GorillaTagger.Instance.StartVibration(!flag, intensity, duration);
			}
		}
	}

	private float GetChargeFrac()
	{
		if (!allowCharging)
		{
			return 1f;
		}
		if (!(chargeTime <= 0f))
		{
			if (!(chargeTime >= maxChargeSeconds))
			{
				return chargeRateCurve.Evaluate(chargeTime / maxChargeSeconds);
			}
			return 1f;
		}
		return 0f;
	}

	private void SetPressState(bool pressed)
	{
		isPressed = pressed;
		velocityEstimatorThresholdMet = false;
	}

	public void OnButtonPressed()
	{
		SetPressState(pressed: true);
		if (shootActivatorType == ShootActivator.ButtonPressed)
		{
			TryShoot();
		}
		else if (allowCharging || shootActivatorType == ShootActivator.VelocityEstimatorThreshold)
		{
			TickSystem<object>.AddTickCallback(this);
		}
	}

	public void OnButtonReleased()
	{
		if (shootActivatorType == ShootActivator.VelocityEstimatorThreshold && velocityEstimatorThresholdMet)
		{
			return;
		}
		ShootActivator shootActivator = shootActivatorType;
		if ((shootActivator != ShootActivator.ButtonReleased && shootActivator != ShootActivator.ButtonReleasedFullCharge) || !TryShoot())
		{
			SetPressState(pressed: false);
			if (allowCharging)
			{
				continuousChargingProperties?.ApplyAll(0f);
				whileCharging?.Invoke(0f);
				onChargeCancelled?.Invoke();
			}
		}
	}

	public void ResetShoot()
	{
		isPressed = false;
		velocityEstimatorThresholdMet = false;
		currentStep = -1;
		lastStep = -1;
		TickSystem<object>.RemoveTickCallback(this);
	}

	private void AttachTrail(int trailHash, GameObject newProjectile, Vector3 location, bool blueTeam, bool orangeTeam)
	{
		GameObject gameObject = ObjectPools.instance.Instantiate(trailHash);
		SlingshotProjectileTrail component = gameObject.GetComponent<SlingshotProjectileTrail>();
		if (component.IsNull())
		{
			ObjectPools.instance.Destroy(gameObject);
		}
		newProjectile.transform.position = location;
		component.AttachTrail(newProjectile, blueTeam, orangeTeam);
	}
}
