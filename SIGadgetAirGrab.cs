using System;
using GorillaLocomotion;
using UnityEngine;

[RequireComponent(typeof(GameGrabbable))]
[RequireComponent(typeof(GameSnappable))]
[RequireComponent(typeof(GameButtonActivatable))]
public class SIGadgetAirGrab : SIGadget
{
	private enum EState
	{
		Idle,
		StartAirGrabbing,
		PreparedToDash,
		DashUsed,
		Count
	}

	private const string preLog = "[SIGadgetAirGrab]  ";

	private const string preErr = "[SIGadgetAirGrab]  ERROR!!!  ";

	[SerializeField]
	private GameSnappable m_snappable;

	[SerializeField]
	private GameButtonActivatable m_buttonActivatable;

	[SerializeField]
	private float m_inputActivateThreshold = 0.35f;

	[SerializeField]
	private float m_inputDeactivateThreshold = 0.25f;

	[SerializeField]
	private AudioSource m_audioSource;

	[SerializeField]
	private SoundBankPlayer onGrabSound;

	[SerializeField]
	private SoundBankPlayer rechargeSound;

	[SerializeField]
	public AudioClip[] m_clips;

	[SerializeField]
	public float[] m_clipVolumes;

	[Tooltip("Yank min/max: How fast you have to be moving your hand for the yank to register and result in a dash.")]
	[SerializeField]
	private float m_yankMinSpeed = 2f;

	[Tooltip("Yank min/max: How fast you have to be moving your hand for the yank to register and result in a dash.")]
	[SerializeField]
	private float m_yankMaxSpeed = 8f;

	[Tooltip("Dash min/max speed: The fastest speed the player will move")]
	[SerializeField]
	private float m_minDashSpeed = 4f;

	private float _maxDashSpeed;

	[SerializeField]
	private float m_maxDashSpeedDefault = 7f;

	[SerializeField]
	private float m_maxDashSpeedUpgraded = 9f;

	private float _maxHoldTime;

	[SerializeField]
	private float m_maxHoldTimeDefault = 3f;

	[SerializeField]
	private float m_maxHoldTimeUpgraded = 5f;

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
	private float m_cooldownDurationDefault = 6f;

	[SerializeField]
	private float m_cooldownDurationUpgrade = 5f;

	[SerializeField]
	private int m_maxSuperchargeUses = 2;

	[SerializeField]
	private Transform m_airGrabXform;

	[SerializeField]
	private GameObject m_canActivateIndicator;

	private bool _isActivated;

	private bool _wasActivated;

	private float _airGrabTime;

	private float _airReleaseSpeed;

	private Vector3 _airReleaseVector;

	private VRRig _attachedVRRig;

	private int _lastAttachedPlayerActorNr;

	private int _attachedPlayerActorNr = int.MinValue;

	private NetPlayer _attachedNetPlayer;

	private bool _isTagged;

	private readonly object[] _launchYoyoRPCArgs = new object[5];

	private EState _state;

	private ResettableUseCounter _groundedUseCounter;

	private bool hasGravityOverride;

	private float _grabStartTime;

	private Vector3 _grabXformInitialScale;

