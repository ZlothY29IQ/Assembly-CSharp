using System;

namespace GorillaTag.CosmeticSystem.Editor;

[Flags]
public enum EEdCosBrowserAntiClippingFilter
{
	None = 0,
	NameTag = 1,
	LeftArm = 2,
	RightArm = 4,
	Chest = 8,
	HuntComputer = 0x10,
	Badge = 0x20,
	BuilderWatch = 0x40,
	FriendshipBraceletLeft = 0x80,
	FriendshipBraceletRight = 0x100,
	All = 0x1FF
}
