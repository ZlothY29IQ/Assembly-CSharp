using System;
using GorillaLocomotion;
using UnityEngine;

[RequireComponent(typeof(GameGrabbable))]
[RequireComponent(typeof(GameSnappable))]
[RequireComponent(typeof(GameButtonActivatable))]
public class SIGadgetAirJuke : SIGadget
{
	private const string preLog = "[SIGadgetAirJuke]  ";

	private const string preErr = "[SIGadgetAirJuke]  ERROR!!!  ";

	[SerializeField]
	private GameSnappable m_snappable;

	[SerializeField]
	private GameButtonActivatable m_buttonActivatable;

	[SerializeField]
	private float m_inputActivateThreshold = 0.35f;

	[SerializeField]
	private float m_inputDeactivateThreshold = 0.25f;

	[Tooltip("Hand min speed: How fast you have to be moving your hand for the dash to trigger.")]
	[SerializeField]
	private float m_handMinSpeed = 2f;

	[Tooltip("Hand move max speed: The fastest hand speed that will be registered.")]
	[SerializeField]
	private float m_handMaxSpeed = 8f;

	[Tooltip("Dash min/max speed: The fastest speed the player will move")]
	[SerializeField]
	private float m_minDashSpeed = 4f;

	private float _maxDashSpeed;

	[SerializeField]
	private float m_maxDashSpeedDefault = 5f;

	[SerializeField]
	private float m_maxDashSpeedUpgraded = 7f;

	[Tooltip("Maps yank speed to dash speed.\nX = Yank Speed (min to max)\nY = Dash Speed (min to max).")]
	[SerializeField]
	private AnimationCurve m_speedMappingCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	[SerializeField]
	private float m_slipperySurfacesTime = 0.25f;

	[SerializeField]
	private float m_maxInfluenceAngleDefault = 10f;

	[SerializeField]
	private float m_maxInfluenceAngleUpgrade = 15f;

	[SerializeField]
	private int m_maxRegularUses = 1;

	[SerializeField]
	private int m_maxSuperchargeUses = 2;

	[SerializeField]
	private ParticleSystem m_particleSystem;

	[SerializeField]
	private SoundBankPlayer singleJukeAudio;

	[SerializeField]
	private SoundBankPlayer reusableJukeAudio;

	[SerializeField]
	private SoundBankPlayer finalJukeAudio;

	[SerializeField]
	private SoundBankPlayer rechargeAudio;

	private GameObject _fxGObj;

	private Transform _fxXform;

	private ParticleSystem.MainModule _fxMain;

	private ParticleSystem.EmissionModule _fxEmission;

	private bool _isActivated;

	private bool _wasActivated;

	private float _dashStartTime;

	private Vector3 _airReleaseVector;

	private bool _isTagged;

	private SIGadgetAirJuke_EState _state;

	private ResettableUseCounter _groundedUseCounter;

	private float _playingFxUntilTimestamp;

	private Vector3 _dashStartFxPos;

	[SerializeField]
	private float m_fxMaxDistance = 3f;

	private int _HandIndex
	{
		get
		{
			if ((m_snappable.snappedToJoint != null && m_snappable.snappedToJoint.jointType == SnapJointType.HandL) || gameEntity.heldByHandIndex == 0)
			{
				return 0;
			}
			if ((m_snappable.snappedToJoint != null && m_snappable.snappedToJoint.jointType == SnapJointType.HandR) || gameEntity.heldByHandIndex == 1)
			{
				return 1;
			}
			return -1;
		}
	}

	private void Awake()
	{
		GameEntity obj = gameEntity;
		obj.OnGrabbed = (Action)Delegate.Combine(obj.OnGrabbed, new Action(_HandleStartInteraction));
		GameEntity obj2 = gameEntity;
		obj2.OnSnapped = (Action)Delegate.Combine(obj2.OnSnapped, new Action(_HandleStartInteraction));
		GameEntity obj3 = gameEntity;
		obj3.OnReleased = (Action)Delegate.Combine(obj3.OnReleased, new Action(_HandleStopInteraction));
		GameEntity obj4 = gameEntity;
		obj4.OnUnsnapped = (Action)Delegate.Combine(obj4.OnUnsnapped, new Action(_HandleStopInteraction));
		_fxGObj = m_particleSystem.gameObject;
		_fxXform = m_particleSystem.transform;
		_fxMain = m_particleSystem.main;
		_fxGObj.SetActive(value: false);
		_fxEmission = m_particleSystem.emission;
		_fxEmission.enabled = false;
		_groundedUseCounter = new ResettableUseCounter(m_maxRegularUses, m_maxSuperchargeUses, OnRecharged);
	}

