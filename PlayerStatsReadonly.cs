using System;

[Serializable]
public readonly struct PlayerStatsReadonly(short ping, short fps, short targetFps)
{
	public readonly short Ping = ping;

	public readonly short FPS = fps;

	public readonly short TargetFPS = targetFps;
}
