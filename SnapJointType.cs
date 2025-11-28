using System;

[Flags]
public enum SnapJointType
{
	None = 0,
	HandL = 1,
	HandR = 4,
	Chest = 8,
	Back = 0x10,
	Head = 0x20,
	Holster = 0x40,
	ForearmL = 0x80,
	ForearmR = 0x100,
	AuxHead = 0x200,
	AuxBody1 = 0x400,
	AuxBody2 = 0x800,
	AuxShoulderL = 0x1000,
	AuxShoulderR = 0x2000,
	Max = 0x4000
}
