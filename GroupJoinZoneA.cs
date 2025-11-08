using System;

[Flags]
public enum GroupJoinZoneA
{
	Basement = 1,
	Beach = 2,
	Cave = 4,
	Canyon = 8,
	City = 0x10,
	Clouds = 0x20,
	Forest = 0x40,
	Mountain = 0x80,
	Rotating = 0x100,
	Mines = 0x200,
	Arena = 0x400,
	ArenaTunnel = 0x800,
	Hoverboard = 0x1000,
	TreeRoom = 0x2000,
	MountainTunnel = 0x4000,
	BasementTunnel = 0x8000,
	RotatingTunnel = 0x10000,
	BeachTunnel = 0x20000,
	CloudsElevator = 0x40000,
	MinesTunnel = 0x80000,
	CavesComputer = 0x100000,
	Metropolis = 0x200000,
	MetropolisTunnel = 0x400000,
	Attic = 0x800000,
	Arcade = 0x1000000,
	ArcadeTunnel = 0x2000000,
	Bayou = 0x4000000,
	BayouTunnel = 0x8000000,
	CustomMaps = 0x10000000,
	MallConnector = 0x20000000,
	MonkeBlocks = 0x40000000
}
