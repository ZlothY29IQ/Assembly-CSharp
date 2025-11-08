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
}
