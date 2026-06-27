using System;
using GorillaExtensions;
using GorillaLocomotion;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(GameGrabbable))]
[RequireComponent(typeof(GameSnappable))]
[RequireComponent(typeof(GameButtonActivatable))]
public class SIGadgetWristJet : SIGadget, I_SIDisruptable, IEnergyGadget
{
	private enum State
	{
		Unactive,
		Active,
		OutOfFuel
	}

	public enum WristJetType
	{
		Basic,
		Jet,
		Propellor
	}

	private const string preLog = "[SIGadgetWristJet]  ";

	private const string preErr = "[SIGadgetWristJet]  ERROR!!!  ";

	private const string preErrBeta = "[SIGadgetWristJet]  ERROR!!!  (beta only log)  ";

	[SerializeField]
	private AudioSource m_thrustLoopAudioSource;

	private bool _hasThrustLoopAudioSource;

	[SerializeField]
	private SIUpgradeBasedGeneric<AudioClip> m_thrustLoopSoundByUpgrade;

	[SerializeField]
	private float m_thrustLoopAudioFadeInTime = 0.1f;

	[SerializeField]
	private float m_thrustLoopAudioFadeOutTime = 0.5f;

	[SerializeField]
	private float m_thrustLoopSoundVolume = 0.33f;

	[SerializeField]
	private AudioClip m_warnFuelLowSound;

	[SerializeField]
	private float m_warnFuelLowThreshold = 0.5f;

	[SerializeField]
	private float m_warnFuelLowSoundVolume = 0.05f;

	private bool _warnFuelLowSoundWasPlayed;

	[Tooltip("This renderer's material will have the `_EmissionDissolveProgress` property changed to visually communicate current fuel amount.")]
	[SerializeField]
	private GTRendererMatSlot[] m_gaugeMatSlots;

	public WristJetType jetType;

	public GameButtonActivatable buttonActivatable;

	public GameObject inactiveStateVisual;

	private bool _hasInactiveStateVisual;

	[FormerlySerializedAs("jetFlame")]
	public GameObject activeStateVisual;

	private bool _hasActiveStateVisual;

	public float jetForce;

	public float fuelGainRate;

	public float fuelSpendRate;

	public float emptiedCooldown;

	public float gravityNegationPercent;

	public float maxVerticalSpeed;

	public float maxHorizontalSpeed;

	[SerializeField]
	private bool rechargeRequiresFloorTouch;

	[SerializeField]
	private float throttleChangeSpeed = 2f;

	[SerializeField]
	[Tooltip("Minimum proportion of thrust allowed with throttle control.")]
	[Range(0f, 1f)]
	private float minimumBurnRate = 0.33f;

	[SerializeField]
	private Transform[] m_throttleFlapXforms;

	private Quaternion[] throttleFlapInitialRots;

	[SerializeField]
	private Quaternion m_throttleFlapMaxRotOffset = Quaternion.Euler(45f, 0f, 0f);

	private float fuelSize;

	private float currentFuel;

	private State state;

	private GTPlayer gtPlayer;

	private float emptiedCooldownResetProgress;

	private bool _floorTouched;

	private float _maxSqrHorizontalSpeed;

	private const float kFUEL_CAPACITY = 10f;

	private MaterialPropertyBlock _gaugeMatPropBlock;

	private bool _throttleControl;

	private float _throttle;

	private float _currentBurnRate;

	private float _baseFuelSpendRate;

	private float _baseJetForce;

	private float _baseMaxVerticalSpeed;

	private float _baseMaxHorizontalSpeed;

	private bool CanRecharge
	{
		get
		{
			if (!rechargeRequiresFloorTouch || _floorTouched)
			{
				return state == State.Unactive;
			}
			return false;
		}
	}

	public bool UsesEnergy => true;

	public bool IsFull => currentFuel >= fuelSize;

