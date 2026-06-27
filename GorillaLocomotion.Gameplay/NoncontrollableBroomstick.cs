using Photon.Pun;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Splines;

namespace GorillaLocomotion.Gameplay;

public class NoncontrollableBroomstick : MonoBehaviour, IGorillaGrabable
{
	public SplineContainer unitySpline;

	public BezierSpline spline;

	public float duration = 30f;

	public float smoothRotationTrackingRate = 0.5f;

	public bool lookForward = true;

	[SerializeField]
	private float SplineProgressOffet;

	private float progress;

	private float smoothRotationTrackingRateExp;

	[SerializeField]
	private bool constantVelocity;

	private float progressPerFixedUpdate;

	private double secondsToCycles;

	private NativeSpline nativeSpline;

	[SerializeField]
	private bool momentaryGrabOnly = true;

	private void Start()
	{
		smoothRotationTrackingRateExp = Mathf.Exp(smoothRotationTrackingRate);
		progressPerFixedUpdate = Time.fixedDeltaTime / duration;
		progress = SplineProgressOffet;
		secondsToCycles = 1.0 / (double)duration;
		if (unitySpline != null)
		{
			nativeSpline = new NativeSpline(unitySpline.Spline, unitySpline.transform.localToWorldMatrix, Allocator.Persistent);
		}
	}

	protected virtual void FixedUpdate()
	{
		if (PhotonNetwork.InRoom)
		{
			double num = PhotonNetwork.Time * secondsToCycles + (double)SplineProgressOffet;
			progress = (float)(num % 1.0);
		}
		else
		{
			progress = (progress + progressPerFixedUpdate) % 1f;
		}
		Quaternion a = Quaternion.identity;
		if (unitySpline != null)
		{
			nativeSpline.Evaluate(progress, out var position, out var tangent, out var _);
			base.transform.position = position;
			if (lookForward)
			{
				a = Quaternion.LookRotation(new Vector3(tangent.x, tangent.y, tangent.z));
			}
		}
		else if (spline != null)
		{
			Vector3 point = spline.GetPoint(progress, constantVelocity);
			base.transform.position = point;
			if (lookForward)
			{
				a = Quaternion.LookRotation(spline.GetDirection(progress, constantVelocity));
			}
		}
		if (lookForward)
		{
			base.transform.rotation = Quaternion.Slerp(a, base.transform.rotation, Mathf.Exp((0f - smoothRotationTrackingRateExp) * Time.deltaTime));
		}
	}

	bool IGorillaGrabable.CanBeGrabbed(GorillaGrabber grabber)
	{
		return true;
	}

	void IGorillaGrabable.OnGrabbed(GorillaGrabber g, out Transform grabbedObject, out Vector3 grabbedLocalPosition)
	{
		grabbedObject = base.transform;
		grabbedLocalPosition = base.transform.InverseTransformPoint(g.transform.position);
	}

	void IGorillaGrabable.OnGrabReleased(GorillaGrabber g)
	{
	}

	private void OnDestroy()
	{
		nativeSpline.Dispose();
	}

	public bool MomentaryGrabOnly()
	{
		return momentaryGrabOnly;
	}
}
