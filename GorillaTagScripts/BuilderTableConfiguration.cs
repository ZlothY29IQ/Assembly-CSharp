using System;

namespace GorillaTagScripts;

[Serializable]
public class BuilderTableConfiguration
{
	public const int CONFIGURATION_VERSION = 0;

	public int version;

	public int[] TableResourceLimits;

	public int[] PlotResourceLimits;

	public int DroppedPieceLimit;

	public string updateCountdownDate;

	public BuilderTableConfiguration()
	{
		version = 0;
		TableResourceLimits = new int[3];
		PlotResourceLimits = new int[3];
		updateCountdownDate = string.Empty;
	}
}
