using System;

[Flags]
public enum GroupJoinZoneA : uint
{
	Basement = 1u,
	Beach = 2u,
	Cave = 4u,
	Canyon = 8u,
	City = 0x10u,
	Clouds = 0x20u,
	Forest = 0x40u,
	Mountain = 0x80u,
	Rotating = 0x100u,
	Mines = 0x200u,
	Arena = 0x400u,
	ArenaTunnel = 0x800u,
	Hoverboard = 0x1000u,
	TreeRoom = 0x2000u,
	MountainTunnel = 0x4000u,
	BasementTunnel = 0x8000u,
	RotatingTunnel = 0x10000u,
	BeachTunnel = 0x20000u,
	CloudsElevator = 0x40000u,
	MinesTunnel = 0x80000u,
	CavesComputer = 0x100000u,
	Metropolis = 0x200000u,
	MetropolisTunnel = 0x400000u,
	Attic = 0x800000u,
	Arcade = 0x1000000u,
	ArcadeTunnel = 0x2000000u,
	Bayou = 0x4000000u,
	BayouTunnel = 0x8000000u,
	CustomMaps = 0x10000000u,
	MallConnector = 0x20000000u,
	MonkeBlocks = 0x40000000u,
	GTFC = 0x80000000u
}
