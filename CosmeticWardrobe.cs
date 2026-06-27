using System;
using GorillaNetworking;
using TMPro;
using UnityEngine;

public class CosmeticWardrobe : MonoBehaviour
{
	[Serializable]
	public class CosmeticWardrobeSelection
	{
		public HeadModel displayHead;

		public CosmeticButton selectButton;

		public CosmeticsController.CosmeticItem currentCosmeticItem;
	}

	[Serializable]
	public class CosmeticWardrobeCategory
	{
		public CosmeticCategoryButton button;

		public CosmeticsController.CosmeticCategory category;

		public CosmeticsController.CosmeticSlots slot1 = CosmeticsController.CosmeticSlots.Count;

		public CosmeticsController.CosmeticSlots slot2 = CosmeticsController.CosmeticSlots.Count;

		public CosmeticsController.CosmeticItem slot1RemovedItem;

		public CosmeticsController.CosmeticItem slot2RemovedItem;
	}

	[SerializeField]
	private CosmeticWardrobeSelection[] cosmeticCollectionDisplays;

	[SerializeField]
	private CosmeticButton[] uniqueCosmeticButtons;

	[SerializeField]
	private CosmeticWardrobeCategory[] cosmeticCategoryButtons;

	[SerializeField]
	private HeadModel currentEquippedDisplay;

	[SerializeField]
	private GorillaPressableButton nextSelection;

	[SerializeField]
	private GorillaPressableButton prevSelection;

	[SerializeField]
	private bool m_useTemporarySet;

	[SerializeField]
	private CosmeticButton previousOutfit;

	[SerializeField]
	private CosmeticButton nextOutfit;

	[SerializeField]
	private TMP_Text outfitText;

	private static int selectedCategoryIndex = 0;

	private static CosmeticsController.CosmeticCategory selectedCategory = CosmeticsController.CosmeticCategory.Hat;

	private static int startingDisplayIndex = 0;

	private static int selectedOutfitIndex = 0;

	private static Action OnWardrobeUpdateCategories;

	private static Action OnWardrobeUpdateDisplays;

	public Vector3 startingHeadSize = new Vector3(0.25f, 0.25f, 0.25f);

	public bool UseTemporarySet
	{
		get
		{
			return m_useTemporarySet;
		}
		set
		{
			bool num = value != m_useTemporarySet;
			m_useTemporarySet = value;
			if (num)
			{
				HandleCosmeticsUpdated();
			}
		}
	}

	private void Start()
	{
		for (int i = 0; i < cosmeticCategoryButtons.Length; i++)
		{
			if (cosmeticCategoryButtons[i].category == selectedCategory)
			{
				selectedCategoryIndex = i;
				break;
			}
		}
		for (int j = 0; j < cosmeticCollectionDisplays.Length; j++)
		{
			cosmeticCollectionDisplays[j].displayHead.transform.localScale = startingHeadSize;
		}
		if ((bool)GorillaTagger.Instance && (bool)GorillaTagger.Instance.offlineVRRig)
		{
			GorillaTagger.Instance.offlineVRRig.OnColorChanged += HandleLocalColorChanged;
			HandleLocalColorChanged(GorillaTagger.Instance.offlineVRRig.playerColor);
		}
		nextSelection.onPressed += HandlePressedNextSelection;
		prevSelection.onPressed += HandlePressedPrevSelection;
		for (int k = 0; k < cosmeticCollectionDisplays.Length; k++)
		{
			cosmeticCollectionDisplays[k].selectButton.onPressed += HandlePressedSelectCosmeticButton;
		}
		for (int l = 0; l < uniqueCosmeticButtons.Length; l++)
		{
			uniqueCosmeticButtons[l].onPressed += HandlePressedSelectCosmeticButtonUnique;
		}
		for (int m = 0; m < cosmeticCategoryButtons.Length; m++)
		{
			cosmeticCategoryButtons[m].button.onPressed += HandleChangeCategory;
			cosmeticCategoryButtons[m].slot1RemovedItem = CosmeticsController.instance.nullItem;
			cosmeticCategoryButtons[m].slot2RemovedItem = CosmeticsController.instance.nullItem;
		}
		CosmeticsController instance = CosmeticsController.instance;
		instance.OnCosmeticsUpdated = (Action)Delegate.Combine(instance.OnCosmeticsUpdated, new Action(HandleCosmeticsUpdated));
		CosmeticsController instance2 = CosmeticsController.instance;
		instance2.OnOutfitsUpdated = (Action)Delegate.Combine(instance2.OnOutfitsUpdated, new Action(UpdateOutfitButtons));
		OnWardrobeUpdateCategories = (Action)Delegate.Combine(OnWardrobeUpdateCategories, new Action(UpdateCategoryButtons));
		OnWardrobeUpdateDisplays = (Action)Delegate.Combine(OnWardrobeUpdateDisplays, new Action(UpdateCosmeticDisplays));
		previousOutfit.onPressed += HandlePressedPrevOutfitButton;
		nextOutfit.onPressed += HandlePressedNextOutfitButton;
		HandleCosmeticsUpdated();
	}

