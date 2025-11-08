using System;
using UnityEngine;

namespace GorillaTag.CosmeticSystem;

[Serializable]
public struct CosmeticAttachInfo
{
	[Tooltip("(Not used for holdables) Determines if the cosmetic part be shown depending on the hand that is used to press the in-game wardrobe \"EQUIP\" button.\n- Both: Show no matter what hand is used.\n- Left: Only show if the left hand selected.\n- Right: Only show if the right hand selected.\n")]
	public StringEnum<ECosmeticSelectSide> selectSide;

	public GTHardCodedBones.SturdyEBone parentBone;

	public XformOffset offset;

	public static CosmeticAttachInfo Identity
	{
		get
		{
			CosmeticAttachInfo result = default(CosmeticAttachInfo);
			result.selectSide = ECosmeticSelectSide.Both;
			result.parentBone = GTHardCodedBones.EBone.None;
			result.offset = XformOffset.Identity;
			return result;
		}
	}

	public CosmeticAttachInfo(ECosmeticSelectSide selectSide, GTHardCodedBones.EBone parentBone, XformOffset offset)
	{
		this.selectSide = selectSide;
		this.parentBone = parentBone;
		this.offset = offset;
	}
}
