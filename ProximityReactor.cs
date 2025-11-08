using System;
using UnityEngine;
using UnityEngine.Events;

public class ProximityReactor : MonoBehaviour
{
	public Transform from;

	public Transform to;

	[Space]
	public float proximityMin;

	public float proximityMax = 1f;

	[NonSerialized]
	[Space]
	private float _distance;

	[NonSerialized]
	private float _distanceLinear;

	[Space]
	public UnityEvent<float> onProximityChanged;

	public UnityEvent<float> onProximityChangedLinear;

	[Space]
	public UnityEvent<float> onBelowMinProximity;

	public UnityEvent<float> onAboveMaxProximity;

	public float proximityRange => proximityMax - proximityMin;

	public float distance => _distance;

	public float distanceLinear => _distanceLinear;

	public void SetRigFrom()
	{
		VRRig componentInParent = GetComponentInParent<VRRig>(includeInactive: true);
		if (componentInParent != null)
		{
			from = componentInParent.transform;
		}
	}

	public void SetRigTo()
	{
		VRRig componentInParent = GetComponentInParent<VRRig>(includeInactive: true);
		if (componentInParent != null)
		{
			to = componentInParent.transform;
		}
	}

	public void SetTransformFrom(Transform t)
	{
		from = t;
	}

	public void SetTransformTo(Transform t)
	{
		to = t;
	}

	private void Setup()
	{
		_distance = 0f;
		_distanceLinear = 0f;
	}

	private void OnEnable()
	{
		Setup();
	}

	private void Update()
	{
		if (!from || !to)
		{
			_distance = 0f;
			_distanceLinear = 0f;
			return;
		}
		Vector3 position = from.position;
		float magnitude = (to.position - position).magnitude;
		if (!_distance.Approx(magnitude))
		{
			onProximityChanged?.Invoke(magnitude);
		}
		_distance = magnitude;
		float num = (proximityRange.Approx0() ? 0f : MathUtils.LinearUnclamped(magnitude, proximityMin, proximityMax, 0f, 1f));
		if (!_distanceLinear.Approx(num))
		{
			onProximityChangedLinear?.Invoke(num);
		}
		_distanceLinear = num;
		if (_distanceLinear < 0f)
		{
			onBelowMinProximity?.Invoke(magnitude);
		}
		if (_distanceLinear > 1f)
		{
			onAboveMaxProximity?.Invoke(magnitude);
		}
	}
}
