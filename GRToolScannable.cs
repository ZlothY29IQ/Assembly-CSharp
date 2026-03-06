public class GRToolScannable : GRScannable
{
	private GRTool tool;

	private GRToolUpgradePiece upgradePiece;

	private GRToolProgressionManager.ToolProgressionMetaData metadata;

	public override void Start()
	{
		base.Start();
		if (gameEntity != null)
		{
			tool = gameEntity.GetComponent<GRTool>();
			upgradePiece = gameEntity.GetComponent<GRToolUpgradePiece>();
		}
	}

	private void FetchMetadata(GhostReactor reactor)
	{
		if (metadata == null)
		{
			GRToolProgressionManager.ToolParts toolParts = GRToolProgressionManager.ToolParts.None;
			if (tool != null)
			{
				toolParts = GRUtils.GetToolPart(tool.toolType);
			}
			else if (upgradePiece != null)
			{
				toolParts = upgradePiece.matchingUpgrade;
			}
			if (toolParts != GRToolProgressionManager.ToolParts.None)
			{
				metadata = reactor.toolProgression.GetPartMetadata(toolParts);
			}
		}
	}

	public override string GetTitleText(GhostReactor reactor)
	{
		FetchMetadata(reactor);
		if (metadata == null)
		{
			return "Unknown";
		}
		return metadata.name;
	}

	public override string GetBodyText(GhostReactor reactor)
	{
		FetchMetadata(reactor);
		if (metadata == null)
		{
			return "Unknown";
		}
		return metadata.description;
	}

	public override string GetAnnotationText(GhostReactor reactor)
	{
		FetchMetadata(reactor);
		if (metadata == null)
		{
			return "Unknown";
		}
		return metadata.annotation;
	}
}
