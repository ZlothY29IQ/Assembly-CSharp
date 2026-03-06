using System;
using System.Runtime.CompilerServices;
using GorillaLocomotion;
using GorillaLocomotion.Climbing;
using GorillaTag.Cosmetics;
using Photon.Pun;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.XR;

public class RCHoverboard : RCVehicle
{
	private enum _EInputSource
	{
		None,
		StickX,
		StickForward,
		StickBack,
		Trigger,
		PrimaryFaceButton
	}

	[Serializable]
	private struct _SingleInputOption(_EInputSource source, AnimationCurve remapCurve)
	{
		public GTOption<StringEnum<_EInputSource>> source = new GTOption<StringEnum<_EInputSource>>(source);

		public GTOption<AnimationCurve> remapCurve = new GTOption<AnimationCurve>(remapCurve);

		public float Get(RCRemoteHoldable.RCInput input)
		{
			float x = source.ResolvedValue.Value switch
			{
				_EInputSource.Trigger => input.trigger, 
				_EInputSource.PrimaryFaceButton => (int)input.buttons, 
				_EInputSource.StickX => input.joystick.x, 
				_EInputSource.StickForward => math.saturate(input.joystick.y), 
				_EInputSource.StickBack => math.saturate(0f - input.joystick.y), 
				_EInputSource.None => 0f, 
				_ => 0f, 
			};
			return remapCurve.ResolvedValue.Evaluate(math.abs(x)) * math.sign(x);
		}
	}

	[SerializeField]
	private _SingleInputOption m_inputTurn = new _SingleInputOption(_EInputSource.StickX, new AnimationCurve(new Keyframe(0f, 0f, 0f, 0f, 0f, 0f), new Keyframe(0.1f, 0f, 0f, 1.25f, 0f, 0f), new Keyframe(0.9f, 1f, 1.25f, 0f, 0f, 0f), new Keyframe(1f, 1f, 0f, 0f, 0f, 0f)));

	[SerializeField]
	private _SingleInputOption m_inputThrustForward = new _SingleInputOption(_EInputSource.Trigger, AnimationCurves.EaseInCirc);

	[SerializeField]
	private _SingleInputOption m_inputThrustBack = new _SingleInputOption(_EInputSource.StickBack, new AnimationCurve(new Keyframe(0f, 0f, 0f, 0f, 0f, 0f), new Keyframe(0.9f, 0f, 0f, 9.9999f, 0.5825f, 0.3767f), new Keyframe(1f, 1f, 9.9999f, 1f, 0f, 0f)));

	[SerializeField]
	private _SingleInputOption m_inputJump = new _SingleInputOption(_EInputSource.PrimaryFaceButton, AnimationCurves.Linear);

	[Tooltip("Desired hover height above ground from this transform's position.")]
	[SerializeField]
	private float m_hoverHeight = 0.2f;

	[Tooltip("Upward force to maintain hover when below hoverHeight.")]
	[SerializeField]
	private float m_hoverForce = 200f;

	[Tooltip("Damping factor to smooth out vertical movement.")]
	[SerializeField]
	private float m_hoverDamp = 5f;

	[SerializeField]
	private LayerMask raycastLayers = -1;

	[SerializeField]
	private bool enableJumpInput = true;

	[Tooltip("Upward impulse force for jump.")]
	[SerializeField]
	private float m_jumpForce = 3.5f;

	private bool _hasJumped;

	[SerializeField]
	[HideInInspector]
	private float m_maxForwardSpeed = 6f;

	[SerializeField]
	[Tooltip("Time (seconds) to reach max forward speed from zero.")]
	private float m_forwardAccelTime = 2f;

	[SerializeField]
	[HideInInspector]
	private float m_maxTurnRate = 720f;

	[Tooltip("Time (seconds) to reach max turning rate.")]
	[SerializeField]
	private float m_turnAccelTime = 0.75f;

	[SerializeField]
	[HideInInspector]
	private float m_maxTiltAngle = 30f;

	[Tooltip("Time (seconds) to reach max tilt angle.")]
	[SerializeField]
	private float m_tiltTime = 0.1f;

	[Tooltip("Audio source for any motor or hover sound.")]
	[SerializeField]
	private AudioSource m_audioSource;

