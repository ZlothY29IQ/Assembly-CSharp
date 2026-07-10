using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using CosmeticRoom;
using Cosmetics;
using ExitGames.Client.Photon;
using GorillaExtensions;
using GorillaLocomotion;
using GorillaNetworking.Store;
using GorillaTag;
using GorillaTag.CosmeticSystem;
using GorillaTag.Cosmetics;
using GorillaTagScripts;
using GorillaTagScripts.Subscription;
using GorillaTagScripts.VirtualStumpCustomMaps;
using Photon.Pun;
using Photon.Realtime;
using PlayFab;
using PlayFab.ClientModels;
using Steamworks;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Serialization;

namespace GorillaNetworking;

public class CosmeticsController : MonoBehaviour, IGorillaSliceableSimple, IBuildValidation
{
	public enum PurchaseItemStages
	{
		Start,
		CheckoutButtonPressed,
		ItemSelected,
		ItemOwned,
		FinalPurchaseAcknowledgement,
		Buying,
		Success,
		Failure
	}

	public enum CosmeticCategory
	{
		None,
		Hat,
		Badge,
		Face,
		Paw,
		Chest,
		Fur,
		Shirt,
		Back,
		Arms,
		Pants,
		TagEffect,
		Count,
		Set,
		Collectable
	}

	public enum CosmeticSlots
	{
		Hat,
		Badge,
		Face,
		ArmLeft,
		ArmRight,
		BackLeft,
		BackRight,
		HandLeft,
		HandRight,
		Chest,
		Fur,
		Shirt,
		Pants,
		Back,
		Arms,
		TagEffect,
		Count
	}

	[Serializable]
	public class CosmeticSet
	{
		public delegate void OnSetActivatedHandler(CosmeticSet prevSet, CosmeticSet currentSet, NetPlayer netPlayer);

		public const int k_fakePackedSlingshotID = -55;

		public CosmeticItem[] items;

		public string[] returnArray = new string[16];

		private static int[][] intArrays = new int[22][]
		{
			new int[0],
			new int[1],
			new int[2],
			new int[3],
			new int[4],
			new int[5],
			new int[6],
			new int[7],
			new int[8],
			new int[9],
			new int[10],
			new int[11],
			new int[12],
			new int[13],
			new int[14],
			new int[15],
			new int[16],
			new int[17],
			new int[18],
			new int[19],
			new int[20],
			new int[21]
		};

		private static CosmeticSet _emptySet;

		private static char[] nameScratchSpace = new char[6];

		public static CosmeticSet EmptySet
		{
			get
			{
				if (_emptySet == null)
				{
					string[] array = new string[16];
					for (int i = 0; i < array.Length; i++)
					{
						array[i] = "NOTHING";
					}
					_emptySet = new CosmeticSet(array, instance);
				}
				return _emptySet;
			}
		}

		public event OnSetActivatedHandler onSetActivatedEvent;

		protected void OnSetActivated(CosmeticSet prevSet, CosmeticSet currentSet, NetPlayer netPlayer)
		{
			if (this.onSetActivatedEvent != null)
			{
				this.onSetActivatedEvent(prevSet, currentSet, netPlayer);
			}
		}

		public CosmeticSet()
		{
			items = new CosmeticItem[16];
		}

		public CosmeticSet(string[] itemNames, CosmeticsController controller)
		{
			items = new CosmeticItem[16];
			for (int i = 0; i < itemNames.Length; i++)
			{
				string displayName = itemNames[i];
				string itemNameFromDisplayName = controller.GetItemNameFromDisplayName(displayName);
				items[i] = controller.GetItemFromDict(itemNameFromDisplayName);
			}
		}

		public CosmeticSet(int[] itemNamesPacked, CosmeticsController controller)
		{
			items = new CosmeticItem[16];
			int num = ((itemNamesPacked.Length != 0) ? itemNamesPacked[0] : 0);
			int num2 = 1;
			for (int i = 0; i < items.Length; i++)
			{
				if ((num & (1 << i)) != 0)
				{
					int num3 = itemNamesPacked[num2];
					if (num3 == -55)
					{
						items[i] = controller.GetItemFromDict("Slingshot");
					}
					else
					{
						nameScratchSpace[0] = (char)(65 + num3 % 26);
						nameScratchSpace[1] = (char)(65 + num3 / 26 % 26);
						nameScratchSpace[2] = (char)(65 + num3 / 676 % 26);
						nameScratchSpace[3] = (char)(65 + num3 / 17576 % 26);
						nameScratchSpace[4] = (char)(65 + num3 / 456976 % 26);
						nameScratchSpace[5] = '.';
						items[i] = controller.GetItemFromDict(new string(nameScratchSpace));
					}
					num2++;
				}
				else
				{
					items[i] = controller.GetItemFromDict("null");
				}
			}
		}

		public void CopyItems(CosmeticSet other)
		{
			for (int i = 0; i < items.Length; i++)
			{
				items[i] = other.items[i];
			}
		}

		public void CopyItemsIntoEmpty(CosmeticSet other)
		{
			for (int i = 0; i < items.Length; i++)
			{
				if (items[i].isNullItem)
				{
					items[i] = other.items[i];
				}
			}
		}

		public void MergeSets(CosmeticSet tryOn, CosmeticSet current)
		{
			for (int i = 0; i < 16; i++)
			{
				if (tryOn == null)
				{
					items[i] = current.items[i];
				}
				else
				{
					items[i] = (tryOn.items[i].isNullItem ? current.items[i] : tryOn.items[i]);
				}
			}
		}

		public void MergeInSets(CosmeticSet playerPref, CosmeticSet tempOverrideSet, Predicate<string> predicate)
		{
			int num = 16;
			for (int i = 0; i < num; i++)
			{
				_ = ref tempOverrideSet.items[i];
				bool flag = predicate(tempOverrideSet.items[i].itemName);
				items[i] = (flag ? tempOverrideSet.items[i] : playerPref.items[i]);
			}
		}

		public void ClearSet(CosmeticItem nullItem)
		{
			for (int i = 0; i < 16; i++)
			{
				items[i] = nullItem;
			}
		}

		public bool IsActive(string name)
		{
			int num = 16;
			for (int i = 0; i < num; i++)
			{
				if (items[i].displayName == name)
				{
					return true;
				}
			}
			return false;
		}

		public bool HasItemOfCategory(CosmeticCategory category)
		{
			int num = 16;
			for (int i = 0; i < num; i++)
			{
				if (!items[i].isNullItem && items[i].itemCategory == category)
				{
					return true;
				}
			}
			return false;
		}

		public bool HasItem(string name)
		{
			int num = 16;
			for (int i = 0; i < num; i++)
			{
				if (!items[i].isNullItem && items[i].displayName == name)
				{
					return true;
				}
			}
			return false;
		}

		public bool HasAnyItems()
		{
			if (items == null || items.Length < 1)
			{
				return false;
			}
			for (int i = 0; i < items.Length; i++)
			{
				if (!items[i].isNullItem)
				{
					return true;
				}
			}
			return false;
		}

		public static bool IsSlotLeftHanded(CosmeticSlots slot)
		{
			if (slot != CosmeticSlots.ArmLeft && slot != CosmeticSlots.BackLeft)
			{
				return slot == CosmeticSlots.HandLeft;
			}
			return true;
		}

		public static bool IsSlotRightHanded(CosmeticSlots slot)
		{
			if (slot != CosmeticSlots.ArmRight && slot != CosmeticSlots.BackRight)
			{
				return slot == CosmeticSlots.HandRight;
			}
			return true;
		}

		public static bool IsHoldable(CosmeticItem item)
		{
			return item.isHoldable;
		}

		public static CosmeticSlots OppositeSlot(CosmeticSlots slot)
		{
			return slot switch
			{
				CosmeticSlots.Hat => CosmeticSlots.Hat, 
				CosmeticSlots.Badge => CosmeticSlots.Badge, 
				CosmeticSlots.Face => CosmeticSlots.Face, 
				CosmeticSlots.ArmLeft => CosmeticSlots.ArmRight, 
				CosmeticSlots.ArmRight => CosmeticSlots.ArmLeft, 
				CosmeticSlots.BackLeft => CosmeticSlots.BackRight, 
				CosmeticSlots.BackRight => CosmeticSlots.BackLeft, 
				CosmeticSlots.HandLeft => CosmeticSlots.HandRight, 
				CosmeticSlots.HandRight => CosmeticSlots.HandLeft, 
				CosmeticSlots.Chest => CosmeticSlots.Chest, 
				CosmeticSlots.Fur => CosmeticSlots.Fur, 
				CosmeticSlots.Shirt => CosmeticSlots.Shirt, 
				CosmeticSlots.Pants => CosmeticSlots.Pants, 
				CosmeticSlots.Back => CosmeticSlots.Back, 
				CosmeticSlots.Arms => CosmeticSlots.Arms, 
				CosmeticSlots.TagEffect => CosmeticSlots.TagEffect, 
				_ => CosmeticSlots.Count, 
			};
		}

		public static string SlotPlayerPreferenceName(CosmeticSlots slot)
		{
			return "slot_" + slot;
		}

		private void ActivateCosmetic(CosmeticSet prevSet, VRRig rig, int slotIndex, CosmeticItemRegistry cosmeticsObjectRegistry, BodyDockPositions bDock)
		{
			CosmeticItem cosmeticItem = prevSet.items[slotIndex];
			string itemNameFromDisplayName = instance.GetItemNameFromDisplayName(cosmeticItem.displayName);
			CosmeticItem parentItem = items[slotIndex];
			string itemNameFromDisplayName2 = instance.GetItemNameFromDisplayName(parentItem.displayName);
			BodyDockPositions.DropPositions dropPositions = CosmeticSlotToDropPosition((CosmeticSlots)slotIndex);
			if ((parentItem.itemCategory != CosmeticCategory.None && !CompareCategoryToSavedCosmeticSlots(parentItem.itemCategory, (CosmeticSlots)slotIndex)) || (parentItem.isHoldable && dropPositions == BodyDockPositions.DropPositions.None))
			{
				return;
			}
			CosmeticItemInstance cosmeticItemInstance = null;
			CosmeticItemInstance cosmeticItemInstance2 = null;
			try
			{
				if (itemNameFromDisplayName == itemNameFromDisplayName2)
				{
					if (parentItem.isNullItem)
					{
						return;
					}
					cosmeticItemInstance = cosmeticsObjectRegistry.Cosmetic(parentItem.displayName);
					if (cosmeticItemInstance == null)
					{
						return;
					}
					if (!rig.IsItemAllowed(itemNameFromDisplayName2))
					{
						cosmeticItemInstance.DisableItem((CosmeticSlots)slotIndex);
						return;
					}
					if (parentItem.isHoldable)
					{
						bDock.TransferrableItemEnableAtPosition(parentItem.displayName, dropPositions);
					}
					cosmeticItemInstance.EnableItem((CosmeticSlots)slotIndex, rig);
					PopulateCollectionDisplay(cosmeticItemInstance, parentItem, rig);
					return;
				}
				if (!cosmeticItem.isNullItem)
				{
					if (cosmeticItem.isHoldable)
					{
						bDock.TransferrableItemDisableAtPosition(dropPositions);
					}
					cosmeticItemInstance2 = cosmeticsObjectRegistry.Cosmetic(cosmeticItem.displayName);
					cosmeticItemInstance2?.DisableItem((CosmeticSlots)slotIndex);
				}
				if (parentItem.isNullItem)
				{
					return;
				}
				if (parentItem.isHoldable)
				{
					bDock.TransferrableItemEnableAtPosition(parentItem.displayName, dropPositions);
				}
				cosmeticItemInstance = cosmeticsObjectRegistry.Cosmetic(parentItem.displayName);
				if (rig.IsItemAllowed(itemNameFromDisplayName2) && cosmeticItemInstance != null)
				{
					cosmeticItemInstance.EnableItem((CosmeticSlots)slotIndex, rig);
					if (rig.isLocal && (slotIndex == 0 || slotIndex == 2))
					{
						PlayerPrefFlags.TouchIf(PlayerPrefFlags.Flag.SHOW_1P_COSMETICS, value: false);
					}
					PopulateCollectionDisplay(cosmeticItemInstance, parentItem, rig);
				}
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
				try
				{
					if (cosmeticItemInstance2 != null && !cosmeticItem.isHoldable)
					{
						cosmeticItemInstance2.DisableItem((CosmeticSlots)slotIndex);
					}
					if (cosmeticItemInstance != null && !parentItem.isHoldable)
					{
						cosmeticItemInstance.DisableItem((CosmeticSlots)slotIndex);
					}
				}
				catch
				{
				}
			}
		}

		public void ActivateCosmetics(CosmeticSet prevSet, VRRig rig, BodyDockPositions bDock, CosmeticItemRegistry cosmeticsObjectRegistry)
		{
			int num = 16;
			for (int i = 0; i < num; i++)
			{
				ActivateCosmetic(prevSet, rig, i, cosmeticsObjectRegistry, bDock);
			}
			OnSetActivated(prevSet, this, rig.creator);
		}

		private static void PopulateCollectionDisplay(CosmeticItemInstance instance, CosmeticItem parentItem, VRRig rig)
		{
			if (parentItem.collectionSlotCount <= 0 || !hasInstance)
			{
				return;
			}
			GameObject gameObject = FindFirstPickupableVariantRoot(instance.objects) ?? FindFirstPickupableVariantRoot(instance.holdableObjects);
			if (gameObject == null)
			{
				CosmeticCollectionDisplay.GetAllForParent(rig, parentItem.itemName, scratchDisplayList);
				for (int i = 0; i < scratchDisplayList.Count; i++)
				{
					CosmeticCollectionDisplay cosmeticCollectionDisplay = scratchDisplayList[i];
					if (cosmeticCollectionDisplay != null && cosmeticCollectionDisplay.gameObject != null && cosmeticCollectionDisplay.GetComponent<PickupableVariant>() != null)
					{
						gameObject = cosmeticCollectionDisplay.gameObject;
						break;
					}
				}
			}
			if (gameObject == null)
			{
				gameObject = FindFirstNonNull(instance.objects) ?? FindFirstNonNull(instance.holdableObjects);
			}
			if (gameObject != null)
			{
				CosmeticCollectionDisplay.DestroyAllForParentExcept(rig, parentItem.itemName, gameObject);
				RemoveStaleDisplaysForParent(rig, parentItem.itemName, gameObject);
				PopulateCollectionDisplayOnRoot(gameObject, parentItem, rig);
			}
		}

		private static void RemoveStaleDisplaysForParent(VRRig rig, string parentPlayFabID, GameObject host)
		{
			if (rig == null || string.IsNullOrEmpty(parentPlayFabID))
			{
				return;
			}
			CosmeticCollectionDisplay[] componentsInChildren = rig.GetComponentsInChildren<CosmeticCollectionDisplay>(includeInactive: true);
			foreach (CosmeticCollectionDisplay cosmeticCollectionDisplay in componentsInChildren)
			{
				if (!(cosmeticCollectionDisplay == null) && !(cosmeticCollectionDisplay.gameObject == host) && !(cosmeticCollectionDisplay.ParentPlayFabID != parentPlayFabID))
				{
					UnityEngine.Object.Destroy(cosmeticCollectionDisplay);
				}
			}
		}

		private static GameObject FindFirstPickupableVariantRoot(IList<GameObject> roots)
		{
			if (roots == null)
			{
				return null;
			}
			for (int i = 0; i < roots.Count; i++)
			{
				GameObject gameObject = roots[i];
				if (!(gameObject == null))
				{
					PickupableVariant componentInChildren = gameObject.GetComponentInChildren<PickupableVariant>(includeInactive: true);
					if (componentInChildren != null)
					{
						return componentInChildren.gameObject;
					}
				}
			}
			return null;
		}

		private static GameObject FindFirstNonNull(IList<GameObject> roots)
		{
			if (roots == null)
			{
				return null;
			}
			for (int i = 0; i < roots.Count; i++)
			{
				if (roots[i] != null)
				{
					return roots[i];
				}
			}
			return null;
		}

		public void DeactivateAllCosmetcs(BodyDockPositions bDock, CosmeticItem nullItem, CosmeticItemRegistry cosmeticObjectRegistry)
		{
			bDock.DisableAllTransferableItems();
			int num = 16;
			for (int i = 0; i < num; i++)
			{
				CosmeticItem cosmeticItem = items[i];
				if (!cosmeticItem.isNullItem)
				{
					CosmeticSlots cosmeticSlot = (CosmeticSlots)i;
					cosmeticObjectRegistry.Cosmetic(cosmeticItem.displayName)?.DisableItem(cosmeticSlot);
					items[i] = nullItem;
				}
			}
		}

		public void LoadFromPlayerPreferences(CosmeticsController controller)
		{
			int num = 16;
			for (int i = 0; i < num; i++)
			{
				CosmeticSlots slot = (CosmeticSlots)i;
				string text = PlayerPrefs.GetString(SlotPlayerPreferenceName(slot), "NOTHING");
				if (text == "null" || text == "NOTHING")
				{
					items[i] = controller.nullItem;
					continue;
				}
				CosmeticItem item = controller.GetItemFromDict(text);
				if (item.isNullItem)
				{
					Debug.Log("LoadFromPlayerPreferences: Could not find item stored in player prefs: \"" + text + "\"");
					items[i] = controller.nullItem;
				}
				else if (item.itemName == "Slingshot")
				{
					items[i] = controller.nullItem;
					PlayerPrefs.SetString(SlotPlayerPreferenceName(slot), "NOTHING");
				}
				else if (!CompareCategoryToSavedCosmeticSlots(item.itemCategory, slot))
				{
					items[i] = controller.nullItem;
				}
				else if (controller.unlockedCosmetics.FindIndex((CosmeticItem x) => item.itemName == x.itemName) >= 0)
				{
					items[i] = item;
				}
				else
				{
					items[i] = controller.nullItem;
				}
			}
		}

