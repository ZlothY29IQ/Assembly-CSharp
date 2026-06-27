using System;
using GorillaLocomotion;
using GorillaLocomotion.Climbing;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(GameGrabbable))]
[RequireComponent(typeof(GameSnappable))]
[RequireComponent(typeof(GameButtonActivatable))]
public class SIGadgetDashYoyo : SIGadget
{
	[Serializable]
	public struct StateMaterialsInfo
	{
		public Material idle;

		public Material ready;

		public Material cooldown;
	}

	private enum EState
	{
		Idle,
		OnCooldown,
		PreparedToThrow,
		Thrown,
		PreparedToDash,
		DashUsed,
		Count
	}

	private const string preLog = "[SIGadgetDashYoyo]  ";

	private const string preErr = "[SIGadgetDashYoyo]  ERROR!!!  ";

	[SerializeField]
	private GameSnappable m_snappable;

	[SerializeField]
	private Transform m_yoyoDefaultPosXform;

	[SerializeField]
	private Transform m_yoyoTarget;

	[SerializeField]
	private Rigidbody m_yoyoTargetRB;

	[SerializeField]
	private GameButtonActivatable m_buttonActivatable;

	[SerializeField]
	private float m_inputActivateThreshold = 0.35f;

	[SerializeField]
	private float m_inputDeactivateThreshold = 0.25f;

	private StateMaterialsInfo _stateMaterials;

	[SerializeField]
	private StateMaterialsInfo m_baseStateMats;

	[SerializeField]
	private StateMaterialsInfo m_tagUpgradeStateMatsWhileTagged;

	[SerializeField]
	private StateMaterialsInfo m_tagUpgradeStateMatsWhileUntagged;

	[SerializeField]
	private MeshRenderer m_yoyoRenderer;

	[SerializeField]
	private AudioSource m_audioSource;

	[SerializeField]
	public AudioClip[] m_clips;

	[SerializeField]
	public float[] m_clipVolumes;

	private float _throwMultiplier;

	[SerializeField]
	private float m_throwMultiplierDefault = 1.5f;

	[SerializeField]
	private float m_throwMultiplierUpgrade = 2f;

	[FormerlySerializedAs("m_tether")]
	[SerializeField]
	private LineRenderer m_tetherLineRenderer;

	[SerializeField]
	private float m_minThrowSpeed = 2f;

	[SerializeField]
	private float m_waitBeforeAutoReturn = 3f;

	[SerializeField]
	private float m_postYankCooldown = 2f;

	[SerializeField]
	private float m_maxYankRecheckTime = 0.2f;

	[SerializeField]
	private float m_yankMinDistance = 0.5f;

	[SerializeField]
	private float m_yankMaxAngle = 60f;

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

	private float _maxInfluenceAngle;

	[SerializeField]
	private float m_maxInfluenceAngleDefault = 10f;

	[SerializeField]
	private float m_maxInfluenceAngleUpgrade = 15f;

	private float _cooldownDuration;

	[SerializeField]
	private float m_cooldownDurationDefault = 6f;

	[SerializeField]
	private float m_cooldownDurationUpgrade = 5f;

	private bool _hasStunUpgrade;

	private bool _hasTagUpgrade;

	private bool _isActivated;

	private bool _wasActivated;

	private float _timeLastThrown;

	private float _successfulYankTime;

	private float _maxEncounteredYankSpeed;

	private Vector3 _yankBeginPos;

	private bool _isRecheckingYank;

	private VRRig _attachedVRRig;

	private int _lastAttachedPlayerActorNr;

	private int _attachedPlayerActorNr = int.MinValue;

	private NetPlayer _attachedNetPlayer;

	private bool _isTagged;

	private readonly object[] _launchYoyoRPCArgs = new object[5];