	private void Awake()
	{
		_maxSqrHorizontalSpeed = maxHorizontalSpeed * maxHorizontalSpeed;
		_hasThrustLoopAudioSource = m_thrustLoopAudioSource != null;
		m_warnFuelLowThreshold = ((m_warnFuelLowSound != null) ? m_warnFuelLowThreshold : (-1f));
		_hasInactiveStateVisual = inactiveStateVisual != null;
		_hasActiveStateVisual = activeStateVisual != null;
		_gaugeMatPropBlock = new MaterialPropertyBlock();
		_baseFuelSpendRate = fuelSpendRate;
		_baseJetForce = jetForce;
		_baseMaxVerticalSpeed = maxVerticalSpeed;
		_baseMaxHorizontalSpeed = maxHorizontalSpeed;
		if (m_gaugeMatSlots == null)
		{
			m_gaugeMatSlots = Array.Empty<GTRendererMatSlot>();
		}
		int num = 0;
		for (int i = 0; i < m_gaugeMatSlots.Length; i++)
		{
			if (m_gaugeMatSlots[i].TryInitialize())
			{
				m_gaugeMatSlots[num] = m_gaugeMatSlots[i];
				num++;
			}
		}
		if (num != m_gaugeMatSlots.Length)
		{
			Array.Resize(ref m_gaugeMatSlots, num);
		}
		throttleFlapInitialRots = ((m_throttleFlapXforms != null) ? new Quaternion[m_throttleFlapXforms.Length] : Array.Empty<Quaternion>());
		for (int j = 0; j < throttleFlapInitialRots.Length; j++)
		{
			if (m_throttleFlapXforms[j] == null)
			{
				throttleFlapInitialRots = Array.Empty<Quaternion>();
				Debug.LogError("[SIGadgetWristJet]  ERROR!!!  Awake: Throttle indicator flaps will not animate because entry is null in " + string.Format("array at `{0}[{1}]`. Path={2}", "m_throttleFlapXforms", j, base.transform.GetPathQ()), this);
				break;
			}
			throttleFlapInitialRots[j] = m_throttleFlapXforms[j].localRotation;
		}
	}

	private void Start()
	{
		gtPlayer = GTPlayer.Instance;
		gameEntity.OnStateChanged += OnEntityStateChanged;
		GameEntity obj = gameEntity;
		obj.OnReleased = (Action)Delegate.Combine(obj.OnReleased, new Action(HandleStopInteraction));
		GameEntity obj2 = gameEntity;
		obj2.OnUnsnapped = (Action)Delegate.Combine(obj2.OnUnsnapped, new Action(HandleStopInteraction));
	}

	protected override void OnEnable()
	{
		base.OnEnable();
		if (m_warnFuelLowThreshold > 0f)
		{
			m_warnFuelLowSound.LoadAudioData();
		}
	}

	protected override void OnDisable()
	{
		if (m_warnFuelLowThreshold > 0f && m_warnFuelLowSound.loadState != AudioDataLoadState.Unloaded)
		{
			m_warnFuelLowSound.UnloadAudioData();
		}
	}

	protected override void Update()
	{
		base.Update();
		if (_hasThrustLoopAudioSource)
		{
			float target = ((state == State.Active) ? m_thrustLoopSoundVolume : 0f);
			float num = ((state == State.Active) ? m_thrustLoopAudioFadeInTime : m_thrustLoopAudioFadeOutTime);
			m_thrustLoopAudioSource.volume = Mathf.MoveTowards(m_thrustLoopAudioSource.volume, target, 1f / num * Time.unscaledDeltaTime);
		}
	}

	private void FixedUpdate()
	{
		if ((IsEquippedLocal() || activatedLocally) && state == State.Active && currentFuel > 0f && buttonActivatable.CheckInput() && !IsBlocked(SIExclusionType.AffectsLocalMovement))
		{
			gtPlayer.AddForce(-Physics.gravity * (gtPlayer.scale * gravityNegationPercent), ForceMode.Acceleration);
			_ApplyClampedThrust();
		}
	}

