using System;
using DefaultNamespace;
using GorillaExtensions;
using GorillaLocomotion;
using GorillaNetworking;
using GorillaTag.CosmeticSystem;
using GorillaTagScripts;
using UnityEngine;
using UnityEngine.Events;

public class EvolvingCosmetic : MonoBehaviour, ICosmeticStateSync
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

		public int minActiveDays;

		public int maxActiveDays;

		public bool requireCurrentSubscription;
	}

	[SerializeField]
	private SubscriptionAgeRule ageRule;

	[SerializeField]
	private AgeAwareGameObject[] ageAwareGameObjects;

	[SerializeField]
	private int capDays = 1;

	[SerializeField]
	private UnityEvent<int> DispatchDaysOnEnable;

	[SerializeField]
	private int maxDays = 1;

	[SerializeField]
	private int multiplier = 1;

	[SerializeField]
	private UnityEvent<float> DispatchDaysOnEnableNormalized;

	private int? _daysAccrued;

	private VRRig m_parentRig;

	public int StateValue => SelectedObjectIndex;

	public int SelectedObjectIndex { get; private set; } = -1;

	public string PlayfabId => base.gameObject.name;

	private void Awake()
	{
		if (EvolvingCosmeticSaveData.Instance.SelectedIndices.TryGetValue(PlayfabId, out var value) && IsIndexAvailable(value))
		{
			SelectedObjectIndex = value;
			ActivateSelectedIndex();
		}
	}

	private void OnEnable()
	{
		if (m_parentRig == null)
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
			m_parentRig = vRRig;
		}
		if (!(m_parentRig == null))
		{
			if (m_parentRig.isLocal)
			{
				SubscriptionManager.OnLocalSubscriptionData = (Action)Delegate.Remove(SubscriptionManager.OnLocalSubscriptionData, new Action(UpdateDaysAccrued));
				SubscriptionManager.OnLocalSubscriptionData = (Action)Delegate.Combine(SubscriptionManager.OnLocalSubscriptionData, new Action(UpdateDaysAccrued));
			}
			_daysAccrued = 0;
			UnselectAll();
			m_parentRig.reliableState.RegisterCosmeticStateSyncTarget(GetStateSyncSlot(), this);
			UpdateDaysAccrued();
		}
	}

	private void UpdateDaysAccrued()
	{
		SubscriptionManager.SubscriptionDetails subscriptionDetails = SubscriptionManager.GetSubscriptionDetails(m_parentRig);
		switch (ageRule)
		{
		case SubscriptionAgeRule.ItemAge:
			_daysAccrued = m_parentRig.CheckCosmeticAge(base.name);
			break;
		case SubscriptionAgeRule.MinItemSubscriptionAge:
			_daysAccrued = Mathf.Min(subscriptionDetails.daysAccrued, m_parentRig.CheckCosmeticAge(base.name));
			break;
		case SubscriptionAgeRule.SubscriptionAge:
			_daysAccrued = subscriptionDetails.daysAccrued;
			break;
		case SubscriptionAgeRule.MinItemSubscriptionAgeActive:
			if (subscriptionDetails.active)
			{
				_daysAccrued = Mathf.Min(subscriptionDetails.daysAccrued, m_parentRig.CheckCosmeticAge(base.name));
			}
			break;
		case SubscriptionAgeRule.SubscriptionAgeActive:
			if (subscriptionDetails.active)
			{
				_daysAccrued = subscriptionDetails.daysAccrued;
			}
			break;
		}
		if (!_daysAccrued.HasValue)
		{
			Debug.LogError("_daysAccrued was not set by end of OnEnable.");
			return;
		}
		int value = _daysAccrued.Value;
		SelectedObjectIndex = FindAgeAwareIndex(value);
		ActivateSelectedIndex();
		DispatchDaysOnEnable?.Invoke(Mathf.Min(value, capDays));
		if (maxDays > 0)
		{
			DispatchDaysOnEnableNormalized?.Invoke(Mathf.Min((float)value / (float)maxDays, 1f) * (float)multiplier);
		}
	}

	private int FindAgeAwareIndex(int daysAccrued)
	{
		if (ageAwareGameObjects.Length == 0)
		{
			return -1;
		}
		if (ageAwareGameObjects[0].minActiveDays > daysAccrued)
		{
			return -1;
		}
		for (int i = 0; i < ageAwareGameObjects.Length; i++)
		{
			if (daysAccrued <= ageAwareGameObjects[i].maxActiveDays)
			{
				return i;
			}
		}
		return ageAwareGameObjects.Length - 1;
	}

	private void OnDisable()
	{
		if (!m_parentRig.IsNull())
		{
			if (m_parentRig.isLocal)
			{
				SubscriptionManager.OnLocalSubscriptionData = (Action)Delegate.Remove(SubscriptionManager.OnLocalSubscriptionData, new Action(UpdateDaysAccrued));
			}
			m_parentRig.reliableState.UnRegisterCosmeticStateSyncTarget(GetStateSyncSlot(), this);
		}
	}

	private void ActivateSelectedIndex()
	{
		if (SelectedObjectIndex < 0)
		{
			UnselectAll();
		}
		else if (IsSelectedIndexAvailable())
		{
			for (int i = 0; i < ageAwareGameObjects.Length; i++)
			{
				ageAwareGameObjects[i].gameObject.SetActive(i == SelectedObjectIndex);
			}
		}
	}

	private bool IsSelectedIndexAvailable()
	{
		return IsIndexAvailable(SelectedObjectIndex);
	}

	private bool IsIndexAvailable(int index)
	{
		if (index < 0 || index >= ageAwareGameObjects.Length)
		{
			return false;
		}
		AgeAwareGameObject ageAwareGameObject = ageAwareGameObjects[index];
		return _daysAccrued.GetValueOrDefault() >= ageAwareGameObject.minActiveDays;
	}

	public void GoBack()
	{
		if (CanGoBack())
		{
			SelectedObjectIndex--;
			ActivateSelectedIndex();
		}
	}

	public void GoForward()
	{
		if (CanGoForward())
		{
			SelectedObjectIndex++;
			ActivateSelectedIndex();
		}
	}

	public void MatchStage(EvolvingCosmetic other)
	{
		while (SelectedObjectIndex > other.SelectedObjectIndex && CanGoBack())
		{
			SelectedObjectIndex--;
		}
		while (SelectedObjectIndex < other.SelectedObjectIndex && CanGoForward())
		{
			SelectedObjectIndex++;
		}
		ActivateSelectedIndex();
	}

	private void UnselectAll()
	{
		SelectedObjectIndex = -1;
		AgeAwareGameObject[] array = ageAwareGameObjects;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].gameObject.SetActive(value: false);
		}
	}

	public bool CanGoBack()
	{
		return IsIndexAvailable(SelectedObjectIndex - 1);
	}

	public bool CanGoForward()
	{
		return IsIndexAvailable(SelectedObjectIndex + 1);
	}

	public void OnStateUpdate(int state)
	{
		if (IsIndexAvailable(state))
		{
			SelectedObjectIndex = state;
			ActivateSelectedIndex();
		}
	}

	private VRRigReliableState.StateSyncSlots GetStateSyncSlot()
	{
		CosmeticSO cosmeticSOFromDisplayName = CosmeticsController.instance.GetCosmeticSOFromDisplayName(PlayfabId);
		return cosmeticSOFromDisplayName.info.category.Value switch
		{
			CosmeticsController.CosmeticCategory.Hat => VRRigReliableState.StateSyncSlots.Hat, 
			CosmeticsController.CosmeticCategory.Shirt => VRRigReliableState.StateSyncSlots.Shirt, 
			CosmeticsController.CosmeticCategory.Face => VRRigReliableState.StateSyncSlots.Face, 
			_ => throw new Exception($"Unhandled CosmeticCategory {cosmeticSOFromDisplayName.info.category.Value}"), 
		};
	}
}