	private EState _state;

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
		_stateMaterials = m_baseStateMats;
		GameEntity obj = gameEntity;
		obj.OnGrabbed = (Action)Delegate.Combine(obj.OnGrabbed, new Action(_HandleStartInteraction));
		GameEntity obj2 = gameEntity;
		obj2.OnSnapped = (Action)Delegate.Combine(obj2.OnSnapped, new Action(_HandleStartInteraction));
		GameEntity obj3 = gameEntity;
		obj3.OnReleased = (Action)Delegate.Combine(obj3.OnReleased, new Action(_HandleStopInteraction));
		GameEntity obj4 = gameEntity;
		obj4.OnUnsnapped = (Action)Delegate.Combine(obj4.OnUnsnapped, new Action(_HandleStopInteraction));
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
			if (_attachedVRRig != null)
			{
				VRRig attachedVRRig = _attachedVRRig;
				attachedVRRig.OnMaterialIndexChanged = (Action<int, int>)Delegate.Remove(attachedVRRig.OnMaterialIndexChanged, new Action<int, int>(_HandleVRRigMaterialIndexChanged));
			}
			_ResetYoYo();
		}
	}

	private void LateUpdate()
	{
		if (!ApplicationQuittingState.IsQuitting)
		{
			EState state = _state;
			if ((uint)(state - 3) <= 2u)
			{
				m_tetherLineRenderer.SetPosition(1, m_tetherLineRenderer.transform.InverseTransformPoint(m_yoyoTarget.position));
			}
		}
	}

	private void _HandleStartInteraction()
	{
		if (ApplicationQuittingState.IsQuitting)
		{
			return;
		}
		_attachedPlayerActorNr = gameEntity.AttachedPlayerActorNr;
		_attachedNetPlayer = NetworkSystem.Instance.GetPlayer(_attachedPlayerActorNr);
		if (GamePlayer.TryGetGamePlayer(_attachedPlayerActorNr, out var out_gamePlayer))
		{
			if (_attachedVRRig != null)
			{
				VRRig attachedVRRig = _attachedVRRig;
				attachedVRRig.OnMaterialIndexChanged = (Action<int, int>)Delegate.Remove(attachedVRRig.OnMaterialIndexChanged, new Action<int, int>(_HandleVRRigMaterialIndexChanged));
			}
			_attachedVRRig = out_gamePlayer.rig;
			VRRig attachedVRRig2 = _attachedVRRig;
			attachedVRRig2.OnMaterialIndexChanged = (Action<int, int>)Delegate.Combine(attachedVRRig2.OnMaterialIndexChanged, new Action<int, int>(_HandleVRRigMaterialIndexChanged));
			int num = (_isTagged ? 2 : 0);
			if (num != _attachedVRRig.setMatIndex)
			{
				_HandleVRRigMaterialIndexChanged(num, _attachedVRRig.setMatIndex);
			}
		}
	}

	private void _HandleStopInteraction()
	{
		_attachedPlayerActorNr = -1;
		_attachedNetPlayer = null;
		if (_attachedVRRig != null)
		{
			VRRig attachedVRRig = _attachedVRRig;
			attachedVRRig.OnMaterialIndexChanged = (Action<int, int>)Delegate.Remove(attachedVRRig.OnMaterialIndexChanged, new Action<int, int>(_HandleVRRigMaterialIndexChanged));
		}
		_attachedVRRig = null;
		if (_isTagged)
		{
			_HandleVRRigMaterialIndexChanged(2, 0);
		}
		if (gameEntity.IsAuthority())
		{
			if (_state == EState.DashUsed || _state == EState.OnCooldown)
			{
				SetStateAuthority(EState.OnCooldown);
			}
			else
			{
				SetStateAuthority(EState.Idle);
			}
			GTPlayer.Instance.ResetRigidbodyInterpolation();
		}
	}

	private void _HandleVRRigMaterialIndexChanged(int oldMatIndex, int newMatIndex)
	{
		if (_attachedPlayerActorNr != -1 && (newMatIndex == 2 || newMatIndex == 1) && _hasTagUpgrade && GorillaGameManager.instance is SuperInfectionGame superInfectionGame)
		{
			_isTagged = _attachedNetPlayer != null && superInfectionGame.IsInfected(_attachedNetPlayer);
			_OnTagStateOrUpgradesChanged();
		}
		else
		{
			_isTagged = false;
			_OnTagStateOrUpgradesChanged();
		}
	}

	protected override void OnUpdateAuthority(float dt)
	{
		base.OnUpdateAuthority(dt);
		_wasActivated = _isActivated;
		_isActivated = _CheckInput();
		if (Time.unscaledTime < _successfulYankTime + m_slipperySurfacesTime)
		{
			GTPlayer.Instance.SetMaximumSlipThisFrame();
		}
		switch (_state)
		{
		case EState.Idle:
			if (_isActivated)
			{
				_PlayHaptic(0.1f);
				SetStateAuthority(EState.PreparedToThrow);
			}
			break;
		case EState.OnCooldown:
			if (Time.unscaledTime > _successfulYankTime + _cooldownDuration)
			{
				_PlayHaptic(0.5f);
				SetStateAuthority(EState.Idle);
			}
			break;
		case EState.PreparedToThrow:
			if (!_isActivated)
			{
				if (_ThrowYoYoTarget())
				{
					_PlayHaptic(0.5f);
					GTPlayer.Instance.RigidbodyInterpolation = RigidbodyInterpolation.None;
					SetStateAuthority(EState.Thrown);
				}
				else
				{
					SetStateAuthority(EState.Idle);
				}
			}
			break;
		case EState.Thrown:
			if (Time.unscaledTime > _timeLastThrown + m_waitBeforeAutoReturn)
			{
				_PlayHaptic(0.75f);
				SetStateAuthority(EState.Idle);
				GTPlayer.Instance.ResetRigidbodyInterpolation();
				break;
			}
			if (GTPlayer.Instance.RigidbodyInterpolation != RigidbodyInterpolation.None)
			{
				GTPlayer.Instance.RigidbodyInterpolation = RigidbodyInterpolation.None;
			}
			if (_isActivated)
			{
				SetStateAuthority(EState.PreparedToDash);
			}
			break;
		case EState.PreparedToDash:
			if (Time.unscaledTime > _timeLastThrown + m_waitBeforeAutoReturn)
			{
				_PlayHaptic(0.75f);
				SetStateAuthority(EState.Idle);
			}
			else if (!_isActivated)
			{
				SetStateAuthority(EState.Thrown);
			}
			else
			{
				_CheckYankProgression();
			}
			break;
		case EState.DashUsed:
			if (Time.unscaledTime > _successfulYankTime + m_postYankCooldown)
			{
				_PlayHaptic(0.1f);
				SetStateAuthority(EState.OnCooldown);
			}
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
			return newStateIndex < 6;
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
			switch (state)
			{
			case EState.OnCooldown:
				_PlayAudio(4);
				break;
			case EState.PreparedToThrow:
				_PlayAudio(5);
				break;
			}
			_ResetYoYo();
			_SetMaterials(_stateMaterials.idle);
			break;
		case EState.OnCooldown:
			_PlayAudio(3);
			_ResetYoYo();
			_SetMaterials(_stateMaterials.cooldown);
			break;
		case EState.PreparedToThrow:
			_PlayAudio(0);
			_SetMaterials(_stateMaterials.ready);
			break;
		case EState.Thrown:
			if (state != EState.PreparedToDash)
			{
				_PlayAudio(1);
			}
			_SetMaterials(_stateMaterials.ready);
			break;
		case EState.PreparedToDash:
			_yankBeginPos = m_yoyoDefaultPosXform.position;
			_SetMaterials(_stateMaterials.ready);
			break;
		case EState.DashUsed:
			_PlayAudio(2);
			_FreezeYoYo();
			_SetMaterials(_stateMaterials.cooldown);
			break;
		}
	}

	private bool _CheckInput()
	{
		float sensitivity = (_wasActivated ? m_inputDeactivateThreshold : m_inputActivateThreshold);
		return m_buttonActivatable.CheckInput(sensitivity);
	}

	private bool _ThrowYoYoTarget()
	{
		Vector3 handVelocity = GamePlayerLocal.instance.GetHandVelocity(_HandIndex);
		if (handVelocity.magnitude < m_minThrowSpeed)
		{
			return false;
		}
		Vector3 handAngularVelocity = GamePlayerLocal.instance.GetHandAngularVelocity(_HandIndex);
		GorillaVelocityTracker bodyVelocityTracker = GTPlayer.Instance.bodyVelocityTracker;
		handVelocity *= _throwMultiplier;
		handVelocity += bodyVelocityTracker.GetAverageVelocity(worldSpace: true, 0.05f);
		_LaunchYoYoShared(handVelocity, handAngularVelocity, m_yoyoTargetRB.transform.position, m_yoyoTargetRB.transform.rotation);
		_timeLastThrown = Time.unscaledTime;
		if (!NetworkSystem.Instance.InRoom)
		{
			return true;
		}
		SuperInfectionManager sIManagerForZone = SuperInfectionManager.GetSIManagerForZone(gameEntity.manager.zone);
		if (sIManagerForZone == null)
		{
			return true;
		}
		_launchYoyoRPCArgs[0] = gameEntity.GetNetId();
		_launchYoyoRPCArgs[1] = handVelocity;
		_launchYoyoRPCArgs[2] = handAngularVelocity;
		_launchYoyoRPCArgs[3] = m_yoyoTargetRB.transform.position;
		_launchYoyoRPCArgs[4] = m_yoyoTargetRB.transform.rotation;
		sIManagerForZone.CallRPC(SuperInfectionManager.ClientToClientRPC.LaunchDashYoyo, _launchYoyoRPCArgs);
		return true;
	}

	internal void RemoteThrowYoYoTarget(Vector3 velocity, Vector3 angVelocity, Vector3 targetPosition, Quaternion targetRotation)
	{
		_LaunchYoYoShared(velocity, angVelocity, targetPosition, targetRotation);
	}

	private void _LaunchYoYoShared(Vector3 velocity, Vector3 angVelocity, Vector3 targetPosition, Quaternion targetRotation)
	{
		m_yoyoTargetRB.transform.parent = null;
		float x = base.transform.lossyScale.x;
		m_yoyoTargetRB.transform.localScale = new Vector3(x, x, x);
		m_yoyoTargetRB.transform.position = targetPosition;
		m_yoyoTargetRB.transform.rotation = targetRotation;
		m_yoyoTargetRB.gameObject.SetActive(value: true);
		m_yoyoTarget.parent = m_yoyoTargetRB.transform;
		m_yoyoTargetRB.isKinematic = false;
		m_yoyoTargetRB.linearVelocity = velocity;
		m_yoyoTargetRB.angularVelocity = angVelocity;
		m_tetherLineRenderer.gameObject.SetActive(value: true);
	}

	private void _FreezeYoYo()
	{
		m_yoyoTargetRB.gameObject.SetActive(value: false);
		m_yoyoTarget.parent = null;
	}

	internal void OnHitPlayer_Authority(SuperInfectionGame siTagGameManager, NetPlayer victimNetPlayer)
	{
		bool num = siTagGameManager.IsInfected(_attachedNetPlayer);
		bool flag = siTagGameManager.IsInfected(victimNetPlayer);
		if (num != flag)
		{
			if (_hasTagUpgrade && !flag)
			{
				siTagGameManager.ReportTag(victimNetPlayer, _attachedNetPlayer);
				return;
			}
			RoomSystem.SendStatusEffectToPlayer(RoomSystem.StatusEffects.SetSlowedTime, victimNetPlayer);
			RoomSystem.SendSoundEffectOnOther(5, 0.125f, victimNetPlayer);
		}
	}

	private void _ResetYoYo()
	{
		m_tetherLineRenderer.gameObject.SetActive(value: false);
		m_yoyoTargetRB.gameObject.SetActive(value: false);
		m_yoyoTarget.SetParent(m_yoyoDefaultPosXform, worldPositionStays: false);
		m_yoyoTarget.transform.localPosition = Vector3.zero;
		m_yoyoTarget.transform.localRotation = Quaternion.identity;
		m_yoyoTargetRB.transform.localScale = Vector3.one;
		m_yoyoTargetRB.transform.SetParent(m_yoyoDefaultPosXform, worldPositionStays: false);
		m_yoyoTargetRB.transform.localPosition = Vector3.zero;
		m_yoyoTargetRB.transform.localRotation = Quaternion.identity;
	}

	private void _SetMaterials(Material mat)
	{
		m_yoyoRenderer.sharedMaterial = mat;
		m_tetherLineRenderer.sharedMaterial = mat;
	}

	private void _CheckYankProgression()
	{
		Vector3 handVelocity = GamePlayerLocal.instance.GetHandVelocity(_HandIndex);
		_maxEncounteredYankSpeed = Mathf.Max(_maxEncounteredYankSpeed, handVelocity.magnitude);
		Vector3 vector = _yankBeginPos - m_yoyoDefaultPosXform.position;
		Vector3 normalized = (-handVelocity.normalized + vector.normalized).normalized;
		Vector3 vector2 = m_yoyoTarget.position - m_yoyoDefaultPosXform.position;
		if (!(vector.magnitude < m_yankMinDistance) && !(_maxEncounteredYankSpeed < m_yankMinSpeed) && !(Vector3.Angle(vector2, normalized) > m_yankMaxAngle) && !IsBlocked(SIExclusionType.AffectsLocalMovement))
		{
			_successfulYankTime = Time.unscaledTime;
			float num = _CalculateDashSpeed(handVelocity.magnitude);
			GTPlayer instance = GTPlayer.Instance;
			instance.SetMaximumSlipThisFrame();
			instance.SetVelocity(Vector3.RotateTowards(vector2.normalized, normalized, _maxInfluenceAngle * (MathF.PI / 180f), 0f) * num);
			_PlayHaptic(2f);
			SetStateAuthority(EState.DashUsed);
		}
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

	private void _OnTagStateOrUpgradesChanged()
	{
		_stateMaterials = ((!_hasTagUpgrade) ? m_baseStateMats : (_isTagged ? m_tagUpgradeStateMatsWhileTagged : m_tagUpgradeStateMatsWhileUntagged));
		switch (_state)
		{
		case EState.Idle:
			_SetMaterials(_stateMaterials.idle);
			break;
		case EState.OnCooldown:
			_SetMaterials(_stateMaterials.cooldown);
			break;
		case EState.PreparedToThrow:
			_SetMaterials(_stateMaterials.ready);
			break;
		case EState.Thrown:
			_SetMaterials(_stateMaterials.ready);
			break;
		case EState.PreparedToDash:
			_SetMaterials(_stateMaterials.ready);
			break;
		case EState.DashUsed:
			_SetMaterials(_stateMaterials.cooldown);
			break;
		}
	}

	public override void ApplyUpgradeNodes(SIUpgradeSet withUpgrades)
	{
		_cooldownDuration = (withUpgrades.Contains(SIUpgradeType.Dash_Yoyo_Cooldown) ? m_cooldownDurationUpgrade : m_cooldownDurationDefault);
		_throwMultiplier = (withUpgrades.Contains(SIUpgradeType.Dash_Yoyo_Range) ? m_throwMultiplierUpgrade : m_throwMultiplierDefault);
		_maxDashSpeed = (withUpgrades.Contains(SIUpgradeType.Dash_Yoyo_Speed) ? m_maxDashSpeedUpgraded : m_maxDashSpeedDefault);
		_maxInfluenceAngle = (withUpgrades.Contains(SIUpgradeType.Dash_Yoyo_Dynamic) ? m_maxInfluenceAngleUpgrade : m_maxInfluenceAngleDefault);
		_hasStunUpgrade = withUpgrades.Contains(SIUpgradeType.Dash_Yoyo_Stun);
		_hasTagUpgrade = withUpgrades.Contains(SIUpgradeType.Dash_Yoyo_Tag);
		_OnTagStateOrUpgradesChanged();
	}
}