		public void ParseSetFromString(CosmeticsController controller, string setString, out Vector3 color)
		{
			color = defaultColor;
			if (setString.IsNullOrEmpty())
			{
				ClearSet(controller.nullItem);
				GTDev.LogError("CosmeticsController ParseSetFromString: null string");
				return;
			}
			int num = 16;
			OutfitData outfitData = new OutfitData();
			try
			{
				outfitData = JsonUtility.FromJson<OutfitData>(setString);
				color = outfitData.color;
			}
			catch (Exception)
			{
				char separator = ',';
				if (controller.outfitSystemConfig != null)
				{
					separator = controller.outfitSystemConfig.itemSeparator;
				}
				string[] array = setString.Split(separator, num);
				if (array == null || array.Length > num)
				{
					ClearSet(controller.nullItem);
					GTDev.LogError($"CosmeticsController ParseSetFromString: wrong number of slots {array.Length} {setString}");
					return;
				}
				outfitData.Clear();
				outfitData.itemIDs = new List<string>(array);
			}
			try
			{
				for (int i = 0; i < num; i++)
				{
					CosmeticSlots slot = (CosmeticSlots)i;
					string text = ((i < outfitData.itemIDs.Count) ? outfitData.itemIDs[i] : "null");
					if (text.IsNullOrEmpty() || text == "null" || text == "NOTHING")
					{
						items[i] = controller.nullItem;
						continue;
					}
					CosmeticItem item = controller.GetItemFromDict(text);
					if (item.isNullItem)
					{
						GTDev.Log("CosmeticsController ParseSetFromString: Could not find item stored in player prefs: \"" + text + "\"");
						items[i] = controller.nullItem;
					}
					else if (!CompareCategoryToSavedCosmeticSlots(item.itemCategory, slot))
					{
						items[i] = controller.nullItem;
					}
					else if (controller.unlockedCosmetics.FindIndex((CosmeticItem x) => item.itemName == x.itemName) >= 0)
					{
						items[i] = item;
					}
					else
					{
						items[i] = controller.nullItem;
					}
				}
			}
			catch (Exception ex2)
			{
				ClearSet(controller.nullItem);
				GTDev.LogError("CosmeticsController: Issue parsing saved outfit string: " + ex2.Message);
			}
		}

		public string[] ToDisplayNameArray()
		{
			int num = 16;
			for (int i = 0; i < num; i++)
			{
				returnArray[i] = (string.IsNullOrEmpty(items[i].displayName) ? "null" : items[i].displayName);
			}
			return returnArray;
		}

		public int[] ToPackedIDArray()
		{
			int num = 0;
			int num2 = 0;
			int num3 = 16;
			for (int i = 0; i < num3; i++)
			{
				if (!items[i].isNullItem && !string.IsNullOrEmpty(items[i].itemName) && (items[i].itemName.Length == 6 || items[i].itemName == "Slingshot"))
				{
					num |= 1 << i;
					num2++;
				}
			}
			if (num == 0)
			{
				return intArrays[0];
			}
			int[] array = intArrays[num2 + 1];
			array[0] = num;
			int num4 = 1;
			for (int j = 0; j < num3; j++)
			{
				if ((num & (1 << j)) != 0)
				{
					string itemName = items[j].itemName;
					if (itemName == "Slingshot")
					{
						array[num4] = -55;
					}
					else
					{
						array[num4] = itemName[0] - 65 + 26 * (itemName[1] - 65 + 26 * (itemName[2] - 65 + 26 * (itemName[3] - 65 + 26 * (itemName[4] - 65))));
					}
					num4++;
				}
			}
			return array;
		}

		public string[] HoldableDisplayNames(bool leftHoldables)
		{
			int num = 16;
			int num2 = 0;
			for (int i = 0; i < num; i++)
			{
				if (items[i].isHoldable && items[i].isHoldable && items[i].itemCategory != CosmeticCategory.Chest)
				{
					if (leftHoldables && BodyDockPositions.IsPositionLeft(CosmeticSlotToDropPosition((CosmeticSlots)i)))
					{
						num2++;
					}
					else if (!leftHoldables && !BodyDockPositions.IsPositionLeft(CosmeticSlotToDropPosition((CosmeticSlots)i)))
					{
						num2++;
					}
				}
			}
			if (num2 == 0)
			{
				return null;
			}
			int num3 = 0;
			string[] array = new string[num2];
			for (int j = 0; j < num; j++)
			{
				if (items[j].isHoldable)
				{
					if (leftHoldables && BodyDockPositions.IsPositionLeft(CosmeticSlotToDropPosition((CosmeticSlots)j)))
					{
						array[num3] = items[j].displayName;
						num3++;
					}
					else if (!leftHoldables && !BodyDockPositions.IsPositionLeft(CosmeticSlotToDropPosition((CosmeticSlots)j)))
					{
						array[num3] = items[j].displayName;
						num3++;
					}
				}
			}
			return array;
		}

		public bool[] ToOnRightSideArray()
		{
			int num = 16;
			bool[] array = new bool[num];
			for (int i = 0; i < num; i++)
			{
				if (items[i].isHoldable && items[i].itemCategory != CosmeticCategory.Chest)
				{
					array[i] = !BodyDockPositions.IsPositionLeft(CosmeticSlotToDropPosition((CosmeticSlots)i));
				}
				else
				{
					array[i] = false;
				}
			}
			return array;
		}
	}

	[Serializable]
	public struct CollectionState
	{
		public int activeIndex;

		public int visibleMask;
	}

	[Serializable]
	public struct CosmeticItem
	{
		[Tooltip("Should match the spreadsheet item name.")]
		public string itemName;

		[Tooltip("Determines what wardrobe section the item will show up in.")]
		public CosmeticCategory itemCategory;

		[Tooltip("If this is a holdable item.")]
		public bool isHoldable;

		[Tooltip("If this is a throwable item and hidden on the wardrobe.")]
		public bool isThrowable;

		[Tooltip("Icon shown in the store menus & hunt watch.")]
		public Sprite itemPicture;

		public string displayName;

		public string itemPictureResourceString;

		[Tooltip("The name shown on the store checkout screen.")]
		public string overrideDisplayName;

		[NonSerialized]
		[DebugReadout]
		public int cost;

		[NonSerialized]
		[DebugReadout]
		public string[] bundledItems;

		[NonSerialized]
		[DebugReadout]
		public bool canTryOn;

		[Tooltip("Set to true if the item takes up both left and right wearable hand slots at the same time. Used for things like mittens/gloves.")]
		public bool bothHandsHoldable;

		public bool bLoadsFromResources;

		public bool bUsesMeshAtlas;

		public Vector3 rotationOffset;

		public Vector3 positionOffset;

		public string meshAtlasResourceString;

		public string meshResourceString;

		public string materialResourceString;

		[HideInInspector]
		public bool isNullItem;

		[NonSerialized]
		public CosmeticCollectionParentLink[] collectionParentLinks;

		[NonSerialized]
		public int collectionSlotCount;

		[NonSerialized]
		public bool collectionIsCycling;

		[NonSerialized]
		public bool collectionUsesIndexTargeting;

		[NonSerialized]
		public string appliedCosmeticPlayFabID;

		public bool IsCollectable
		{
			get
			{
				CosmeticCollectionParentLink[] array = collectionParentLinks;
				if (array != null)
				{
					return array.Length > 0;
				}
				return false;
			}
		}

		public bool IsCollectableOf(string parentPlayFabID)
		{
			if (collectionParentLinks == null)
			{
				return false;
			}
			for (int i = 0; i < collectionParentLinks.Length; i++)
			{
				if (collectionParentLinks[i].parentPlayFabID == parentPlayFabID)
				{
					return true;
				}
			}
			return false;
		}

		public int GetTargetSlotIndexForParent(string parentPlayFabID)
		{
			if (collectionParentLinks != null)
			{
				for (int i = 0; i < collectionParentLinks.Length; i++)
				{
					if (collectionParentLinks[i].parentPlayFabID == parentPlayFabID)
					{
						return collectionParentLinks[i].targetSlotIndex;
					}
				}
			}
			return -1;
		}

		public int GetSeriesIndexForParent(string parentPlayFabID)
		{
			if (collectionParentLinks != null)
			{
				for (int i = 0; i < collectionParentLinks.Length; i++)
				{
					if (collectionParentLinks[i].parentPlayFabID == parentPlayFabID)
					{
						return collectionParentLinks[i].seriesIndex;
					}
				}
			}
			return -1;
		}
	}

	[Serializable]
	public class IAPRequestBody
	{
		public string sku;

		public string mothershipId;

		public string mothershipToken;

		public string mothershipEnvId;

		public string mothershipDeploymentId;

		public Dictionary<string, string> customTags;
	}

	private class ValidatedCreatorCode
	{
		public string terminalId { get; set; }

		public string memberCode { get; set; }

		public string groupId { get; set; }
	}

	public enum EWearingCosmeticSet
	{
		NotASet,
		NotWearing,
		Partial,
		Complete
	}

	public class OutfitData
	{
		public const int OUTFIT_DATA_VERSION = 1;

		public int version;

		public List<string> itemIDs;

		public Vector3 color;

		public OutfitData()
		{
			version = 1;
			itemIDs = new List<string>(16);
			color = defaultColor;
		}

		public void Clear()
		{
			itemIDs.Clear();
			color = defaultColor;
		}
	}

	[FormerlySerializedAs("v2AllCosmeticsInfoAssetRef")]
	[FormerlySerializedAs("newSysAllCosmeticsAssetRef")]
	[SerializeField]
	public GTAssetRef<AllCosmeticsArraySO> v2_allCosmeticsInfoAssetRef;

	private readonly Dictionary<string, CosmeticInfoV2> _allCosmeticsDictV2 = new Dictionary<string, CosmeticInfoV2>();

	public Action V2_allCosmeticsInfoAssetRef_OnPostLoad;

	public const int maximumTransferrableItems = 5;

	[OnEnterPlay_SetNull]
	public static volatile CosmeticsController instance;

	public static Action<string, string> PushTerminalMessage;

	public Action V2_OnGetCosmeticsPlayFabCatalogData_PostSuccess;

	public Action OnGetCurrency;

	private string purchaseLocation;

	[FormerlySerializedAs("allCosmetics")]
	[SerializeField]
	private List<CosmeticItem> _allCosmetics;

	public Dictionary<string, CosmeticItem> _allCosmeticsDict = new Dictionary<string, CosmeticItem>(2048);

	public Dictionary<string, string> _allCosmeticsItemIDsfromDisplayNamesDict = new Dictionary<string, string>(2048);

	public CosmeticItem nullItem;

	public string catalog;

	private string[] tempStringArray;

	private CosmeticItem tempItem;

	private VRRigAnchorOverrides anchorOverrides;

	public List<CatalogItem> catalogItems;

	public bool tryGetCatalogTwice;

	private bool catalogRequestInFlight;

	private bool catalogRequestRerunQueued;

	public CustomMapCosmeticsData customMapCosmeticsData;

	[NonSerialized]
	public CosmeticSet tryOnSet = new CosmeticSet();

	public int numFittingRoomButtons = 12;

	public List<FittingRoom> fittingRooms = new List<FittingRoom>();

	public CosmeticStand[] cosmeticStands;

	public List<CosmeticItem> currentCart = new List<CosmeticItem>();

	public PurchaseItemStages currentPurchaseItemStage;

	public List<ItemCheckout> itemCheckouts = new List<ItemCheckout>();

	public CosmeticItem itemToBuy;

	private bool foundCosmetic;

	private int attempts;

	private string finalLine;

	private string leftCheckoutPurchaseButtonString;

	private string rightCheckoutPurchaseButtonString;

	private bool leftCheckoutPurchaseButtonOn;

	private bool rightCheckoutPurchaseButtonOn;

	private bool isLastHandTouchedLeft;

	private CosmeticSet cachedSet = new CosmeticSet();

	public readonly List<WardrobeInstance> wardrobes = new List<WardrobeInstance>();

	public List<CosmeticItem> unlockedCosmetics = new List<CosmeticItem>(2048);

	public List<CosmeticItem> unlockedHats = new List<CosmeticItem>(512);

	public List<CosmeticItem> unlockedFaces = new List<CosmeticItem>(512);

	public List<CosmeticItem> unlockedBadges = new List<CosmeticItem>(512);

	public List<CosmeticItem> unlockedPaws = new List<CosmeticItem>(512);

	public List<CosmeticItem> unlockedChests = new List<CosmeticItem>(512);

	public List<CosmeticItem> unlockedFurs = new List<CosmeticItem>(512);

	public List<CosmeticItem> unlockedShirts = new List<CosmeticItem>(512);

	public List<CosmeticItem> unlockedPants = new List<CosmeticItem>(512);

	public List<CosmeticItem> unlockedBacks = new List<CosmeticItem>(512);

	public List<CosmeticItem> unlockedArms = new List<CosmeticItem>(512);

	public List<CosmeticItem> unlockedTagFX = new List<CosmeticItem>(512);

	public List<CosmeticItem> unlockedThrowables = new List<CosmeticItem>(512);

	public int[] cosmeticsPages = new int[11];

	private List<CosmeticItem>[] itemLists = new List<CosmeticItem>[11];

	private int wardrobeType;

	[NonSerialized]
	public CosmeticSet currentWornSet = new CosmeticSet();

	[NonSerialized]
	public CosmeticSet tempUnlockedSet = new CosmeticSet();

	[NonSerialized]
	public CosmeticSet activeMergedSet = new CosmeticSet();

	[NonSerialized]
	public CosmeticItem tryOnCollectableItem;

	public string concatStringCosmeticsAllowed = "";

	public Action OnCosmeticsUpdated;

	[NonSerialized]
	public Dictionary<string, List<CosmeticItem>> collectablesByParentID = new Dictionary<string, List<CosmeticItem>>();

	[NonSerialized]
	public Dictionary<(VRRig rig, string parentID), CollectionState> localCycleStates = new Dictionary<(VRRig, string), CollectionState>();

	private static readonly List<CosmeticCollectionDisplay> scratchDisplayList = new List<CosmeticCollectionDisplay>();

	private static int[] cycleStatesArray = Array.Empty<int>();

	private static readonly List<CosmeticItem> scratchCanonicalCollectables = new List<CosmeticItem>();

	private static readonly List<CosmeticItem> scratchCanonicalIndexList = new List<CosmeticItem>();

	public int currencyBalance;

	public string currencyName;

	public List<CurrencyBoard> currencyBoards;

	public string itemToPurchase;

	public bool buyingBundle;

	public bool confirmedDidntPlayInBeta;

	public bool playedInBeta;

	public bool gotMyDaily;

	public bool checkedDaily;

	public string currentPurchaseID;

	public bool hasPrice;

	private int searchIndex;

	private int iterator;

	private CosmeticItem cosmeticItemVar;

	[SerializeField]
	private CosmeticSO m_earlyAccessSupporterPackCosmeticSO;

	public EarlyAccessButton[] earlyAccessButtons;

	private BundleList bundleList = new BundleList();

	public string BundleSkuName = "2024_i_lava_you_pack";

	public string BundlePlayfabItemName = "LSABG.";

	public int BundleShinyRocks = 10000;

	public DateTime currentTime;

	public string lastDailyLogin;

	public UserDataRecord userDataRecord;

	public int secondsUntilTomorrow;

	public float secondsToWaitToCheckDaily = 10f;

	private int updateCosmeticsRetries;

	private int maxUpdateCosmeticsRetries;

	private GetUserInventoryResult latestInventory;

	private string returnString;

	private bool checkoutCartButtonPressedWithLeft;

	private ValidatedCreatorCode validatedCreatorCode;

	private Callback<MicroTxnAuthorizationResponse_t> _steamMicroTransactionAuthorizationResponse;

	private static readonly List<CosmeticSlots> _g_default_outAppliedSlotsList_for_applyCosmeticItemToSet = new List<CosmeticSlots>(16);

	[SerializeField]
	private CosmeticOutfitSystemConfig outfitSystemConfig;

	private CosmeticSet[] savedOutfits;

	private Vector3[] savedColors;

	private static OutfitData outfitDataTemp;

	private string outfitStringMothership = string.Empty;

	private string outfitStringPendingSave = string.Empty;

	private static bool saveOutfitInProgress = false;

	private static bool loadOutfitsInProgress = false;

	private static bool loadedSavedOutfits = false;

	private static int selectedOutfit = 0;

	private static int maxOutfits = -1;

	private static readonly Vector3 defaultColor = new Vector3(0f, 0f, 0f);

	public Action OnOutfitsUpdated;

	public static Action<float, float, float> OnPlayerColorSet;

	private StringBuilder sb = new StringBuilder(256);

	public CosmeticInfoV2[] v2_allCosmetics { get; private set; }

	public bool v2_allCosmeticsInfoAssetRef_isLoaded { get; private set; }

	public bool v2_isCosmeticPlayFabCatalogDataLoaded { get; private set; }

	[field: OnEnterPlay_Set(false)]
	public static bool hasInstance { get; private set; }

	public string PurchaseLocation
	{
		get
		{
			return purchaseLocation;
		}
		set
		{
			purchaseLocation = value;
		}
	}

	public List<CosmeticItem> allCosmetics
	{
		get
		{
			return _allCosmetics;
		}
		set
		{
			_allCosmetics = value;
		}
	}

	public bool allCosmeticsDict_isInitialized { get; private set; }

	public Dictionary<string, CosmeticItem> allCosmeticsDict => _allCosmeticsDict;

	public bool allCosmeticsItemIDsfromDisplayNamesDict_isInitialized { get; private set; }

	public Dictionary<string, string> allCosmeticsItemIDsfromDisplayNamesDict => _allCosmeticsItemIDsfromDisplayNamesDict;

	public CosmeticAnchorAntiIntersectOffsets defaultClipOffsets => CosmeticAnchorAntiIntersectOffsets.Identity;

	public bool isHidingCosmeticsFromRemotePlayers { get; private set; }

	public int CurrencyBalance => currencyBalance;

	public CosmeticSO EarlyAccessSupporterPackCosmeticSO => m_earlyAccessSupporterPackCosmeticSO;

	public static int SelectedOutfit => selectedOutfit;

	private void V2Awake()
	{
		_allCosmetics = null;
		StartCoroutine(V2_allCosmeticsInfoAssetRefSO_LoadCoroutine());
	}

	private IEnumerator V2_allCosmeticsInfoAssetRefSO_LoadCoroutine()
	{
		while (!PlayFabAuthenticator.instance)
		{
			yield return new WaitForSecondsRealtime(1f);
		}
		float[] retryWaitTimes = new float[15]
		{
			1f, 2f, 4f, 4f, 10f, 10f, 10f, 10f, 10f, 10f,
			10f, 10f, 10f, 10f, 30f
		};
		int retryCount = 0;
		AsyncOperationHandle<AllCosmeticsArraySO> newSysAllCosmeticsAsyncOp;
		while (true)
		{
			Debug.Log($"Attempting to load runtime key \"{v2_allCosmeticsInfoAssetRef.RuntimeKey}\" " + $"(Attempt: {retryCount + 1})");
			newSysAllCosmeticsAsyncOp = v2_allCosmeticsInfoAssetRef.LoadAssetAsync();
			yield return newSysAllCosmeticsAsyncOp;
			if (ApplicationQuittingState.IsQuitting)
			{
				yield break;
			}
			if (!newSysAllCosmeticsAsyncOp.IsValid())
			{
				Debug.LogError("`newSysAllCosmeticsAsyncOp` (should never happen) became invalid some how.");
			}
			if (newSysAllCosmeticsAsyncOp.Status == AsyncOperationStatus.Succeeded)
			{
				break;
			}
			Debug.LogError($"Failed to load \"{v2_allCosmeticsInfoAssetRef.RuntimeKey}\". " + "Error: " + newSysAllCosmeticsAsyncOp.OperationException.Message);
			float time = retryWaitTimes[Mathf.Min(retryCount, retryWaitTimes.Length - 1)];
			yield return new WaitForSecondsRealtime(time);
			retryCount++;
		}
		V2_allCosmeticsInfoAssetRef_LoadSucceeded(newSysAllCosmeticsAsyncOp.Result);
	}

