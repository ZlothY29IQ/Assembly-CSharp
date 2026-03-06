using System;
using GorillaExtensions;
using GorillaLocomotion;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace GorillaTag.Cosmetics;

public class ThrowablePickupableCosmetic : TransferrableObject
{
	[Tooltip("Child object with the PickupableCosmetic script")]
	[SerializeField]
	private PickupableVariant pickupableVariant;

	[Tooltip("cosmetics released at a greater distance from the dock than the threshold will be placed in world instead of returning to the dock")]
	[SerializeField]
	private float returnToDockDistanceThreshold = 0.7f;

	[FormerlySerializedAs("OnReturnToDockPosition")]
	[Space]
	public UnityEvent OnReturnToDockPositionLocal;

	public UnityEvent OnReturnToDockPositionShared;

	[FormerlySerializedAs("OnGrabFromDockPosition")]
	public UnityEvent OnGrabLocal;

	private RubberDuckEvents _events;

	private TransferrableObject transferrableObject;

	private bool isLocal;

	private NetPlayer owner;

	private CallLimiter callLimiterRelease = new CallLimiter(10, 2f);

	private CallLimiter callLimiterReturn = new CallLimiter(10, 2f);

	private new void Awake()
	{
		transferrableObject = GetComponent<TransferrableObject>();
	}

	internal override void OnEnable()
	{
		base.OnEnable();
		if (_events == null)
		{
			_events = base.gameObject.GetOrAddComponent<RubberDuckEvents>();
			owner = ((transferrableObject.myOnlineRig != null) ? transferrableObject.myOnlineRig.creator : ((transferrableObject.myRig != null) ? (transferrableObject.myRig.creator ?? NetworkSystem.Instance.LocalPlayer) : null));
			if (owner != null)
			{
				_events.Init(owner);
				isLocal = owner.IsLocal;
			}
		}
		if (_events != null)
		{
			_events.Activate.reliable = true;
			_events.Deactivate.reliable = true;
			_events.Activate += new Action<int, int, object[], PhotonMessageInfoWrapped>(OnReleaseEvent);
			_events.Deactivate += new Action<int, int, object[], PhotonMessageInfoWrapped>(OnReturnToDockEvent);
		}
	}

	internal override void OnDisable()
	{
		base.OnDisable();
		if (_events != null)
		{
			_events.Activate -= new Action<int, int, object[], PhotonMessageInfoWrapped>(OnReleaseEvent);
			_events.Deactivate -= new Action<int, int, object[], PhotonMessageInfoWrapped>(OnReturnToDockEvent);
			_events.Dispose();
			_events = null;
		}
		if (pickupableVariant != null && pickupableVariant.enabled)
		{
			pickupableVariant.DelayedPickup();
		}
	}

	public override void OnGrab(InteractionPoint pointGrabbed, GameObject grabbingHand)
	{
		if (VRRigCache.Instance.localRig.Rig != ownerRig)
		{
			return;
		}
		if (pickupableVariant.enabled)
		{
			if (PhotonNetwork.InRoom && _events != null && _events.Activate != null)
			{
				_events.Activate.RaiseOthers(false);
			}
			base.transform.position = pickupableVariant.transform.position;
			base.transform.rotation = pickupableVariant.transform.rotation;
			pickupableVariant.Pickup();
			if (grabbingHand == EquipmentInteractor.instance.leftHand && currentState == PositionState.OnLeftArm)
			{
				canAutoGrabLeft = false;
				interpState = InterpolateState.None;
				currentState = PositionState.InRightHand;
			}
			else if (grabbingHand == EquipmentInteractor.instance.rightHand && currentState == PositionState.OnRightArm)
			{
				canAutoGrabRight = false;
				interpState = InterpolateState.None;
				currentState = PositionState.InLeftHand;
			}
		}
		OnGrabLocal?.Invoke();
		base.OnGrab(pointGrabbed, grabbingHand);
	}

