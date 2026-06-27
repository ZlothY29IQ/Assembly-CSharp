using System;
using UnityEngine;

namespace GorillaTag.Gravity;

public class ChangingBasicGravityZone : BasicGravityZone
{
	[Header("Change Value To Trigger Gravity Strength Change At Set Value (false to true and true to false both work, but value must change the frame you want it changed)")]
	public bool ExternalTriggerSetGravityStrength;

	public float ExternalSetGravityStrength;

	private bool lastExternalTriggerSetMatched = true;

	private bool lastValueWhenSet;

	private bool m_strengthDirty;

	private float m_targetGravityStrength;

	private float m_lerpToGravitySpeed;

	private bool m_directionDity;

	private Vector3 m_targetGravityDirection;

	private float m_lerpToDirectionSpeed;

	[SerializeField]
	private float m_changeStrengthTime;

	[SerializeField]
	private float m_changeDirectionTime;

	private ICallbackUnique m_thisCallbackUnique;

	protected override void Awake()
	{
		base.Awake();
		m_thisCallbackUnique = this;
		m_strengthDirty = false;
		m_directionDity = false;
	}

	protected override void OnDisable()
	{
		base.OnDisable();
		if (m_strengthDirty)
		{
			m_strengthDirty = false;
			gravityStrength = m_targetGravityStrength;
		}
		if (m_directionDity)
		{
			m_directionDity = false;
			m_gravityDirection = m_targetGravityDirection;
		}
	}

	public void Update()
	{
		if (lastValueWhenSet != ExternalTriggerSetGravityStrength)
		{
			if (!lastExternalTriggerSetMatched)
			{
				SetGravityStrength(ExternalSetGravityStrength);
				lastValueWhenSet = ExternalTriggerSetGravityStrength;
				lastExternalTriggerSetMatched = true;
			}
			else
			{
				ExternalTriggerSetGravityStrength = lastValueWhenSet;
				lastExternalTriggerSetMatched = false;
			}
		}
		else
		{
			lastExternalTriggerSetMatched = true;
		}
	}

	public void SetGravityStrength(float strength)
	{
		SetGravityStrength(strength, m_changeStrengthTime);
	}

	public void SetGravityDirection(Vector3 dir)
	{
		SetGravityDirection(dir, m_changeDirectionTime);
	}

	public void SetGravityStrength(float strength, float time)
	{
		m_targetGravityStrength = strength;
		if (time == 0f || !m_thisCallbackUnique.Registered)
		{
			gravityStrength = m_targetGravityStrength;
			m_strengthDirty = false;
		}
		else
		{
			m_lerpToGravitySpeed = (strength - gravityStrength) / time;
			m_strengthDirty = true;
		}
	}

	public void SetGravityDirection(Vector3 direction, float time)
	{
		m_targetGravityDirection = direction.normalized;
		if (time == 0f || !m_thisCallbackUnique.Registered)
		{
			m_gravityDirection = m_targetGravityDirection;
			m_directionDity = false;
		}
		else
		{
			float num = Vector3.Angle(m_gravityDirection, direction) * (MathF.PI / 180f);
			m_lerpToDirectionSpeed = num / time;
			m_directionDity = true;
		}
	}

	public void SetRotationIntent(bool rotate)
	{
		rotateTarget = rotate;
	}

	public override void CallBack()
	{
		if (m_strengthDirty)
		{
			gravityStrength = Mathf.MoveTowards(gravityStrength, m_targetGravityStrength, m_lerpToGravitySpeed * Time.fixedDeltaTime);
			if (Mathf.Approximately(gravityStrength, m_targetGravityStrength))
			{
				m_strengthDirty = false;
			}
		}
		if (m_directionDity)
		{
			m_gravityDirection = Vector3.RotateTowards(m_gravityDirection, m_targetGravityDirection, m_lerpToDirectionSpeed * Time.fixedDeltaTime, 0f);
			if (m_gravityDirection == m_targetGravityDirection)
			{
				m_directionDity = false;
			}
		}
		base.CallBack();
	}
}
