public class GRUtils
{
	public static string GetToolName(GRTool.GRToolType toolType)
	{
		return toolType switch
		{
			GRTool.GRToolType.Club => "Baton", 
			GRTool.GRToolType.Collector => "Collector", 
			GRTool.GRToolType.Flash => "Flash", 
			GRTool.GRToolType.Lantern => "Lantern", 
			GRTool.GRToolType.Revive => "Revive", 
			GRTool.GRToolType.ShieldGun => "Shield", 
			GRTool.GRToolType.DirectionalShield => "Deflector", 
			GRTool.GRToolType.HockeyStick => "Stick", 
			GRTool.GRToolType.DockWrist => "Dock", 
			_ => "Unknown", 
		};
	}

	public static GRToolProgressionManager.ToolParts GetToolPart(GRTool.GRToolType toolType)
	{
		return toolType switch
		{
			GRTool.GRToolType.Club => GRToolProgressionManager.ToolParts.Baton, 
			GRTool.GRToolType.Collector => GRToolProgressionManager.ToolParts.Collector, 
			GRTool.GRToolType.Flash => GRToolProgressionManager.ToolParts.Flash, 
			GRTool.GRToolType.Lantern => GRToolProgressionManager.ToolParts.Lantern, 
			GRTool.GRToolType.Revive => GRToolProgressionManager.ToolParts.Revive, 
			GRTool.GRToolType.ShieldGun => GRToolProgressionManager.ToolParts.ShieldGun, 
			GRTool.GRToolType.DirectionalShield => GRToolProgressionManager.ToolParts.DirectionalShield, 
			GRTool.GRToolType.HockeyStick => GRToolProgressionManager.ToolParts.HockeyStick, 
			GRTool.GRToolType.DockWrist => GRToolProgressionManager.ToolParts.DockWrist, 
			_ => GRToolProgressionManager.ToolParts.None, 
		};
	}
}
