using System;
using GorillaLocomotion;
using Photon.Pun;
using UnityEngine;

public class SIGadgetWing : SIGadget
{
	[SerializeField]
	private GameButtonActivatable m_buttonActivatable;

	[SerializeField]
	private float m_flapStrength;

	[SerializeField]
	private float m_flapDecayedStrength;

	[SerializeField]
	private float m_decayDuration;

	[SerializeField]
	private float m_liftStrength;

	[SerializeField]
	private float m_liftCap;

	[SerializeField]
	private Transform m_wingCenter;

	[SerializeField]
	private GTAnimator m_gtAnimator;

	private Vector3 _lastWingPos;

	private SIGadgetWing_EState _state;

	private void Awake()
	{
		if (m_buttonActivatable == null)
		{
			m_buttonActivatable = GetComponent<GameButtonActivatable>();
		}
		GameEntity obj = gameEntity;
		obj.OnGrabbed = (Action)Delegate.Combine(obj.OnGrabbed, new Action(OnGrabbed));
		GameEntity obj2 = gameEntity;
		obj2.OnSnapped = (Action)Delegate.Combine(obj2.OnSnapped, new Action(OnSnapped));
		GameEntity obj3 = gameEntity;
		obj3.OnReleased = (Action)Delegate.Combine(obj3.OnReleased, new Action(OnReleased));
		GameEntity obj4 = gameEntity;
		obj4.OnUnsnapped = (Action)Delegate.Combine(obj4.OnUnsnapped, new Action(OnUnsnapped));
	}

	private void OnGrabbed()
	{
		_lastWingPos = m_wingCenter.transform.position;
	}

	private void OnSnapped()
	{
		_lastWingPos = m_wingCenter.transform.position;
	}

	private void OnReleased()
	{
	}

	private void OnUnsnapped()
	{
	}

	protected override void OnUpdateAuthority(float dt)
	{
		Vector3 position = m_wingCenter.transform.position;
		SIGadgetWing_EState state = _state;
		_state = (m_buttonActivatable.CheckInput() ? SIGadgetWing_EState.TriggerPressed : SIGadgetWing_EState.Idle);
		if (state != _state)
		{
			gameEntity.RequestState(gameEntity.id, (long)_state);
			_lastWingPos = position;
		}
		if (_state == SIGadgetWing_EState.TriggerPressed)
		{
			Vector3 lhs = _lastWingPos - position;
			Vector3 up = m_wingCenter.transform.up;
			float num = Mathf.Max(Vector3.Dot(lhs, up), 0f);
			double num2 = PhotonNetwork.Time - (double)GTPlayer.Instance.LastTouchedGroundAtNetworkTime;
			float num3 = Mathf.Lerp(m_flapStrength, m_flapDecayedStrength, (float)num2 / m_decayDuration);
			if (!IsBlocked(SIExclusionType.AffectsLocalMovement))
			{
				Vector3 force = up * (num * num3);
				GTPlayer.Instance.AddForce(force, ForceMode.Impulse);
				_lastWingPos = position;
			}
		}
	}

	protected override void OnUpdateRemote(float dt)
	{
	}

	public override void OnEntityStateChange(long prevState, long newState)
	{
		if (newState != prevState && newState >= 0 && newState < 2)
		{
			m_gtAnimator.SetState(newState);
		}
	}
}