	private void V2_allCosmeticsInfoAssetRef_LoadSucceeded(AllCosmeticsArraySO allCosmeticsSO)
	{
		v2_allCosmetics = new CosmeticInfoV2[allCosmeticsSO.sturdyAssetRefs.Length];
		for (int i = 0; i < allCosmeticsSO.sturdyAssetRefs.Length; i++)
		{
			v2_allCosmetics[i] = allCosmeticsSO.sturdyAssetRefs[i].obj.info;
		}
		_allCosmetics = new List<CosmeticItem>(allCosmeticsSO.sturdyAssetRefs.Length);
		for (int j = 0; j < v2_allCosmetics.Length; j++)
		{
			CosmeticInfoV2 value = v2_allCosmetics[j];
			string playFabID = value.playFabID;
			_allCosmeticsDictV2[playFabID] = value;
			CosmeticItem cosmeticItem = new CosmeticItem
			{
				itemName = playFabID,
				itemCategory = value.category,
				isHoldable = value.hasHoldableParts,
				displayName = playFabID,
				itemPicture = value.icon,
				overrideDisplayName = value.displayName,
				bothHandsHoldable = value.usesBothHandSlots,
				isNullItem = false,
				collectionParentLinks = value.collectionParentLinks
			};
			CosmeticCollectionSlotDefinition[] collectionSlots = value.collectionSlots;
			cosmeticItem.collectionSlotCount = ((collectionSlots != null) ? collectionSlots.Length : 0);
			cosmeticItem.collectionIsCycling = value.collectionIsCycling;
			cosmeticItem.collectionUsesIndexTargeting = value.collectionUsesIndexTargeting;
			cosmeticItem.appliedCosmeticPlayFabID = value.appliedCosmeticPlayFabID ?? string.Empty;
			CosmeticItem item = cosmeticItem;
			_allCosmetics.Add(item);
		}
		collectablesByParentID = new Dictionary<string, List<CosmeticItem>>();
		for (int k = 0; k < _allCosmetics.Count; k++)
		{
			CosmeticCollectionParentLink[] collectionParentLinks = _allCosmetics[k].collectionParentLinks;
			if (collectionParentLinks == null)
			{
				continue;
			}
			for (int l = 0; l < collectionParentLinks.Length; l++)
			{
				string parentPlayFabID = collectionParentLinks[l].parentPlayFabID;
				if (!string.IsNullOrEmpty(parentPlayFabID))
				{
					if (!collectablesByParentID.TryGetValue(parentPlayFabID, out var value2))
					{
						value2 = new List<CosmeticItem>();
						collectablesByParentID[parentPlayFabID] = value2;
					}
					value2.Add(_allCosmetics[k]);
				}
			}
		}
		v2_allCosmeticsInfoAssetRef_isLoaded = true;
		V2_allCosmeticsInfoAssetRef_OnPostLoad?.Invoke();
	}

	public bool TryGetCosmeticInfoV2(string playFabId, out CosmeticInfoV2 cosmeticInfo)
	{
		return _allCosmeticsDictV2.TryGetValue(playFabId, out cosmeticInfo);
	}

	private void V2_ConformCosmeticItemV1DisplayName(ref CosmeticItem cosmetic)
	{
		if (!(cosmetic.itemName == cosmetic.displayName))
		{
			cosmetic.overrideDisplayName = cosmetic.displayName;
			cosmetic.displayName = cosmetic.itemName;
		}
	}

	internal void InitializeCosmeticStands()
	{
		CosmeticStand[] array = cosmeticStands;
		foreach (CosmeticStand cosmeticStand in array)
		{
			if (cosmeticStand != null)
			{
				cosmeticStand.InitializeCosmetic();
			}
		}
	}

	private string ConsumePurchaseLocation()
	{
		if (purchaseLocation.IsNullOrEmpty())
		{
			return GorillaTagger.Instance.offlineVRRig.zoneEntity.currentZone.ToString();
		}
		string result = purchaseLocation;
		purchaseLocation = null;
		return result;
	}

	public void AddWardrobeInstance(WardrobeInstance instance)
	{
		wardrobes.Add(instance);
		UpdateWardrobeModelsAndButtons();
	}

	public void RemoveWardrobeInstance(WardrobeInstance instance)
	{
		wardrobes.Remove(instance);
	}

	public bool IsOwnedByPlayFabID(string playFabID)
	{
		return unlockedCosmetics.FindIndex((CosmeticItem x) => x.itemName == playFabID) >= 0;
	}

	public int GetOwnedCollectableCount(string parentPlayFabID)
	{
		int num = 0;
		for (int i = 0; i < unlockedCosmetics.Count; i++)
		{
			if (unlockedCosmetics[i].IsCollectableOf(parentPlayFabID))
			{
				num++;
			}
		}
		return num;
	}

	private int GetRemainingCollectableSlots(string parentPlayFabID)
	{
		if (!allCosmeticsDict.TryGetValue(parentPlayFabID, out var value))
		{
			return 0;
		}
		List<CosmeticItem> value2;
		int num = ((!value.collectionIsCycling) ? value.collectionSlotCount : (collectablesByParentID.TryGetValue(parentPlayFabID, out value2) ? value2.Count : 0)) - GetOwnedCollectableCount(parentPlayFabID);
		if (num <= 0)
		{
			return 0;
		}
		return num;
	}

	public bool CanPurchaseCollectable(string collectablePlayFabID)
	{
		if (!allCosmeticsDict.TryGetValue(collectablePlayFabID, out var value))
		{
			return false;
		}
		if (!value.IsCollectable)
		{
			return true;
		}
		CosmeticCollectionParentLink[] collectionParentLinks = value.collectionParentLinks;
		for (int i = 0; i < collectionParentLinks.Length; i++)
		{
			string parentPlayFabID = collectionParentLinks[i].parentPlayFabID;
			if (!string.IsNullOrEmpty(parentPlayFabID) && IsOwnedByPlayFabID(parentPlayFabID) && GetRemainingCollectableSlots(parentPlayFabID) > 0)
			{
				return true;
			}
		}
		return false;
	}

	public void BuildCanonicalCollectableOrder(string parentPlayFabID, List<CosmeticItem> result)
	{
		result.Clear();
		if (string.IsNullOrEmpty(parentPlayFabID) || !collectablesByParentID.TryGetValue(parentPlayFabID, out var value))
		{
			return;
		}
		result.AddRange(value);
		CosmeticInfoV2 cosmeticInfo;
		bool seriesOrder = TryGetCosmeticInfoV2(parentPlayFabID, out cosmeticInfo) && cosmeticInfo.collectionIsCycling && cosmeticInfo.collectionUsesSeriesOrder;
		result.Sort(delegate(CosmeticItem a, CosmeticItem b)
		{
			if (seriesOrder)
			{
				int num = a.GetSeriesIndexForParent(parentPlayFabID);
				int num2 = b.GetSeriesIndexForParent(parentPlayFabID);
				if (num < 0)
				{
					num = int.MaxValue;
				}
				if (num2 < 0)
				{
					num2 = int.MaxValue;
				}
				if (num != num2)
				{
					return num.CompareTo(num2);
				}
			}
			return string.CompareOrdinal(a.itemName, b.itemName);
		});
	}

	public int GetCanonicalCollectableIndex(string parentPlayFabID, string itemName)
	{
		BuildCanonicalCollectableOrder(parentPlayFabID, scratchCanonicalIndexList);
		for (int i = 0; i < scratchCanonicalIndexList.Count; i++)
		{
			if (scratchCanonicalIndexList[i].itemName == itemName)
			{
				return i;
			}
		}
		return -1;
	}

	public static void PopulateCollectionDisplayOnRoot(GameObject rootObj, CosmeticItem parentItem, VRRig rig)
	{
		if (rootObj == null || parentItem.collectionSlotCount <= 0 || !hasInstance || !instance.TryGetCosmeticInfoV2(parentItem.itemName, out var cosmeticInfo) || cosmeticInfo.collectionSlots == null || cosmeticInfo.collectionSlots.Length == 0)
		{
			return;
		}
		if (!rootObj.TryGetComponent<CosmeticCollectionDisplay>(out var component))
		{
			component = rootObj.AddComponent<CosmeticCollectionDisplay>();
		}
		List<CosmeticItem> list = new List<CosmeticItem>();
		bool flag = false;
		if (rig.isLocal)
		{
			instance.BuildCanonicalCollectableOrder(parentItem.itemName, scratchCanonicalCollectables);
			CosmeticItem cosmeticItem = instance.tryOnCollectableItem;
			flag = !cosmeticItem.isNullItem && cosmeticItem.IsCollectableOf(parentItem.itemName) && VRRig.LocalRig != null && VRRig.LocalRig.inTryOnRoom;
			for (int i = 0; i < scratchCanonicalCollectables.Count; i++)
			{
				CosmeticItem item = scratchCanonicalCollectables[i];
				bool num = instance.IsOwnedByPlayFabID(item.itemName);
				bool flag2 = flag && item.itemName == cosmeticItem.itemName;
				if (num || flag2)
				{
					list.Add(item);
				}
			}
			if (component.ContentMatches(list))
			{
				CosmeticCollectionDisplay.Register(rig, parentItem.itemName, component, rig.isLocal);
				return;
			}
		}
		else
		{
			instance.BuildCanonicalCollectableOrder(parentItem.itemName, list);
			if (component.ContentMatches(list))
			{
				CosmeticCollectionDisplay.Register(rig, parentItem.itemName, component, rig.isLocal);
				if (rig.remoteCycleStates.TryGetValue(parentItem.itemName, out var value))
				{
					component.SetVisibleMask(value.visibleMask);
					component.SetActiveIndex(value.activeIndex);
				}
				return;
			}
		}
		component.Populate(list, cosmeticInfo, rootObj.transform);
		if (flag)
		{
			component.PersistLocalState();
		}
		CosmeticCollectionDisplay.Register(rig, parentItem.itemName, component, rig.isLocal);
		CollectionState value3;
		if (rig.isLocal)
		{
			if (instance.localCycleStates.TryGetValue((rig, parentItem.itemName), out var value2))
			{
				component.SetVisibleMask(value2.visibleMask);
				component.SetActiveIndex(value2.activeIndex);
			}
		}
		else if (rig.remoteCycleStates.TryGetValue(parentItem.itemName, out value3))
		{
			component.SetVisibleMask(value3.visibleMask);
			component.SetActiveIndex(value3.activeIndex);
		}
	}

	public void Awake()
	{
		if (instance == null)
		{
			instance = this;
			hasInstance = true;
		}
		else if (instance != this)
		{
			UnityEngine.Object.Destroy(base.gameObject);
			return;
		}
		V2Awake();
		if (base.gameObject.activeSelf)
		{
			catalog = "DLC";
			currencyName = "SR";
			nullItem = default(CosmeticItem);
			nullItem.itemName = "null";
			nullItem.displayName = "NOTHING";
			nullItem.itemPicture = Resources.Load<Sprite>("CosmeticNull_Icon");
			nullItem.itemPictureResourceString = "";
			nullItem.overrideDisplayName = "NOTHING";
			nullItem.meshAtlasResourceString = "";
			nullItem.meshResourceString = "";
			nullItem.materialResourceString = "";
			nullItem.isNullItem = true;
			_allCosmeticsDict[nullItem.itemName] = nullItem;
			_allCosmeticsItemIDsfromDisplayNamesDict[nullItem.displayName] = nullItem.itemName;
			tryOnCollectableItem = nullItem;
			for (int i = 0; i < 16; i++)
			{
				tryOnSet.items[i] = nullItem;
				tempUnlockedSet.items[i] = nullItem;
				activeMergedSet.items[i] = nullItem;
			}
			cosmeticsPages[0] = 0;
			cosmeticsPages[1] = 0;
			cosmeticsPages[2] = 0;
			cosmeticsPages[3] = 0;
			cosmeticsPages[4] = 0;
			cosmeticsPages[5] = 0;
			cosmeticsPages[6] = 0;
			cosmeticsPages[7] = 0;
			cosmeticsPages[8] = 0;
			cosmeticsPages[9] = 0;
			cosmeticsPages[10] = 0;
			itemLists[0] = unlockedHats;
			itemLists[1] = unlockedFaces;
			itemLists[2] = unlockedBadges;
			itemLists[3] = unlockedPaws;
			itemLists[4] = unlockedFurs;
			itemLists[5] = unlockedShirts;
			itemLists[6] = unlockedPants;
			itemLists[7] = unlockedArms;
			itemLists[8] = unlockedBacks;
			itemLists[9] = unlockedChests;
			itemLists[10] = unlockedTagFX;
			updateCosmeticsRetries = 0;
			maxUpdateCosmeticsRetries = 5;
			StartCoroutine(CheckCanGetDaily());
		}
		CreatorCodes.Initialize();
	}

	public void Start()
	{
		PlayFabTitleDataCache.Instance.GetTitleData("BundleData", delegate(string data)
		{
			bundleList.FromJson(data);
		}, delegate(PlayFabError e)
		{
			Debug.LogError($"Error getting bundle data: {e}");
		});
		anchorOverrides = GorillaTagger.Instance.offlineVRRig.GetComponent<VRRigAnchorOverrides>();
	}

	public void OnEnable()
	{
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
		if (SteamManager.Initialized && _steamMicroTransactionAuthorizationResponse == null)
		{
			_steamMicroTransactionAuthorizationResponse = Callback<MicroTxnAuthorizationResponse_t>.Create(ProcessSteamCallback);
		}
	}

