using Liv.Lck.GorillaTag;

namespace Docking;

public class LivCameraDockable : Dockable
{
	public override void Dock()
	{
		base.Dock();
		if (currentDock is LivCameraDock)
		{
			GTLckController gTLckController = GetComponent<GTLckController>() ?? GetComponentInParent<GTLckController>();
			if (gTLckController != null)
			{
				gTLckController.ApplyCameraSettings(((LivCameraDock)currentDock).cameraSettings);
			}
			rotate = !gTLckController.IsTabletFollowingPlayer;
		}
	}
}
