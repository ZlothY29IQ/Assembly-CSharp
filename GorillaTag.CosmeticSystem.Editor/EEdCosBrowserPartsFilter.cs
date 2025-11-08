using System;

namespace GorillaTag.CosmeticSystem.Editor;

[Flags]
public enum EEdCosBrowserPartsFilter
{
	None = 0,
	NoParts = 1,
	Holdable = 2,
	Functional = 4,
	Wardrobe = 8,
	Store = 0x10,
	FirstPerson = 0x20,
	LocalRig = 0x40,
	All = 0x7F
}
