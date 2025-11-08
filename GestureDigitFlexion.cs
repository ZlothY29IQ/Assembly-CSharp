using System;

[Flags]
public enum GestureDigitFlexion : uint
{
	None = 0u,
	Open = 0x10u,
	Closed = 0x20u,
	Bent = 0x40u
}