	public void OnDisable()
	{
		_steamMicroTransactionAuthorizationResponse?.Unregister();
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	public void SliceUpdate()
	{
	}

	public static bool CompareCategoryToSavedCosmeticSlots(CosmeticCategory category, CosmeticSlots slot)
	{
		switch (category)
		{
		case CosmeticCategory.Hat:
			return slot == CosmeticSlots.Hat;
		case CosmeticCategory.Badge:
			return CosmeticSlots.Badge == slot;
		case CosmeticCategory.Face:
			return CosmeticSlots.Face == slot;
		case CosmeticCategory.Fur:
			return CosmeticSlots.Fur == slot;
		case CosmeticCategory.Paw:
			if (slot != CosmeticSlots.HandRight)
			{
				return slot == CosmeticSlots.HandLeft;
			}
			return true;
		case CosmeticCategory.Shirt:
			return CosmeticSlots.Shirt == slot;
		case CosmeticCategory.Back:
			if (slot != CosmeticSlots.BackLeft)
			{
				return slot == CosmeticSlots.BackRight;
			}
			return true;
		case CosmeticCategory.Arms:
			if (slot != CosmeticSlots.ArmLeft)
			{
				return slot == CosmeticSlots.ArmRight;
			}
			return true;
		case CosmeticCategory.Chest:
			return CosmeticSlots.Chest == slot;
		case CosmeticCategory.Pants:
			return CosmeticSlots.Pants == slot;
		case CosmeticCategory.TagEffect:
			return CosmeticSlots.TagEffect == slot;
		default:
			return false;
		}
	}

	public static CosmeticSlots CategoryToNonTransferrableSlot(CosmeticCategory category)
	{
		return category switch
		{
			CosmeticCategory.Hat => CosmeticSlots.Hat, 
			CosmeticCategory.Badge => CosmeticSlots.Badge, 
			CosmeticCategory.Face => CosmeticSlots.Face, 
			CosmeticCategory.Fur => CosmeticSlots.Fur, 
			CosmeticCategory.Paw => CosmeticSlots.HandRight, 
			CosmeticCategory.Shirt => CosmeticSlots.Shirt, 
			CosmeticCategory.Back => CosmeticSlots.Back, 
			CosmeticCategory.Arms => CosmeticSlots.Arms, 
			CosmeticCategory.Chest => CosmeticSlots.Chest, 
			CosmeticCategory.Pants => CosmeticSlots.Pants, 
			CosmeticCategory.TagEffect => CosmeticSlots.TagEffect, 
			_ => CosmeticSlots.Count, 
		};
	}

	private CosmeticSlots DropPositionToCosmeticSlot(BodyDockPositions.DropPositions pos)
	{
		return pos switch
		{
			BodyDockPositions.DropPositions.LeftArm => CosmeticSlots.ArmLeft, 
			BodyDockPositions.DropPositions.RightArm => CosmeticSlots.ArmRight, 
			BodyDockPositions.DropPositions.LeftBack => CosmeticSlots.BackLeft, 
			BodyDockPositions.DropPositions.RightBack => CosmeticSlots.BackRight, 
			BodyDockPositions.DropPositions.Chest => CosmeticSlots.Chest, 
			_ => CosmeticSlots.Count, 
		};
	}

	private static BodyDockPositions.DropPositions CosmeticSlotToDropPosition(CosmeticSlots slot)
	{
		return slot switch
		{
			CosmeticSlots.ArmLeft => BodyDockPositions.DropPositions.LeftArm, 
			CosmeticSlots.ArmRight => BodyDockPositions.DropPositions.RightArm, 
			CosmeticSlots.BackLeft => BodyDockPositions.DropPositions.LeftBack, 
			CosmeticSlots.BackRight => BodyDockPositions.DropPositions.RightBack, 
			CosmeticSlots.Chest => BodyDockPositions.DropPositions.Chest, 
			_ => BodyDockPositions.DropPositions.None, 
		};
	}

	public void AddItemCheckout(ItemCheckout newItemCheckout)
	{
		if (!itemCheckouts.Contains(newItemCheckout))
		{
			itemCheckouts.Add(newItemCheckout);
			UpdateShoppingCart();
			FormattedPurchaseText(finalLine, leftCheckoutPurchaseButtonString, rightCheckoutPurchaseButtonString, leftCheckoutPurchaseButtonOn, rightCheckoutPurchaseButtonOn);
			if (!itemToBuy.isNullItem)
			{
				RefreshItemToBuyPreview();
			}
		}
	}

	public void RemoveItemCheckout(ItemCheckout checkoutToRemove)
	{
		itemCheckouts.RemoveIfContains(checkoutToRemove);
	}

	public void AddFittingRoom(FittingRoom newFittingRoom)
	{
		if (!fittingRooms.Contains(newFittingRoom))
		{
			fittingRooms.Add(newFittingRoom);
			UpdateShoppingCart();
		}
	}

	public void RemoveFittingRoom(FittingRoom fittingRoomToRemove)
	{
		fittingRooms.RemoveIfContains(fittingRoomToRemove);
	}

	private void SaveItemPreference(CosmeticSlots slot, int slotIdx, CosmeticItem newItem)
	{
		PlayerPrefs.SetString(CosmeticSet.SlotPlayerPreferenceName(slot), newItem.itemName);
		PlayerPrefs.Save();
	}

	public void SaveCurrentItemPreferences()
	{
		for (int i = 0; i < 16; i++)
		{
			CosmeticSlots slot = (CosmeticSlots)i;
			CosmeticItem newItem = currentWornSet.items[i];
			if (newItem.itemName == "Slingshot")
			{
				newItem = nullItem;
			}
			SaveItemPreference(slot, i, newItem);
		}
	}

	private void ApplyCosmeticToSet(CosmeticSet set, CosmeticItem newItem, int slotIdx, CosmeticSlots slot, bool applyToPlayerPrefs, List<CosmeticSlots> appliedSlots)
	{
		CosmeticItem cosmeticItem = ((set.items[slotIdx].itemName == newItem.itemName) ? nullItem : newItem);
		set.items[slotIdx] = cosmeticItem;
		if (applyToPlayerPrefs)
		{
			SaveItemPreference(slot, slotIdx, cosmeticItem);
		}
		appliedSlots.Add(slot);
	}

	public static void ClearTryOnCollectable()
	{
		if (hasInstance)
		{
			instance.tryOnCollectableItem = instance.nullItem;
		}
	}

	private void PrivApplyCosmeticItemToSet(CosmeticSet set, CosmeticItem newItem, bool isLeftHand, bool applyToPlayerPrefs, List<CosmeticSlots> appliedSlots)
	{
		if (newItem.isNullItem)
		{
			return;
		}
		if (newItem.itemCategory == CosmeticCategory.Collectable)
		{
			if (set == tryOnSet)
			{
				tryOnCollectableItem = newItem;
			}
			return;
		}
		if (set == tryOnSet)
		{
			ClearTryOnCollectable();
		}
		VRRig.LocalRig.cosmeticsObjectRegistry.Cosmetic(newItem.itemName);
		if (CosmeticSet.IsHoldable(newItem))
		{
			BodyDockPositions.DockingResult dockingResult = GorillaTagger.Instance.offlineVRRig.GetComponent<BodyDockPositions>().ToggleWithHandedness(newItem.displayName, isLeftHand, newItem.bothHandsHoldable);
			foreach (BodyDockPositions.DropPositions item in dockingResult.positionsDisabled)
			{
				CosmeticSlots cosmeticSlots = DropPositionToCosmeticSlot(item);
				if (cosmeticSlots != CosmeticSlots.Count)
				{
					int num = (int)cosmeticSlots;
					set.items[num] = nullItem;
					if (applyToPlayerPrefs)
					{
						SaveItemPreference(cosmeticSlots, num, nullItem);
					}
				}
			}
			{
				foreach (BodyDockPositions.DropPositions item2 in dockingResult.dockedPosition)
				{
					if (item2 != BodyDockPositions.DropPositions.None)
					{
						CosmeticSlots cosmeticSlots2 = DropPositionToCosmeticSlot(item2);
						int num2 = (int)cosmeticSlots2;
						set.items[num2] = newItem;
						if (applyToPlayerPrefs)
						{
							SaveItemPreference(cosmeticSlots2, num2, newItem);
						}
						appliedSlots.Add(cosmeticSlots2);
					}
				}
				return;
			}
		}
		if (newItem.itemCategory == CosmeticCategory.Paw)
		{
			CosmeticSlots cosmeticSlots3 = (isLeftHand ? CosmeticSlots.HandLeft : CosmeticSlots.HandRight);
			int slotIdx = (int)cosmeticSlots3;
			ApplyCosmeticToSet(set, newItem, slotIdx, cosmeticSlots3, applyToPlayerPrefs, appliedSlots);
			CosmeticSlots cosmeticSlots4 = CosmeticSet.OppositeSlot(cosmeticSlots3);
			int num3 = (int)cosmeticSlots4;
			if (newItem.bothHandsHoldable)
			{
				ApplyCosmeticToSet(set, nullItem, num3, cosmeticSlots4, applyToPlayerPrefs, appliedSlots);
				return;
			}
			if (set.items[num3].itemName == newItem.itemName)
			{
				ApplyCosmeticToSet(set, nullItem, num3, cosmeticSlots4, applyToPlayerPrefs, appliedSlots);
			}
			if (set.items[num3].bothHandsHoldable)
			{
				ApplyCosmeticToSet(set, nullItem, num3, cosmeticSlots4, applyToPlayerPrefs, appliedSlots);
			}
		}
		else
		{
			CosmeticSlots cosmeticSlots5 = CategoryToNonTransferrableSlot(newItem.itemCategory);
			int slotIdx2 = (int)cosmeticSlots5;
			ApplyCosmeticToSet(set, newItem, slotIdx2, cosmeticSlots5, applyToPlayerPrefs, appliedSlots);
		}
	}

	public void ApplyCosmeticItemToSet(CosmeticSet set, CosmeticItem newItem, bool isLeftHand, bool applyToPlayerPrefs)
	{
		ApplyCosmeticItemToSet(set, newItem, isLeftHand, applyToPlayerPrefs, _g_default_outAppliedSlotsList_for_applyCosmeticItemToSet);
	}

	public void ApplyCosmeticItemToSet(CosmeticSet set, CosmeticItem newItem, bool isLeftHand, bool applyToPlayerPrefs, List<CosmeticSlots> outAppliedSlotsList)
	{
		outAppliedSlotsList.Clear();
		if (newItem.itemCategory == CosmeticCategory.Set)
		{
			bool flag = false;
			Dictionary<CosmeticItem, bool> dictionary = new Dictionary<CosmeticItem, bool>();
			string[] bundledItems = newItem.bundledItems;
			foreach (string itemID in bundledItems)
			{
				CosmeticItem itemFromDict = GetItemFromDict(itemID);
				if (AnyMatch(set, itemFromDict))
				{
					flag = true;
					dictionary.Add(itemFromDict, value: true);
				}
				else
				{
					dictionary.Add(itemFromDict, value: false);
				}
			}
			{
				foreach (KeyValuePair<CosmeticItem, bool> item in dictionary)
				{
					if (flag)
					{
						if (item.Value)
						{
							PrivApplyCosmeticItemToSet(set, item.Key, isLeftHand, applyToPlayerPrefs, outAppliedSlotsList);
						}
					}
					else
					{
						PrivApplyCosmeticItemToSet(set, item.Key, isLeftHand, applyToPlayerPrefs, outAppliedSlotsList);
					}
				}
				return;
			}
		}
		PrivApplyCosmeticItemToSet(set, newItem, isLeftHand, applyToPlayerPrefs, outAppliedSlotsList);
	}

	public void RemoveCosmeticItemFromSet(CosmeticSet set, string itemName, bool applyToPlayerPrefs)
	{
		cachedSet.CopyItems(set);
		for (int i = 0; i < 16; i++)
		{
			if (set.items[i].displayName == itemName)
			{
				set.items[i] = nullItem;
				if (applyToPlayerPrefs)
				{
					SaveItemPreference((CosmeticSlots)i, i, nullItem);
				}
			}
		}
		VRRig offlineVRRig = GorillaTagger.Instance.offlineVRRig;
		BodyDockPositions component = offlineVRRig.GetComponent<BodyDockPositions>();
		set.ActivateCosmetics(cachedSet, offlineVRRig, component, offlineVRRig.cosmeticsObjectRegistry);
	}

	private async void RepressButton(FittingRoomButton pressedButton, bool isLeftHand)
	{
		float timeEntered = Time.time;
		float maxTime = 1f;
		if (pressedButton.currentCosmeticItem.itemCategory == CosmeticCategory.Set)
		{
			CosmeticItem itemSet = pressedButton.currentCosmeticItem;
			bool flag = true;
			while (flag)
			{
				if (Time.time > timeEntered + maxTime)
				{
					return;
				}
				await Awaitable.EndOfFrameAsync();
				flag = false;
				for (int i = 0; i < itemSet.bundledItems.Length; i++)
				{
					if (VRRig.LocalRig.cosmeticsObjectRegistry.Cosmetic(itemSet.bundledItems[i]) == null)
					{
						flag = true;
					}
				}
			}
		}
		else
		{
			while (VRRig.LocalRig.cosmeticsObjectRegistry.Cosmetic(pressedButton.currentCosmeticItem.itemName) == null)
			{
				if (Time.time > timeEntered + maxTime)
				{
					return;
				}
				await Awaitable.EndOfFrameAsync();
			}
		}
		PressFittingRoomButton(pressedButton, isLeftHand);
	}

	public void PressFittingRoomButton(FittingRoomButton pressedFittingRoomButton, bool isLeftHand)
	{
		if (pressedFittingRoomButton.currentCosmeticItem.itemName == null || pressedFittingRoomButton.currentCosmeticItem.itemName == nullItem.itemName || pressedFittingRoomButton.currentCosmeticItem.itemName == "")
		{
			return;
		}
		if (pressedFittingRoomButton.currentCosmeticItem.itemCategory == CosmeticCategory.Set)
		{
			CosmeticItem currentCosmeticItem = pressedFittingRoomButton.currentCosmeticItem;
			bool flag = false;
			for (int i = 0; i < currentCosmeticItem.bundledItems.Length; i++)
			{
				if (VRRig.LocalRig.cosmeticsObjectRegistry.Cosmetic(currentCosmeticItem.bundledItems[i]) == null)
				{
					flag = true;
				}
			}
			if (flag)
			{
				RepressButton(pressedFittingRoomButton, isLeftHand);
				return;
			}
		}
		else if (VRRig.LocalRig.cosmeticsObjectRegistry.Cosmetic(pressedFittingRoomButton.currentCosmeticItem.itemName) == null)
		{
			RepressButton(pressedFittingRoomButton, isLeftHand);
			return;
		}
		BundleManager.instance._tryOnBundlesStand?.ClearSelectedBundle();
		ApplyCosmeticItemToSet(tryOnSet, pressedFittingRoomButton.currentCosmeticItem, isLeftHand, applyToPlayerPrefs: false);
		UpdateShoppingCart();
		UpdateWornCosmetics(sync: true);
	}

	public EWearingCosmeticSet CheckIfCosmeticSetMatchesItemSet(CosmeticSet set, string itemName)
	{
		EWearingCosmeticSet eWearingCosmeticSet = EWearingCosmeticSet.NotASet;
		CosmeticItem cosmeticItem = allCosmeticsDict[itemName];
		if (cosmeticItem.bundledItems.Length != 0)
		{
			string[] bundledItems = cosmeticItem.bundledItems;
			foreach (string key in bundledItems)
			{
				if (AnyMatch(set, allCosmeticsDict[key]))
				{
					switch (eWearingCosmeticSet)
					{
					case EWearingCosmeticSet.NotASet:
						eWearingCosmeticSet = EWearingCosmeticSet.Complete;
						break;
					case EWearingCosmeticSet.NotWearing:
						eWearingCosmeticSet = EWearingCosmeticSet.Partial;
						break;
					}
				}
				else
				{
					switch (eWearingCosmeticSet)
					{
					case EWearingCosmeticSet.NotASet:
						eWearingCosmeticSet = EWearingCosmeticSet.NotWearing;
						break;
					case EWearingCosmeticSet.Complete:
						eWearingCosmeticSet = EWearingCosmeticSet.Partial;
						break;
					}
				}
			}
		}
		return eWearingCosmeticSet;
	}

	public void PressCosmeticStandButton(CosmeticStand pressedStand)
	{
		searchIndex = currentCart.IndexOf(pressedStand.thisCosmeticItem);
		if (searchIndex != -1)
		{
			GorillaTelemetry.PostShopEvent(GorillaTagger.Instance.offlineVRRig, GTShopEventType.cart_item_remove, pressedStand.thisCosmeticItem);
			currentCart.RemoveAt(searchIndex);
			pressedStand.isOn = false;
			for (int i = 0; i < 16; i++)
			{
				if (pressedStand.thisCosmeticItem.itemName == tryOnSet.items[i].itemName)
				{
					tryOnSet.items[i] = nullItem;
				}
			}
		}
		else
		{
			GorillaTelemetry.PostShopEvent(GorillaTagger.Instance.offlineVRRig, GTShopEventType.cart_item_add, pressedStand.thisCosmeticItem);
			currentCart.Insert(0, pressedStand.thisCosmeticItem);
			pressedStand.isOn = true;
			if (currentCart.Count > numFittingRoomButtons)
			{
				CosmeticStand[] array = cosmeticStands;
				foreach (CosmeticStand cosmeticStand in array)
				{
					if (!(cosmeticStand == null) && cosmeticStand.thisCosmeticItem.itemName == currentCart[numFittingRoomButtons].itemName)
					{
						cosmeticStand.isOn = false;
						cosmeticStand.UpdateColor();
						break;
					}
				}
				currentCart.RemoveAt(numFittingRoomButtons);
			}
		}
		pressedStand.UpdateColor();
		UpdateShoppingCart();
	}

	public void PressWardrobeItemButton(CosmeticItem cosmeticItem, bool isLeftHand, bool isTempCosm)
	{
		if (!cosmeticItem.isNullItem)
		{
			CosmeticItem itemFromDict = GetItemFromDict(cosmeticItem.itemName);
			if (isTempCosm)
			{
				PressTemporaryWardrobeItemButton(itemFromDict, isLeftHand);
			}
			else
			{
				PressWardrobeItemButton(itemFromDict, isLeftHand);
			}
			UpdateWornCosmetics(sync: true);
			OnCosmeticsUpdated?.Invoke();
		}
	}

	private void PressWardrobeItemButton(CosmeticItem item, bool isLeftHand)
	{
		List<CosmeticSlots> list = CollectionPool<List<CosmeticSlots>, CosmeticSlots>.Get();
		if (list.Capacity < 16)
		{
			list.Capacity = 16;
		}
		ApplyCosmeticItemToSet(currentWornSet, item, isLeftHand, applyToPlayerPrefs: true, list);
		foreach (CosmeticSlots item2 in list)
		{
			tryOnSet.items[(int)item2] = nullItem;
		}
		CollectionPool<List<CosmeticSlots>, CosmeticSlots>.Release(list);
		UpdateShoppingCart();
	}

	private void PressTemporaryWardrobeItemButton(CosmeticItem item, bool isLeftHand)
	{
		ApplyCosmeticItemToSet(tempUnlockedSet, item, isLeftHand, applyToPlayerPrefs: false);
	}

	public void PressWardrobeFunctionButton(string function)
	{
		switch (function)
		{
		case "left":
			cosmeticsPages[wardrobeType]--;
			if (cosmeticsPages[wardrobeType] < 0)
			{
				cosmeticsPages[wardrobeType] = (itemLists[wardrobeType].Count - 1) / 3;
			}
			break;
		case "right":
			cosmeticsPages[wardrobeType]++;
			if (cosmeticsPages[wardrobeType] > (itemLists[wardrobeType].Count - 1) / 3)
			{
				cosmeticsPages[wardrobeType] = 0;
			}
			break;
		case "hat":
			if (wardrobeType == 0)
			{
				return;
			}
			wardrobeType = 0;
			break;
		case "face":
			if (wardrobeType == 1)
			{
				return;
			}
			wardrobeType = 1;
			break;
		case "badge":
			if (wardrobeType == 2)
			{
				return;
			}
			wardrobeType = 2;
			break;
		case "hand":
			if (wardrobeType == 3)
			{
				return;
			}
			wardrobeType = 3;
			break;
		case "fur":
			if (wardrobeType == 4)
			{
				return;
			}
			wardrobeType = 4;
			break;
		case "outfit":
			if (wardrobeType == 5)
			{
				return;
			}
			wardrobeType = 5;
			break;
		case "arms":
			if (wardrobeType == 6)
			{
				return;
			}
			wardrobeType = 6;
			break;
		case "back":
			if (wardrobeType == 7)
			{
				return;
			}
			wardrobeType = 7;
			break;
		case "chest":
			if (wardrobeType == 8)
			{
				return;
			}
			wardrobeType = 8;
			break;
		case "reserved":
			if (wardrobeType == 9)
			{
				return;
			}
			wardrobeType = 9;
			break;
		case "tagEffect":
			if (wardrobeType == 10)
			{
				return;
			}
			wardrobeType = 10;
			break;
		}
		UpdateWardrobeModelsAndButtons();
		OnCosmeticsUpdated?.Invoke();
	}

	public void ClearCheckout(bool sendEvent)
	{
		if (sendEvent)
		{
			GorillaTelemetry.PostShopEvent(GorillaTagger.Instance.offlineVRRig, GTShopEventType.checkout_cancel, currentCart);
		}
		itemToBuy = nullItem;
		RefreshItemToBuyPreview();
		currentPurchaseItemStage = PurchaseItemStages.Start;
		ProcessPurchaseItemState(null, isLeftHand: false);
	}

	public bool RemoveItemFromCart(CosmeticItem cosmeticItem)
	{
		searchIndex = currentCart.IndexOf(cosmeticItem);
		if (searchIndex != -1)
		{
			currentCart.RemoveAt(searchIndex);
			for (int i = 0; i < 16; i++)
			{
				if (cosmeticItem.itemName == tryOnSet.items[i].itemName)
				{
					tryOnSet.items[i] = nullItem;
				}
			}
			return true;
		}
		return false;
	}

	public void ClearCheckoutAndCart(bool sendEvent)
	{
		currentCart.Clear();
		tryOnSet.ClearSet(nullItem);
		ClearTryOnCollectable();
		ClearCheckout(sendEvent);
	}

	public void PressCheckoutCartButton(CheckoutCartButton pressedCheckoutCartButton, bool isLeftHand)
	{
		if (currentPurchaseItemStage != PurchaseItemStages.Buying)
		{
			currentPurchaseItemStage = PurchaseItemStages.CheckoutButtonPressed;
			tryOnSet.ClearSet(nullItem);
			ClearTryOnCollectable();
			if (itemToBuy.displayName == pressedCheckoutCartButton.currentCosmeticItem.displayName)
			{
				itemToBuy = nullItem;
				RefreshItemToBuyPreview();
			}
			else
			{
				itemToBuy = pressedCheckoutCartButton.currentCosmeticItem;
				checkoutCartButtonPressedWithLeft = isLeftHand;
				RefreshItemToBuyPreview();
			}
			ProcessPurchaseItemState(null, isLeftHand);
			UpdateShoppingCart();
		}
	}

	private void RefreshItemToBuyPreview()
	{
		if (itemToBuy.bundledItems != null && itemToBuy.bundledItems.Length != 0)
		{
			List<string> list = new List<string>();
			string[] bundledItems = itemToBuy.bundledItems;
			foreach (string itemID in bundledItems)
			{
				tempItem = GetItemFromDict(itemID);
				list.Add(tempItem.displayName);
			}
			for (iterator = 0; iterator < itemCheckouts.Count; iterator++)
			{
				if (!itemCheckouts[iterator].IsNull())
				{
					itemCheckouts[iterator].checkoutHeadModel.SetCosmeticActiveArray(list.ToArray(), new bool[list.Count]);
				}
			}
		}
		else
		{
			for (iterator = 0; iterator < itemCheckouts.Count; iterator++)
			{
				if (!itemCheckouts[iterator].IsNull())
				{
					itemCheckouts[iterator].checkoutHeadModel.SetCosmeticActive(itemToBuy.displayName);
				}
			}
		}
		ApplyCosmeticItemToSet(tryOnSet, itemToBuy, checkoutCartButtonPressedWithLeft, applyToPlayerPrefs: false);
		UpdateWornCosmetics(sync: true);
	}

	public void PressPurchaseItemButton(PurchaseItemButton pressedPurchaseItemButton, bool isLeftHand)
	{
		ProcessPurchaseItemState(pressedPurchaseItemButton.buttonSide, isLeftHand);
	}

	public async void PurchaseBundle(StoreBundle bundleToPurchase, ICreatorCodeProvider ccp)
	{
		if (!(bundleToPurchase.playfabBundleID != "NULL"))
		{
			return;
		}
		ccp.GetCreatorCode(out var code, out var groups);
		ATM_Manager.instance.SwitchToStage(ATM_Manager.ATMStages.Begin);
		currentPurchaseItemStage = PurchaseItemStages.Start;
		ProcessPurchaseItemState("left", isLeftHand: false);
		buyingBundle = true;
		if (bundleToPurchase.nexusCreatorCode != null)
		{
			code = bundleToPurchase.nexusCreatorCode.Code;
			groups = new NexusGroupId[1] { bundleToPurchase.nexusCreatorCode.GroupId };
		}
		if (code.IsNullOrEmpty())
		{
			itemToPurchase = bundleToPurchase.playfabBundleID;
			SteamPurchase();
			return;
		}
		itemToPurchase = bundleToPurchase.playfabBundleID;
		NexusManager.MemberCode memberCode = await CreatorCodes.CheckValidationCoroutineJIT(ccp.TerminalId, code, groups);
		if (memberCode != null)
		{
			if (buyingBundle)
			{
				SetValidatedCreatorCode(memberCode.memberCode, memberCode.groupId.Code, ccp.TerminalId);
				SteamPurchase();
			}
		}
		else
		{
			OnCreatorCodeFailure();
		}
	}

	private void OnCreatorCodeFailure()
	{
		buyingBundle = false;
	}

	public void PressEarlyAccessButton()
	{
		currentPurchaseItemStage = PurchaseItemStages.Start;
		ProcessPurchaseItemState("left", isLeftHand: false);
		buyingBundle = true;
		itemToPurchase = BundlePlayfabItemName;
		ATM_Manager.instance.shinyRocksCost = BundleShinyRocks;
		SteamPurchase();
	}

	public void ProcessPurchaseItemState(string buttonSide, bool isLeftHand)
	{
		switch (currentPurchaseItemStage)
		{
		case PurchaseItemStages.Start:
			itemToBuy = nullItem;
			FormattedPurchaseText("SELECT AN ITEM FROM YOUR CART TO PURCHASE!");
			UpdateShoppingCart();
			break;
		case PurchaseItemStages.CheckoutButtonPressed:
			GorillaTelemetry.PostShopEvent(GorillaTagger.Instance.offlineVRRig, GTShopEventType.checkout_start, currentCart);
			searchIndex = unlockedCosmetics.FindIndex((CosmeticItem x) => itemToBuy.itemName == x.itemName);
			if (searchIndex > -1)
			{
				FormattedPurchaseText("YOU ALREADY OWN THIS ITEM!", "-", "-", leftButtonOn: true, rightButtonOn: true);
				currentPurchaseItemStage = PurchaseItemStages.ItemOwned;
			}
			else if (itemToBuy.cost <= currencyBalance)
			{
				FormattedPurchaseText("DO YOU WANT TO BUY THIS ITEM?", "NO!", "YES!");
				currentPurchaseItemStage = PurchaseItemStages.ItemSelected;
			}
			else
			{
				FormattedPurchaseText("INSUFFICIENT SHINY ROCKS FOR THIS ITEM!", "-", "-", leftButtonOn: true, rightButtonOn: true);
				currentPurchaseItemStage = PurchaseItemStages.Start;
			}
			break;
		case PurchaseItemStages.ItemSelected:
			if (buttonSide == "right")
			{
				GorillaTelemetry.PostShopEvent(GorillaTagger.Instance.offlineVRRig, GTShopEventType.item_select, itemToBuy);
				FormattedPurchaseText("ARE YOU REALLY SURE?", "YES! I NEED IT!", "LET ME THINK ABOUT IT");
				currentPurchaseItemStage = PurchaseItemStages.FinalPurchaseAcknowledgement;
			}
			else
			{
				currentPurchaseItemStage = PurchaseItemStages.CheckoutButtonPressed;
				ProcessPurchaseItemState(null, isLeftHand);
			}
			break;
		case PurchaseItemStages.FinalPurchaseAcknowledgement:
			if (buttonSide == "left")
			{
				FormattedPurchaseText("PURCHASING ITEM...", "-", "-", leftButtonOn: true, rightButtonOn: true);
				currentPurchaseItemStage = PurchaseItemStages.Buying;
				isLastHandTouchedLeft = isLeftHand;
				PurchaseItem();
			}
			else
			{
				currentPurchaseItemStage = PurchaseItemStages.CheckoutButtonPressed;
				ProcessPurchaseItemState(null, isLeftHand);
			}
			break;
		case PurchaseItemStages.Failure:
			FormattedPurchaseText("ERROR IN PURCHASING ITEM! NO MONEY WAS SPENT. SELECT ANOTHER ITEM.", "-", "-", leftButtonOn: true, rightButtonOn: true);
			break;
		case PurchaseItemStages.Success:
		{
			FormattedPurchaseText("SUCCESS! ENJOY YOUR NEW ITEM!", "-", "-", leftButtonOn: true, rightButtonOn: true);
			GorillaTagger.Instance.offlineVRRig.AddCosmetic(itemToBuy.itemName);
			CosmeticItem itemFromDict = GetItemFromDict(itemToBuy.itemName);
			if (itemFromDict.bundledItems != null)
			{
				string[] bundledItems = itemFromDict.bundledItems;
				foreach (string cosmeticId in bundledItems)
				{
					GorillaTagger.Instance.offlineVRRig.AddCosmetic(cosmeticId);
				}
			}
			tryOnSet.ClearSet(nullItem);
			ClearTryOnCollectable();
			UpdateShoppingCart();
			ApplyCosmeticItemToSet(currentWornSet, itemFromDict, isLeftHand, applyToPlayerPrefs: true);
			UpdateShoppingCart();
			UpdateWornCosmetics();
			UpdateWardrobeModelsAndButtons();
			OnCosmeticsUpdated?.Invoke();
			break;
		}
		case PurchaseItemStages.ItemOwned:
		case PurchaseItemStages.Buying:
			break;
		}
	}

	public void FormattedPurchaseText(string finalLineVar, string leftPurchaseButtonText = null, string rightPurchaseButtonText = null, bool leftButtonOn = false, bool rightButtonOn = false)
	{
		finalLine = finalLineVar;
		if (leftPurchaseButtonText != null)
		{
			leftCheckoutPurchaseButtonString = leftPurchaseButtonText;
			leftCheckoutPurchaseButtonOn = leftButtonOn;
		}
		if (rightPurchaseButtonText != null)
		{
			rightCheckoutPurchaseButtonString = rightPurchaseButtonText;
			rightCheckoutPurchaseButtonOn = rightButtonOn;
		}
		string newText = "SELECTION: " + GetItemDisplayName(itemToBuy) + "\nITEM COST: " + itemToBuy.cost + "\nYOU HAVE: " + currencyBalance + "\n\n" + finalLine;
		for (iterator = 0; iterator < itemCheckouts.Count; iterator++)
		{
			if (!itemCheckouts[iterator].IsNull())
			{
				itemCheckouts[iterator].UpdatePurchaseText(newText, leftPurchaseButtonText, rightPurchaseButtonText, leftButtonOn, rightButtonOn);
			}
		}
	}

	public void PurchaseItem()
	{
		PlayFabClientAPI.PurchaseItem(new PurchaseItemRequest
		{
			ItemId = itemToBuy.itemName,
			Price = itemToBuy.cost,
			VirtualCurrency = currencyName,
			CatalogVersion = catalog
		}, delegate(PurchaseItemResult result)
		{
			if (result.Items.Count > 0)
			{
				foreach (ItemInstance item in result.Items)
				{
					CosmeticItem itemFromDict = GetItemFromDict(itemToBuy.itemName);
					if (itemFromDict.itemCategory == CosmeticCategory.Set)
					{
						UnlockItem(item.ItemId);
						string[] bundledItems = itemFromDict.bundledItems;
						foreach (string itemIdToUnlock in bundledItems)
						{
							UnlockItem(itemIdToUnlock);
						}
					}
					else
					{
						UnlockItem(item.ItemId);
					}
				}
				UpdateMyCosmetics();
				if (NetworkSystem.Instance.InRoom)
				{
					StartCoroutine(CheckIfMyCosmeticsUpdated(itemToBuy.itemName));
				}
				currentPurchaseItemStage = PurchaseItemStages.Success;
				currencyBalance -= itemToBuy.cost;
				UpdateShoppingCart();
				ProcessPurchaseItemState(null, isLastHandTouchedLeft);
			}
			else
			{
				currentPurchaseItemStage = PurchaseItemStages.Failure;
				ProcessPurchaseItemState(null, isLeftHand: false);
			}
		}, delegate
		{
			currentPurchaseItemStage = PurchaseItemStages.Failure;
			ProcessPurchaseItemState(null, isLeftHand: false);
		});
	}

	private void UnlockItem(string itemIdToUnlock, bool relock = false)
	{
		int num = allCosmetics.FindIndex((CosmeticItem x) => itemIdToUnlock == x.itemName);
		if (num <= -1)
		{
			return;
		}
		ModifyUnlockList(unlockedCosmetics, num, relock);
		if (relock)
		{
			concatStringCosmeticsAllowed.Replace(allCosmetics[num].itemName, string.Empty);
		}
		else
		{
			concatStringCosmeticsAllowed += allCosmetics[num].itemName;
		}
		switch (allCosmetics[num].itemCategory)
		{
		case CosmeticCategory.Hat:
			ModifyUnlockList(unlockedHats, num, relock);
			break;
		case CosmeticCategory.Fur:
			ModifyUnlockList(unlockedFurs, num, relock);
			break;
		case CosmeticCategory.Badge:
			ModifyUnlockList(unlockedBadges, num, relock);
			break;
		case CosmeticCategory.Face:
			ModifyUnlockList(unlockedFaces, num, relock);
			break;
		case CosmeticCategory.Chest:
			ModifyUnlockList(unlockedChests, num, relock);
			break;
		case CosmeticCategory.Paw:
			if (!allCosmetics[num].isThrowable)
			{
				ModifyUnlockList(unlockedPaws, num, relock);
			}
			else
			{
				ModifyUnlockList(unlockedThrowables, num, relock);
			}
			break;
		case CosmeticCategory.Shirt:
			ModifyUnlockList(unlockedShirts, num, relock);
			break;
		case CosmeticCategory.Back:
			ModifyUnlockList(unlockedBacks, num, relock);
			break;
		case CosmeticCategory.Arms:
			ModifyUnlockList(unlockedArms, num, relock);
			break;
		case CosmeticCategory.Pants:
			ModifyUnlockList(unlockedPants, num, relock);
			break;
		case CosmeticCategory.TagEffect:
			ModifyUnlockList(unlockedTagFX, num, relock);
			break;
		case CosmeticCategory.Set:
		{
			string[] bundledItems = allCosmetics[num].bundledItems;
			foreach (string itemIdToUnlock2 in bundledItems)
			{
				UnlockItem(itemIdToUnlock2);
			}
			break;
		}
		case CosmeticCategory.Count:
		case CosmeticCategory.Collectable:
			break;
		}
	}

	private void ModifyUnlockList(List<CosmeticItem> list, int index, bool relock)
	{
		if (!relock && !list.Contains(allCosmetics[index]))
		{
			list.Add(allCosmetics[index]);
		}
		else if (relock && list.Contains(allCosmetics[index]))
		{
			list.Remove(allCosmetics[index]);
		}
	}

	private IEnumerator CheckIfMyCosmeticsUpdated(string itemToBuyID)
	{
		Debug.Log("Cosmetic updated check!");
		yield return new WaitForSecondsRealtime(1f);
		foundCosmetic = false;
		attempts = 0;
		while (!foundCosmetic && attempts < 10 && NetworkSystem.Instance.InRoom)
		{
			PlayFabClientAPI.GetSharedGroupData(new PlayFab.ClientModels.GetSharedGroupDataRequest
			{
				Keys = new List<string> { "Inventory" },
				SharedGroupId = NetworkSystem.Instance.LocalPlayer.UserId + "Inventory"
			}, delegate(GetSharedGroupDataResult result)
			{
				attempts++;
				foreach (KeyValuePair<string, PlayFab.ClientModels.SharedGroupDataRecord> datum in result.Data)
				{
					if (datum.Value.Value.Contains(itemToBuyID))
					{
						PhotonNetwork.RaiseEvent(199, null, new RaiseEventOptions
						{
							Receivers = ReceiverGroup.Others
						}, SendOptions.SendReliable);
						foundCosmetic = true;
					}
				}
				if (foundCosmetic)
				{
					UpdateWornCosmetics(sync: true);
				}
			}, delegate(PlayFabError error)
			{
				attempts++;
				ReauthOrBan(error);
			});
			yield return new WaitForSecondsRealtime(1f);
		}
		Debug.Log("done!");
	}

	public void UpdateWardrobeModelsAndButtons()
	{
		foreach (WardrobeInstance wardrobe in wardrobes)
		{
			wardrobe.wardrobeItemButtons[0].currentCosmeticItem = ((cosmeticsPages[wardrobeType] * 3 < itemLists[wardrobeType].Count) ? itemLists[wardrobeType][cosmeticsPages[wardrobeType] * 3] : nullItem);
			wardrobe.wardrobeItemButtons[1].currentCosmeticItem = ((cosmeticsPages[wardrobeType] * 3 + 1 < itemLists[wardrobeType].Count) ? itemLists[wardrobeType][cosmeticsPages[wardrobeType] * 3 + 1] : nullItem);
			wardrobe.wardrobeItemButtons[2].currentCosmeticItem = ((cosmeticsPages[wardrobeType] * 3 + 2 < itemLists[wardrobeType].Count) ? itemLists[wardrobeType][cosmeticsPages[wardrobeType] * 3 + 2] : nullItem);
			for (iterator = 0; iterator < wardrobe.wardrobeItemButtons.Length; iterator++)
			{
				CosmeticItem currentCosmeticItem = wardrobe.wardrobeItemButtons[iterator].currentCosmeticItem;
				wardrobe.wardrobeItemButtons[iterator].isOn = !currentCosmeticItem.isNullItem && AnyMatch(currentWornSet, currentCosmeticItem);
				wardrobe.wardrobeItemButtons[iterator].UpdateColor();
			}
			wardrobe.wardrobeItemButtons[0].controlledModel.SetCosmeticActive(wardrobe.wardrobeItemButtons[0].currentCosmeticItem.displayName);
			wardrobe.wardrobeItemButtons[1].controlledModel.SetCosmeticActive(wardrobe.wardrobeItemButtons[1].currentCosmeticItem.displayName);
			wardrobe.wardrobeItemButtons[2].controlledModel.SetCosmeticActive(wardrobe.wardrobeItemButtons[2].currentCosmeticItem.displayName);
			wardrobe.selfDoll.SetCosmeticActiveArray(currentWornSet.ToDisplayNameArray(), currentWornSet.ToOnRightSideArray());
		}
	}

	public int GetCategorySize(CosmeticCategory category)
	{
		int indexForCategory = GetIndexForCategory(category);
		if (indexForCategory != -1)
		{
			return itemLists[indexForCategory].Count;
		}
		return 0;
	}

	public CosmeticItem GetCosmetic(int category, int cosmeticIndex)
	{
		if (cosmeticIndex >= itemLists[category].Count || cosmeticIndex < 0)
		{
			return nullItem;
		}
		return itemLists[category][cosmeticIndex];
	}

	public CosmeticItem GetCosmetic(CosmeticCategory category, int cosmeticIndex)
	{
		return GetCosmetic(GetIndexForCategory(category), cosmeticIndex);
	}

	private int GetIndexForCategory(CosmeticCategory category)
	{
		return category switch
		{
			CosmeticCategory.Hat => 0, 
			CosmeticCategory.Face => 1, 
			CosmeticCategory.Badge => 2, 
			CosmeticCategory.Paw => 3, 
			CosmeticCategory.Fur => 4, 
			CosmeticCategory.Shirt => 5, 
			CosmeticCategory.Pants => 6, 
			CosmeticCategory.Arms => 7, 
			CosmeticCategory.Back => 8, 
			CosmeticCategory.Chest => 9, 
			CosmeticCategory.TagEffect => 10, 
			_ => 0, 
		};
	}

	public bool IsCosmeticEquipped(CosmeticItem cosmetic)
	{
		return AnyMatch(currentWornSet, cosmetic);
	}

	public bool IsCosmeticEquipped(CosmeticItem cosmetic, bool tempSet)
	{
		if (!tempSet)
		{
			return IsCosmeticEquipped(cosmetic);
		}
		return IsTemporaryCosmeticEquipped(cosmetic);
	}

	public bool IsTemporaryCosmeticEquipped(CosmeticItem cosmetic)
	{
		return AnyMatch(tempUnlockedSet, cosmetic);
	}

	public CosmeticItem GetSlotItem(CosmeticSlots slot, bool checkOpposite = true, bool tempSet = false)
	{
		int num = (int)slot;
		if (checkOpposite)
		{
			num = (int)CosmeticSet.OppositeSlot(slot);
		}
		if (!tempSet)
		{
			return currentWornSet.items[num];
		}
		return tempUnlockedSet.items[num];
	}

	public string[] GetCurrentlyWornCosmetics(bool tempSet = false)
	{
		if (!tempSet)
		{
			return currentWornSet.ToDisplayNameArray();
		}
		return tempUnlockedSet.ToDisplayNameArray();
	}

	public bool[] GetCurrentRightEquippedSided(bool tempSet = false)
	{
		if (!tempSet)
		{
			return currentWornSet.ToOnRightSideArray();
		}
		return tempUnlockedSet.ToOnRightSideArray();
	}

	public void UpdateShoppingCart()
	{
		for (iterator = 0; iterator < itemCheckouts.Count; iterator++)
		{
			if (!itemCheckouts[iterator].IsNull())
			{
				itemCheckouts[iterator].UpdateFromCart(currentCart, itemToBuy);
			}
		}
		for (iterator = 0; iterator < fittingRooms.Count; iterator++)
		{
			if (!fittingRooms[iterator].IsNull())
			{
				fittingRooms[iterator].UpdateFromCart(currentCart, tryOnSet);
			}
		}
		UpdateWardrobeModelsAndButtons();
	}

	public void UpdateWornCosmetics()
	{
		UpdateWornCosmetics(sync: false, playfx: false);
	}

	public void UpdateWornCosmetics(bool sync)
	{
		UpdateWornCosmetics(sync, playfx: false);
	}

	public void UpdateWornCosmetics(bool sync, bool playfx)
	{
		VRRig localRig = VRRig.LocalRig;
		activeMergedSet.MergeInSets(currentWornSet, tempUnlockedSet, (string id) => PlayerCosmeticsSystem.LocalPlayerInTemporaryCosmeticSpace() || PlayerCosmeticsSystem.IsTemporaryCosmeticAllowed(localRig, id));
		GorillaTagger.Instance.offlineVRRig.LocalUpdateCosmeticsWithTryon(activeMergedSet, tryOnSet, playfx);
		if (!sync || !(GorillaTagger.Instance.myVRRig != null))
		{
			return;
		}
		if (isHidingCosmeticsFromRemotePlayers)
		{
			GorillaTagger.Instance.myVRRig.SendRPC("RPC_HideAllCosmetics", RpcTarget.All);
			return;
		}
		int[] array = activeMergedSet.ToPackedIDArray();
		int[] array2 = tryOnSet.ToPackedIDArray();
		GorillaTagger.Instance.myVRRig.SendRPC("RPC_UpdateCosmeticsWithTryonPacked", RpcTarget.Others, array, array2, playfx);
		CosmeticCollectionDisplay.GetDisplaysForRig(GorillaTagger.Instance.offlineVRRig, scratchDisplayList);
		if (scratchDisplayList.Count > 0)
		{
			int num = scratchDisplayList.Count * 3;
			if (cycleStatesArray.Length != num)
			{
				cycleStatesArray = new int[num];
			}
			for (int num2 = 0; num2 < scratchDisplayList.Count; num2++)
			{
				CosmeticCollectionDisplay cosmeticCollectionDisplay = scratchDisplayList[num2];
				string parentPlayFabID = cosmeticCollectionDisplay.ParentPlayFabID;
				cycleStatesArray[num2 * 3] = parentPlayFabID[0] - 65 + 26 * (parentPlayFabID[1] - 65 + 26 * (parentPlayFabID[2] - 65 + 26 * (parentPlayFabID[3] - 65 + 26 * (parentPlayFabID[4] - 65))));
				CosmeticItem? activeCollectable = cosmeticCollectionDisplay.ActiveCollectable;
				cycleStatesArray[num2 * 3 + 1] = (activeCollectable.HasValue ? GetCanonicalCollectableIndex(parentPlayFabID, activeCollectable.Value.itemName) : cosmeticCollectionDisplay.ActiveIndex);
				cycleStatesArray[num2 * 3 + 2] = cosmeticCollectionDisplay.VisibleMask;
			}
			GorillaTagger.Instance.myVRRig.SendRPC("RPC_UpdateCosmeticsWithCollectablesPacked", RpcTarget.Others, cycleStatesArray);
		}
	}

	public CosmeticItem GetItemFromDict(string itemID)
	{
		if (!allCosmeticsDict.TryGetValue(itemID, out cosmeticItemVar))
		{
			return nullItem;
		}
		return cosmeticItemVar;
	}

	public string GetItemNameFromDisplayName(string displayName)
	{
		if (displayName == "" || displayName == null)
		{
			return "null";
		}
		if (!allCosmeticsItemIDsfromDisplayNamesDict.TryGetValue(displayName, out returnString))
		{
			return "null";
		}
		return returnString;
	}

	public CosmeticSO GetCosmeticSOFromDisplayName(string displayName)
	{
		string itemNameFromDisplayName = GetItemNameFromDisplayName(displayName);
		if (itemNameFromDisplayName.Equals("null"))
		{
			return null;
		}
		AllCosmeticsArraySO allCosmeticsArraySO = v2_allCosmeticsInfoAssetRef.Asset as AllCosmeticsArraySO;
		if (allCosmeticsArraySO == null)
		{
			GTDev.LogWarning("null AllCosmeticsArraySO");
			return null;
		}
		CosmeticSO cosmeticSO = allCosmeticsArraySO.SearchForCosmeticSO(itemNameFromDisplayName);
		if (cosmeticSO != null)
		{
			return cosmeticSO;
		}
		GTDev.Log("Could not find cosmetic info for " + itemNameFromDisplayName);
		return null;
	}

	public CosmeticAnchorAntiIntersectOffsets GetClipOffsetsFromDisplayName(string displayName)
	{
		string itemNameFromDisplayName = GetItemNameFromDisplayName(displayName);
		if (itemNameFromDisplayName.Equals("null"))
		{
			return defaultClipOffsets;
		}
		AllCosmeticsArraySO allCosmeticsArraySO = v2_allCosmeticsInfoAssetRef.Asset as AllCosmeticsArraySO;
		if (allCosmeticsArraySO == null)
		{
			GTDev.LogWarning("null AllCosmeticsArraySO");
			return defaultClipOffsets;
		}
		CosmeticSO cosmeticSO = allCosmeticsArraySO.SearchForCosmeticSO(itemNameFromDisplayName);
		if (cosmeticSO != null)
		{
			return cosmeticSO.info.anchorAntiIntersectOffsets;
		}
		GTDev.Log("Could not find cosmetic info for " + itemNameFromDisplayName);
		return defaultClipOffsets;
	}

	public bool AnyMatch(CosmeticSet set, CosmeticItem item)
	{
		if (item.itemCategory != CosmeticCategory.Set)
		{
			return set.IsActive(item.displayName);
		}
		if (item.itemCategory == CosmeticCategory.Set && item.bundledItems != null)
		{
			if (item.bundledItems.Length == 1)
			{
				return AnyMatch(set, GetItemFromDict(item.bundledItems[0]));
			}
			if (item.bundledItems.Length == 2)
			{
				if (!AnyMatch(set, GetItemFromDict(item.bundledItems[0])))
				{
					return AnyMatch(set, GetItemFromDict(item.bundledItems[1]));
				}
				return true;
			}
			if (item.bundledItems.Length >= 3)
			{
				if (!AnyMatch(set, GetItemFromDict(item.bundledItems[0])) && !AnyMatch(set, GetItemFromDict(item.bundledItems[1])))
				{
					return AnyMatch(set, GetItemFromDict(item.bundledItems[2]));
				}
				return true;
			}
		}
		return false;
	}

	public void Initialize()
	{
		if (!base.gameObject.activeSelf)
		{
			return;
		}
		if (v2_allCosmeticsInfoAssetRef_isLoaded)
		{
			if (SubscriptionManager.LocalSubscriptionDataResolved)
			{
				GetCosmeticsPlayFabCatalogData();
				return;
			}
			SubscriptionManager.OnLocalSubscriptionDataResolved = (Action)Delegate.Remove(SubscriptionManager.OnLocalSubscriptionDataResolved, new Action(Initialize));
			SubscriptionManager.OnLocalSubscriptionDataResolved = (Action)Delegate.Combine(SubscriptionManager.OnLocalSubscriptionDataResolved, new Action(Initialize));
		}
		else
		{
			V2_allCosmeticsInfoAssetRef_OnPostLoad = (Action)Delegate.Remove(V2_allCosmeticsInfoAssetRef_OnPostLoad, new Action(Initialize));
			V2_allCosmeticsInfoAssetRef_OnPostLoad = (Action)Delegate.Combine(V2_allCosmeticsInfoAssetRef_OnPostLoad, new Action(Initialize));
		}
	}

	public void GetLastDailyLogin()
	{
		PlayFabClientAPI.GetUserReadOnlyData(new PlayFab.ClientModels.GetUserDataRequest(), delegate(GetUserDataResult result)
		{
			if (result.Data.TryGetValue("DailyLogin", out userDataRecord))
			{
				lastDailyLogin = userDataRecord.Value;
			}
			else
			{
				lastDailyLogin = "NONE";
				StartCoroutine(GetMyDaily());
			}
		}, delegate(PlayFabError error)
		{
			Debug.Log("Got error getting read-only user data:");
			Debug.Log(error.GenerateErrorReport());
			lastDailyLogin = "FAILED";
			if (error.Error == PlayFabErrorCode.NotAuthenticated)
			{
				PlayFabAuthenticator.instance.AuthenticateWithPlayFab();
			}
			else if (error.Error == PlayFabErrorCode.AccountBanned)
			{
				Application.Quit();
				NetworkSystem.Instance.ReturnToSinglePlayer();
				UnityEngine.Object.DestroyImmediate(PhotonNetworkController.Instance);
				UnityEngine.Object.DestroyImmediate(GTPlayer.Instance);
				GameObject[] array = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
				for (int i = 0; i < array.Length; i++)
				{
					UnityEngine.Object.Destroy(array[i]);
				}
			}
		});
	}

	private IEnumerator CheckCanGetDaily()
	{
		while (!KIDManager.InitialisationComplete)
		{
			yield return new WaitForSecondsRealtime(1f);
		}
		while (!PlayFabClientAPI.IsClientLoggedIn())
		{
			yield return new WaitForSecondsRealtime(1f);
		}
		while (true)
		{
			if (GorillaComputer.instance != null && GorillaComputer.instance.startupMillis != 0L)
			{
				currentTime = new DateTime((GorillaComputer.instance.startupMillis + (long)(Time.realtimeSinceStartup * 1000f)) * 10000);
				secondsUntilTomorrow = (int)(currentTime.AddDays(1.0).Date - currentTime).TotalSeconds;
				if (string.IsNullOrEmpty(lastDailyLogin))
				{
					GetLastDailyLogin();
				}
				else
				{
					string text = currentTime.ToString("o").Substring(0, 10);
					if (text == lastDailyLogin)
					{
						checkedDaily = true;
						gotMyDaily = true;
					}
					else if (text != lastDailyLogin)
					{
						checkedDaily = true;
						gotMyDaily = false;
						StartCoroutine(GetMyDaily());
					}
					else if (lastDailyLogin == "FAILED")
					{
						GetLastDailyLogin();
					}
				}
				secondsToWaitToCheckDaily = (checkedDaily ? 60f : 10f);
				UpdateCurrencyBoards();
				yield return new WaitForSecondsRealtime(secondsToWaitToCheckDaily);
			}
			else
			{
				yield return new WaitForSecondsRealtime(1f);
			}
		}
	}

	private IEnumerator GetMyDaily()
	{
		yield return new WaitForSecondsRealtime(10f);
		GorillaServer.Instance.TryDistributeCurrency(delegate
		{
			GetCurrencyBalance();
			GetLastDailyLogin();
		}, delegate(PlayFabError error)
		{
			if (error.Error == PlayFabErrorCode.NotAuthenticated)
			{
				PlayFabAuthenticator.instance.AuthenticateWithPlayFab();
			}
			else if (error.Error == PlayFabErrorCode.AccountBanned)
			{
				Application.Quit();
				NetworkSystem.Instance.ReturnToSinglePlayer();
				UnityEngine.Object.DestroyImmediate(PhotonNetworkController.Instance);
				UnityEngine.Object.DestroyImmediate(GTPlayer.Instance);
				GameObject[] array = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
				for (int i = 0; i < array.Length; i++)
				{
					UnityEngine.Object.Destroy(array[i]);
				}
			}
		});
	}

	public void GetCosmeticsPlayFabCatalogData()
	{
		if (!v2_allCosmeticsInfoAssetRef_isLoaded)
		{
			throw new Exception("Method `GetCosmeticsPlayFabCatalogData` was called before `v2_allCosmeticsInfoAssetRef` was loaded. Listen to callback `V2_allCosmeticsInfoAssetRef_OnPostLoad` or check `v2_allCosmeticsInfoAssetRef_isLoaded` before trying to get PlayFab catalog data.");
		}
		if (!SubscriptionManager.LocalSubscriptionDataResolved)
		{
			throw new Exception("Method `GetCosmeticsPlayFabCatalogData` was called before local subscription data was resolved. Listen to callback `OnLocalSubscriptionDataResolved` or check `LocalSubscriptionDataResolved` before trying to get PlayFab catalog data.");
		}
		if (catalogRequestInFlight)
		{
			catalogRequestRerunQueued = true;
			return;
		}
		catalogRequestInFlight = true;
		tryGetCatalogTwice = false;
		GetCosmeticsPlayFabCatalogDataInternal();
	}

	private void GetCosmeticsPlayFabCatalogDataInternal()
	{
		PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(), delegate(GetUserInventoryResult result)
		{
			PlayFabClientAPI.GetCatalogItems(new GetCatalogItemsRequest
			{
				CatalogVersion = catalog
			}, delegate(GetCatalogItemsResult getCatalogItemsResult)
			{
				try
				{
					unlockedCosmetics.Clear();
					unlockedHats.Clear();
					unlockedBadges.Clear();
					unlockedFaces.Clear();
					unlockedPaws.Clear();
					unlockedFurs.Clear();
					unlockedShirts.Clear();
					unlockedPants.Clear();
					unlockedArms.Clear();
					unlockedBacks.Clear();
					unlockedChests.Clear();
					unlockedTagFX.Clear();
					unlockedThrowables.Clear();
					catalogItems = getCatalogItemsResult.Catalog;
					foreach (CatalogItem catalogItem in catalogItems)
					{
						if (!BuilderSetManager.IsItemIDBuilderItem(catalogItem.ItemId))
						{
							searchIndex = allCosmetics.FindIndex((CosmeticItem x) => catalogItem.ItemId == x.itemName);
							if (searchIndex > -1)
							{
								tempStringArray = null;
								hasPrice = false;
								if (catalogItem.Bundle != null)
								{
									tempStringArray = catalogItem.Bundle.BundledItems.ToArray();
								}
								if (catalogItem.VirtualCurrencyPrices.TryGetValue(currencyName, out var value))
								{
									hasPrice = true;
								}
								CosmeticItem cosmetic = allCosmetics[searchIndex];
								cosmetic.itemName = catalogItem.ItemId;
								cosmetic.displayName = catalogItem.DisplayName;
								cosmetic.cost = (int)value;
								cosmetic.bundledItems = tempStringArray;
								cosmetic.canTryOn = hasPrice;
								if (cosmetic.itemCategory == CosmeticCategory.Paw)
								{
									CosmeticInfoV2 cosmeticInfoV = v2_allCosmetics[searchIndex];
									cosmetic.isThrowable = cosmeticInfoV.isThrowable && !cosmeticInfoV.hasWardrobeParts;
								}
								if (cosmetic.displayName == null)
								{
									string text = "null";
									if ((bool)allCosmetics[searchIndex].itemPicture)
									{
										text = allCosmetics[searchIndex].itemPicture.name;
									}
									string debugCosmeticSOName = v2_allCosmetics[searchIndex].debugCosmeticSOName;
									Debug.LogError($"Cosmetic encountered with a null displayName at index {searchIndex}! " + "Setting displayName to id: \"" + allCosmetics[searchIndex].itemName + "\". iconName=\"" + text + "\".cosmeticSOName=\"" + debugCosmeticSOName + "\". ");
									cosmetic.displayName = cosmetic.itemName;
								}
								V2_ConformCosmeticItemV1DisplayName(ref cosmetic);
								_allCosmetics[searchIndex] = cosmetic;
								_allCosmeticsDict[cosmetic.itemName] = cosmetic;
								_allCosmeticsItemIDsfromDisplayNamesDict[cosmetic.displayName] = cosmetic.itemName;
								_allCosmeticsItemIDsfromDisplayNamesDict[cosmetic.overrideDisplayName] = cosmetic.itemName;
							}
						}
					}
					for (int num = _allCosmetics.Count - 1; num > -1; num--)
					{
						tempItem = _allCosmetics[num];
						if (tempItem.itemCategory == CosmeticCategory.Set && tempItem.canTryOn)
						{
							string[] bundledItems = tempItem.bundledItems;
							foreach (string setItemName in bundledItems)
							{
								searchIndex = _allCosmetics.FindIndex((CosmeticItem x) => setItemName == x.itemName);
								if (searchIndex > -1)
								{
									tempItem = _allCosmetics[searchIndex];
									tempItem.canTryOn = true;
									_allCosmetics[searchIndex] = tempItem;
									_allCosmeticsDict[_allCosmetics[searchIndex].itemName] = tempItem;
									_allCosmeticsItemIDsfromDisplayNamesDict[_allCosmetics[searchIndex].displayName] = tempItem.itemName;
								}
							}
						}
					}
					foreach (KeyValuePair<string, StoreBundle> item2 in BundleManager.instance.storeBundlesById)
					{
						item2.Deconstruct(out var key, out var value2);
						string key2 = key;
						StoreBundle bundleData = value2;
						int num3 = _allCosmetics.FindIndex((CosmeticItem x) => bundleData.playfabBundleID == x.itemName);
						if (num3 > 0 && _allCosmetics[num3].bundledItems != null)
						{
							string[] bundledItems = _allCosmetics[num3].bundledItems;
							foreach (string setItemName2 in bundledItems)
							{
								searchIndex = _allCosmetics.FindIndex((CosmeticItem x) => setItemName2 == x.itemName);
								if (searchIndex > -1)
								{
									tempItem = _allCosmetics[searchIndex];
									tempItem.canTryOn = true;
									_allCosmetics[searchIndex] = tempItem;
									_allCosmeticsDict[_allCosmetics[searchIndex].itemName] = tempItem;
									_allCosmeticsItemIDsfromDisplayNamesDict[_allCosmetics[searchIndex].displayName] = tempItem.itemName;
								}
							}
						}
						if (!bundleData.HasPrice)
						{
							num3 = catalogItems.FindIndex((CatalogItem ci) => ci.Bundle != null && ci.ItemId == bundleData.playfabBundleID);
							if (num3 > 0)
							{
								if (catalogItems[num3].VirtualCurrencyPrices.TryGetValue("RM", out var value3))
								{
									BundleManager.instance.storeBundlesById[key2].TryUpdatePrice(value3);
								}
								else
								{
									BundleManager.instance.storeBundlesById[key2].TryUpdatePrice();
								}
							}
						}
					}
					searchIndex = _allCosmetics.FindIndex((CosmeticItem x) => "Slingshot" == x.itemName);
					if (searchIndex < 0)
					{
						throw new MissingReferenceException("CosmeticsController: Cannot find default slingshot! it is required for players that do not have another slingshot equipped and are playing Paintbrawl.");
					}
					_allCosmeticsDict["Slingshot"] = _allCosmetics[searchIndex];
					_allCosmeticsItemIDsfromDisplayNamesDict[_allCosmetics[searchIndex].displayName] = _allCosmetics[searchIndex].itemName;
					allCosmeticsDict_isInitialized = true;
					allCosmeticsItemIDsfromDisplayNamesDict_isInitialized = true;
					Dictionary<string, string> dictionary = new Dictionary<string, string>();
					foreach (ItemInstance item in result.Inventory)
					{
						if (!BuilderSetManager.IsItemIDBuilderItem(item.ItemId))
						{
							if (item.ItemId == m_earlyAccessSupporterPackCosmeticSO.info.playFabID)
							{
								CosmeticSO[] setCosmetics = m_earlyAccessSupporterPackCosmeticSO.info.setCosmetics;
								foreach (CosmeticSO cosmeticSO in setCosmetics)
								{
									if (allCosmeticsDict.TryGetValue(cosmeticSO.info.playFabID, out var value4))
									{
										unlockedCosmetics.Add(value4);
									}
								}
							}
							BundleManager.instance.MarkBundleOwnedByPlayFabID(item.ItemId);
							if (!dictionary.ContainsKey(item.ItemId))
							{
								searchIndex = allCosmetics.FindIndex((CosmeticItem x) => item.ItemId == x.itemName);
								if (searchIndex > -1)
								{
									dictionary[item.ItemId] = item.ItemId;
									unlockedCosmetics.Add(allCosmetics[searchIndex]);
								}
							}
						}
					}
					foreach (CosmeticItem unlockedCosmetic in unlockedCosmetics)
					{
						if (unlockedCosmetic.itemCategory == CosmeticCategory.Hat && !unlockedHats.Contains(unlockedCosmetic))
						{
							unlockedHats.Add(unlockedCosmetic);
						}
						else if (unlockedCosmetic.itemCategory == CosmeticCategory.Face && !unlockedFaces.Contains(unlockedCosmetic))
						{
							unlockedFaces.Add(unlockedCosmetic);
						}
						else if (unlockedCosmetic.itemCategory == CosmeticCategory.Badge && !unlockedBadges.Contains(unlockedCosmetic))
						{
							unlockedBadges.Add(unlockedCosmetic);
						}
						else if (unlockedCosmetic.itemCategory == CosmeticCategory.Paw)
						{
							if (!unlockedCosmetic.isThrowable && !unlockedPaws.Contains(unlockedCosmetic))
							{
								unlockedPaws.Add(unlockedCosmetic);
							}
							else if (unlockedCosmetic.isThrowable && !unlockedThrowables.Contains(unlockedCosmetic))
							{
								unlockedThrowables.Add(unlockedCosmetic);
							}
						}
						else if (unlockedCosmetic.itemCategory == CosmeticCategory.Fur && !unlockedFurs.Contains(unlockedCosmetic))
						{
							unlockedFurs.Add(unlockedCosmetic);
						}
						else if (unlockedCosmetic.itemCategory == CosmeticCategory.Shirt && !unlockedShirts.Contains(unlockedCosmetic))
						{
							unlockedShirts.Add(unlockedCosmetic);
						}
						else if (unlockedCosmetic.itemCategory == CosmeticCategory.Arms && !unlockedArms.Contains(unlockedCosmetic))
						{
							unlockedArms.Add(unlockedCosmetic);
						}
						else if (unlockedCosmetic.itemCategory == CosmeticCategory.Back && !unlockedBacks.Contains(unlockedCosmetic))
						{
							unlockedBacks.Add(unlockedCosmetic);
						}
						else if (unlockedCosmetic.itemCategory == CosmeticCategory.Chest && !unlockedChests.Contains(unlockedCosmetic))
						{
							unlockedChests.Add(unlockedCosmetic);
						}
						else if (unlockedCosmetic.itemCategory == CosmeticCategory.Pants && !unlockedPants.Contains(unlockedCosmetic))
						{
							unlockedPants.Add(unlockedCosmetic);
						}
						else if (unlockedCosmetic.itemCategory == CosmeticCategory.TagEffect && !unlockedTagFX.Contains(unlockedCosmetic))
						{
							unlockedTagFX.Add(unlockedCosmetic);
						}
						concatStringCosmeticsAllowed += unlockedCosmetic.itemName;
					}
					BuilderSetManager.instance.OnGotInventoryItems(result, getCatalogItemsResult);
					currencyBalance = result.VirtualCurrency[currencyName];
					playedInBeta = result.VirtualCurrency.TryGetValue("TC", out var value5) && value5 > 0;
					OnGetCurrency?.Invoke();
					BundleManager.instance.CheckIfBundlesOwned();
					StoreUpdater.instance.Initialize();
					currentWornSet.LoadFromPlayerPreferences(this);
					LoadSavedOutfits();
					if (!ATM_Manager.instance.alreadyBegan)
					{
						ATM_Manager.instance.SwitchToStage(ATM_Manager.ATMStages.Begin);
						ATM_Manager.instance.alreadyBegan = true;
					}
					ProcessPurchaseItemState(null, isLeftHand: false);
					UpdateShoppingCart();
					UpdateCurrencyBoards();
					ConfirmIndividualCosmeticsSharedGroup(result);
					OnCosmeticsUpdated?.Invoke();
					v2_isCosmeticPlayFabCatalogDataLoaded = true;
					V2_OnGetCosmeticsPlayFabCatalogData_PostSuccess?.Invoke();
					CosmeticsV2Spawner_Dirty.PrepareLoadOpInfos();
				}
				finally
				{
					CompleteGetCosmeticsPlayFabCatalogData();
				}
			}, delegate(PlayFabError error)
			{
				if (error.Error == PlayFabErrorCode.NotAuthenticated)
				{
					PlayFabAuthenticator.instance.AuthenticateWithPlayFab();
				}
				else if (error.Error == PlayFabErrorCode.AccountBanned)
				{
					Application.Quit();
					NetworkSystem.Instance.ReturnToSinglePlayer();
					UnityEngine.Object.DestroyImmediate(PhotonNetworkController.Instance);
					UnityEngine.Object.DestroyImmediate(GTPlayer.Instance);
					GameObject[] array = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
					for (int i = 0; i < array.Length; i++)
					{
						UnityEngine.Object.Destroy(array[i]);
					}
				}
				if (!tryGetCatalogTwice)
				{
					tryGetCatalogTwice = true;
					GetCosmeticsPlayFabCatalogDataInternal();
				}
				else
				{
					CompleteGetCosmeticsPlayFabCatalogData();
				}
			});
		}, delegate(PlayFabError error)
		{
			if (error.Error == PlayFabErrorCode.NotAuthenticated)
			{
				PlayFabAuthenticator.instance.AuthenticateWithPlayFab();
			}
			else if (error.Error == PlayFabErrorCode.AccountBanned)
			{
				Application.Quit();
				NetworkSystem.Instance.ReturnToSinglePlayer();
				UnityEngine.Object.DestroyImmediate(PhotonNetworkController.Instance);
				UnityEngine.Object.DestroyImmediate(GTPlayer.Instance);
				GameObject[] array = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
				for (int i = 0; i < array.Length; i++)
				{
					UnityEngine.Object.Destroy(array[i]);
				}
			}
			if (!tryGetCatalogTwice)
			{
				tryGetCatalogTwice = true;
				GetCosmeticsPlayFabCatalogDataInternal();
			}
			else
			{
				CompleteGetCosmeticsPlayFabCatalogData();
			}
		});
	}