	[Tooltip("Looping motor/hover sound clip.")]
	[SerializeField]
	private AudioClip m_hoverSound;

	[Tooltip("Volume range for the hover sound (x = min, y = max).")]
	[SerializeField]
	private float2 m_hoverSoundVolumeMinMax = new float2(0.1f, 0.5f);

	[Tooltip("Time it takes for the volume to reach max value.")]
	[SerializeField]
	private float m_hoverSoundVolumeRampTime = 1f;

	private bool _hasAudioSource;

	private bool _hasHoverSound;

	private float _forwardAccel;

	private float _turnAccel;

	private float _tiltAccel;

	private float _currentTurnRate;

	private float _currentTurnAngle;

	private float _currentTiltAngle;

	private float _motorLevel;

	private float _MaxForwardSpeed
	{
		get
		{
			return m_maxForwardSpeed;
		}
		set
		{
			m_maxForwardSpeed = value;
			_forwardAccel = value / math.max(0.01f, m_forwardAccelTime);
		}
	}

	private float _MaxTurnRate
	{
		get
		{
			return m_maxTurnRate;
		}
		set
		{
			m_maxTurnRate = value;
			_turnAccel = value / math.max(1E-06f, m_turnAccelTime);
		}
	}

	private float _MaxTiltAngle
	{
		get
		{
			return m_maxTiltAngle;
		}
		set
		{
			m_maxTiltAngle = value;
			_tiltAccel = value / math.max(1E-06f, m_tiltTime);
		}
	}

	protected override void Awake()
	{
		base.Awake();
		_hasAudioSource = m_audioSource != null;
		_hasHoverSound = m_hoverSound != null;
		_MaxForwardSpeed = m_maxForwardSpeed;
		_MaxTurnRate = m_maxTurnRate;
		_MaxTiltAngle = m_maxTiltAngle;
	}

	protected override void AuthorityBeginDocked()
	{
		base.AuthorityBeginDocked();
		_currentTurnRate = 0f;
		_currentTiltAngle = 0f;
		float3 to = _ProjectOnPlane(base.transform.forward, math.up());
		_currentTurnAngle = _SignedAngle(new float3(0f, 0f, 1f), to, new float3(0f, 1f, 0f));
		_motorLevel = 0f;
		if (_hasAudioSource)
		{
			m_audioSource.Stop();
			m_audioSource.volume = 0f;
		}
		if (connectedRemote == null)
		{
			base.gameObject.SetActive(value: false);
		}
	}

	protected override void AuthorityUpdate(float dt)
	{
		base.AuthorityUpdate(dt);
		if (localState == State.Mobilized)
		{
			float x = math.length(activeInput.joystick);
			_motorLevel = math.saturate(x);
			if (hasNetworkSync)
			{
				networkSync.syncedState.dataA = (byte)(uint)(_motorLevel * 255f);
			}
		}
		else
		{
			_motorLevel = 0f;
		}
	}

	protected override void RemoteUpdate(float dt)
	{
		base.RemoteUpdate(dt);
		if (localState == State.Mobilized && hasNetworkSync)
		{
			_motorLevel = (float)(int)networkSync.syncedState.dataA / 255f;
		}
		else
		{
			_motorLevel = 0f;
		}
	}

	protected override void SharedUpdate(float dt)
	{
		base.SharedUpdate(dt);
		switch (localState)
		{
		case State.Mobilized:
			if (_hasAudioSource && _hasHoverSound)
			{
				if (localStatePrev != State.Mobilized)
				{
					m_audioSource.volume = 0f;
					m_audioSource.clip = m_hoverSound;
					m_audioSource.loop = true;
					m_audioSource.GTPlay();
				}
				else
				{
					float target = math.lerp(m_hoverSoundVolumeMinMax.x, m_hoverSoundVolumeMinMax.y, _motorLevel);
					float maxDelta = m_hoverSoundVolumeMinMax.y / m_hoverSoundVolumeRampTime * dt;
					m_audioSource.volume = _MoveTowards(m_audioSource.volume, target, maxDelta);
				}
			}
			break;
		case State.Disabled:
		case State.DockedLeft:
		case State.DockedRight:
		case State.Crashed:
			break;
		}
	}