	private void HandleStopInteraction()
	{
		if (gameEntity.IsAuthority())
		{
			SetStateAuthority(State.Unactive);
		}
	}

	protected override void OnUpdateAuthority(float dt)
	{
		base.OnUpdateAuthority(dt);
		bool flag = buttonActivatable.CheckInput();
		if (!_floorTouched)
		{
			_floorTouched = gtPlayer.IsGroundedButt || gtPlayer.IsGroundedHand;
		}
		if (_throttleControl)
		{
			Vector2 joystickInput = GetJoystickInput();
			if (Mathf.Abs(joystickInput.y) > 0.75f && Mathf.Abs(joystickInput.x) < 0.5f)
			{
				_throttle = Mathf.Clamp01(_throttle + joystickInput.y * throttleChangeSpeed * Time.deltaTime);
				_currentBurnRate = Mathf.Lerp(minimumBurnRate, 1f, _throttle);
				UpdateThrottleIndicator();
			}
		}
		switch (state)
		{
		case State.Unactive:
			if (flag && !IsBlocked(SIExclusionType.AffectsLocalMovement))
			{
				SetStateAuthority(State.Active);
			}
			break;
		case State.Active:
			currentFuel = Mathf.Clamp(currentFuel - dt * fuelSpendRate * _currentBurnRate, 0f, fuelSize);
			_floorTouched = false;
			gtPlayer.ThrusterActiveAtFrame = Time.frameCount;
			if (flag && m_warnFuelLowThreshold > 0f)
			{
				float num = currentFuel / fuelSize;
				if (_warnFuelLowSoundWasPlayed && num > m_warnFuelLowThreshold)
				{
					_warnFuelLowSoundWasPlayed = false;
				}
				else if (!_warnFuelLowSoundWasPlayed && num <= m_warnFuelLowThreshold)
				{
					_warnFuelLowSoundWasPlayed = true;
					gameEntity.audioSource.GTPlayOneShot(m_warnFuelLowSound, m_warnFuelLowSoundVolume);
				}
			}
			if (!flag || currentFuel <= 0f)
			{
				SetStateAuthority(State.OutOfFuel);
			}
			break;
		case State.OutOfFuel:
			if (!flag)
			{
				emptiedCooldownResetProgress += dt;
			}
			else if (currentFuel > 0f)
			{
				SetStateAuthority(State.Active);
			}
			if (emptiedCooldownResetProgress > emptiedCooldown)
			{
				emptiedCooldownResetProgress = 0f;
				SetStateAuthority(State.Unactive);
			}
			break;
		}
		float value = currentFuel / fuelSize;
		for (int i = 0; i < m_gaugeMatSlots.Length; i++)
		{
			_gaugeMatPropBlock.SetFloat(ShaderProps._EmissionDissolveProgress, value);
			m_gaugeMatSlots[i].renderer.SetPropertyBlock(_gaugeMatPropBlock, m_gaugeMatSlots[i].slot);
		}
	}

	private void UpdateThrottleIndicator()
	{
		for (int i = 0; i < throttleFlapInitialRots.Length; i++)
		{
			Quaternion b = throttleFlapInitialRots[i] * m_throttleFlapMaxRotOffset;
			m_throttleFlapXforms[i].localRotation = Quaternion.Lerp(throttleFlapInitialRots[i], b, _throttle);
		}
	}

	private void _ApplyClampedThrust()
	{
		Vector3 rigidbodyVelocity = gtPlayer.RigidbodyVelocity;
		float num = jetForce * _currentBurnRate;
		Vector3 vector = rigidbodyVelocity + base.transform.forward * (num * Time.fixedDeltaTime);
		Vector3 vector2 = new Vector3(vector.x, 0f, vector.z);
		if (vector2.sqrMagnitude > _maxSqrHorizontalSpeed)
		{
			float magnitude = new Vector3(rigidbodyVelocity.x, 0f, rigidbodyVelocity.z).magnitude;
			vector2 = Vector3.ClampMagnitude(vector2, Mathf.Max(maxHorizontalSpeed, magnitude));
		}
		Vector3 vector3 = vector2;
		vector3.y = ((vector.y > maxVerticalSpeed) ? Mathf.Max(maxVerticalSpeed, rigidbodyVelocity.y) : vector.y);
		gtPlayer.AddForce(vector3 - rigidbodyVelocity, ForceMode.VelocityChange);
	}

