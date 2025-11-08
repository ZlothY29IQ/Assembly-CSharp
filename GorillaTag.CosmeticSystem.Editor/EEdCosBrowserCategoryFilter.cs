using System;

namespace GorillaTag.CosmeticSystem.Editor;

[Flags]
public enum EEdCosBrowserCategoryFilter
{
	None = 0,
	Hat = 1,
	Badge = 2,
	Face = 4,
	Paw = 8,
	Chest = 0x10,
	Fur = 0x20,
	Shirt = 0x40,
	Back = 0x80,
	Arms = 0x100,
	Pants = 0x200,
	TagEffect = 0x400,
	Set = 0x1000,
	All = 0x17FF
}