	protected void FixedUpdate()
	{
		if (base.HasLocalAuthority && localState == State.Mobilized)
		{
			float fixedDeltaTime = Time.fixedDeltaTime;
			float num = m_inputThrustForward.Get(activeInput) - m_inputThrustBack.Get(activeInput);
			float num2 = m_inputTurn.Get(activeInput);
			float num3 = m_inputJump.Get(activeInput);
			RaycastHit hitInfo;
			bool flag = Physics.Raycast(base.transform.position, Vector3.down, out hitInfo, 10f, raycastLayers, QueryTriggerInteraction.Collide);
			bool flag2 = flag && hitInfo.distance <= m_hoverHeight + 0.1f;
			if (enableJumpInput && num3 > 0.001f && flag2 && !_hasJumped)
			{
				rb.AddForce(Vector3.up * m_jumpForce, ForceMode.Impulse);
				_hasJumped = true;
			}
			else if (num3 <= 0.001f)
			{
				_hasJumped = false;
			}
			float target = num2 * _MaxTurnRate;
			_currentTurnRate = _MoveTowards(_currentTurnRate, target, _turnAccel * fixedDeltaTime);
			_currentTurnAngle += _currentTurnRate * fixedDeltaTime;
			float target2 = math.lerp(0f - m_maxTiltAngle, m_maxTiltAngle, math.unlerp(-1f, 1f, num));
			_currentTiltAngle = _MoveTowards(_currentTiltAngle, target2, _tiltAccel * fixedDeltaTime);
			base.transform.rotation = quaternion.EulerXYZ(math.radians(new float3(_currentTiltAngle, _currentTurnAngle, 0f)));
			float3 float5 = base.transform.forward;
			float num4 = math.dot(float5, rb.linearVelocity);
			float num5 = num * m_maxForwardSpeed;
			float num6 = ((math.abs(num5) > 0.001f && ((num5 > 0f && num4 < num5) || (num5 < 0f && num4 > num5))) ? math.sign(num5) : 0f);
			rb.AddForce(float5 * _forwardAccel * num6 * rb.mass, ForceMode.Force);
			if (flag)
			{
				float num7 = math.saturate(m_hoverHeight - hitInfo.distance);
				float num8 = math.dot(rb.linearVelocity, Vector3.up);
				float num9 = num7 * m_hoverForce - num8 * m_hoverDamp;
				rb.AddForce(math.up() * num9, ForceMode.Force);
			}
		}
	}

	protected void OnCollisionEnter(Collision collision)
	{
		GameObject gameObject = collision.collider.gameObject;
		bool flag = gameObject.IsOnLayer(UnityLayer.GorillaThrowable);
		bool flag2 = gameObject.IsOnLayer(UnityLayer.GorillaHand);
		if (!(flag || flag2) || localState != State.Mobilized)
		{
			return;
		}
		Vector3 vector = Vector3.zero;
		if (flag2)
		{
			GorillaHandClimber component = gameObject.GetComponent<GorillaHandClimber>();
			if (component != null)
			{
				vector = GTPlayer.Instance.GetHandVelocityTracker(component.xrNode == XRNode.LeftHand).GetAverageVelocity(worldSpace: true);
			}
		}
		else if (collision.rigidbody != null)
		{
			vector = collision.rigidbody.linearVelocity;
		}
		if ((flag || vector.sqrMagnitude > 0.01f) && base.HasLocalAuthority)
		{
			AuthorityApplyImpact(vector, flag);
			if (networkSync != null)
			{
				networkSync.photonView.RPC("HitRCVehicleRPC", RpcTarget.Others, vector, flag);
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private float _MoveTowards(float current, float target, float maxDelta)
	{
		if (!(math.abs(target - current) <= maxDelta))
		{
			return current + math.sign(target - current) * maxDelta;
		}
		return target;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private float _SignedAngle(float3 from, float3 to, float3 axis)
	{
		float3 x = math.normalize(from);
		float3 y = math.normalize(to);
		float x2 = math.acos(math.dot(x, y));
		float num = math.sign(math.dot(math.cross(x, y), axis));
		return math.degrees(x2) * num;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private float3 _ProjectOnPlane(float3 vector, float3 planeNormal)
	{
		return vector - math.dot(vector, planeNormal) * planeNormal;
	}
}
