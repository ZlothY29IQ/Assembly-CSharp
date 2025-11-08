using System;

[Flags]
public enum GestureAlignment : uint
{
	None = 0u,
	TowardFace = 0x80u,
	AwayFromFace = 0x100u,
	WorldUp = 0x200u,
	WorldDown = 0x400u
}
