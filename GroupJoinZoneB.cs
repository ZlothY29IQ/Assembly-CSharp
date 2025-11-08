using System;

[Flags]
public enum GroupJoinZoneB
{
	HoverboardTunnel = 1,
	Critters = 2,
	CrittersTunnel = 4,
	GhostReactor = 8,
	MonkeBlocksShared = 0x10,
	MonkeBlocksSharedTunnel = 0x20,
	GhostReactorTunnel = 0x40,
	RankedForest = 0x80,
	RankedForestTunnel = 0x100,
	GhostReactorDrill = 0x200
}
