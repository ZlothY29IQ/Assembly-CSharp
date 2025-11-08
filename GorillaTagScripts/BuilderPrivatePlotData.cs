namespace GorillaTagScripts;

public struct BuilderPrivatePlotData
{
	public BuilderPiecePrivatePlot.PlotState plotState;

	public int ownerActorNumber;

	public bool isUnderCapacityLeft;

	public bool isUnderCapacityRight;

	public BuilderPrivatePlotData(BuilderPiecePrivatePlot plot)
	{
		plotState = plot.plotState;
		ownerActorNumber = plot.GetOwnerActorNumber();
		isUnderCapacityLeft = false;
		isUnderCapacityRight = false;
	}
}
