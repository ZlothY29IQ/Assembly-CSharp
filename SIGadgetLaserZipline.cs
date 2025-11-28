using System;
using GorillaExtensions;
using GorillaLocomotion;
using UnityEngine;

public class SIGadgetLaserZipline : SIGadget, ICallBack
{
	[SerializeField]
	private GameButtonActivatable m_buttonActivatable;

	[SerializeField]
	private Transform zipline;

	[SerializeField]
	private GameObject laserBeam;

	[SerializeField]
	private float speedBoost;

	[SerializeField]
	private float cooldownDuration;

	[SerializeField]
	private bool cooldownOnUseUntilTouchGround;

	private bool wasTriggerPressed;

	private bool isLineBroken;

	private Quaternion activatedAtRotation;

	private Vector3 activatedAtPoint;

	private Vector3 ziplineDirection;

	private float coolingDownUntilTimestamp;

	private bool coolingDownUntilNextTouchGround;

	private void Awake()
	{
		m_buttonActivatable = GetComponent<GameButtonActivatable>();
		laserBeam.SetActive(value: false);
		GameEntity obj = gameEntity;
		obj.OnGrabbed = (Action)Delegate.Combine(obj.OnGrabbed, new Action(OnGrabbed));
		GameEntity obj2 = gameEntity;
		obj2.OnSnapped = (Action)Delegate.Combine(obj2.OnSnapped, new Action(OnSnapped));
		GameEntity obj3 = gameEntity;
		obj3.OnReleased = (Action)Delegate.Combine(obj3.OnReleased, new Action(OnReleased));
		GameEntity obj4 = gameEntity;
		obj4.OnUnsnapped = (Action)Delegate.Combine(obj4.OnUnsnapped, new Action(OnUnsnapped));
		gameEntity.OnStateChanged += OnEntityStateChanged;
	}

	private void OnGrabbed()
	{
	}

	private void OnSnapped()
	{
	}

	private void OnReleased()
	{
		wasTriggerPressed = false;
	}

	private void OnUnsnapped()
	{
		wasTriggerPressed = false;
	}

	protected override void OnUpdateAuthority(float dt)
	{
		bool num = m_buttonActivatable.CheckInput();
		if (coolingDownUntilNextTouchGround && (GTPlayer.Instance.IsGroundedHand || GTPlayer.Instance.IsGroundedButt))
		{
			coolingDownUntilNextTouchGround = false;
		}
		if (num)
		{
			if (isLineBroken)
			{
				return;
			}
			if (GTPlayer.Instance.IsGroundedButt || GTPlayer.Instance.IsGroundedHand)
			{
				isLineBroken = true;
				laserBeam.SetActive(value: false);
				return;
			}
			if (!wasTriggerPressed)
			{
				if (Time.time < coolingDownUntilTimestamp || coolingDownUntilNextTouchGround)
				{
					isLineBroken = true;
					laserBeam.SetActive(value: false);
					return;
				}
				laserBeam.SetActive(value: true);
				laserBeam.transform.localPosition = Vector3.zero;
				VRRig.LocalRig.AddLateUpdateCallback(this);
				activatedAtPoint = zipline.transform.position;
				ziplineDirection = zipline.transform.forward;
				if (ziplineDirection.y > 0f)
				{
					ziplineDirection = -ziplineDirection;
				}
				if (ziplineDirection.y > -0.5f)
				{
					ziplineDirection = GTPlayer.Instance.mainCamera.transform.forward.WithY(-0.5f).normalized;
				}
				activatedAtRotation = Quaternion.LookRotation(ziplineDirection);
			}
			float magnitude = GTPlayer.Instance.RigidbodyVelocity.magnitude;
			float num2 = Mathf.Lerp(Vector3.Dot(GTPlayer.Instance.RigidbodyVelocity, ziplineDirection), magnitude, 0.5f) - speedBoost * ziplineDirection.y * Time.deltaTime;
			GTPlayer.Instance.SetVelocity(ziplineDirection * num2);
			wasTriggerPressed = true;
		}
		else if (wasTriggerPressed)
		{
			laserBeam.SetActive(value: false);
			zipline.transform.localRotation = Quaternion.identity;
			isLineBroken = false;
			wasTriggerPressed = false;
			coolingDownUntilTimestamp = Time.time + cooldownDuration;
			coolingDownUntilNextTouchGround = cooldownOnUseUntilTouchGround;
			GTPlayer.Instance.SetVelocity(GTPlayer.Instance.AveragedVelocity);
		}
		else
		{
			isLineBroken = false;
		}
	}

	protected override void OnUpdateRemote(float dt)
	{
	}

	private void OnEntityStateChanged(long oldState, long newState)
	{
	}

	public void CallBack()
	{
		if (!wasTriggerPressed || isLineBroken)
		{
			VRRig.LocalRig.RemoveLateUpdateCallback(this);
			return;
		}
		Vector3 point = activatedAtPoint - zipline.transform.position;
		point = GTExt.ProjectOnPlane(point, Vector3.zero, ziplineDirection);
		if (point.sqrMagnitude > 1f)
		{
			isLineBroken = true;
			laserBeam.SetActive(value: false);
			return;
		}
		GTPlayer.Instance.transform.position += point;
		zipline.transform.rotation = activatedAtRotation;
		Vector3 position = activatedAtPoint + Vector3.Project(zipline.transform.position - activatedAtPoint, ziplineDirection);
		laserBeam.transform.position = position;
	}
}
