using System;
using GorillaLocomotion;
using UnityEngine;

public class NativeSizeVolume : MonoBehaviour
{
	[Serializable]
	private enum NativeSizeVolumeAction
	{
		None,
		ApplySettings,
		ResetSize
	}

	[SerializeField]
	private Collider triggerVolume;

	[SerializeField]
	private NativeSizeChangerSettings settings;

	[SerializeField]
	private NativeSizeVolumeAction OnEnterAction;

	[SerializeField]
	private NativeSizeVolumeAction OnExitAction;

	private void OnTriggerEnter(Collider other)
	{
		GTPlayer componentInParent = other.GetComponentInParent<GTPlayer>();
		if (!(componentInParent == null))
		{
			switch (OnEnterAction)
			{
			case NativeSizeVolumeAction.ApplySettings:
				settings.WorldPosition = base.transform.position;
				componentInParent.SetNativeScale(settings);
				break;
			case NativeSizeVolumeAction.ResetSize:
				componentInParent.SetNativeScale(null);
				break;
			}
		}
	}

	private void OnTriggerExit(Collider other)
	{
		GTPlayer componentInParent = other.GetComponentInParent<GTPlayer>();
		if (!(componentInParent == null))
		{
			switch (OnExitAction)
			{
			case NativeSizeVolumeAction.ApplySettings:
				settings.WorldPosition = base.transform.position;
				componentInParent.SetNativeScale(settings);
				break;
			case NativeSizeVolumeAction.ResetSize:
				componentInParent.SetNativeScale(null);
				break;
			}
		}
	}
}
