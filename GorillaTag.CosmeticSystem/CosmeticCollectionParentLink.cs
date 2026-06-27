using System;
using UnityEngine;

namespace GorillaTag.CosmeticSystem;

[Serializable]
public struct CosmeticCollectionParentLink
{
	[Tooltip("PlayFab ID of a parent (collection) cosmetic this sub-item attaches to. The sub-item will be shown on this parent whenever the parent is equipped.")]
	public string parentPlayFabID;

	[Tooltip("Slot index (0-based) this sub-item occupies on this parent. Set to Any (-1) for interchangeable items that fill any open slot in acquisition order. The value can differ per parent, so the same sub-item may sit in slot 0 on one parent and slot 2 on another.")]
	public int targetSlotIndex;

	[Tooltip("[Cycling parent with 'Use Series Order' enabled] This sub-item's position in THIS parent's numbered series (start from 0). Items cycle in ascending order of this value and gaps are skipped. Leave at -1 if this parent does not use series ordering. May differ per parent.")]
	public int seriesIndex;
}
