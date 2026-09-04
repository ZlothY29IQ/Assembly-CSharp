using System;

[Flags]
public enum SystemProperties
{
	SwapInterval = 1,
	HalfRefreshRate = 2,
	GPULevel = 4,
	CPULevel = 8,
	Headlock = 0x10,
	HeadlockTranslationX = 0x20,
	HeadlockTranslationY = 0x40,
	HeadlockTranslationZ = 0x80,
	PhaseSyncAdditionalPadding = 0x100,
	PhaseSyncDelayOverride = 0x200,
	PhaseSyncPredictionTime = 0x400,
	PhaseSync = 0x800,
	RefreshRate = 0x1000,
	PredictionTime = 0x2000
}