	public override bool OnRelease(DropZone zoneReleased, GameObject releasingHand)
	{
		if (!base.OnRelease(zoneReleased, releasingHand))
		{
			return false;
		}
		if (!(VRRigCache.Instance.localRig.Rig == ownerRig))
		{
			return false;
		}
		Vector3 position = base.transform.position;
		bool isLeftHand = releasingHand == EquipmentInteractor.instance.leftHand;
		Vector3 averageVelocity = GTPlayer.Instance.GetInteractPointVelocityTracker(isLeftHand).GetAverageVelocity(worldSpace: true);
		float scale = GTPlayer.Instance.scale;
		bool flag = DistanceToDock() > returnToDockDistanceThreshold;
		if (PhotonNetwork.InRoom && _events != null)
		{
			if (flag && _events.Activate != null)
			{
				_events.Activate.RaiseOthers(true, position, averageVelocity, scale);
				OnReleaseEventLocal(position, averageVelocity, scale);
			}
			else if (!flag && _events.Deactivate != null)
			{
				_events.Deactivate.RaiseAll();
				OnReturnToDockPositionLocal?.Invoke();
			}
		}
		else if (flag)
		{
			OnReleaseEventLocal(position, averageVelocity, scale);
		}
		else
		{
			OnReturnToDockPositionLocal?.Invoke();
			OnReturnToDockPositionShared?.Invoke();
		}
		return true;
	}

	private void OnReleaseEvent(int sender, int target, object[] args, PhotonMessageInfoWrapped info)
	{
		if (sender != target || info.senderID != ownerRig.creator.ActorNumber)
		{
			return;
		}
		MonkeAgent.IncrementRPCCall(info, "OnReleaseEvent");
		if (!callLimiterRelease.CheckCallTime(Time.time))
		{
			return;
		}
		object obj = args[0];
		if (!(obj is bool))
		{
			return;
		}
		if ((bool)obj)
		{
			if (args[1] is Vector3 newVal && args[2] is Vector3 inVel && args[3] is float value)
			{
				Vector3 v = base.transform.position;
				Vector3 forward = base.transform.forward;
				v.SetValueSafe(in newVal);
				if (ownerRig.IsPositionInRange(v, 20f))
				{
					forward = ownerRig.ClampVelocityRelativeToPlayerSafe(inVel, 50f);
					float playerScale = value.ClampSafe(0.01f, 1f);
					OnReleaseEventLocal(v, forward, playerScale);
				}
			}
		}
		else
		{
			pickupableVariant.Pickup();
		}
	}

	private void OnReturnToDockEvent(int sender, int target, object[] args, PhotonMessageInfoWrapped info)
	{
		if (sender == target && info.senderID == ownerRig.creator.ActorNumber)
		{
			MonkeAgent.IncrementRPCCall(info, "OnReturnToDockEvent");
			if (callLimiterReturn.CheckCallTime(Time.time))
			{
				OnReturnToDockPositionShared?.Invoke();
			}
		}
	}

	private void OnReleaseEventLocal(Vector3 startPosition, Vector3 releaseVelocity, float playerScale)
	{
		pickupableVariant.Release(this, startPosition, releaseVelocity, playerScale);
	}

	private float DistanceToDock()
	{
		float result = 0f;
		if (currentState == PositionState.OnRightShoulder)
		{
			result = Vector3.Distance(ownerRig.myBodyDockPositions.rightBackTransform.position, base.transform.position);
		}
		else if (currentState == PositionState.OnLeftShoulder)
		{
			result = Vector3.Distance(ownerRig.myBodyDockPositions.leftBackTransform.position, base.transform.position);
		}
		else if (currentState == PositionState.OnRightArm)
		{
			result = Vector3.Distance(ownerRig.myBodyDockPositions.rightArmTransform.position, base.transform.position);
		}
		else if (currentState == PositionState.OnLeftArm)
		{
			result = Vector3.Distance(ownerRig.myBodyDockPositions.leftArmTransform.position, base.transform.position);
		}
		else if (currentState == PositionState.OnChest)
		{
			result = Vector3.Distance(ownerRig.myBodyDockPositions.chestTransform.position, base.transform.position);
		}
		return result;
	}
}
