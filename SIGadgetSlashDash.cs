using System;
using GorillaLocomotion;
using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(GameGrabbable))]
[RequireComponent(typeof(GameSnappable))]
[RequireComponent(typeof(GameButtonActivatable))]
public class SIGadgetSlashDash : SIGadget
{
	private const string preLog = "[SIGadgetSlashDash]  ";

	private const string preErr = "[SIGadgetSlashDash]  ERROR!!!  ";

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

	private float _coolDown;

	[SerializeField]
	private float m_coolDownDefault = 1f;

	[SerializeField]
	private float m_coolDownUpgraded = 0.5f;

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
	private ParticleSystem m_particleSystem;

	private GameObject _fxGObj;

	private Transform _fxXform;

	private ParticleSystem.MainModule _fxMain;

	private bool _isActivated;

	private bool _wasActivated;

	private float _dashStartTime;

	private float _dashStartNetworkTime;

	private Vector3 _airReleaseVector;

	private bool _isTagged;

	private SIGadgetSlashDash_EState _state;

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

	private void Start()
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
		if (gameEntity.IsAuthority() && _state != SIGadgetSlashDash_EState.DashUsed)
		{
			_SetStateAuthority(SIGadgetSlashDash_EState.Idle);
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
		if (Time.unscaledTime < _dashStartTime + m_slipperySurfacesTime)
		{
			GTPlayer.Instance.SetMaximumSlipThisFrame();
		}
		switch (_state)
		{
		case SIGadgetSlashDash_EState.Idle:
			if (_isActivated)
			{
				_PlayHaptic(0.1f);
				_SetStateAuthority(SIGadgetSlashDash_EState.TriggerPressHold);
			}
			break;
		case SIGadgetSlashDash_EState.TriggerPressHold:
			if (!_isActivated)
			{
				_DoDash();
			}
			break;
		case SIGadgetSlashDash_EState.DashUsed:
			if (GTPlayer.Instance.LastTouchedGroundAtNetworkTime > _dashStartNetworkTime)
			{
				_SetStateAuthority(SIGadgetSlashDash_EState.Idle);
			}
			break;
		}
		_OnUpdateShared();
	}

	protected override void OnUpdateRemote(float dt)
	{
		base.OnUpdateRemote(dt);
		SIGadgetSlashDash_EState newState = (SIGadgetSlashDash_EState)gameEntity.GetState();
		_TrySetStateShared(newState);
		_OnUpdateShared();
	}

	private void _OnUpdateShared()
	{
		switch (_state)
		{
		case SIGadgetSlashDash_EState.Idle:
			_fxGObj.SetActive(value: false);
			break;
		case SIGadgetSlashDash_EState.TriggerPressHold:
			_fxGObj.SetActive(value: true);
			_fxMain.startColor = new ParticleSystem.MinMaxGradient(Color.gray3);
			_UpdateFxRotation();
			break;
		case SIGadgetSlashDash_EState.DashUsed:
			_fxMain.startColor = new ParticleSystem.MinMaxGradient(Color.white);
			_UpdateFxRotation();
			break;
		}
	}

	private void _UpdateFxRotation()
	{
		Vector3 vector = _fxXform.rotation.eulerAngles * (MathF.PI / 180f);
		_fxMain.startRotationX = new ParticleSystem.MinMaxCurve(vector.x);
		_fxMain.startRotationY = new ParticleSystem.MinMaxCurve(vector.y);
		_fxMain.startRotationZ = new ParticleSystem.MinMaxCurve(vector.z);
	}

	private void _SetStateAuthority(SIGadgetSlashDash_EState newState)
	{
		if (_TrySetStateShared(newState))
		{
			gameEntity.RequestState(gameEntity.id, (long)newState);
		}
	}

	private bool _TrySetStateShared(SIGadgetSlashDash_EState newState)
	{
		long num = (long)newState;
		if (newState == _state || num < 0 || num >= 3)
		{
			return false;
		}
		_state = newState;
		return true;
	}

	private bool _CheckInput()
	{
		float sensitivity = (_wasActivated ? m_inputDeactivateThreshold : m_inputActivateThreshold);
		return m_buttonActivatable.CheckInput(checkHeld: true, checkSnapped: true, sensitivity);
	}

	private void _DoDash()
	{
		Vector3 handVelocity = GamePlayerLocal.instance.GetHandVelocity(_HandIndex);
		if (!(handVelocity.magnitude < m_handMinSpeed))
		{
			_dashStartTime = Time.unscaledTime;
			_dashStartNetworkTime = (float)PhotonNetwork.Time;
			float num = _CalculateDashSpeed(handVelocity.magnitude);
			GTPlayer instance = GTPlayer.Instance;
			instance.SetMaximumSlipThisFrame();
			Vector3 normalized = handVelocity.normalized;
			instance.SetVelocity(normalized * (0f - num));
			_PlayHaptic(2f);
			_SetStateAuthority(SIGadgetSlashDash_EState.DashUsed);
		}
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

	public override void ApplyUpgradeNodes(SIUpgradeSet withUpgrades)
	{
		_maxDashSpeed = (withUpgrades.Contains(SIUpgradeType.Dash_Slash_Speed) ? m_maxDashSpeedUpgraded : m_maxDashSpeedDefault);
		_coolDown = (withUpgrades.Contains(SIUpgradeType.Dash_Slash_Cooldown) ? m_coolDownUpgraded : m_coolDownDefault);
	}
}
