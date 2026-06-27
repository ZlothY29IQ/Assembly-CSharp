using System;
using GorillaNetworking;
using UnityEngine;

namespace GorillaTag.CosmeticSystem;

[Serializable]
public struct CosmeticInfoV2 : ISerializationCallbackReceiver
{
	public bool enabled;

	[Tooltip("// TODO: (2024-09-27 MattO) season will determine what addressables bundle it will be in and wheter it should be active based on release time of season.\n\nThe assigned season will determine what folder the Cosmetic will go in and how it will be listed in the Cosmetic Browser.")]
	[Delayed]
	public SeasonSO season;

	[Tooltip("Name that is displayed in the store during purchasing.")]
	[Delayed]
	public string displayName;

	[Tooltip("ID used on the PlayFab servers that must be unique. If this does not exist on the playfab servers then an error will be thrown. In notion search for \"Cosmetics - Adding a PlayFab ID\".")]
	[Delayed]
	public string playFabID;

	public Sprite icon;

	[Tooltip("Category determines which category button in the user's wardrobe (which are the two rows of buttons with equivalent names) have to be pressed to access the cosmetic along with others in the same category.")]
	public StringEnum<CosmeticsController.CosmeticCategory> category;

	[Obsolete("(2024-08-13 MattO) Will be removed after holdables array is fully implemented. Check length of `holdableParts` instead.")]
	[HideInInspector]
	public bool isHoldable;

	public bool isThrowable;

	[HideInInspector]
	public int[] throwableMaterialGrabIndices;

	[HideInInspector]
	public int throwableIndex;

	public bool usesBothHandSlots;

	public bool hideWardrobeMannequin;

	public const string holdableParts_infoBoxShortMsg = "\"Holdable Parts\" must have a Holdable component (or inherits like TransferrableObject).";

	public const string holdableParts_infoBoxDetailedMsg = "\"Holdable Parts\" must have a Holdable component (or inherits like TransferrableObject).\n\nHoldables are prefabs that have Holdable components. The prefab asset's transform will be moved between the listed \n attach points on \"Gorilla Player Networked.prefab\" when grabbed by the player \n";

	[Space]
	[Tooltip("\"Holdable Parts\" must have a Holdable component (or inherits like TransferrableObject).\n\nHoldables are prefabs that have Holdable components. The prefab asset's transform will be moved between the listed \n attach points on \"Gorilla Player Networked.prefab\" when grabbed by the player \n")]
	public CosmeticPart[] holdableParts;

	public const string functionalParts_infoBoxShortMsg = "\"Wearable Parts\" will be attached to \"Gorilla Player Networked.prefab\" instances.";

	public const string functionalParts_infoBoxDetailedMsg = "\"Wearable Parts\" will be attached to \"Gorilla Player Networked.prefab\" instances.\n\nThese individual parts which also handle the core functionality of the cosmetic. In most cases there will only be one part, there can be multiple parts for cases like rings which might be on both left and right hands.\n\nThese parts will be parented to the bones of  \"Gorilla Player Networked.prefab\" instances which includes the VRRig component.\n\nIf a \"First Person View\" part or \"Local Rig Part\" is set it will be enabled instead of the wearable parts for the local player";

	[Space]
	[Tooltip("\"Wearable Parts\" will be attached to \"Gorilla Player Networked.prefab\" instances.\n\nThese individual parts which also handle the core functionality of the cosmetic. In most cases there will only be one part, there can be multiple parts for cases like rings which might be on both left and right hands.\n\nThese parts will be parented to the bones of  \"Gorilla Player Networked.prefab\" instances which includes the VRRig component.\n\nIf a \"First Person View\" part or \"Local Rig Part\" is set it will be enabled instead of the wearable parts for the local player")]
	public CosmeticPart[] functionalParts;

	public const string wardrobeParts_infoBoxShortMsg = "\"Wardrobe Parts\" will be attached to \"Head Model.prefab\" instances.";

	public const string wardrobeParts_infoBoxDetailedMsg = "\"Wardrobe Parts\" will be attached to \"Head Model.prefab\" instances.\n\nThese parts should be static meshes not skinned and not have any scripts attached. They should only be simple visual representations.\n\nThese prefabs are shown on the satellite wardrobe, and in the store (if \"Store Parts\" is left empty)";

	[Space]
	[Tooltip("\"Wardrobe Parts\" will be attached to \"Head Model.prefab\" instances.\n\nThese parts should be static meshes not skinned and not have any scripts attached. They should only be simple visual representations.\n\nThese prefabs are shown on the satellite wardrobe, and in the store (if \"Store Parts\" is left empty)")]
	public CosmeticPart[] wardrobeParts;

	public const string storeParts_infoBoxShortMsg = "\"Store Parts\" are spawned into the Dynamic Cosmetic Stands in city.";

