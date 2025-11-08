using System.Collections.Generic;
using GorillaGameModes;
using GorillaNetworking;
using UnityEngine;
using UnityEngine.Events;

namespace GorillaTag.Cosmetics;

public class CosmeticSwapper : MonoBehaviour
{
	public enum SwapMode
	{
		AllAtOnce,
		StepByStep
	}

	public struct CosmeticState
	{
		public string cosmeticId;

		public CosmeticsController.CosmeticItem replacedItem;

		public CosmeticsController.CosmeticSlots slot;

		public bool isLeftHand;
	}

	[SerializeField]
	private List<string> cosmeticIDs = new List<string>();

	[SerializeField]
	private SwapMode swapMode = SwapMode.StepByStep;

	[SerializeField]
	private float stepTimeout = 10f;

	[Tooltip("Hold final step as long as the swapper is being called within the timeframe")]
	[SerializeField]
	private bool holdFinalStep = true;

	[SerializeField]
	private UnityEvent<VRRig> OnSwappingSequenceCompleted;

	[SerializeField]
	private List<GameModeType> gameModeExclusion = new List<GameModeType>();

	private CosmeticsController controller;

	private void Awake()
	{
		controller = CosmeticsController.instance;
	}

	private void OnEnable()
	{
		PlayerCosmeticsSystem.UnlockTemporaryCosmeticsGlobal(cosmeticIDs);
	}

	private void OnDisable()
	{
		PlayerCosmeticsSystem.LockTemporaryCosmeticsGlobal(cosmeticIDs);
	}

	public void SwapInCosmetic(VRRig vrRig)
	{
		TriggerSwap(vrRig);
	}

	public SwapMode GetCurrentMode()
	{
		return swapMode;
	}

	public bool ShouldHoldFinalStep()
	{
		return holdFinalStep;
	}

	public int GetCurrentStepIndex(VRRig rig)
	{
		if (rig == null)
		{
			return 0;
		}
		return rig.CosmeticStepIndex;
	}

	public int GetNumberOfSteps()
	{
		return cosmeticIDs.Count;
	}

	private void TriggerSwap(VRRig rig)
	{
		if ((GorillaGameManager.instance != null && gameModeExclusion.Contains(GorillaGameManager.instance.GameType())) || rig == null || controller == null || cosmeticIDs.Count == 0 || rig != GorillaTagger.Instance.offlineVRRig)
		{
			return;
		}
		rig.SetCosmeticSwapper(this, stepTimeout);
		if (swapMode == SwapMode.AllAtOnce)
		{
			foreach (string cosmeticID in cosmeticIDs)
			{
				CosmeticState? cosmeticState = SwapInCosmeticWithReturn(cosmeticID, rig);
				if (cosmeticState.HasValue)
				{
					rig.AddNewSwappedCosmetic(cosmeticState.Value);
				}
			}
			return;
		}
		int cosmeticStepIndex = rig.CosmeticStepIndex;
		if (cosmeticStepIndex < 0 || cosmeticStepIndex >= cosmeticIDs.Count)
		{
			return;
		}
		string nameOrId = cosmeticIDs[cosmeticStepIndex];
		CosmeticState? cosmeticState2 = SwapInCosmeticWithReturn(nameOrId, rig);
		if (!cosmeticState2.HasValue)
		{
			return;
		}
		rig.AddNewSwappedCosmetic(cosmeticState2.Value);
		if (cosmeticStepIndex == cosmeticIDs.Count - 1)
		{
			if (holdFinalStep)
			{
				rig.MarkFinalCosmeticStep();
			}
			if (OnSwappingSequenceCompleted != null)
			{
				OnSwappingSequenceCompleted.Invoke(rig);
			}
		}
		else
		{
			rig.UnmarkFinalCosmeticStep();
		}
	}

	private CosmeticState? SwapInCosmeticWithReturn(string nameOrId, VRRig rig)
	{
		if (controller == null)
		{
			return null;
		}
		CosmeticsController.CosmeticItem cosmeticItem = FindItem(nameOrId);
		if (cosmeticItem.isNullItem)
		{
			Debug.LogWarning("Cosmetic not found: " + nameOrId);
			return null;
		}
		bool isLeftHand;
		CosmeticsController.CosmeticSlots cosmeticSlot = GetCosmeticSlot(cosmeticItem, out isLeftHand);
		if (cosmeticSlot == CosmeticsController.CosmeticSlots.Count)
		{
			Debug.LogWarning("Could not determine slot for: " + cosmeticItem.displayName);
			return null;
		}
		CosmeticsController.CosmeticItem replacedItem = controller.currentWornSet.items[(int)cosmeticSlot];
		controller.ApplyCosmeticItemToSet(controller.tempUnlockedSet, cosmeticItem, isLeftHand, applyToPlayerPrefs: false);
		controller.UpdateWornCosmetics(sync: true);
		CosmeticState value = default(CosmeticState);
		value.cosmeticId = nameOrId;
		value.replacedItem = replacedItem;
		value.slot = cosmeticSlot;
		value.isLeftHand = isLeftHand;
		return value;
	}

	public void RestorePreviousCosmetic(CosmeticState state, VRRig rig)
	{
		if (controller == null)
		{
			return;
		}
		CosmeticsController.CosmeticItem cosmeticItem = FindItem(state.cosmeticId);
		if (!cosmeticItem.isNullItem)
		{
			controller.RemoveCosmeticItemFromSet(controller.tempUnlockedSet, cosmeticItem.displayName, applyToPlayerPrefs: false);
			if (!state.replacedItem.isNullItem)
			{
				controller.ApplyCosmeticItemToSet(controller.tempUnlockedSet, state.replacedItem, state.isLeftHand, applyToPlayerPrefs: false);
			}
			controller.UpdateWornCosmetics(sync: true);
		}
	}

	private CosmeticsController.CosmeticItem FindItem(string nameOrId)
	{
		if (controller.allCosmeticsDict.TryGetValue(nameOrId, out var value))
		{
			return value;
		}
		if (controller.allCosmeticsItemIDsfromDisplayNamesDict.TryGetValue(nameOrId, out var value2))
		{
			return controller.GetItemFromDict(value2);
		}
		return controller.nullItem;
	}

	private CosmeticsController.CosmeticSlots GetCosmeticSlot(CosmeticsController.CosmeticItem item, out bool isLeftHand)
	{
		isLeftHand = false;
		if (item.isHoldable)
		{
			CosmeticsController.CosmeticSet currentWornSet = controller.currentWornSet;
			CosmeticsController.CosmeticItem cosmeticItem = currentWornSet.items[7];
			CosmeticsController.CosmeticItem cosmeticItem2 = currentWornSet.items[8];
			if (cosmeticItem.isNullItem || (!cosmeticItem2.isNullItem && item.itemName == cosmeticItem.itemName))
			{
				isLeftHand = true;
			}
			if (!isLeftHand)
			{
				return CosmeticsController.CosmeticSlots.HandRight;
			}
			return CosmeticsController.CosmeticSlots.HandLeft;
		}
		return CosmeticsController.CategoryToNonTransferrableSlot(item.itemCategory);
	}
}