	private void OnDestroy()
	{
		if ((bool)GorillaTagger.Instance && (bool)GorillaTagger.Instance.offlineVRRig)
		{
			GorillaTagger.Instance.offlineVRRig.OnColorChanged -= HandleLocalColorChanged;
		}
		nextSelection.onPressed -= HandlePressedNextSelection;
		prevSelection.onPressed -= HandlePressedPrevSelection;
		for (int i = 0; i < cosmeticCollectionDisplays.Length; i++)
		{
			cosmeticCollectionDisplays[i].selectButton.onPressed -= HandlePressedSelectCosmeticButton;
		}
		for (int j = 0; j < uniqueCosmeticButtons.Length; j++)
		{
			uniqueCosmeticButtons[j].onPressed -= HandlePressedSelectCosmeticButtonUnique;
		}
		for (int k = 0; k < cosmeticCategoryButtons.Length; k++)
		{
			cosmeticCategoryButtons[k].button.onPressed -= HandleChangeCategory;
		}
		CosmeticsController instance = CosmeticsController.instance;
		instance.OnCosmeticsUpdated = (Action)Delegate.Remove(instance.OnCosmeticsUpdated, new Action(HandleCosmeticsUpdated));
		CosmeticsController instance2 = CosmeticsController.instance;
		instance2.OnOutfitsUpdated = (Action)Delegate.Remove(instance2.OnOutfitsUpdated, new Action(UpdateOutfitButtons));
		OnWardrobeUpdateCategories = (Action)Delegate.Remove(OnWardrobeUpdateCategories, new Action(UpdateCategoryButtons));
		OnWardrobeUpdateDisplays = (Action)Delegate.Remove(OnWardrobeUpdateDisplays, new Action(UpdateCosmeticDisplays));
		previousOutfit.onPressed -= HandlePressedPrevOutfitButton;
		nextOutfit.onPressed -= HandlePressedNextOutfitButton;
	}

	private void HandlePressedNextSelection(GorillaPressableButton button, bool isLeft)
	{
		startingDisplayIndex += cosmeticCollectionDisplays.Length;
		if (startingDisplayIndex >= CosmeticsController.instance.GetCategorySize(selectedCategory))
		{
			startingDisplayIndex = 0;
		}
		OnWardrobeUpdateDisplays?.Invoke();
	}

	private void HandlePressedPrevSelection(GorillaPressableButton button, bool isLeft)
	{
		startingDisplayIndex -= cosmeticCollectionDisplays.Length;
		if (startingDisplayIndex < 0)
		{
			int categorySize = CosmeticsController.instance.GetCategorySize(selectedCategory);
			int num;
			if (categorySize % cosmeticCollectionDisplays.Length == 0)
			{
				num = categorySize - cosmeticCollectionDisplays.Length;
			}
			else
			{
				num = categorySize / cosmeticCollectionDisplays.Length;
				num *= cosmeticCollectionDisplays.Length;
			}
			startingDisplayIndex = num;
		}
		OnWardrobeUpdateDisplays?.Invoke();
	}

