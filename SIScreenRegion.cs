using System.Collections.Generic;
using UnityEngine;

public class SIScreenRegion : MonoBehaviour
{
	private HashSet<GorillaTriggerColliderHandIndicator> handIndicators = new HashSet<GorillaTriggerColliderHandIndicator>();

	private bool _hasPressedButton;

	public bool HasPressedButton => _hasPressedButton;

	private void OnTriggerEnter(Collider other)
	{
		GorillaTriggerColliderHandIndicator componentInParent = other.GetComponentInParent<GorillaTriggerColliderHandIndicator>();
		if ((object)componentInParent != null)
		{
			handIndicators.Add(componentInParent);
		}
	}

	private void OnTriggerExit(Collider other)
	{
		GorillaTriggerColliderHandIndicator componentInParent = other.GetComponentInParent<GorillaTriggerColliderHandIndicator>();
		if ((object)componentInParent != null)
		{
			handIndicators.Remove(componentInParent);
			if (handIndicators.Count == 0)
			{
				ClearPressedIndicator();
			}
		}
	}

	public void RegisterButtonPress()
	{
		if (handIndicators.Count > 0)
		{
			_hasPressedButton = true;
		}
	}

	private void ClearPressedIndicator()
	{
		_hasPressedButton = false;
	}
}
