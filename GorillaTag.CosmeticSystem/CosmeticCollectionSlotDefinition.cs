using System;
using UnityEngine;

namespace GorillaTag.CosmeticSystem;

[Serializable]
public struct CosmeticCollectionSlotDefinition
{
	[Tooltip("Position, rotation and scale of this slot relative to the parent cosmetic's root transform. Edit visually using the Cosmetic Editor Stage.")]
	public XformOffset offset;
}