	private async void RepressButton(GorillaPressableButton button, bool isLeft, string itemName)
	{
		float startTime = Time.time;
		float maxTime = 5f;
		while (VRRig.LocalRig.cosmeticsObjectRegistry.Cosmetic(itemName) == null)
		{
			if (Time.time > startTime + maxTime)
			{
				return;
			}
			await Awaitable.NextFrameAsync();
		}
		HandlePressedSelectCosmeticButton(button, isLeft);
	}

	private void HandlePressedSelectCosmeticButton(GorillaPressableButton button, bool isLeft)
	{
		for (int i = 0; i < cosmeticCollectionDisplays.Length; i++)
		{
			if (!(cosmeticCollectionDisplays[i].selectButton == button))
			{
				continue;
			}
			if (string.IsNullOrEmpty(cosmeticCollectionDisplays[i].currentCosmeticItem.itemName) || cosmeticCollectionDisplays[i].currentCosmeticItem.itemName == "NOTHING")
			{
				break;
			}
			if (VRRig.LocalRig.cosmeticsObjectRegistry.Cosmetic(cosmeticCollectionDisplays[i].currentCosmeticItem.itemName) == null)
			{
				RepressButton(button, isLeft, cosmeticCollectionDisplays[i].currentCosmeticItem.itemName);
				continue;
			}
			CosmeticsController.instance.PressWardrobeItemButton(cosmeticCollectionDisplays[i].currentCosmeticItem, isLeft, m_useTemporarySet);
			if (isLeft)
			{
				cosmeticCategoryButtons[selectedCategoryIndex].slot2RemovedItem = CosmeticsController.instance.nullItem;
			}
			else
			{
				cosmeticCategoryButtons[selectedCategoryIndex].slot1RemovedItem = CosmeticsController.instance.nullItem;
			}
			break;
		}
	}

	private async void RepressUniqueButton(GorillaPressableButton button, bool isLeft, string itemName)
	{
		float startTime = Time.time;
		float maxTime = 5f;
		while (VRRig.LocalRig.cosmeticsObjectRegistry.Cosmetic(itemName) == null)
		{
			if (Time.time > startTime + maxTime)
			{
				return;
			}
			await Awaitable.NextFrameAsync();
		}
		HandlePressedSelectCosmeticButtonUnique(button, isLeft);
	}

	private void HandlePressedSelectCosmeticButtonUnique(GorillaPressableButton button, bool isLeft)
	{
		for (int i = 0; i < uniqueCosmeticButtons.Length; i++)
		{
			if (!(uniqueCosmeticButtons[i] == button))
			{
				continue;
			}
			if (string.IsNullOrEmpty(uniqueCosmeticButtons[i].SetCosmeticItemID) || uniqueCosmeticButtons[i].SetCosmeticItemID == "NOTHING")
			{
				break;
			}
			if (VRRig.LocalRig.cosmeticsObjectRegistry.Cosmetic(uniqueCosmeticButtons[i].SetCosmeticItemID) == null)
			{
				Debug.LogWarning("Could not find cosmetic " + uniqueCosmeticButtons[i].SetCosmeticItemID + " in cosmetic registry!");
				RepressUniqueButton(button, isLeft, uniqueCosmeticButtons[i].SetCosmeticItemID);
				continue;
			}
			if (CosmeticsController.instance.allCosmeticsDict.TryGetValue(uniqueCosmeticButtons[i].SetCosmeticItemID, out var value))
			{
				CosmeticsController.instance.PressWardrobeItemButton(value, isLeft, m_useTemporarySet);
			}
			else
			{
				Debug.LogWarning("Could not find cosmetic in dictionary named " + uniqueCosmeticButtons[i].SetCosmeticItemID + "!");
			}
			break;
		}
	}

