using System.Linq;
using GorillaNetworking;
using UnityEngine;

public sealed class FlickerManager : MonoBehaviour
{
	public float[] FlickerDurations;

	public float FlickerFadeInDuration;

	public float FlickerFadeOutDuration;

	public int LightmapIndex;

	private int _flickerIndex;

	private float _nextFlickerTime = float.MinValue;

	private void Awake()
	{
		if (FlickerDurations.Length % 2 != 0)
		{
			Debug.LogWarning("FlickerManager should have an even number of steps; removing last entry.");
			FlickerDurations = FlickerDurations.Take(FlickerDurations.Length - 1).ToArray();
		}
		if (FlickerDurations.Length == 0)
		{
			Debug.LogWarning("No flicker durations set for FlickerManager, disabling.");
			Object.Destroy(this);
		}
	}

	private void Update()
	{
		float serverTime = GetServerTime();
		if (!(serverTime < _nextFlickerTime))
		{
			BetterDayNightManager.instance.AnimateLightFlash(LightmapIndex, FlickerFadeInDuration, FlickerDurations[_flickerIndex], FlickerFadeOutDuration);
			_nextFlickerTime = serverTime + FlickerDurations[_flickerIndex + 1];
			_flickerIndex = (_flickerIndex + 2) % FlickerDurations.Length;
		}
	}

	private static float GetServerTime()
	{
		return (float)(GorillaComputer.instance.GetServerTime() - GorillaComputer.instance.startupTime).TotalSeconds;
	}
}
