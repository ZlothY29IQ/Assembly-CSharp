using System;
using Drawing;
using GorillaLocomotion;
using UnityEngine;

[RequireComponent(typeof(GameGrabbable))]
[RequireComponent(typeof(GameSnappable))]
[RequireComponent(typeof(GameButtonActivatable))]
public class SIGadgetSlipMitt : SIGadget
{
	private enum EState
	{
		Idle,
		Slip,
		DashUsed,
		Count
	}

	private const string preLog = "[SIGadgetSlipMitt]  ";

	private const string preErr = "[SIGadgetSlipMitt]  ERROR!!!  ";

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

	private GTPlayer.HandState _attachedHandState;

	private int _lastAttachedPlayerActorNr;

	private int _attachedPlayerActorNr = int.MinValue;

	private bool _isTagged;

	private EState _state;

	private RaycastHit[] _raycastHitResults = new RaycastHit[1];

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
			_attachedPlayerActorNr = gameEntity.AttachedPlayerActorNr;
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
			SetStateAuthority(EState.Idle);
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
			if (_isActivated && !IsBlocked(SIExclusionType.AffectsLocalMovement))
			{
				_PlayHaptic(0.1f);
				GTPlayer.Instance.SetGravityOverride(this, _HandleGTPlayerOnUpdateGravity);
				SetStateAuthority(EState.Slip);
			}
			break;
		case EState.Slip:
			if (!_isActivated)
			{
				SetStateAuthority(EState.Idle);
				GTPlayer.Instance.UnsetGravityOverride(this);
				break;
			}
			_airReleaseSpeed = 0f;
			if (_HandIndex == 0)
			{
				GTPlayer.Instance.SetLeftMaximumSlipThisFrame();
				_attachedHandState = GTPlayer.Instance.LeftHand;
			}
			else
			{
				GTPlayer.Instance.SetRightMaximumSlipThisFrame();
				_attachedHandState = GTPlayer.Instance.RightHand;
			}
			break;
		}
	}

	private void _HandleGTPlayerOnUpdateGravity(GTPlayer gtPlayer)
	{
		Transform handFollower = _attachedHandState.handFollower;
		Ray ray = new Ray(handFollower.position, handFollower.forward);
		int value = gtPlayer.locomotionEnabledLayers.value;
		float maxDistance = 1f;
		float num = 20f;
		int num2 = Physics.RaycastNonAlloc(ray, _raycastHitResults, maxDistance, value, QueryTriggerInteraction.Ignore);
		_ = ref _raycastHitResults[0];
		Vector3 gravity = Physics.gravity;
		Vector3 vector = ray.direction * num;
		Vector3 vector2 = ((num2 > 0) ? vector : gravity);
		Draw.ingame.Arrow(ray.origin, ray.origin + ray.direction);
		gtPlayer.AddForce(vector2 * gtPlayer.scale, ForceMode.Acceleration);
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
			return newStateIndex < 3;
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
		if (newState != _state && _CanChangeState((long)newState))
		{
			_state = newState;
			if (_state != EState.Idle)
			{
				_ = 1;
			}
		}
	}

	private bool _CheckInput()
	{
		float sensitivity = (_wasActivated ? m_inputDeactivateThreshold : m_inputActivateThreshold);
		return m_buttonActivatable.CheckInput(sensitivity);
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