	private void HandleChangeCategory(GorillaPressableButton button, bool isLeft)
	{
		for (int i = 0; i < cosmeticCategoryButtons.Length; i++)
		{
			CosmeticWardrobeCategory cosmeticWardrobeCategory = cosmeticCategoryButtons[i];
			if (!(cosmeticWardrobeCategory.button == button))
			{
				continue;
			}
			if (selectedCategory == cosmeticWardrobeCategory.category)
			{
				CosmeticsController.CosmeticItem cosmeticItem = CosmeticsController.instance.nullItem;
				if (cosmeticWardrobeCategory.slot1 != CosmeticsController.CosmeticSlots.Count)
				{
					cosmeticItem = CosmeticsController.instance.GetSlotItem(cosmeticWardrobeCategory.slot1, checkOpposite: true, m_useTemporarySet);
				}
				CosmeticsController.CosmeticItem cosmeticItem2 = CosmeticsController.instance.nullItem;
				if (cosmeticWardrobeCategory.slot2 != CosmeticsController.CosmeticSlots.Count)
				{
					cosmeticItem2 = CosmeticsController.instance.GetSlotItem(cosmeticWardrobeCategory.slot2, checkOpposite: true, m_useTemporarySet);
				}
				bool flag = selectedCategory == CosmeticsController.CosmeticCategory.Arms;
				if (!cosmeticItem.isNullItem || !cosmeticItem2.isNullItem)
				{
					if (!cosmeticItem.isNullItem)
					{
						cosmeticWardrobeCategory.slot1RemovedItem = cosmeticItem;
						CosmeticsController.instance.PressWardrobeItemButton(cosmeticItem, flag, m_useTemporarySet);
					}
					if (!cosmeticItem2.isNullItem)
					{
						cosmeticWardrobeCategory.slot2RemovedItem = cosmeticItem2;
						CosmeticsController.instance.PressWardrobeItemButton(cosmeticItem2, !flag, m_useTemporarySet);
					}
					OnWardrobeUpdateDisplays?.Invoke();
					OnWardrobeUpdateCategories?.Invoke();
				}
				else if (!cosmeticWardrobeCategory.slot1RemovedItem.isNullItem || !cosmeticWardrobeCategory.slot2RemovedItem.isNullItem)
				{
					if (!cosmeticWardrobeCategory.slot1RemovedItem.isNullItem)
					{
						CosmeticsController.instance.PressWardrobeItemButton(cosmeticWardrobeCategory.slot1RemovedItem, flag, m_useTemporarySet);
						cosmeticWardrobeCategory.slot1RemovedItem = CosmeticsController.instance.nullItem;
					}
					if (!cosmeticWardrobeCategory.slot2RemovedItem.isNullItem)
					{
						CosmeticsController.instance.PressWardrobeItemButton(cosmeticWardrobeCategory.slot2RemovedItem, !flag, m_useTemporarySet);
						cosmeticWardrobeCategory.slot2RemovedItem = CosmeticsController.instance.nullItem;
					}
					OnWardrobeUpdateDisplays?.Invoke();
					OnWardrobeUpdateCategories?.Invoke();
				}
			}
			else
			{
				selectedCategory = cosmeticWardrobeCategory.category;
				selectedCategoryIndex = i;
				startingDisplayIndex = 0;
				OnWardrobeUpdateDisplays?.Invoke();
				OnWardrobeUpdateCategories?.Invoke();
			}
			break;
		}
	}

	private void HandleCosmeticsUpdated()
	{
		string[] currentlyWornCosmetics = CosmeticsController.instance.GetCurrentlyWornCosmetics(m_useTemporarySet);
		bool[] currentRightEquippedSided = CosmeticsController.instance.GetCurrentRightEquippedSided(m_useTemporarySet);
		currentEquippedDisplay.SetCosmeticActiveArray(currentlyWornCosmetics, currentRightEquippedSided);
		UpdateCategoryButtons();
		UpdateCosmeticDisplays();
		UpdateOutfitButtons();
	}

	private void HandleLocalColorChanged(Color newColor)
	{
		MeshRenderer component = currentEquippedDisplay.GetComponent<MeshRenderer>();
		if (component != null)
		{
			component.material.color = newColor;
		}
	}

	private void HandlePressedPrevOutfitButton(GorillaPressableButton button, bool isLeft)
	{
		CosmeticsController.instance.PressWardrobeScrollOutfit(forward: false);
	}

