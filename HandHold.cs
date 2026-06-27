using System.Collections.Generic;
using GT_CustomMapSupportRuntime;
using GorillaExtensions;
using GorillaLocomotion.Gameplay;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class HandHold : MonoBehaviour, IGorillaGrabable
{
	private enum HandSnapMethod
	{
		None,
		SnapToCenterPoint,
		SnapToNearestEdge,
		SnapToXAxisPoint,
		SnapToYAxisPoint,
		SnapToZAxisPoint
	}

	public delegate void HandHoldPositionEvent(HandHold hh, bool lh, Vector3 pos);

	public delegate void HandHoldEvent(HandHold hh, bool lh);

	private Dictionary<Transform, Transform> attached = new Dictionary<Transform, Transform>();

	[SerializeField]
	private HandSnapMethod handSnapMethod;

	[SerializeField]
	private bool rotatePlayerWhenHeld;

	[SerializeField]
	private UnityEvent<Vector3> OnGrab;

	[SerializeField]
	private UnityEvent<HandHold> OnGrabHandHold;

	[SerializeField]
	private UnityEvent<bool> OnGrabHanded;

	[SerializeField]
	private UnityEvent OnRelease;

	[SerializeField]
	private UnityEvent<HandHold> OnReleaseHandHold;

	private bool initialized;

	private Collider myCollider;

	private Tappable myTappable;

	[Tooltip("Turning this on disables \"pregrabbing\". Use pregrabbing to allow players to catch a handhold even if they have squeezed the trigger too soon. Useful if you're anticipating jumping players needed to grab while airborne")]
	[SerializeField]
	private bool forceMomentary = true;

	private List<GorillaGrabber> currentGrabbers = new List<GorillaGrabber>();

	public static event HandHoldPositionEvent HandPositionRequestOverride;

	public static event HandHoldEvent HandPositionReleaseOverride;

	public void OnDisable()
	{
		for (int i = 0; i < currentGrabbers.Count; i++)
		{
			if (currentGrabbers[i].IsNotNull())
			{
				currentGrabbers[i].Ungrab(this);
			}
		}
	}

	private void Initialize()
	{
		if (!initialized)
		{
			myTappable = GetComponent<Tappable>();
			myCollider = GetComponent<Collider>();
			initialized = true;
		}
	}

	public virtual bool CanBeGrabbed(GorillaGrabber grabber)
	{
		return true;
	}

	void IGorillaGrabable.OnGrabbed(GorillaGrabber g, out Transform grabbedTransform, out Vector3 localGrabbedPosition)
	{
		Initialize();
		grabbedTransform = base.transform;
		Vector3 position = g.transform.position;
		localGrabbedPosition = base.transform.InverseTransformPoint(position);
		g.Player.AddHandHold(base.transform, localGrabbedPosition, g, g.IsLeftHand, rotatePlayerWhenHeld, out var grabbedVelocity);
		currentGrabbers.AddIfNew(g);
		if (handSnapMethod != HandSnapMethod.None && HandHold.HandPositionRequestOverride != null)
		{
			HandHold.HandPositionRequestOverride(this, g.IsLeftHand, CalculateOffset(position));
		}
		OnGrab?.Invoke(grabbedVelocity);
		OnGrabHandHold?.Invoke(this);
		OnGrabHanded?.Invoke(g.IsLeftHand);
		if (myTappable != null)
		{
			myTappable.OnGrab();
		}
	}

	void IGorillaGrabable.OnGrabReleased(GorillaGrabber g)
	{
		Initialize();
		g.Player.RemoveHandHold(g, g.IsLeftHand);
		currentGrabbers.Remove(g);
		if (handSnapMethod != HandSnapMethod.None && HandHold.HandPositionReleaseOverride != null)
		{
			HandHold.HandPositionReleaseOverride(this, g.IsLeftHand);
		}
		OnRelease?.Invoke();
		OnReleaseHandHold?.Invoke(this);
		if (myTappable != null)
		{
			myTappable.OnRelease();
		}
	}

	private Vector3 CalculateOffset(Vector3 position)
	{
		switch (handSnapMethod)
		{
		case HandSnapMethod.SnapToNearestEdge:
			if (myCollider == null)
			{
				myCollider = GetComponent<Collider>();
				if (myCollider is MeshCollider && !(myCollider as MeshCollider).convex)
				{
					handSnapMethod = HandSnapMethod.None;
					return Vector3.zero;
				}
			}
			return base.transform.position - myCollider.ClosestPoint(position);
		case HandSnapMethod.SnapToXAxisPoint:
			return base.transform.position - base.transform.TransformPoint(Vector3.right * base.transform.InverseTransformPoint(position).x);
		case HandSnapMethod.SnapToYAxisPoint:
			return base.transform.position - base.transform.TransformPoint(Vector3.up * base.transform.InverseTransformPoint(position).y);
		case HandSnapMethod.SnapToZAxisPoint:
			return base.transform.position - base.transform.TransformPoint(Vector3.forward * base.transform.InverseTransformPoint(position).z);
		default:
			return Vector3.zero;
		}
	}

	public bool MomentaryGrabOnly()
	{
		return forceMomentary;
	}

	public void CopyProperties(HandHoldSettings handHoldSettings)
	{
		handSnapMethod = (HandSnapMethod)handHoldSettings.handSnapMethod;
		rotatePlayerWhenHeld = handHoldSettings.rotatePlayerWhenHeld;
		forceMomentary = !handHoldSettings.allowPreGrab;
	}
}