	private Vector3 lastRequestedPlayerPos;

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
		_groundedUseCounter = new ResettableUseCounter(1, m_maxSuperchargeUses, OnRecharge);
		AudioClip[] clips = m_clips;
		foreach (AudioClip audioClip in clips)
		{
			if ((bool)audioClip)
			{
				audioClip.LoadAudioData();
			}
		}
		_grabXformInitialScale = m_airGrabXform.localScale;
	}

	private void OnRecharge(bool recharged)
	{
		if (recharged)
		{
			rechargeSound.Play();
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

	private void ClearGravityOverride()
	{
		GTPlayer.Instance.UnsetGravityOverride(this);
		hasGravityOverride = false;
	}

	private new void OnDisable()
	{
		if (hasGravityOverride)
		{
			ClearGravityOverride();
		}
		if (m_airGrabXform != null)
		{
			m_airGrabXform.gameObject.SetActive(value: false);
			m_airGrabXform.SetParent(base.transform, worldPositionStays: false);
		}
	}

	private void _HandleStartInteraction()
	{
		if (!ApplicationQuittingState.IsQuitting)
		{
			_attachedPlayerActorNr = gameEntity.AttachedPlayerActorNr;
			_attachedNetPlayer = NetworkSystem.Instance.GetPlayer(_attachedPlayerActorNr);
			if (GamePlayer.TryGetGamePlayer(_attachedPlayerActorNr, out var out_gamePlayer))
			{
				_attachedVRRig = out_gamePlayer.rig;
			}
		}
	}

	private void _HandleStopInteraction()
	{
		if (hasGravityOverride)
		{
			ClearGravityOverride();
		}
		_attachedPlayerActorNr = -1;
		_attachedNetPlayer = null;
		_attachedVRRig = null;
		m_airGrabXform.gameObject.SetActive(value: false);
		if (gameEntity.IsAuthority())
		{
			if (_state == EState.DashUsed)
			{
				SetStateAuthority(EState.DashUsed);
			}
			else
			{
				SetStateAuthority(EState.Idle);
			}
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
		if (Time.unscaledTime < _airGrabTime + m_slipperySurfacesTime)
		{
			instance.SetMaximumSlipThisFrame();
		}
		switch (_state)
		{
		case EState.Idle:
			if (_isActivated && !IsBlocked(SIExclusionType.AffectsLocalMovement))
			{
				if (_groundedUseCounter.TryUse())
				{
					UpdateUsageIndicator();
					_PlayHaptic(2f);
					SetStateAuthority(EState.StartAirGrabbing);
				}
			}
			else if (instance.IsGroundedButt || instance.IsGroundedHand)
			{
				_groundedUseCounter.Reset();
				UpdateUsageIndicator();
			}
			break;
		case EState.StartAirGrabbing:
			if (_isActivated)
			{
				_grabStartTime = Time.unscaledTime;
				_airReleaseSpeed = 0f;
				m_airGrabXform.SetParent(null, worldPositionStays: false);
				m_airGrabXform.position = GTPlayer.Instance.GetControllerTransform(_HandIndex == 0).position;
				m_airGrabXform.gameObject.SetActive(value: true);
				m_airGrabXform.transform.localScale = _grabXformInitialScale;
				GTPlayer.Instance.SetVelocity(Vector3.zero);
				lastRequestedPlayerPos = GTPlayer.Instance.transform.position;
				GTPlayer.Instance.SetGravityOverride(this, GravityOverrideFunction);
				hasGravityOverride = true;
				SetStateAuthority(EState.PreparedToDash);
			}
			else
			{
				m_airGrabXform.transform.parent = base.transform;
				m_airGrabXform.gameObject.SetActive(value: false);
			}
			break;
		case EState.PreparedToDash:
		{
			if (!_isActivated)
			{
				_DoDash();
				break;
			}
			if (Time.unscaledTime > _grabStartTime + _maxHoldTime)
			{
				_DoDash();
				break;
			}
			float num = (Time.unscaledTime - _grabStartTime) / _maxHoldTime;
			m_airGrabXform.localScale = _grabXformInitialScale * (1f - num);
			_UpdateAirGrab();
			break;
		}
		case EState.DashUsed:
			m_airGrabXform.transform.parent = base.transform;
			m_airGrabXform.gameObject.SetActive(value: false);
			ClearGravityOverride();
			SetStateAuthority(EState.Idle);
			break;
		}
	}

	private void UpdateUsageIndicator()
	{
		m_canActivateIndicator?.SetActive(_groundedUseCounter.IsReady);
	}

	private void GravityOverrideFunction(GTPlayer player)
	{
	}

	protected override void OnUpdateRemote(float dt)
	{
		base.OnUpdateRemote(dt);
		EState eState = (EState)gameEntity.GetState();
		if (eState != _state)
		{
			_SetStateShared(eState);
			if (_state == EState.PreparedToDash)
			{
				m_airGrabXform.transform.parent = base.transform;
				m_airGrabXform.transform.position = ((_HandIndex == 0) ? GetAttachedPlayerRig().leftHand : GetAttachedPlayerRig().rightHand).GetExtrapolatedControllerPosition();
				m_airGrabXform.gameObject.SetActive(value: true);
			}
		}
	}

	private static bool _CanChangeState(long newStateIndex)
	{
		if (newStateIndex >= 0)
		{
			return newStateIndex < 4;
		}
		return false;
	}

	private void SetStateAuthority(EState newState)
	{
		_SetStateShared(newState);
		gameEntity.RequestState(gameEntity.id, (long)newState);
	}

	private void _SetStateShared(EState newState)
	{
		if (newState == _state || !_CanChangeState((long)newState))
		{
			return;
		}
		EState state = _state;
		_state = newState;
		switch (_state)
		{
		case EState.Idle:
			m_airGrabXform.gameObject.SetActive(value: false);
			break;
		case EState.StartAirGrabbing:
			if (state != EState.PreparedToDash)
			{
				onGrabSound.Play();
			}
			break;
		case EState.DashUsed:
			_PlayAudio(2);
			break;
		case EState.PreparedToDash:
			break;
		}
	}

	private bool _CheckInput()
	{
		float sensitivity = (_wasActivated ? m_inputDeactivateThreshold : m_inputActivateThreshold);
		return m_buttonActivatable.CheckInput(sensitivity);
	}

	private void _UpdateAirGrab()
	{
		GTPlayer instance = GTPlayer.Instance;
		Vector3 vector = instance.transform.position - lastRequestedPlayerPos;
		m_airGrabXform.position += vector;
		Transform controllerTransform = instance.GetControllerTransform(_HandIndex == 0);
		Vector3 vector2 = m_airGrabXform.position - controllerTransform.position;
		instance.SetVelocity(Vector3.zero);
		lastRequestedPlayerPos = instance.transform.position + vector2;
		instance.RigidbodyMovePosition(lastRequestedPlayerPos);
		_ = GamePlayerLocal.instance.GetHandVelocity(_HandIndex).magnitude;
	}

	private void _DoDash()
	{
		_airGrabTime = Time.unscaledTime;
		Vector3 averagedVelocity = GTPlayer.Instance.AveragedVelocity;
		float num = _CalculateDashSpeed(averagedVelocity.magnitude);
		GTPlayer instance = GTPlayer.Instance;
		instance.SetMaximumSlipThisFrame();
		instance.SetVelocity(averagedVelocity.normalized * num);
		_PlayHaptic(2f);
		SetStateAuthority(EState.DashUsed);
	}

	private float _CalculateDashSpeed(float currentYankSpeed)
	{
		float time = Mathf.InverseLerp(m_yankMinSpeed, m_yankMaxSpeed, currentYankSpeed);
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

	private void _PlayAudio(int index)
	{
		m_audioSource.clip = m_clips[index];
		m_audioSource.volume = m_clipVolumes[index];
		m_audioSource.GTPlay();
	}

	public override void ApplyUpgradeNodes(SIUpgradeSet withUpgrades)
	{
		_maxDashSpeed = (withUpgrades.Contains(SIUpgradeType.AirControl_AirGrab_Speed) ? m_maxDashSpeedUpgraded : m_maxDashSpeedDefault);
		_maxHoldTime = (withUpgrades.Contains(SIUpgradeType.AirControl_AirGrab_HoldTime) ? m_maxHoldTimeUpgraded : m_maxHoldTimeDefault);
	}
}
