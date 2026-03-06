using System;
using GorillaLocomotion;
using GorillaTagScripts;
using UnityEngine;
using UnityEngine.Events;

public class EvolvingCosmetic : MonoBehaviour
{
	private enum SubscriptionAgeRule
	{
		ItemAge,
		MinItemSubscriptionAge,
		SubscriptionAge,
		MinItemSubscriptionAgeActive,
		SubscriptionAgeActive
	}

	[Serializable]
	private struct AgeAwareGameObject
	{
		public GameObject gameObject;

		public bool requireCurrentSubscription;

		public int minActiveDays;

		public int maxActiveDays;
	}

	[SerializeField]
	private SubscriptionAgeRule ageRule;

	[SerializeField]
	private AgeAwareGameObject[] ageAwareGameObjects;

	[SerializeField]
	private UnityEvent<int> DispatchDaysOnEnable;

	[SerializeField]
	private int maxDays = 1;

	[SerializeField]
	private UnityEvent<float> DispatchDaysOnEnableNormalized;

	private void OnEnable()
	{
		VRRig vRRig = GetComponentInParent<VRRig>();
		if (vRRig == null)
		{
			if (GetComponentInParent<GTPlayer>() == null)
			{
				return;
			}
			vRRig = VRRig.LocalRig;
		}
		if (vRRig == null)
		{
			return;
		}
		SubscriptionManager.SubscriptionDetails subscriptionDetails = SubscriptionManager.GetSubscriptionDetails(vRRig);
		int num = 0;
		switch (ageRule)
		{
		case SubscriptionAgeRule.ItemAge:
			num = vRRig.CheckCosmeticAge(base.name);
			break;
		case SubscriptionAgeRule.MinItemSubscriptionAge:
			num = Mathf.Min(subscriptionDetails.daysAccrued, vRRig.CheckCosmeticAge(base.name));
			break;
		case SubscriptionAgeRule.SubscriptionAge:
			num = subscriptionDetails.daysAccrued;
			break;
		case SubscriptionAgeRule.MinItemSubscriptionAgeActive:
			if (subscriptionDetails.active)
			{
				num = Mathf.Min(subscriptionDetails.daysAccrued, vRRig.CheckCosmeticAge(base.name));
			}
			break;
		case SubscriptionAgeRule.SubscriptionAgeActive:
			if (subscriptionDetails.active)
			{
				num = subscriptionDetails.daysAccrued;
			}
			break;
		}
		for (int i = 0; i < ageAwareGameObjects.Length; i++)
		{
			AgeAwareGameObject ageAwareGameObject = ageAwareGameObjects[i];
			ageAwareGameObject.gameObject.SetActive((!ageAwareGameObject.requireCurrentSubscription || subscriptionDetails.active) && num >= ageAwareGameObject.minActiveDays && (ageAwareGameObject.maxActiveDays == 0 || num < ageAwareGameObject.maxActiveDays));
		}
		DispatchDaysOnEnable?.Invoke(num);
		if (maxDays > 0)
		{
			DispatchDaysOnEnableNormalized?.Invoke(Mathf.Min((float)num / (float)maxDays, 1f));
		}
	}
}
