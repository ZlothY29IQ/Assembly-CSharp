using UnityEngine;

public class BetterBaker : MonoBehaviour
{
	public struct LightMapMap
	{
		public string timeOfDayName;

		public GameObject lightObject;
	}

	public string bakeryLightmapDirectory;

	public string dayNightLightmapsDirectory;

	public GameObject[] allLights;
}
