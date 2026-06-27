using UnityEngine;

public class GameLightOverride : MonoBehaviour
{
	public void MaxGameLightOverride(int newMaxLights)
	{
		GameLightingManager.instance.SetMaxLights(newMaxLights);
	}

	private void OnDisable()
	{
		GameLightingManager.instance.SetMaxLights(20);
	}
}
