using System;
using UnityEngine;

public class BetterBakerSettings : MonoBehaviour
{
	[Serializable]
	public struct LightMapMap
	{
		[SerializeField]
		public string timeOfDayName;

		[SerializeField]
		public GameObject sceneLightObject;
	}

	[SerializeField]
	public GameObject[] lightMapMaps = new GameObject[9];
}
