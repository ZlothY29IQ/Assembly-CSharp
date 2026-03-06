namespace GorillaTagScripts;

public struct BuilderPrivatePlotData(BuilderPiecePrivatePlot plot)
{
	public BuilderPiecePrivatePlot.PlotState plotState = plot.plotState;

	public int ownerActorNumber = plot.GetOwnerActorNumber();

	public bool isUnderCapacityLeft = false;

	public bool isUnderCapacityRight = false;
}
