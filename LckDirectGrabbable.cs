using System;
using GorillaLocomotion.Gameplay;
using Liv.Lck.GorillaTag;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;

public class LckDirectGrabbable : MonoBehaviour, IGorillaGrabable
{
	public UnityEvent OnTabletGrabbed = new UnityEvent();

	public UnityEvent OnTabletReleased = new UnityEvent();

	[SerializeField]
	private Transform _originalTargetParent;

	public Transform target;

	[SerializeField]
	private bool _precise;

	private GorillaGrabber _grabber;

	public GorillaGrabber grabber => _grabber;

	public bool isGrabbed => _grabber != null;

	public event Action onGrabbed;

	public event Action onReleased;

	public Vector3 GetLocalGrabbedPosition(GorillaGrabber grabber)
	{
		if (grabber == null)
		{
			return Vector3.zero;
		}
		return base.transform.InverseTransformPoint(grabber.transform.position);
	}

	public bool CanBeGrabbed(GorillaGrabber grabber)
	{
		if (!(_grabber == null))
		{
			return grabber == _grabber;
		}
		return true;
	}

	public void OnGrabbed(GorillaGrabber grabber, out Transform grabbedTransform, out Vector3 localGrabbedPosition)
	{
		if (!base.isActiveAndEnabled)
		{
			_grabber = null;
			grabbedTransform = grabber.transform;
			localGrabbedPosition = Vector3.zero;
			return;
		}
		if (_grabber != null && _grabber != grabber)
		{
			ForceRelease();
		}
		if (_precise && IsSlingshotHeldInHand(out var leftHand, out var rightHand) && ((grabber.XrNode == XRNode.LeftHand && leftHand) || (grabber.XrNode == XRNode.RightHand && rightHand)))
		{
			_grabber = null;
			grabbedTransform = grabber.transform;
			localGrabbedPosition = Vector3.zero;
			return;
		}
		_grabber = grabber;
		GtColliderTriggerProcessor.CurrentGrabbedHand = grabber.XrNode;
		GtColliderTriggerProcessor.IsGrabbingTablet = true;
		grabbedTransform = base.transform;
		localGrabbedPosition = GetLocalGrabbedPosition(_grabber);
		target.SetParent(grabber.transform, worldPositionStays: true);
		this.onGrabbed?.Invoke();
		OnTabletGrabbed?.Invoke();
	}

	public void OnGrabReleased(GorillaGrabber grabber)
	{
		target.transform.SetParent(_originalTargetParent, worldPositionStays: true);
		_grabber = null;
		GtColliderTriggerProcessor.IsGrabbingTablet = false;
		this.onReleased?.Invoke();
		OnTabletReleased?.Invoke();
	}

	public void ForceGrab(GorillaGrabber grabber)
	{
		grabber.Inject(base.transform, GetLocalGrabbedPosition(grabber));
	}

	public void ForceRelease()
	{
		if (!(_grabber == null))
		{
			_grabber.Inject(null, Vector3.zero);
		}
	}

	private bool IsSlingshotHeldInHand(out bool leftHand, out bool rightHand)
	{
		VRRig rig = VRRigCache.Instance.localRig.Rig;
		if (rig == null || rig.projectileWeapon == null)
		{
			leftHand = false;
			rightHand = false;
			return false;
		}
		leftHand = rig.projectileWeapon.InLeftHand();
		rightHand = rig.projectileWeapon.InRightHand();
		return rig.projectileWeapon.InHand();
	}

	public void SetOriginalTargetParent(Transform parent)
	{
		_originalTargetParent = parent;
	}

	public bool MomentaryGrabOnly()
	{
		return true;
	}
}