	public const string storeParts_infoBoxDetailedMsg = "\"Store Parts\" are spawned into the Dynamic Cosmetic Stands in city.\nStore parts only need to be specified if the store display should be different than the wardrobe display";

	[Space]
	[Tooltip("\"Store Parts\" are spawned into the Dynamic Cosmetic Stands in city.\nStore parts only need to be specified if the store display should be different than the wardrobe display")]
	public CosmeticPart[] storeParts;

	public const string firstPersonViewParts_infoBoxShortMsg = "\"First Person View Parts\" will be attached to the local monke's camera.\nFirst person parts are enabled instead of \"Wearable Parts\" for the local player";

	public const string firstPersonViewParts_infoBoxDetailedMsg = "\"First Person View Parts\" will be attached to the local monke's camera.\nFirst person parts are enabled instead of \"Wearable Parts\" for the local player\nThese are used for any peripheral view meshes on the No Mirror layer, usually on HAT or FACE items";

	[Space]
	[Tooltip("\"First Person View Parts\" will be attached to the local monke's camera.\nFirst person parts are enabled instead of \"Wearable Parts\" for the local player\nThese are used for any peripheral view meshes on the No Mirror layer, usually on HAT or FACE items")]
	public CosmeticPart[] firstPersonViewParts;

	public const string localRigParts_infoBoxShortMsg = "\"Local Mirror Parts\" will be attached to the local player's rig instead of \"Wearable Parts\".";

	public const string localRigParts_infoBoxDetailedMsg = "\"Local Mirror Parts\" will be attached to the local player's rig instead of \"Wearable Parts\".\nThese objects can be used in addition to first person view parts.\nThese can be used for mirror view meshes (usually HAT or FACE items)\nAny item with GTPosRotConstraints should be parented to the rig and not the camera";

	[Space]
	[Tooltip("\"Local Mirror Parts\" will be attached to the local player's rig instead of \"Wearable Parts\".\nThese objects can be used in addition to first person view parts.\nThese can be used for mirror view meshes (usually HAT or FACE items)\nAny item with GTPosRotConstraints should be parented to the rig and not the camera")]
	public CosmeticPart[] localRigParts;

	[Space]
	[Tooltip("When this cosmetic is equipped, these offsets will be applied to the other objects on the player that are likely to clip\nSHIRT items ususally offset the badge, nametag, and chest items\n PAW items usually offset the hunt computer and builder watch")]
	public CosmeticAnchorAntiIntersectOffsets anchorAntiIntersectOffsets;

	[Space]
	[Tooltip("TODO COMMENT")]
	public CosmeticSO[] setCosmetics;

	[Space]
	[Tooltip("For parent (collection) cosmetics: the slots that collectables snap into. Each entry defines the slot type and its local space offset from the cosmetic's root. Edit slot positions visually via the Cosmetic Editor Stage. The slot count is implicit from this array's length.")]
	public CosmeticCollectionSlotDefinition[] collectionSlots;

	[Tooltip("For parent (collection) cosmetics: when true only one collectable is visible at a time and the player can cycle through them. When false all slots are shown simultaneously")]
	public bool collectionIsCycling;

	[Tooltip("[Non-cycling collections only] When true, each sub-item is placed at the specific physical slot position declared by its 'Target Slot Index', rather than filling slots in purchase order. Use this when sub-items must always occupy a fixed dedicated position on the parent ")]
	public bool collectionUsesIndexTargeting;

	[Tooltip("[Cycling collections only] When true, sub-items are cycled through in ascending Series Index order rather than purchase order, regardless of when they were acquired. Gaps in the series are skipped only owned items appear, but always in their correct numbered sequence (e.g. comic reade always cycle as #1 → #2 → #3 even if bought out of order).")]
	public bool collectionUsesSeriesOrder;

	[Space]
	[Tooltip("For sub-item (collectable) cosmetics: the parent cosmetics this sub-item attaches to. A sub-item can list multiple parents; it is shown on every listed parent that is equipped, and each entry carries its own target slot index, so the same sub-item can occupy a different slot on different parents. At least one listed parent must be owned before this sub-item can be purchased. Leave empty if this is not a sub-item.")]
	public CosmeticCollectionParentLink[] collectionParentLinks;

	[HideInInspector]
	public string collectionParentPlayFabID;

	[HideInInspector]
	public int collectionTargetSlotIndex;

	[HideInInspector]
	public int collectionSeriesIndex;

	[Tooltip("PlayFab ID of the cosmetic to apply to a hit player via Cosmetic Swapper (e.g. chicken sword) tech. Distinct from this sub-item's own visual.")]
	public string appliedCosmeticPlayFabID;

	[NonSerialized]
	public string debugCosmeticSOName;

