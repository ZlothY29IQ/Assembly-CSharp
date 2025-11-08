using UnityEngine;

public class FlagForLighting : MonoBehaviour
{
	public enum TimeOfDay
	{
		Sunrise,
		TenAM,
		Noon,
		ThreePM,
		Sunset,
		Night,
		RainingDay,
		RainingNight,
		None
	}

	public TimeOfDay myTimeOfDay;
}
