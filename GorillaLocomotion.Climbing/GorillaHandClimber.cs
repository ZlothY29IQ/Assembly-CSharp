using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace GorillaLocomotion.Climbing;

public class GorillaHandClimber : MonoBehaviour
{
	[SerializeField]
	private GTPlayer player;

	[SerializeField]
	private EquipmentInteractor equipmentInteractor;

	private List<GorillaClimbable> potentialClimbables = new List<GorillaClimbable>();

	[Header("Non-hand input should have the component disabled")]
	public XRNode xrNode = XRNode.LeftHand;

	[NonSerialized]
	public bool isClimbing;

	[NonSerialized]
	public bool queuedToBecomeValidToGrabAgain;

	[NonSerialized]
	public GorillaClimbable dontReclimbLast;

	[NonSerialized]
	public Vector3 lastAutoReleasePos = Vector3.zero;

	public GorillaGrabber grabber;

	public Transform handRoot;

	private const float DIST_FOR_CLEAR_RELEASE = 0.35f;

	private const float DIST_FOR_GRAB = 0.15f;

	private Collider col;

	private bool canRelease = true;

	public bool isClimbingOrGrabbing
	{
		get
		{
			if (!isClimbing)
			{
				return grabber.isGrabbing;
			}
			return true;
		}
	}

	private void Awake()
	{
		col = GetComponent<Collider>();
		grabber = GetComponent<GorillaGrabber>();
	}

	public void CheckHandClimber()
	{
		for (int num = potentialClimbables.Count - 1; num >= 0; num--)
		{
			GorillaClimbable gorillaClimbable = potentialClimbables[num];
			if (gorillaClimbable == null || !gorillaClimbable.isActiveAndEnabled)
			{
				potentialClimbables.RemoveAt(num);
			}
			else if (gorillaClimbable.climbOnlyWhileSmall && !ZoneManagement.IsInZone(GTZone.monkeBlocksShared) && player.scale > 0.99f)
			{
				potentialClimbables.RemoveAt(num);
			}
		}
		bool grab = ControllerInputPoller.GetGrab(xrNode);
		bool grabRelease = ControllerInputPoller.GetGrabRelease(xrNode);
		if (!isClimbing)
		{
			if (queuedToBecomeValidToGrabAgain && Vector3.Distance(lastAutoReleasePos, handRoot.localPosition) >= 0.35f)
			{
				queuedToBecomeValidToGrabAgain = false;
			}
			if (grabRelease)
			{
				queuedToBecomeValidToGrabAgain = false;
				dontReclimbLast = null;
			}
			GorillaClimbable closestClimbable = GetClosestClimbable();
			if (!queuedToBecomeValidToGrabAgain && (bool)closestClimbable && grab && CanInitiateClimb() && closestClimbable != dontReclimbLast)
			{
				if (closestClimbable is GorillaClimbableRef gorillaClimbableRef)
				{
					player.BeginClimbing(gorillaClimbableRef.climb, this, gorillaClimbableRef);
				}
				else
				{
					player.BeginClimbing(closestClimbable, this);
				}
			}
		}
		else if (grabRelease && canRelease)
		{
			player.EndClimbing(this, startingNewClimb: false);
		}
		grabber.CheckGrabber(CanInitiateClimb() && grab);
	}

	private bool CanInitiateClimb()
	{
		if (!isClimbing && !equipmentInteractor.GetIsHolding(xrNode) && !equipmentInteractor.builderPieceInteractor.GetIsHolding(xrNode) && !equipmentInteractor.IsGrabDisabled(xrNode) && !GamePlayerLocal.IsHandHolding(xrNode))
		{
			return !player.inOverlay;
		}
		return false;
	}

	public void SetCanRelease(bool canRelease)
	{
		this.canRelease = canRelease;
	}

	public GorillaClimbable GetClosestClimbable()
	{
		if (potentialClimbables.Count == 0)
		{
			return null;
		}
		if (potentialClimbables.Count == 1)
		{
			return potentialClimbables[0];
		}
		Vector3 position = base.transform.position;
		Bounds bounds = col.bounds;
		float num = 0.15f;
		GorillaClimbable result = null;
		foreach (GorillaClimbable potentialClimbable in potentialClimbables)
		{
			float num2 = float.MaxValue;
			if ((bool)potentialClimbable.colliderCache)
			{
				if (!bounds.Intersects(potentialClimbable.colliderCache.bounds))
				{
					continue;
				}
				Vector3 b = potentialClimbable.colliderCache.ClosestPoint(position);
				num2 = Vector3.Distance(position, b);
			}
			else
			{
				num2 = Vector3.Distance(position, potentialClimbable.transform.position);
			}
			if (num2 < num)
			{
				result = potentialClimbable;
				num = num2;
			}
		}
		return result;
	}

	private void OnTriggerEnter(Collider other)
	{
		GorillaClimbableRef component2;
		if (other.TryGetComponent<GorillaClimbable>(out var component))
		{
			potentialClimbables.Add(component);
		}
		else if (other.TryGetComponent<GorillaClimbableRef>(out component2))
		{
			potentialClimbables.Add(component2);
		}
	}

	private void OnTriggerExit(Collider other)
	{
		GorillaClimbableRef component2;
		if (other.TryGetComponent<GorillaClimbable>(out var component))
		{
			potentialClimbables.Remove(component);
		}
		else if (other.TryGetComponent<GorillaClimbableRef>(out component2))
		{
			potentialClimbables.Remove(component2);
		}
	}

	public void ForceStopClimbing(bool startingNewClimb = false, bool doDontReclimb = false)
	{
		player.EndClimbing(this, startingNewClimb, doDontReclimb);
	}
}
