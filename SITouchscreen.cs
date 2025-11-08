using System.Collections.Generic;
using UnityEngine;

public class SITouchscreen : MonoBehaviour
{
	public Transform controllingTransform;

	public float lastTouched;

	public Vector3 lastPosition;

	private Dictionary<Collider, GorillaTriggerColliderHandIndicator> fingerTouchDict = new Dictionary<Collider, GorillaTriggerColliderHandIndicator>();

	private HashSet<Collider> notFingerTouchDict = new HashSet<Collider>();

	private void OnTriggerEnter(Collider other)
	{
		OnTriggerStay(other);
	}

	private void OnTriggerStay(Collider other)
	{
		Transform indicator = GetIndicator(other);
		if (indicator != null)
		{
			controllingTransform = indicator;
			lastTouched = Time.time;
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (!(controllingTransform == null) && !(GetIndicator(other) != controllingTransform))
		{
			controllingTransform = null;
		}
	}

	private Transform GetIndicator(Collider other)
	{
		if (notFingerTouchDict.Contains(other))
		{
			return null;
		}
		if (!fingerTouchDict.TryGetValue(other, out var value))
		{
			value = other.GetComponentInParent<GorillaTriggerColliderHandIndicator>();
			if (value == null)
			{
				notFingerTouchDict.Add(other);
				return null;
			}
			fingerTouchDict.Add(other, value);
		}
		return value.transform;
	}
}
