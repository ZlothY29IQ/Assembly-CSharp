using System;

[Flags]
public enum ArcadeButtons
{
	GRAB = 1,
	UP = 2,
	DOWN = 4,
	LEFT = 8,
	RIGHT = 0x10,
	B0 = 0x20,
	B1 = 0x40,
	TRIGGER = 0x80
}
