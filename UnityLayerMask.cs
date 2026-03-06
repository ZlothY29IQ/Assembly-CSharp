using System;

[Flags]
public enum UnityLayerMask
{
	Everything = -1,
	Nothing = 0,
	Default = 1,
	TransparentFX = 2,
	IgnoreRaycast = 4,
	Water = 0x10,
	UI = 0x20,
	MeshBakerAtlas = 0x40,
	GorillaEquipment = 0x80,
	GorillaBodyCollider = 0x100,
	GorillaObject = 0x200,
	GorillaHand = 0x400,
	GorillaTrigger = 0x800,
	MetaReportScreen = 0x1000,
	GorillaHead = 0x2000,
	GorillaTagCollider = 0x4000,
	GorillaBoundary = 0x8000,
	GorillaEquipmentContainer = 0x10000,
	LCKHide = 0x20000,
	GorillaInteractable = 0x40000,
	FirstPersonOnly = 0x80000,
	GorillaParticle = 0x100000,
	GorillaCosmetics = 0x200000,
	MirrorOnly = 0x400000,
	GorillaThrowable = 0x800000,
	GorillaHandSocket = 0x1000000,
	GorillaCosmeticParticle = 0x2000000,
	BuilderProp = 0x4000000,
	NoMirror = 0x8000000,
	GorillaSlingshotCollider = 0x10000000,
	RopeSwing = 0x20000000,
	Prop = 0x40000000,
	Bake = int.MinValue
}