	private void OnRecharged(bool recharged)
	{
		if (recharged)
		{
			rechargeAudio.Play();
		}
	}

	private void OnDestroy()
	{
		if (!ApplicationQuittingState.IsQuitting)
		{
			GameEntity obj = gameEntity;
			obj.OnGrabbed = (Action)Delegate.Remove(obj.OnGrabbed, new Action(_HandleStartInteraction));
			GameEntity obj2 = gameEntity;
			obj2.OnSnapped = (Action)Delegate.Remove(obj2.OnSnapped, new Action(_HandleStartInteraction));
			GameEntity obj3 = gameEntity;
			obj3.OnReleased = (Action)Delegate.Remove(obj3.OnReleased, new Action(_HandleStopInteraction));
			GameEntity obj4 = gameEntity;
			obj4.OnUnsnapped = (Action)Delegate.Remove(obj4.OnUnsnapped, new Action(_HandleStopInteraction));
		}
	}

	private void _HandleStartInteraction()
	{
		_ = ApplicationQuittingState.IsQuitting;
	}

	private void _HandleStopInteraction()
	{
		if (gameEntity.IsAuthority() && _state != SIGadgetAirJuke_EState.DashUsed)
		{
			_SetStateAuthority(SIGadgetAirJuke_EState.Idle);
		}
	}

	protected void FixedUpdate()
	{
		if ((!IsEquippedLocal() && !activatedLocally) || ApplicationQuittingState.IsQuitting)
		{
			return;
		}
		_wasActivated = _isActivated;
		_isActivated = _CheckInput();
		GTPlayer instance = GTPlayer.Instance;
		if (Time.unscaledTime < _dashStartTime + m_slipperySurfacesTime)
		{
			instance.SetMaximumSlipThisFrame();
		}
		switch (_state)
		{
		case SIGadgetAirJuke_EState.Idle:
			if (_isActivated)
			{
				if (_groundedUseCounter.IsReady)
				{
					_PlayHaptic(0.1f);
					_SetStateAuthority(SIGadgetAirJuke_EState.TriggerPressHold);
				}
			}
			else if ((instance.IsGroundedButt && !instance.bodyGroundIsSlippery) || _IsHandGroundedSteerable(instance))
			{
				_groundedUseCounter.Reset();
			}
			break;
		case SIGadgetAirJuke_EState.TriggerPressHold:
			if (!_isActivated)
			{
				_DoDash();
			}
			break;
		case SIGadgetAirJuke_EState.DashUsed:
			_SetStateAuthority(SIGadgetAirJuke_EState.Idle);
			break;
		}
		_OnUpdateShared();
	}

	protected override void OnUpdateRemote(float dt)
	{
		base.OnUpdateRemote(dt);
		SIGadgetAirJuke_EState sIGadgetAirJuke_EState = (SIGadgetAirJuke_EState)gameEntity.GetState();
		if (sIGadgetAirJuke_EState == SIGadgetAirJuke_EState.DashUsed && _state != SIGadgetAirJuke_EState.DashUsed)
		{
			_playingFxUntilTimestamp = Time.time + 0.75f;
			_dashStartFxPos = _fxXform.position;
			singleJukeAudio.Play();
		}
		_TrySetStateShared(sIGadgetAirJuke_EState);
		_OnUpdateShared();
	}

	private void _OnUpdateShared()
	{
		if (_state == SIGadgetAirJuke_EState.TriggerPressHold)
		{
			_fxGObj.SetActive(value: true);
			_fxEmission.enabled = true;
			_fxMain.startColor = new ParticleSystem.MinMaxGradient(Color.gray3);
			_UpdateFxRotation();
		}
		else if (Time.time <= _playingFxUntilTimestamp)
		{
			_fxGObj.SetActive(value: true);
			_fxEmission.enabled = Vector3.Distance(_fxXform.position, _dashStartFxPos) < m_fxMaxDistance;
			_fxMain.startColor = new ParticleSystem.MinMaxGradient(Color.white);
			_UpdateFxRotation();
		}
		else
		{
			_fxGObj.SetActive(value: false);
		}
	}