	private void CompleteGetCosmeticsPlayFabCatalogData()
	{
		catalogRequestInFlight = false;
		if (catalogRequestRerunQueued)
		{
			catalogRequestRerunQueued = false;
			GetCosmeticsPlayFabCatalogData();
		}
	}

	public void SteamPurchase()
	{
		if (string.IsNullOrEmpty(itemToPurchase))
		{
			Debug.Log("Unable to start steam purchase process. itemToPurchase is not set.");
			return;
		}
		Debug.Log($"attempting to purchase item through steam. Is this a bundle purchase: {buyingBundle}");
		PlayFabClientAPI.StartPurchase(GetStartPurchaseRequest(), ProcessStartPurchaseResponse, ProcessSteamPurchaseError);
	}

	private StartPurchaseRequest GetStartPurchaseRequest()
	{
		return new StartPurchaseRequest
		{
			CatalogVersion = catalog,
			Items = new List<ItemPurchaseRequest>
			{
				new ItemPurchaseRequest
				{
					ItemId = itemToPurchase,
					Quantity = 1u,
					Annotation = "Purchased via in-game store"
				}
			}
		};
	}

	private void ProcessStartPurchaseResponse(StartPurchaseResult result)
	{
		Debug.Log("successfully started purchase. attempted to pay for purchase through steam");
		currentPurchaseID = result.OrderId;
		PlayFabClientAPI.PayForPurchase(GetPayForPurchaseRequest(currentPurchaseID), ProcessPayForPurchaseResult, ProcessSteamPurchaseError);
	}

