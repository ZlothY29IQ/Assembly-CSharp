using UnityEngine;

namespace GorillaTagScripts;

public class SceneBasedObject : MonoBehaviour
{
	public GTZone zone;

	public bool IsLocalPlayerInScene()
	{
		if (ZoneManagement.instance.GetAllLoadedScenes().Count > 1 && zone == GTZone.forest)
		{
			return false;
		}
		if (ZoneManagement.instance.IsSceneLoaded(zone))
		{
			return true;
		}
		return false;
	}
}