	private void OnEntityStateChanged(long oldState, long newState)
	{
		int num = (int)oldState;
		State state = (State)newState;
		if (num != (int)state)
		{
			SetState(state);
		}
	}

	private void SetStateAuthority(State newState)
	{
		SetState(newState);
		gameEntity.RequestState(gameEntity.id, (long)newState);
	}

	private void SetState(State newState)
	{
		if (state == newState)
		{
			return;
		}
		state = newState;
		switch (state)
		{
		case State.Unactive:
			if (_hasInactiveStateVisual)
			{
				inactiveStateVisual.SetActive(value: true);
			}
			if (_hasActiveStateVisual)
			{
				activeStateVisual.SetActive(value: false);
			}
			break;
		case State.Active:
			if (_hasInactiveStateVisual)
			{
				inactiveStateVisual.SetActive(value: false);
			}
			if (_hasActiveStateVisual)
			{
				activeStateVisual.SetActive(value: true);
			}
			break;
		case State.OutOfFuel:
			if (_hasInactiveStateVisual)
			{
				inactiveStateVisual.SetActive(value: true);
			}
			if (_hasActiveStateVisual)
			{
				activeStateVisual.SetActive(value: false);
			}
			break;
		}
	}

	public override void ApplyUpgradeNodes(SIUpgradeSet withUpgrades)
	{
		_throttleControl = withUpgrades.Contains(SIUpgradeType.Thruster_Throttle_Control);
		if (_throttleControl)
		{
			UpdateThrottleIndicator();
		}
		switch (jetType)
		{
		case WristJetType.Jet:
			fuelSpendRate = _baseFuelSpendRate * (withUpgrades.Contains(SIUpgradeType.Thruster_Jet_Duration) ? 0.8f : 1f);
			jetForce = _baseJetForce * (withUpgrades.Contains(SIUpgradeType.Thruster_Jet_Accel) ? 1.2f : 1f);
			break;
		case WristJetType.Propellor:
			fuelSpendRate = _baseFuelSpendRate * (withUpgrades.Contains(SIUpgradeType.Thruster_Prop_Duration) ? 0.8f : 1f);
			maxVerticalSpeed = _baseMaxVerticalSpeed * (withUpgrades.Contains(SIUpgradeType.Thruster_Prop_Speed) ? 1.2f : 1f);
			maxHorizontalSpeed = _baseMaxHorizontalSpeed * (withUpgrades.Contains(SIUpgradeType.Thruster_Prop_Speed) ? 1.2f : 1f);
			break;
		}
		if (_hasThrustLoopAudioSource && m_thrustLoopSoundByUpgrade.TryGetActiveValue(withUpgrades, out var out_value))
		{
			m_thrustLoopAudioSource.clip = out_value;
			m_thrustLoopAudioSource.Play();
		}
	}

	public void Disrupt(float disruptTime)
	{
		emptiedCooldownResetProgress = 0f - disruptTime;
		SetState(State.OutOfFuel);
	}

	public override void OnEntityInit()
	{
		emptiedCooldownResetProgress = 0f;
		if (_hasInactiveStateVisual)
		{
			inactiveStateVisual.SetActive(value: true);
		}
		if (_hasActiveStateVisual)
		{
			activeStateVisual.SetActive(value: false);
		}
		currentFuel = (fuelSize = 10f);
		_throttle = (_currentBurnRate = 1f);
	}

	public void UpdateRecharge(float dt)
	{
		if (CanRecharge)
		{
			currentFuel = Mathf.Clamp(currentFuel + dt * fuelGainRate, 0f, fuelSize);
		}
	}
}
