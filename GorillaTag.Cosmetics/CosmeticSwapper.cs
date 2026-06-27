using System.Collections.Generic;
using GorillaGameModes;
using GorillaNetworking;
using UnityEngine;
using UnityEngine.Events;

namespace GorillaTag.Cosmetics;

public class CosmeticSwapper : MonoBehaviour, ITickSystemTick
{
	private enum SwapMode
	{
		AllAtOnce,
		StepByStep,
		Random
	}

	private struct CosmeticState
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

	[Tooltip("Optional. When assigned, TriggerSwap sources the cosmetic ID from the cycle controller's active sub-item instead of the cosmeticIDs list. Use SwapMode.Random to call CycleRandom() automatically on each hit before reading the active sub-item.")]
	[SerializeField]
	private SubCosmeticCycleController cycleController;

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

	private Stack<CosmeticState> newSwappedCosmetics = new Stack<CosmeticState>();

	private float lastCosmeticSwapTime = float.PositiveInfinity;

	private bool isAtFinalCosmeticStep;

	private int CosmeticStepIndex => newSwappedCosmetics.Count;

	public bool TickRunning { get; set; }

	private void Awake()
	{
		controller = CosmeticsController.instance;
	}

	private void OnEnable()
	{
		TickSystem<object>.AddTickCallback(this);
		PlayerCosmeticsSystem.UnlockTemporaryCosmeticsGlobal(cosmeticIDs);
	}

	private void OnDisable()
	{
		PlayerCosmeticsSystem.LockTemporaryCosmeticsGlobal(cosmeticIDs);
		TickSystem<object>.RemoveTickCallback(this);
	}

	public void SwapInCosmetic(VRRig vrRig)
	{
		TriggerSwap(vrRig);
	}

	private SwapMode GetCurrentMode()
	{
		return swapMode;
	}

	private bool ShouldHoldFinalStep()
	{
		return holdFinalStep;
	}

	public int GetCurrentStepIndex(VRRig rig)
	{
		if (rig == null)
		{
			return 0;
		}
		return CosmeticStepIndex;
	}

	public int GetNumberOfSteps()
	{
		if (cycleController != null)
		{
			return cycleController.Count;
		}
		return cosmeticIDs.Count;
	}

	private void TriggerSwap(VRRig rig)
	{
		if ((GorillaGameManager.instance != null && gameModeExclusion.Contains(GorillaGameManager.instance.GameType())) || rig == null || controller == null || rig != GorillaTagger.Instance.offlineVRRig)
		{
			return;
		}
		if (cycleController != null)
		{
			if (swapMode == SwapMode.Random)
			{
				cycleController.CycleRandom();
			}
			string appliedCosmeticID = cycleController.GetAppliedCosmeticID();
			if (!string.IsNullOrEmpty(appliedCosmeticID))
			{
				CosmeticState? cosmeticState = SwapInCosmeticWithReturn(appliedCosmeticID, rig);
				if (cosmeticState.HasValue)
				{
					AddNewSwappedCosmetic(cosmeticState.Value);
				}
			}
		}
		else
		{
			if (cosmeticIDs.Count == 0)
			{
				return;
			}
			if (swapMode == SwapMode.Random)
			{
				string nameOrId = cosmeticIDs[Random.Range(0, cosmeticIDs.Count)];
				CosmeticState? cosmeticState2 = SwapInCosmeticWithReturn(nameOrId, rig);
				if (cosmeticState2.HasValue)
				{
					AddNewSwappedCosmetic(cosmeticState2.Value);
				}
				return;
			}
			if (swapMode == SwapMode.AllAtOnce)
			{
				if (newSwappedCosmetics.Count > 0)
				{
					return;
				}
				{
					foreach (string cosmeticID in cosmeticIDs)
					{
						CosmeticState? cosmeticState3 = SwapInCosmeticWithReturn(cosmeticID, rig);
						if (cosmeticState3.HasValue)
						{
							AddNewSwappedCosmetic(cosmeticState3.Value);
						}
					}
					return;
				}
			}
			int cosmeticStepIndex = CosmeticStepIndex;
			if (cosmeticStepIndex < 0 || cosmeticStepIndex >= cosmeticIDs.Count)
			{
				return;
			}
			string nameOrId2 = cosmeticIDs[cosmeticStepIndex];
			CosmeticState? cosmeticState4 = SwapInCosmeticWithReturn(nameOrId2, rig);
			if (!cosmeticState4.HasValue)
			{
				return;
			}
			AddNewSwappedCosmetic(cosmeticState4.Value);
			if (cosmeticStepIndex == cosmeticIDs.Count - 1)
			{
				if (holdFinalStep)
				{
					MarkFinalCosmeticStep();
				}
				if (OnSwappingSequenceCompleted != null)
				{
					OnSwappingSequenceCompleted.Invoke(rig);
				}
			}
			else
			{
				UnmarkFinalCosmeticStep();
			}
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
		if (!replacedItem.isNullItem && replacedItem.itemName == cosmeticItem.itemName)
		{
			return null;
		}
		controller.ApplyCosmeticItemToSet(controller.tempUnlockedSet, cosmeticItem, isLeftHand, applyToPlayerPrefs: false);
		controller.UpdateWornCosmetics(sync: true);
		return new CosmeticState
		{
			cosmeticId = nameOrId,
			replacedItem = replacedItem,
			slot = cosmeticSlot,
			isLeftHand = isLeftHand
		};
	}

	private void RestorePreviousCosmetic(CosmeticState state)
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

	public void Tick()
	{
		if (newSwappedCosmetics.Count <= 0)
		{
			return;
		}
		if (GetCurrentMode() == SwapMode.StepByStep)
		{
			if (isAtFinalCosmeticStep && ShouldHoldFinalStep())
			{
				if (Time.time - lastCosmeticSwapTime <= stepTimeout)
				{
					return;
				}
				isAtFinalCosmeticStep = false;
			}
			if (Time.time - lastCosmeticSwapTime > stepTimeout)
			{
				while (newSwappedCosmetics.Count > 0)
				{
					CosmeticState state = newSwappedCosmetics.Pop();
					RestorePreviousCosmetic(state);
				}
				isAtFinalCosmeticStep = false;
				lastCosmeticSwapTime = float.PositiveInfinity;
			}
		}
		else if (GetCurrentMode() == SwapMode.AllAtOnce && Time.time - lastCosmeticSwapTime > stepTimeout)
		{
			while (newSwappedCosmetics.Count > 0)
			{
				CosmeticState state2 = newSwappedCosmetics.Pop();
				RestorePreviousCosmetic(state2);
			}
			lastCosmeticSwapTime = float.PositiveInfinity;
			isAtFinalCosmeticStep = false;
		}
	}

	private void AddNewSwappedCosmetic(CosmeticState state)
	{
		newSwappedCosmetics.Push(state);
		lastCosmeticSwapTime = Time.time;
	}

	private void MarkFinalCosmeticStep()
	{
		isAtFinalCosmeticStep = true;
		lastCosmeticSwapTime = Time.time;
	}

	private void UnmarkFinalCosmeticStep()
	{
		isAtFinalCosmeticStep = false;
	}
}