	private static PayForPurchaseRequest GetPayForPurchaseRequest(string orderId)
	{
		return new PayForPurchaseRequest
		{
			OrderId = orderId,
			ProviderName = "Steam",
			Currency = "RM"
		};
	}

	private static void ProcessPayForPurchaseResult(PayForPurchaseResult result)
	{
		Debug.Log("succeeded on sending request for paying with steam! waiting for response");
	}

	private void ProcessSteamCallback(MicroTxnAuthorizationResponse_t callBackResponse)
	{
		if (SubscriptionKiosk.ProcessingSubscriptionPurchase)
		{
			return;
		}
		Debug.Log("Steam has called back that the user has finished the payment interaction");
		if (callBackResponse.m_bAuthorized == 0)
		{
			Debug.Log("Steam has indicated that the payment was not authorised.");
		}
		if (buyingBundle)
		{
			PlayFabClientAPI.ConfirmPurchase(GetConfirmBundlePurchaseRequest(), delegate
			{
				ProcessConfirmPurchaseSuccess();
			}, ProcessConfirmPurchaseError);
		}
		else
		{
			PlayFabClientAPI.ConfirmPurchase(GetConfirmATMPurchaseRequest(), delegate
			{
				ProcessConfirmPurchaseSuccess();
			}, ProcessConfirmPurchaseError);
		}
	}

