using System;
using UnityEngine;

public class AnnoyingFeatureDisabler : MonoBehaviour
{
	[SerializeField]
	private bool RequestDisableOnEnable = true;

	[SerializeField]
	private bool CancelRequestDisableOnDisable = true;

	public static event Action<bool> OnRequestDisable;

	private void OnEnable()
	{
		if (RequestDisableOnEnable)
		{
			RequestDisable(v: true);
		}
	}

	private void OnDisable()
	{
		if (CancelRequestDisableOnDisable)
		{
			RequestDisable(v: false);
		}
	}

	public void RequestDisable(bool v)
	{
		AnnoyingFeatureDisabler.OnRequestDisable?.Invoke(v);
	}
}