	private void _UpdateFxRotation()
	{
		Vector3 vector = _fxXform.rotation.eulerAngles * (MathF.PI / 180f);
		_fxMain.startRotationX = new ParticleSystem.MinMaxCurve(vector.x);
		_fxMain.startRotationY = new ParticleSystem.MinMaxCurve(vector.y);
		_fxMain.startRotationZ = new ParticleSystem.MinMaxCurve(vector.z);
	}

	private void _SetStateAuthority(SIGadgetAirJuke_EState newState)
	{
		if (_TrySetStateShared(newState))
		{
			gameEntity.RequestState(gameEntity.id, (long)newState);
		}
	}

	private bool _TrySetStateShared(SIGadgetAirJuke_EState newState)
	{
		long num = (long)newState;
		if (newState == _state || num < 0 || num >= 3)
		{
			return false;
		}
		if (newState == SIGadgetAirJuke_EState.DashUsed && _state != SIGadgetAirJuke_EState.DashUsed)
		{
			_playingFxUntilTimestamp = Time.time + 0.75f;
			_dashStartFxPos = _fxXform.position;
		}
		_state = newState;
		return true;
	}

	private bool _CheckInput()
	{
		float sensitivity = (_wasActivated ? m_inputDeactivateThreshold : m_inputActivateThreshold);
		return m_buttonActivatable.CheckInput(sensitivity);
	}

	private void _DoDash()
	{
		if (IsBlocked(SIExclusionType.AffectsLocalMovement))
		{
			_SetStateAuthority(SIGadgetAirJuke_EState.Idle);
			return;
		}
		Vector3 handVelocity = GamePlayerLocal.instance.GetHandVelocity(_HandIndex);
		if (handVelocity.magnitude < m_handMinSpeed || !_groundedUseCounter.TryUse())
		{
			_SetStateAuthority(SIGadgetAirJuke_EState.Idle);
			return;
		}
		_dashStartTime = Time.unscaledTime;
		float num = _CalculateDashSpeed(handVelocity.magnitude);
		GTPlayer instance = GTPlayer.Instance;
		instance.SetMaximumSlipThisFrame();
		Vector3 normalized = handVelocity.normalized;
		instance.SetVelocity(normalized * (0f - num));
		_PlayHaptic(2f);
		SuperInfectionManager activeSuperInfectionManager = SuperInfectionManager.activeSuperInfectionManager;
		if ((object)activeSuperInfectionManager != null && !activeSuperInfectionManager.IsSupercharged)
		{
			singleJukeAudio.Play();
		}
		else if (_groundedUseCounter.IsReady)
		{
			reusableJukeAudio.Play();
		}
		else
		{
			finalJukeAudio.Play();
		}
		_SetStateAuthority(SIGadgetAirJuke_EState.DashUsed);
	}

	private float _CalculateDashSpeed(float currentYankSpeed)
	{
		float time = Mathf.InverseLerp(m_handMinSpeed, m_handMaxSpeed, currentYankSpeed);
		float t = m_speedMappingCurve.Evaluate(time);
		return Mathf.Lerp(m_minDashSpeed, _maxDashSpeed, t);
	}

	private void _PlayHaptic(float strengthMultiplier)
	{
		if (FindAttachedHand(out var isLeft))
		{
			GorillaTagger.Instance.StartVibration(isLeft, GorillaTagger.Instance.tapHapticStrength * strengthMultiplier, GorillaTagger.Instance.tapHapticDuration);
		}
	}

	private static bool _IsHandGroundedSteerable(GTPlayer player)
	{
		ref readonly GTPlayer.HandState leftHandRef = ref player.LeftHandRef;
		ref readonly GTPlayer.HandState rightHandRef = ref player.RightHandRef;
		if ((!leftHandRef.isColliding || _IsRechargeBlocked(leftHandRef.surfaceOverride)) && (!rightHandRef.isColliding || _IsRechargeBlocked(rightHandRef.surfaceOverride)) && !player.isClimbing && !leftHandRef.isHolding)
		{
			return rightHandRef.isHolding;
		}
		return true;
	}

	private static bool _IsRechargeBlocked(GorillaSurfaceOverride surface)
	{
		if (surface != null && surface.extraVelMultiplier > 0.99f)
		{
			return surface.extraVelMultiplier < 1f;
		}
		return false;
	}

	public override void ApplyUpgradeNodes(SIUpgradeSet withUpgrades)
	{
		_maxDashSpeed = (withUpgrades.Contains(SIUpgradeType.AirControl_AirJuke_Speed) ? m_maxDashSpeedUpgraded : m_maxDashSpeedDefault);
	}
}
