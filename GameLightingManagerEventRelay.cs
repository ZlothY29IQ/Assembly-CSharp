using UnityEngine;

public class GameLightingManagerEventRelay : MonoBehaviour
{
	public void SetCustomDynamicLightingEnabled(bool value)
	{
		if (GameLightingManager.instance == null)
		{
			Debug.LogError("GameLightingManagerEventRelay :: GameLightingManager has not been instanced!");
		}
		else
		{
			GameLightingManager.instance.ZoneEnableCustomDynamicLighting(value);
		}
	}

	public void SetNearsightedDimLightIntensity(float value)
	{
		if (GameLightingManager.instance == null)
		{
			Debug.LogError("GameLightingManagerEventRelay :: GameLightingManager has not been instanced!");
		}
		else
		{
			GameLightingManager.instance.GR_NearsightedDimLight.intensity = value;
		}
	}
}
