using System;

[Serializable]
public readonly struct PlayerStatsReadonly
{
	public readonly short Ping;

	public readonly short FPS;

	public readonly short TargetFPS;

	public readonly SystemProperties SystemPropertiesFlags;

	public bool SwapInterval => SystemPropertiesFlags.HasFlag(SystemProperties.SwapInterval);

	public bool HalfRefreshRate => SystemPropertiesFlags.HasFlag(SystemProperties.HalfRefreshRate);

	public bool GPULevel => SystemPropertiesFlags.HasFlag(SystemProperties.GPULevel);

	public bool CPULevel => SystemPropertiesFlags.HasFlag(SystemProperties.CPULevel);

	public bool Headlock => SystemPropertiesFlags.HasFlag(SystemProperties.Headlock);

	public bool HeadlockTranslationX => SystemPropertiesFlags.HasFlag(SystemProperties.HeadlockTranslationX);

	public bool HeadlockTranslationY => SystemPropertiesFlags.HasFlag(SystemProperties.HeadlockTranslationY);

	public bool HeadlockTranslationZ => SystemPropertiesFlags.HasFlag(SystemProperties.HeadlockTranslationZ);

	public bool PhaseSyncAdditionalPadding => SystemPropertiesFlags.HasFlag(SystemProperties.PhaseSyncAdditionalPadding);

	public bool PhaseSyncDelayOverride => SystemPropertiesFlags.HasFlag(SystemProperties.PhaseSyncDelayOverride);

	public bool PhaseSyncPredictionTime => SystemPropertiesFlags.HasFlag(SystemProperties.PhaseSyncPredictionTime);

	public bool PhaseSync => SystemPropertiesFlags.HasFlag(SystemProperties.PhaseSync);

	public bool RefreshRate => SystemPropertiesFlags.HasFlag(SystemProperties.RefreshRate);

	public bool PredictionTime => SystemPropertiesFlags.HasFlag(SystemProperties.PredictionTime);

	public PlayerStatsReadonly(short ping, short fps, short targetFps, SystemProperties flags)
	{
		Ping = ping;
		FPS = fps;
		TargetFPS = targetFps;
		SystemPropertiesFlags = flags;
	}
}