	private void HandlePressedNextOutfitButton(GorillaPressableButton button, bool isLeft)
	{
		CosmeticsController.instance.PressWardrobeScrollOutfit(forward: true);
	}

	private void UpdateCosmeticDisplays()
	{
		for (int i = 0; i < cosmeticCollectionDisplays.Length; i++)
		{
			CosmeticsController.CosmeticItem cosmetic = CosmeticsController.instance.GetCosmetic(selectedCategory, startingDisplayIndex + i);
			CosmeticWardrobeSelection obj = cosmeticCollectionDisplays[i];
			obj.currentCosmeticItem = cosmetic;
			obj.displayHead.SetCosmeticActive(cosmetic.displayName);
			obj.selectButton.enabled = !cosmetic.isNullItem;
			obj.selectButton.isOn = !cosmetic.isNullItem && CosmeticsController.instance.IsCosmeticEquipped(cosmetic, m_useTemporarySet);
			obj.selectButton.UpdateColor();
		}
		int categorySize = CosmeticsController.instance.GetCategorySize(selectedCategory);
		nextSelection.enabled = categorySize > cosmeticCollectionDisplays.Length;
		nextSelection.UpdateColor();
		prevSelection.enabled = categorySize > cosmeticCollectionDisplays.Length;
		prevSelection.UpdateColor();
	}

	private void UpdateCategoryButtons()
	{
		for (int i = 0; i < cosmeticCategoryButtons.Length; i++)
		{
			CosmeticWardrobeCategory cosmeticWardrobeCategory = cosmeticCategoryButtons[i];
			if (cosmeticWardrobeCategory.slot1 != CosmeticsController.CosmeticSlots.Count)
			{
				CosmeticsController.CosmeticItem slotItem = CosmeticsController.instance.GetSlotItem(cosmeticWardrobeCategory.slot1, checkOpposite: false, m_useTemporarySet);
				if (cosmeticWardrobeCategory.slot2 != CosmeticsController.CosmeticSlots.Count)
				{
					CosmeticsController.CosmeticItem slotItem2 = CosmeticsController.instance.GetSlotItem(cosmeticWardrobeCategory.slot2, checkOpposite: false, m_useTemporarySet);
					if (slotItem.bothHandsHoldable)
					{
						cosmeticWardrobeCategory.button.SetIcon(slotItem.isNullItem ? null : slotItem.itemPicture);
					}
					else if (slotItem2.bothHandsHoldable)
					{
						cosmeticWardrobeCategory.button.SetIcon(slotItem2.isNullItem ? null : slotItem2.itemPicture);
					}
					else
					{
						cosmeticWardrobeCategory.button.SetDualIcon(slotItem.isNullItem ? null : slotItem.itemPicture, slotItem2.isNullItem ? null : slotItem2.itemPicture);
					}
				}
				else
				{
					cosmeticWardrobeCategory.button.SetIcon(slotItem.isNullItem ? null : slotItem.itemPicture);
				}
			}
			int categorySize = CosmeticsController.instance.GetCategorySize(cosmeticWardrobeCategory.category);
			cosmeticWardrobeCategory.button.enabled = categorySize > 0;
			cosmeticWardrobeCategory.button.isOn = selectedCategory == cosmeticWardrobeCategory.category;
			cosmeticWardrobeCategory.button.UpdateColor();
		}
	}

	private void UpdateOutfitButtons()
	{
		bool flag = CosmeticsController.CanScrollOutfits();
		int num = CosmeticsController.SelectedOutfit + 1;
		nextOutfit.enabled = flag;
		previousOutfit.enabled = flag;
		nextOutfit.UpdateColor();
		previousOutfit.UpdateColor();
		outfitText.text = "Outfit #" + num;
	}

	public bool WardrobeButtonsInitialized()
	{
		for (int i = 0; i < cosmeticCategoryButtons.Length; i++)
		{
			if (!cosmeticCategoryButtons[i].button.Initialized)
			{
				return false;
			}
		}
		for (int i = 0; i < cosmeticCollectionDisplays.Length; i++)
		{
			if (!cosmeticCollectionDisplays[i].selectButton.Initialized)
			{
				return false;
			}
		}
		return true;
	}
}
