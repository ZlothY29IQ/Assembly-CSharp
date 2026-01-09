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

	private bool hasActiveCallback;

	private VRRig activeCallbackOnRig;

	private bool wasTriggerPressed;

	private bool isLineBroken;

	private bool wasSlidingUngrounded;

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

	private void ClearCallback()
	{
		if (hasActiveCallback)
		{
			activeCallbackOnRig.RemoveLateUpdateCallback(this);
			activeCallbackOnRig = null;
			hasActiveCallback = false;
			SIPlayer.LocalPlayer.OnKnockback -= OnKnockback;
		}
	}

	private void OnDestroy()
	{
		ClearCallback();
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
		ClearCallback();
	}

	private void OnUnsnapped()
	{
		wasTriggerPressed = false;
		ClearCallback();
	}

	protected override void OnUpdateAuthority(float dt)
	{
		bool num = m_buttonActivatable.CheckInput();
		bool flag = GTPlayer.Instance.IsGroundedButt || GTPlayer.Instance.IsGroundedHand || GTPlayer.Instance.IsTentacleActive;
		if (coolingDownUntilNextTouchGround && flag)
		{
			coolingDownUntilNextTouchGround = false;
		}
		if (num)
		{
			if (isLineBroken)
			{
				return;
			}
			if (flag)
			{
				if (wasSlidingUngrounded)
				{
					isLineBroken = true;
					laserBeam.SetActive(value: false);
					gameEntity.RequestState(gameEntity.id, GetStateLong());
					return;
				}
			}
			else
			{
				wasSlidingUngrounded = true;
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
				SIPlayer.LocalPlayer.OnKnockback += OnKnockback;
				activeCallbackOnRig = VRRig.LocalRig;
				hasActiveCallback = true;
				activatedAtPoint = zipline.transform.position;
				ziplineDirection = zipline.transform.forward;
				if (ziplineDirection.y > 0f)
				{
					ziplineDirection = -ziplineDirection;
				}
				if (ziplineDirection.y > -0.5f)
				{
					ziplineDirection.y = 0f;
					ziplineDirection.Normalize();
					ziplineDirection.y = -0.5f;
					ziplineDirection.Normalize();
				}
				activatedAtRotation = Quaternion.LookRotation(ziplineDirection);
				wasTriggerPressed = true;
				wasSlidingUngrounded = !GTPlayer.Instance.IsGroundedButt && !GTPlayer.Instance.IsGroundedHand;
				gameEntity.RequestState(gameEntity.id, GetStateLong());
			}
			Vector3 rigidbodyVelocity = GTPlayer.Instance.RigidbodyVelocity;
			GTPlayer.Instance.LaserZiplineActiveAtFrame = Time.frameCount + 1;
			float magnitude = rigidbodyVelocity.magnitude;
			float num2 = Vector3.Dot(GTPlayer.Instance.RigidbodyVelocity, ziplineDirection);
			if (num2 < 0f)
			{
				GTPlayer.Instance.SetVelocity(ziplineDirection * num2);
			}
			else
			{
				float num3 = Mathf.Lerp(num2, magnitude, 0.5f) - speedBoost * ziplineDirection.y * Time.deltaTime;
				GTPlayer.Instance.SetVelocity(ziplineDirection * num3);
			}
			wasTriggerPressed = true;
		}
		else if (wasTriggerPressed)
		{
			laserBeam.SetActive(value: false);
			zipline.transform.localRotation = Quaternion.identity;
			isLineBroken = false;
			wasTriggerPressed = false;
			wasSlidingUngrounded = false;
			coolingDownUntilTimestamp = Time.time + cooldownDuration;
			coolingDownUntilNextTouchGround = cooldownOnUseUntilTouchGround;
			GTPlayer.Instance.SetVelocity(GTPlayer.Instance.AveragedVelocity);
			gameEntity.RequestState(gameEntity.id, GetStateLong());
		}
		else
		{
			isLineBroken = false;
		}
	}

	private long GetStateLong()
	{
		if (wasTriggerPressed && !isLineBroken)
		{
			return BitPackUtils.PackAnchoredPosRotForNetwork(activatedAtPoint, activatedAtRotation);
		}
		return 0L;
	}

	protected override void OnUpdateRemote(float dt)
	{
	}

	private void OnKnockback(Vector3 knockbackVector)
	{
		if (wasTriggerPressed)
		{
			isLineBroken = true;
			laserBeam.SetActive(value: false);
		}
	}

	private void OnEntityStateChanged(long oldState, long newState)
	{
		if (IsEquippedLocal() || activatedLocally)
		{
			return;
		}
		if (newState != 0L)
		{
			int attachedPlayerActorNumber = GetAttachedPlayerActorNumber();
			if (attachedPlayerActorNumber >= 1 && GamePlayer.TryGetGamePlayer(attachedPlayerActorNumber, out var out_gamePlayer))
			{
				BitPackUtils.UnpackAnchoredPosRotForNetwork(newState, out_gamePlayer.rig.transform.position, out var pos, out var rot);
				activatedAtPoint = pos;
				activatedAtRotation = rot;
				ziplineDirection = rot * Vector3.forward;
				laserBeam.SetActive(value: true);
				out_gamePlayer.rig.AddLateUpdateCallback(this);
				activeCallbackOnRig = out_gamePlayer.rig;
				hasActiveCallback = true;
				wasTriggerPressed = true;
				isLineBroken = false;
			}
		}
		else
		{
			wasTriggerPressed = false;
			isLineBroken = false;
			laserBeam.SetActive(value: false);
			ClearCallback();
		}
	}

	public void CallBack()
	{
		if (!wasTriggerPressed || isLineBroken)
		{
			ClearCallback();
			return;
		}
		if (IsEquippedLocal())
		{
			Vector3 point = activatedAtPoint - zipline.transform.position;
			point = GTExt.ProjectOnPlane(point, Vector3.zero, ziplineDirection);
			if (point.sqrMagnitude > 1f)
			{
				isLineBroken = true;
				laserBeam.SetActive(value: false);
				return;
			}
			GTPlayer.Instance.transform.position += point;
		}
		zipline.transform.rotation = activatedAtRotation;
		Vector3 position = activatedAtPoint + Vector3.Project(zipline.transform.position - activatedAtPoint, ziplineDirection);
		laserBeam.transform.position = position;
	}
}
