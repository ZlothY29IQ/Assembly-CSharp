using System;
using System.Collections.Generic;
using UnityEngine;

public class AnnoyingFeatureSentinel : MonoBehaviour
{
	private List<IAnnoyingFeature> annoyingFeatures = new List<IAnnoyingFeature>();

	private int disableRequests;

	private void OnEnable()
	{
		AnnoyingFeatureDisabler.OnRequestDisable += AnnoyingFeatureDisabler_OnRequestDisable;
		AnnoyingNetworkComponent.AlertActive += AlertActive;
		AnnoyingMonoBehaviour.AlertActive += AlertActive;
	}

	private void OnDisable()
	{
		AnnoyingFeatureDisabler.OnRequestDisable -= AnnoyingFeatureDisabler_OnRequestDisable;
		AnnoyingNetworkComponent.AlertActive -= AlertActive;
		AnnoyingMonoBehaviour.AlertActive -= AlertActive;
	}

	private void OnDestroy()
	{
		AnnoyingFeatureDisabler.OnRequestDisable -= AnnoyingFeatureDisabler_OnRequestDisable;
		AnnoyingNetworkComponent.AlertActive -= AlertActive;
		AnnoyingMonoBehaviour.AlertActive -= AlertActive;
	}

	private void AlertActive(IAnnoyingFeature af, bool b)
	{
		if (b && !annoyingFeatures.Contains(af))
		{
			annoyingFeatures.Add(af);
			af.Allowed(disableRequests == 0);
		}
		else if (!b && annoyingFeatures.Contains(af))
		{
			annoyingFeatures.Remove(af);
			af.Allowed(v: true);
		}
	}

	private void AnnoyingFeatureDisabler_OnRequestDisable(bool v)
	{
		if (v)
		{
			disableRequests++;
		}
		else
		{
			disableRequests = Math.Max(0, disableRequests - 1);
		}
		for (int i = 0; i < annoyingFeatures.Count; i++)
		{
			annoyingFeatures[i].Allowed(disableRequests == 0);
		}
	}
}