	public bool hasHoldableParts
	{
		get
		{
			CosmeticPart[] array = holdableParts;
			if (array != null)
			{
				return array.Length > 0;
			}
			return false;
		}
	}

	public bool hasWardrobeParts
	{
		get
		{
			CosmeticPart[] array = wardrobeParts;
			if (array != null)
			{
				return array.Length > 0;
			}
			return false;
		}
	}

	public bool hasStoreParts
	{
		get
		{
			CosmeticPart[] array = storeParts;
			if (array != null)
			{
				return array.Length > 0;
			}
			return false;
		}
	}

	public bool hasFunctionalParts
	{
		get
		{
			CosmeticPart[] array = functionalParts;
			if (array != null)
			{
				return array.Length > 0;
			}
			return false;
		}
	}

	public bool hasFirstPersonViewParts
	{
		get
		{
			CosmeticPart[] array = firstPersonViewParts;
			if (array != null)
			{
				return array.Length > 0;
			}
			return false;
		}
	}

	public bool hasLocalRigParts
	{
		get
		{
			CosmeticPart[] array = localRigParts;
			if (array != null)
			{
				return array.Length > 0;
			}
			return false;
		}
	}

	public bool isCollectionSubItem
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

	public bool IsSubItemOfParent(string parentPlayFabID)
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

	public CosmeticInfoV2(string displayName)
	{
		enabled = true;
		season = null;
		this.displayName = displayName;
		playFabID = "";
		category = CosmeticsController.CosmeticCategory.None;
		icon = null;
		isHoldable = false;
		isThrowable = false;
		usesBothHandSlots = false;
		hideWardrobeMannequin = false;
		holdableParts = new CosmeticPart[0];
		functionalParts = new CosmeticPart[0];
		wardrobeParts = new CosmeticPart[0];
		storeParts = new CosmeticPart[0];
		firstPersonViewParts = new CosmeticPart[0];
		localRigParts = new CosmeticPart[0];
		setCosmetics = new CosmeticSO[0];
		collectionSlots = Array.Empty<CosmeticCollectionSlotDefinition>();
		collectionIsCycling = false;
		collectionParentLinks = Array.Empty<CosmeticCollectionParentLink>();
		collectionParentPlayFabID = string.Empty;
		collectionTargetSlotIndex = -1;
		anchorAntiIntersectOffsets = default(CosmeticAnchorAntiIntersectOffsets);
		debugCosmeticSOName = "__UNINITIALIZED__";
		throwableMaterialGrabIndices = new int[0];
		throwableIndex = -1;
		collectionUsesIndexTargeting = false;
		collectionUsesSeriesOrder = false;
		collectionSeriesIndex = -1;
		appliedCosmeticPlayFabID = null;
	}

	void ISerializationCallbackReceiver.OnBeforeSerialize()
	{
	}

	void ISerializationCallbackReceiver.OnAfterDeserialize()
	{
		_OnAfterDeserialize_InitializePartsArray(ref holdableParts, ECosmeticPartType.Holdable);
		_OnAfterDeserialize_InitializePartsArray(ref functionalParts, ECosmeticPartType.Functional);
		_OnAfterDeserialize_InitializePartsArray(ref wardrobeParts, ECosmeticPartType.Wardrobe);
		_OnAfterDeserialize_InitializePartsArray(ref storeParts, ECosmeticPartType.Store);
		_OnAfterDeserialize_InitializePartsArray(ref firstPersonViewParts, ECosmeticPartType.FirstPerson);
		_OnAfterDeserialize_InitializePartsArray(ref localRigParts, ECosmeticPartType.LocalRig);
		if (setCosmetics == null)
		{
			setCosmetics = Array.Empty<CosmeticSO>();
		}
		if ((collectionParentLinks == null || collectionParentLinks.Length == 0) && !string.IsNullOrEmpty(collectionParentPlayFabID))
		{
			collectionParentLinks = new CosmeticCollectionParentLink[1]
			{
				new CosmeticCollectionParentLink
				{
					parentPlayFabID = collectionParentPlayFabID,
					targetSlotIndex = collectionTargetSlotIndex,
					seriesIndex = collectionSeriesIndex
				}
			};
		}
		if (collectionParentLinks == null)
		{
			collectionParentLinks = Array.Empty<CosmeticCollectionParentLink>();
		}
	}

	private void _OnAfterDeserialize_InitializePartsArray(ref CosmeticPart[] parts, ECosmeticPartType partType)
	{
		for (int i = 0; i < parts.Length; i++)
		{
			parts[i].partType = partType;
			ref CosmeticAttachInfo[] attachAnchors = ref parts[i].attachAnchors;
			if (attachAnchors == null)
			{
				attachAnchors = Array.Empty<CosmeticAttachInfo>();
			}
		}
	}
}
