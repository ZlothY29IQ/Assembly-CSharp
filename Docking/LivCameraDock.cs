using Liv.Lck.GorillaTag;

namespace Docking;

public class LivCameraDock : Dock
{
	public GtCameraDockSettings cameraSettings;

	private void Reset()
	{
		cameraSettings.fov = 80f;
	}

	private void OnValidate()
	{
		if (cameraSettings.forceFov && (cameraSettings.fov < 30f || cameraSettings.fov > 110f))
		{
			cameraSettings.fov = 80f;
		}
	}
}