	private ConfirmPurchaseRequest GetConfirmBundlePurchaseRequest()
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>
		{
			{
				"PlayerName",
				GorillaComputer.instance.savedName
			},
			{
				"Location",
				ConsumePurchaseLocation()
			}
		};
		if (validatedCreatorCode != null)
		{
			dictionary.Add("NexusCreatorId", validatedCreatorCode.memberCode);
			dictionary.Add("NexusGroupId", validatedCreatorCode.groupId);
		}
		return new ConfirmPurchaseRequest
		{
			OrderId = currentPurchaseID,
			CustomTags = dictionary
		};
	}

	private ConfirmPurchaseRequest GetConfirmATMPurchaseRequest()
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>
		{
			{
				"PlayerName",
				GorillaComputer.instance.savedName
			},
			{
				"Location",
				ConsumePurchaseLocation()
			}
		};
		if (validatedCreatorCode != null)
		{
			dictionary.Add("NexusCreatorId", validatedCreatorCode.memberCode);
			dictionary.Add("NexusGroupId", validatedCreatorCode.groupId);
		}
		return new ConfirmPurchaseRequest
		{
			OrderId = currentPurchaseID,
			CustomTags = dictionary
		};
	}

	private void ProcessConfirmPurchaseSuccess()
	{
		if (buyingBundle)
		{
			if (validatedCreatorCode != null && PushTerminalMessage != null)
			{
				PushTerminalMessage(validatedCreatorCode.terminalId, "THIS PURCHASE SUPPORTED\n" + CreatorCodes.supportedMember.name + "!");
			}
			buyingBundle = false;
			UpdateMyCosmetics();
			StartCoroutine(CheckIfMyCosmeticsUpdated(BundlePlayfabItemName));
		}
		else
		{
			ATM_Manager.instance.SwitchToStage(ATM_Manager.ATMStages.Success);
		}
		GetCurrencyBalance();
		UpdateCurrencyBoards();
		GetCosmeticsPlayFabCatalogData();
		GorillaTagger.Instance.offlineVRRig.GetCosmeticsPlayFabCatalogData();
	}

	private void ProcessConfirmPurchaseError(PlayFabError error)
	{
		ProcessSteamPurchaseError(error);
		ATM_Manager.instance.SwitchToStage(ATM_Manager.ATMStages.Failure);
		UpdateCurrencyBoards();
	}

	private void ProcessSteamPurchaseError(PlayFabError error)
	{
		switch (error.Error)
		{
		case PlayFabErrorCode.StoreNotFound:
			Debug.Log($"Attempted to load {itemToPurchase} from {catalog} but received an error: {error}");
			break;
		case PlayFabErrorCode.PurchaseInitializationFailure:
		case PlayFabErrorCode.InvalidPurchaseTransactionStatus:
		case PlayFabErrorCode.DuplicatePurchaseTransactionId:
			Debug.Log($"Attempted to pay for order {currentPurchaseID}, however received an error: {error}");
			break;
		case PlayFabErrorCode.InvalidPaymentProvider:
			Debug.Log($"Attempted to connect to steam as payment provider, but received error: {error}");
			break;
		case PlayFabErrorCode.InsufficientFunds:
			Debug.Log($"Attempting to do purchase through steam, steam has returned insufficient funds: {error}");
			break;
		case PlayFabErrorCode.PurchaseDoesNotExist:
			Debug.Log($"Attempting to confirm purchase for order {currentPurchaseID} but received error: {error}");
			break;
		case PlayFabErrorCode.FailedByPaymentProvider:
			Debug.Log($"Attempted to pay for order, but has been Failed by Steam with error: {error}");
			break;
		case PlayFabErrorCode.InternalServerError:
			Debug.Log($"PlayFab threw an internal server error: {error}");
			break;
		case PlayFabErrorCode.NotAuthenticated:
			PlayFabAuthenticator.instance.AuthenticateWithPlayFab();
			break;
		case PlayFabErrorCode.AccountBanned:
		{
			PhotonNetwork.Disconnect();
			UnityEngine.Object.DestroyImmediate(PhotonNetworkController.Instance);
			UnityEngine.Object.DestroyImmediate(GTPlayer.Instance);
			GameObject[] array = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
			for (int i = 0; i < array.Length; i++)
			{
				UnityEngine.Object.Destroy(array[i]);
			}
			Application.Quit();
			break;
		}
		default:
			Debug.Log($"Steam purchase flow returned error: {error}");
			break;
		}
		ATM_Manager.instance.SwitchToStage(ATM_Manager.ATMStages.Failure);
	}

	public void UpdateCurrencyBoards()
	{
		FormattedPurchaseText(finalLine);
		for (iterator = 0; iterator < currencyBoards.Count; iterator++)
		{
			if (currencyBoards[iterator].IsNotNull())
			{
				currencyBoards[iterator].UpdateCurrencyBoard(checkedDaily, gotMyDaily, currencyBalance, secondsUntilTomorrow);
			}
		}
	}

	public void AddCurrencyBoard(CurrencyBoard newCurrencyBoard)
	{
		if (!currencyBoards.Contains(newCurrencyBoard))
		{
			currencyBoards.Add(newCurrencyBoard);
			newCurrencyBoard.UpdateCurrencyBoard(checkedDaily, gotMyDaily, currencyBalance, secondsUntilTomorrow);
		}
	}

	public void RemoveCurrencyBoard(CurrencyBoard currencyBoardToRemove)
	{
		currencyBoards.Remove(currencyBoardToRemove);
	}

	public void GetCurrencyBalance()
	{
		PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(), delegate(GetUserInventoryResult result)
		{
			currencyBalance = result.VirtualCurrency[currencyName];
			UpdateCurrencyBoards();
			OnGetCurrency?.Invoke();
		}, delegate(PlayFabError error)
		{
			if (error.Error == PlayFabErrorCode.NotAuthenticated)
			{
				PlayFabAuthenticator.instance.AuthenticateWithPlayFab();
			}
			else if (error.Error == PlayFabErrorCode.AccountBanned)
			{
				Application.Quit();
				NetworkSystem.Instance.ReturnToSinglePlayer();
				UnityEngine.Object.DestroyImmediate(PhotonNetworkController.Instance);
				UnityEngine.Object.DestroyImmediate(GTPlayer.Instance);
				GameObject[] array = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
				for (int i = 0; i < array.Length; i++)
				{
					UnityEngine.Object.Destroy(array[i]);
				}
			}
		});
	}

	public string GetItemDisplayName(CosmeticItem item)
	{
		if (item.overrideDisplayName != null && item.overrideDisplayName != "")
		{
			return item.overrideDisplayName;
		}
		return item.displayName;
	}

	public void UpdateMyCosmetics()
	{
		if (!(GorillaServer.Instance == null))
		{
			GorillaServer.Instance.UpdateUserCosmetics();
		}
	}

	private void AlreadyOwnAllBundleButtons()
	{
		EarlyAccessButton[] array = earlyAccessButtons;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].AlreadyOwn();
		}
	}

	public void CheckCosmeticsSharedGroup()
	{
		updateCosmeticsRetries++;
		if (updateCosmeticsRetries < maxUpdateCosmeticsRetries)
		{
			StartCoroutine(WaitForNextCosmeticsAttempt());
		}
	}

	private IEnumerator WaitForNextCosmeticsAttempt()
	{
		int num = (int)Mathf.Pow(3f, updateCosmeticsRetries + 1);
		yield return new WaitForSecondsRealtime(num);
		ConfirmIndividualCosmeticsSharedGroup(latestInventory);
	}

	private void ConfirmIndividualCosmeticsSharedGroup(GetUserInventoryResult inventory)
	{
		latestInventory = inventory;
		if (PhotonNetwork.LocalPlayer.UserId == null)
		{
			StartCoroutine(WaitForNextCosmeticsAttempt());
			return;
		}
		PlayFabClientAPI.GetSharedGroupData(new PlayFab.ClientModels.GetSharedGroupDataRequest
		{
			Keys = new List<string> { "Inventory" },
			SharedGroupId = PhotonNetwork.LocalPlayer.UserId + "Inventory"
		}, delegate(GetSharedGroupDataResult result)
		{
			bool flag = true;
			foreach (KeyValuePair<string, PlayFab.ClientModels.SharedGroupDataRecord> datum in result.Data)
			{
				foreach (ItemInstance item in inventory.Inventory)
				{
					if (item.CatalogVersion == instance.catalog && !datum.Value.Value.Contains(item.ItemId))
					{
						flag = false;
						break;
					}
				}
			}
			if (!flag || result.Data.Count == 0)
			{
				UpdateMyCosmetics();
			}
			else
			{
				updateCosmeticsRetries = 0;
			}
		}, delegate(PlayFabError error)
		{
			ReauthOrBan(error);
			CheckCosmeticsSharedGroup();
		});
	}

	public void ReauthOrBan(PlayFabError error)
	{
		if (error.Error == PlayFabErrorCode.NotAuthenticated)
		{
			PlayFabAuthenticator.instance.AuthenticateWithPlayFab();
		}
		else if (error.Error == PlayFabErrorCode.AccountBanned)
		{
			Application.Quit();
			PhotonNetwork.Disconnect();
			UnityEngine.Object.DestroyImmediate(PhotonNetworkController.Instance);
			UnityEngine.Object.DestroyImmediate(GTPlayer.Instance);
			GameObject[] array = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
			for (int i = 0; i < array.Length; i++)
			{
				UnityEngine.Object.Destroy(array[i]);
			}
		}
	}

	public void ProcessExternalUnlock(string itemID, bool autoEquip, bool isLeftHand)
	{
		UnlockItem(itemID);
		GorillaTagger.Instance.offlineVRRig.AddCosmetic(itemID);
		UpdateMyCosmetics();
		if (!autoEquip)
		{
			return;
		}
		CosmeticItem itemFromDict = GetItemFromDict(itemID);
		GorillaTelemetry.PostShopEvent(GorillaTagger.Instance.offlineVRRig, GTShopEventType.external_item_claim, itemFromDict);
		List<CosmeticSlots> list = CollectionPool<List<CosmeticSlots>, CosmeticSlots>.Get();
		if (list.Capacity < 16)
		{
			list.Capacity = 16;
		}
		ApplyCosmeticItemToSet(currentWornSet, itemFromDict, isLeftHand, applyToPlayerPrefs: true, list);
		foreach (CosmeticSlots item in list)
		{
			tryOnSet.items[(int)item] = nullItem;
		}
		CollectionPool<List<CosmeticSlots>, CosmeticSlots>.Release(list);
		UpdateShoppingCart();
		UpdateWornCosmetics(sync: true);
		OnCosmeticsUpdated?.Invoke();
	}

	public void AddTempUnlockToWardrobe(string cosmeticID)
	{
		int num = allCosmetics.FindIndex((CosmeticItem x) => cosmeticID == x.itemName);
		if (num < 0)
		{
			return;
		}
		switch (allCosmetics[num].itemCategory)
		{
		case CosmeticCategory.Hat:
			ModifyUnlockList(unlockedHats, num, relock: false);
			break;
		case CosmeticCategory.Fur:
			ModifyUnlockList(unlockedFurs, num, relock: false);
			break;
		case CosmeticCategory.Badge:
			ModifyUnlockList(unlockedBadges, num, relock: false);
			break;
		case CosmeticCategory.Face:
			ModifyUnlockList(unlockedFaces, num, relock: false);
			break;
		case CosmeticCategory.Chest:
			ModifyUnlockList(unlockedChests, num, relock: false);
			break;
		case CosmeticCategory.Paw:
			if (!allCosmetics[num].isThrowable)
			{
				ModifyUnlockList(unlockedPaws, num, relock: false);
			}
			else
			{
				ModifyUnlockList(unlockedThrowables, num, relock: false);
			}
			break;
		case CosmeticCategory.Shirt:
			ModifyUnlockList(unlockedShirts, num, relock: false);
			break;
		case CosmeticCategory.Back:
			ModifyUnlockList(unlockedBacks, num, relock: false);
			break;
		case CosmeticCategory.Arms:
			ModifyUnlockList(unlockedArms, num, relock: false);
			break;
		case CosmeticCategory.Pants:
			ModifyUnlockList(unlockedPants, num, relock: false);
			break;
		case CosmeticCategory.TagEffect:
			ModifyUnlockList(unlockedTagFX, num, relock: false);
			break;
		case CosmeticCategory.Set:
		{
			string[] bundledItems = allCosmetics[num].bundledItems;
			foreach (string cosmeticID2 in bundledItems)
			{
				AddTempUnlockToWardrobe(cosmeticID2);
			}
			break;
		}
		case CosmeticCategory.Count:
			break;
		}
	}

	public void RemoveTempUnlockFromWardrobe(string cosmeticID)
	{
		int num = allCosmetics.FindIndex((CosmeticItem x) => cosmeticID == x.itemName);
		if (num < 0)
		{
			return;
		}
		switch (allCosmetics[num].itemCategory)
		{
		case CosmeticCategory.Hat:
			ModifyUnlockList(unlockedHats, num, relock: true);
			break;
		case CosmeticCategory.Fur:
			ModifyUnlockList(unlockedFurs, num, relock: true);
			break;
		case CosmeticCategory.Badge:
			ModifyUnlockList(unlockedBadges, num, relock: true);
			break;
		case CosmeticCategory.Face:
			ModifyUnlockList(unlockedFaces, num, relock: true);
			break;
		case CosmeticCategory.Chest:
			ModifyUnlockList(unlockedChests, num, relock: true);
			break;
		case CosmeticCategory.Paw:
			if (!allCosmetics[num].isThrowable)
			{
				ModifyUnlockList(unlockedPaws, num, relock: true);
			}
			else
			{
				ModifyUnlockList(unlockedThrowables, num, relock: true);
			}
			break;
		case CosmeticCategory.Shirt:
			ModifyUnlockList(unlockedShirts, num, relock: true);
			break;
		case CosmeticCategory.Back:
			ModifyUnlockList(unlockedBacks, num, relock: true);
			break;
		case CosmeticCategory.Arms:
			ModifyUnlockList(unlockedArms, num, relock: true);
			break;
		case CosmeticCategory.Pants:
			ModifyUnlockList(unlockedPants, num, relock: true);
			break;
		case CosmeticCategory.TagEffect:
			ModifyUnlockList(unlockedTagFX, num, relock: true);
			break;
		case CosmeticCategory.Set:
		{
			string[] bundledItems = allCosmetics[num].bundledItems;
			foreach (string cosmeticID2 in bundledItems)
			{
				RemoveTempUnlockFromWardrobe(cosmeticID2);
			}
			break;
		}
		case CosmeticCategory.Count:
			break;
		}
	}

	public bool BuildValidationCheck()
	{
		if (m_earlyAccessSupporterPackCosmeticSO == null)
		{
			Debug.LogError("m_earlyAccessSupporterPackCosmeticSO is empty, everything will break!");
			return false;
		}
		return true;
	}

	public void SetHideCosmeticsFromRemotePlayers(bool hideCosmetics)
	{
		if (hideCosmetics != isHidingCosmeticsFromRemotePlayers)
		{
			isHidingCosmeticsFromRemotePlayers = hideCosmetics;
			GorillaTagger.Instance.offlineVRRig.reliableState.SetIsDirty();
			UpdateWornCosmetics(sync: true);
		}
	}

	public bool ValidatePackedItems(int[] packed)
	{
		if (packed == null)
		{
			return false;
		}
		if (packed.Length == 0)
		{
			return true;
		}
		int num = 0;
		int num2 = packed[0];
		for (int i = 0; i < 16; i++)
		{
			if ((num2 & (1 << i)) != 0)
			{
				num++;
			}
		}
		return packed.Length == num + 1;
	}

	public static int[] PackCollectableItems(List<CosmeticItem> items)
	{
		if (items == null || items.Count == 0)
		{
			return Array.Empty<int>();
		}
		int[] array = new int[items.Count];
		for (int i = 0; i < items.Count; i++)
		{
			string itemName = items[i].itemName;
			array[i] = itemName[0] - 65 + 26 * (itemName[1] - 65 + 26 * (itemName[2] - 65 + 26 * (itemName[3] - 65 + 26 * (itemName[4] - 65))));
		}
		return array;
	}

	public CosmeticItem[] UnpackCollectableItems(int[] packed)
	{
		if (packed == null || packed.Length == 0)
		{
			return Array.Empty<CosmeticItem>();
		}
		char[] array = new char[6] { '\0', '\0', '\0', '\0', '\0', '.' };
		CosmeticItem[] array2 = new CosmeticItem[packed.Length];
		for (int i = 0; i < packed.Length; i++)
		{
			int num = packed[i];
			array[0] = (char)(65 + num % 26);
			array[1] = (char)(65 + num / 26 % 26);
			array[2] = (char)(65 + num / 676 % 26);
			array[3] = (char)(65 + num / 17576 % 26);
			array[4] = (char)(65 + num / 456976 % 26);
			array2[i] = GetItemFromDict(new string(array));
		}
		return array2;
	}

	public void SetValidatedCreatorCode(string memberCode, string groupCode, string terminalId)
	{
		validatedCreatorCode = new ValidatedCreatorCode();
		validatedCreatorCode.memberCode = memberCode;
		validatedCreatorCode.groupId = groupCode;
		validatedCreatorCode.terminalId = terminalId;
	}

	public static bool CanScrollOutfits()
	{
		if (loadedSavedOutfits)
		{
			return !saveOutfitInProgress;
		}
		return false;
	}

	public void PressWardrobeScrollOutfit(bool forward)
	{
		int num = selectedOutfit;
		if (forward)
		{
			num = (num + 1) % maxOutfits;
		}
		else
		{
			num--;
			if (num < 0)
			{
				num = maxOutfits - 1;
			}
		}
		LoadSavedOutfit(num);
	}

	public void LoadSavedOutfit(int newOutfitIndex)
	{
		if (!CanScrollOutfits() || newOutfitIndex == selectedOutfit || newOutfitIndex < 0 || newOutfitIndex >= maxOutfits)
		{
			return;
		}
		savedOutfits[selectedOutfit].CopyItems(currentWornSet);
		savedColors[selectedOutfit] = new Vector3(VRRig.LocalRig.playerColor.r, VRRig.LocalRig.playerColor.g, VRRig.LocalRig.playerColor.b);
		SaveOutfitsToMothership();
		selectedOutfit = newOutfitIndex;
		PlayerPrefs.SetInt(outfitSystemConfig.selectedOutfitPref, selectedOutfit);
		PlayerPrefs.Save();
		CosmeticSet outfit = savedOutfits[selectedOutfit];
		bool flag = true;
		for (int i = 0; i < 16; i++)
		{
			CosmeticSlots cosmeticSlots = (CosmeticSlots)i;
			if ((cosmeticSlots != CosmeticSlots.ArmLeft && cosmeticSlots != CosmeticSlots.ArmRight) || flag)
			{
				ApplyNewItem(outfit, i);
			}
		}
		UpdateMonkeColor(savedColors[selectedOutfit], saveToPrefs: true);
		SaveCurrentItemPreferences();
		UpdateShoppingCart();
		UpdateWornCosmetics(sync: true, playfx: true);
		UpdateWardrobeModelsAndButtons();
		OnCosmeticsUpdated?.Invoke();
	}

	private void ApplyNewItem(CosmeticSet outfit, int i)
	{
		currentWornSet.items[i] = outfit.items[i];
		if (!outfit.items[i].isNullItem)
		{
			tryOnSet.items[i] = nullItem;
		}
	}

	private async void LoadSavedOutfits()
	{
		try
		{
			while (!SubscriptionManager.LocalSubscriptionDataResolved)
			{
				await Task.Yield();
			}
			maxOutfits = (SubscriptionManager.IsLocalSubscribed() ? outfitSystemConfig.subscriberMaxOutfits : outfitSystemConfig.nonSubscriberMaxOutfits);
			if (!loadedSavedOutfits && !loadOutfitsInProgress)
			{
				loadOutfitsInProgress = true;
				savedOutfits = new CosmeticSet[maxOutfits];
				savedColors = new Vector3[maxOutfits];
				if (!MothershipClientApiUnity.GetUserDataValue(outfitSystemConfig.mothershipKey, GetSavedOutfitsSuccess, GetSavedOutfitsFail))
				{
					GTDev.LogError("CosmeticsController LoadSavedOutfits GetUserDataValue failed");
					ClearOutfits();
					loadOutfitsInProgress = false;
					loadedSavedOutfits = true;
					OnOutfitsUpdated?.Invoke();
				}
			}
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
		}
	}

	private void GetSavedOutfitsSuccess(MothershipUserData response)
	{
		if (response != null && response.value != null && response.value.Length > 0)
		{
			try
			{
				byte[] bytes = Convert.FromBase64String(response.value);
				outfitStringMothership = Encoding.UTF8.GetString(bytes);
				StringToOutfits(outfitStringMothership);
			}
			catch (Exception ex)
			{
				GTDev.LogError("CosmeticsController GetSavedOutfitsSuccess error decoding " + ex.Message);
				ClearOutfits();
			}
		}
		else
		{
			ClearOutfits();
		}
		GetSavedOutfitsComplete();
	}

	private void GetSavedOutfitsFail(MothershipError error, int status)
	{
		GTDev.LogError($"CosmeticsController GetSavedOutfitsFail {status} {error.Message}");
		ClearOutfits();
		GetSavedOutfitsComplete();
	}

	private void GetSavedOutfitsComplete()
	{
		int num = PlayerPrefs.GetInt(outfitSystemConfig.selectedOutfitPref, 0);
		if (num < 0 || num >= maxOutfits)
		{
			num = 0;
		}
		else
		{
			CosmeticSet cosmeticSet = new CosmeticSet();
			cosmeticSet.LoadFromPlayerPreferences(this);
			if (cosmeticSet.HasAnyItems())
			{
				savedOutfits[num].CopyItems(cosmeticSet);
			}
			float num2 = PlayerPrefs.GetFloat("redValue", 0f);
			float num3 = PlayerPrefs.GetFloat("greenValue", 0f);
			float num4 = PlayerPrefs.GetFloat("blueValue", 0f);
			if (num2 > 0f || num3 > 0f || num4 > 0f)
			{
				savedColors[num] = new Vector3(num2, num3, num4);
			}
		}
		selectedOutfit = num;
		currentWornSet.CopyItems(savedOutfits[selectedOutfit]);
		UpdateMonkeColor(savedColors[selectedOutfit], saveToPrefs: true);
		loadedSavedOutfits = true;
		loadOutfitsInProgress = false;
		OnOutfitsUpdated?.Invoke();
	}

	private void UpdateMonkeColor(Vector3 col, bool saveToPrefs)
	{
		float num = Mathf.Clamp(col.x, 0f, 1f);
		float num2 = Mathf.Clamp(col.y, 0f, 1f);
		float num3 = Mathf.Clamp(col.z, 0f, 1f);
		GorillaTagger.Instance.UpdateColor(num, num2, num3);
		GorillaComputer.instance.UpdateColor(num, num2, num3);
		if (OnPlayerColorSet != null)
		{
			OnPlayerColorSet(num, num2, num3);
		}
		if (NetworkSystem.Instance.InRoom)
		{
			GorillaTagger.Instance.myVRRig.SendRPC("RPC_InitializeNoobMaterial", RpcTarget.All, num, num2, num3);
		}
		if (saveToPrefs)
		{
			PlayerPrefs.SetFloat("redValue", num);
			PlayerPrefs.SetFloat("greenValue", num2);
			PlayerPrefs.SetFloat("blueValue", num3);
			PlayerPrefs.Save();
		}
	}

	private void SaveOutfitsToMothership()
	{
		if (!loadedSavedOutfits || saveOutfitInProgress)
		{
			return;
		}
		string mothershipKey = outfitSystemConfig.mothershipKey;
		outfitStringPendingSave = OutfitsToString();
		if (!outfitStringPendingSave.Equals(outfitStringMothership))
		{
			saveOutfitInProgress = true;
			if (!MothershipClientApiUnity.SetUserDataValue(mothershipKey, outfitStringPendingSave, SaveOutfitsToMothershipSuccess, SaveOutfitsToMothershipFail))
			{
				GTDev.LogError("CosmeticsController SaveOutfitToMothership SetUserDataValue failed");
				saveOutfitInProgress = false;
			}
		}
	}

	private void SaveOutfitsToMothershipSuccess(SetUserDataResponse response)
	{
		outfitStringMothership = outfitStringPendingSave;
		saveOutfitInProgress = false;
		OnOutfitsUpdated?.Invoke();
		response.Dispose();
	}

	private void SaveOutfitsToMothershipFail(MothershipError error, int status)
	{
		GTDev.LogError($"CosmeticsController SaveOutfitsToMothershipFail {status} " + error.Message);
		saveOutfitInProgress = false;
	}

	private string OutfitsToString()
	{
		if (!loadedSavedOutfits)
		{
			return string.Empty;
		}
		outfitDataTemp = new OutfitData();
		sb.Clear();
		for (int i = 0; i < savedOutfits.Length; i++)
		{
			outfitDataTemp.Clear();
			CosmeticSet cosmeticSet = savedOutfits[i];
			for (int j = 0; j < cosmeticSet.items.Length; j++)
			{
				CosmeticItem cosmeticItem = cosmeticSet.items[j];
				string item = ((cosmeticItem.isNullItem || string.IsNullOrEmpty(cosmeticItem.displayName)) ? "null" : cosmeticItem.displayName);
				outfitDataTemp.itemIDs.Add(item);
			}
			if (VRRig.LocalRig != null)
			{
				outfitDataTemp.color = savedColors[i];
			}
			sb.Append(JsonUtility.ToJson(outfitDataTemp));
			if (i < savedOutfits.Length - 1)
			{
				sb.Append(outfitSystemConfig.outfitSeparator);
			}
		}
		return sb.ToString();
	}

	private void ClearOutfits()
	{
		for (int i = 0; i < savedOutfits.Length; i++)
		{
			savedOutfits[i] = new CosmeticSet();
			savedOutfits[i].ClearSet(nullItem);
			savedColors[i] = defaultColor;
		}
	}

	private void StringToOutfits(string response)
	{
		if (response.IsNullOrEmpty())
		{
			ClearOutfits();
			return;
		}
		try
		{
			string[] array = response.Split(outfitSystemConfig.outfitSeparator);
			for (int i = 0; i < maxOutfits; i++)
			{
				savedOutfits[i] = new CosmeticSet();
				if (i >= array.Length)
				{
					savedOutfits[i].ClearSet(nullItem);
					savedColors[i] = defaultColor;
					continue;
				}
				string text = array[i];
				if (text.IsNullOrEmpty())
				{
					savedOutfits[i].ClearSet(nullItem);
					savedColors[i] = defaultColor;
				}
				else
				{
					savedOutfits[i].ParseSetFromString(this, text, out var color);
					savedColors[i] = color;
				}
			}
		}
		catch (Exception ex)
		{
			GTDev.LogError("CosmeticsController StringToOutfit Error parsing " + ex.Message);
			ClearOutfits();
		}
	}
}
