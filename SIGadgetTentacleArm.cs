using System;
using GorillaExtensions;
using GorillaLocomotion;
using UnityEngine;

public class SIGadgetTentacleArm : SIGadget
{
	[SerializeField]
	private GameObject claw;

	[SerializeField]
	private LayerMask worldCollisionLayers;

	[SerializeField]
	private Transform marker;

	[SerializeField]
	private float maxTentacleLength;

	private bool isLeftHanded;

	private Vector3 knownSafePosition;

	private Vector3 clawHoldAdjustment;

	private Vector3 clawAnchorPosition;

	private Vector3 lastRequestedPlayerPosition;

	private Quaternion clawRotationOnGrab;

	private bool isGripBroken;

	public bool isAnchored { get; private set; }

	private void Awake()
	{
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
		isLeftHanded = gameEntity.heldByHandIndex == 0;
	}

	private void OnSnapped()
	{
		isLeftHanded = gameEntity.snappedJoint == SnapJointType.ArmL;
	}

	private void OnReleased()
	{
		ClearClawAnchor();
	}

	private void OnUnsnapped()
	{
	}

	protected override void OnUpdateAuthority(float dt)
	{
		_ = GTPlayer.Instance.headCollider.transform.position;
		Vector3 position = GTPlayer.Instance.bodyCollider.transform.position;
		Vector3 position2 = base.transform.position;
		if (!isLeftHanded)
		{
			_ = GTPlayer.Instance.RightHand;
		}
		else
		{
			_ = GTPlayer.Instance.LeftHand;
		}
		Vector3 vector = position2 - position;
		GTPlayer.HandState obj = (isLeftHanded ? GTPlayer.Instance.LeftHand : GTPlayer.Instance.RightHand);
		Transform controllerTransform = obj.controllerTransform;
		float num = (isLeftHanded ? ControllerInputPoller.instance.leftControllerIndexFloat : ControllerInputPoller.instance.rightControllerIndexFloat);
		bool flag = num >= 0.9f;
		if (isGripBroken)
		{
			if (flag)
			{
				num = 0f;
				flag = false;
			}
			else
			{
				isGripBroken = false;
			}
		}
		Vector3 vector2 = position2 + vector;
		Quaternion quaternion = controllerTransform.transform.rotation * Quaternion.Euler(90f, 0f, 0f);
		if ((knownSafePosition - vector2).IsLongerThan(3f))
		{
			knownSafePosition = position2;
		}
		float num2 = 0.15f;
		claw.transform.rotation = base.transform.rotation;
		RaycastHit hitInfo;
		bool flag2 = Physics.SphereCast(new Ray(knownSafePosition, vector2 - knownSafePosition), num2, out hitInfo, (vector2 - knownSafePosition).magnitude, worldCollisionLayers);
		if (flag2)
		{
			_ = (hitInfo.point - vector2).magnitude;
		}
		if (isAnchored)
		{
			if (flag)
			{
				Vector3 position3 = GTPlayer.Instance.transform.position;
				clawHoldAdjustment -= position3 - lastRequestedPlayerPosition;
				Vector3 vector3 = clawAnchorPosition - (vector2 + clawHoldAdjustment);
				GTPlayer.Instance.RequestTentacleMove(isLeftHanded, vector3);
				lastRequestedPlayerPosition = position3 + vector3;
				if ((clawAnchorPosition - base.transform.position).IsLongerThan(maxTentacleLength))
				{
					isGripBroken = true;
					ClearClawAnchor();
				}
				else
				{
					claw.transform.position = clawAnchorPosition;
					claw.transform.rotation = clawRotationOnGrab;
				}
				return;
			}
			ClearClawAnchor();
		}
		Vector3 vector4 = vector2;
		Quaternion quaternion2 = quaternion;
		if (flag2)
		{
			knownSafePosition += (vector2 - knownSafePosition).normalized * (hitInfo.distance - num2 * 2.01f);
			marker.transform.position = hitInfo.point;
			marker.transform.rotation = Quaternion.LookRotation(-hitInfo.normal, quaternion * Vector3.up);
			vector4 = hitInfo.point + hitInfo.normal * Mathf.Lerp(0.1f, 0.01f, num);
			quaternion2 = Quaternion.Lerp(quaternion, Quaternion.LookRotation(-hitInfo.normal, quaternion * Vector3.up), num * 0.5f + 0.5f);
		}
		else
		{
			knownSafePosition = vector2;
		}
		claw.transform.position = vector4;
		claw.transform.rotation = quaternion2;
		if (!isAnchored && flag && flag2)
		{
			SetClawAnchor(vector4, quaternion2, vector4 - vector2);
		}
	}

	private void SetClawAnchor(Vector3 clawPosition, Quaternion clawRotation, Vector3 adjustment)
	{
		isAnchored = true;
		clawHoldAdjustment = adjustment;
		clawAnchorPosition = clawPosition;
		clawRotationOnGrab = clawRotation;
		lastRequestedPlayerPosition = GTPlayer.Instance.transform.position;
		GTPlayer.Instance.SetGravityOverride(this, GravityOverrideFunction);
	}

	private void ClearClawAnchor()
	{
		isAnchored = false;
		GTPlayer.Instance.SetVelocity(GTPlayer.Instance.AveragedVelocity);
		GTPlayer.Instance.UnsetGravityOverride(this);
	}

	private void GravityOverrideFunction(GTPlayer player)
	{
	}

	protected override void OnUpdateRemote(float dt)
	{
	}

	private void OnEntityStateChanged(long oldState, long newState)
	{
	}
}
