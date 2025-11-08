using System;

[Flags]
public enum GestureNodeFlags : uint
{
	None = 0u,
	HandLeft = 1u,
	HandRight = 2u,
	HandOpen = 4u,
	HandClosed = 8u,
	DigitOpen = 0x10u,
	DigitClosed = 0x20u,
	DigitBent = 0x40u,
	TowardFace = 0x80u,
	AwayFromFace = 0x100u,
	AxisWorldUp = 0x200u,
	AxisWorldDown = 0x400u
}
