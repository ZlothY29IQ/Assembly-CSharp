public class GorillaModifyDayNightTrigger : GorillaTriggerBox
{
	public bool clearModifiedTime;

	public int timeOfDayIndex;

	public bool setFixedWeather;

	public BetterDayNightManager.WeatherType fixedWeather;

	public override void OnBoxTriggered()
	{
		base.OnBoxTriggered();
		if (clearModifiedTime)
		{
			BetterDayNightManager.instance.currentSetting = TimeSettings.Normal;
		}
		else
		{
			_ = timeOfDayIndex % BetterDayNightManager.instance.timeOfDayRange.Length;
			BetterDayNightManager.instance.SetTimeOfDay(timeOfDayIndex);
			BetterDayNightManager.instance.SetOverrideIndex(timeOfDayIndex);
		}
		if (setFixedWeather)
		{
			BetterDayNightManager.instance.SetFixedWeather(fixedWeather);
		}
		else
		{
			BetterDayNightManager.instance.ClearFixedWeather();
		}
	}
}
