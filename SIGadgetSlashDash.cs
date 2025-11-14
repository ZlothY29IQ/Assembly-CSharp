using System;
using GorillaLocomotion;
using UnityEngine;

[RequireComponent(typeof(GameGrabbable))]
[RequireComponent(typeof(GameSnappable))]
[RequireComponent(typeof(GameButtonActivatable))]
public class SIGadgetSlashDash : SIGadget
{
	private enum EState
	{
		Idle,
		StartAirGrabbing,
		PreparedToDash,
		DashUsed,
		Count
	}

	private const string preLog = "[SIGadgetSlashDash]  ";

	private const string preErr = "[SIGadgetSlashDash]  ERROR!!!  ";

	[SerializeField]
	private GameSnappable m_snappable;

	[SerializeField]
	private Transform m_yoyoDefaultPosXform;

	[SerializeField]
	private GameButtonActivatable m_buttonActivatable;

	[SerializeField]
	private float m_inputActivateThreshold = 0.35f;

	[SerializeField]
	private float m_inputDeactivateThreshold = 0.25f;

	[SerializeField]
	private MeshRenderer m_yoyoRenderer;

	[SerializeField]
	private AudioSource m_audioSource;

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
	private float m_maxDashSpeedDefault = 11f;

	[SerializeField]
	private float m_maxDashSpeedUpgraded = 13f;

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
	private Transform m_airGrabXform;

	private bool _isActivated;

	private bool _wasActivated;

	private float _airGrabTime;

	private float _airReleaseSpeed;

	private Vector3 _airReleaseVector;

	private VRRig _attachedVRRig;

	private int _lastAttachedPlayerActorNr;

	private int _attachedPlayerActorNr = int.MinValue;

	private bool _isTagged;

	private EState _state;

	private int _HandIndex
	{
		get
		{
			if ((m_snappable.snappedToJoint != null && m_snappable.snappedToJoint.jointType == SnapJointType.ArmL) || gameEntity.heldByHandIndex == 0)
			{
				return 0;
			}
			if ((m_snappable.snappedToJoint != null && m_snappable.snappedToJoint.jointType == SnapJointType.ArmR) || gameEntity.heldByHandIndex == 1)
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
		AudioClip[] clips = m_clips;
		foreach (AudioClip audioClip in clips)
		{
			if ((bool)audioClip)
			{
				audioClip.LoadAudioData();
			}
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
		if (!ApplicationQuittingState.IsQuitting)
		{
			_attachedPlayerActorNr = GetAttachedPlayerActorNumber();
			if (GamePlayer.TryGetGamePlayer(_attachedPlayerActorNr, out var out_gamePlayer))
			{
				_attachedVRRig = out_gamePlayer.rig;
			}
		}
	}

	private void _HandleStopInteraction()
	{
		_attachedPlayerActorNr = -1;
		_attachedVRRig = null;
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
		if (Time.unscaledTime < _airGrabTime + m_slipperySurfacesTime)
		{
			GTPlayer.Instance.SetMaximumSlipThisFrame();
		}
		switch (_state)
		{
		case EState.Idle:
			if (_isActivated)
			{
				_PlayHaptic(0.1f);
				SetStateAuthority(EState.StartAirGrabbing);
			}
			break;
		case EState.StartAirGrabbing:
			if (_isActivated)
			{
				_airReleaseSpeed = 0f;
				SetStateAuthority(EState.PreparedToDash);
			}
			break;
		case EState.PreparedToDash:
			if (!_isActivated)
			{
				_DoDash();
			}
			else
			{
				_DoAirGrab();
			}
			break;
		case EState.DashUsed:
			SetStateAuthority(EState.Idle);
			break;
		}
	}

	protected override void OnUpdateRemote(float dt)
	{
		base.OnUpdateRemote(dt);
		EState eState = (EState)gameEntity.GetState();
		if (eState != _state)
		{
			_SetStateShared(eState);
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
		case EState.StartAirGrabbing:
			if (state != EState.PreparedToDash)
			{
				_PlayAudio(1);
			}
			break;
		case EState.DashUsed:
			_PlayAudio(2);
			break;
		case EState.Idle:
		case EState.PreparedToDash:
			break;
		}
	}

	private bool _CheckInput()
	{
		float sensitivity = (_wasActivated ? m_inputDeactivateThreshold : m_inputActivateThreshold);
		return m_buttonActivatable.CheckInput(checkHeld: true, checkSnapped: true, sensitivity);
	}

	private void _DoAirGrab()
	{
		_ = GamePlayerLocal.instance.GetHandVelocity(_HandIndex).magnitude;
	}

	private void _DoDash()
	{
		_airGrabTime = Time.unscaledTime;
		Vector3 handVelocity = GamePlayerLocal.instance.GetHandVelocity(_HandIndex);
		float num = _CalculateDashSpeed(handVelocity.magnitude);
		GTPlayer instance = GTPlayer.Instance;
		instance.SetMaximumSlipThisFrame();
		instance.SetVelocity(handVelocity.normalized * (0f - num));
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
		_maxDashSpeed = (withUpgrades.Contains(SIUpgradeType.Dash_Yoyo_Speed) ? m_maxDashSpeedUpgraded : m_maxDashSpeedDefault);
	}
}
