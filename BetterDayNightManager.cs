using System;
using System.Collections;
using System.Collections.Generic;
using GorillaNetworking;
using UnityEngine;

public class BetterDayNightManager : MonoBehaviour, IGorillaSliceableSimple, ITimeOfDaySystem
{
	public enum Season
	{
		Winter,
		Spring,
		Summer,
		Fall
	}

	public enum WeatherType
	{
		None,
		Raining,
		All
	}

	private class ScheduledEvent
	{
		public long lastDayCalled;

		public int hour;

		public Action action;
	}

	public const int TIME_OF_DAY_COUNT = 10;

	[OnEnterPlay_SetNull]
	public static volatile BetterDayNightManager instance;

	[OnEnterPlay_Clear]
	public static List<PerSceneRenderData> allScenesRenderData = new List<PerSceneRenderData>();

	public Shader standard;

	public Shader standardCutout;

	public Shader gorillaUnlit;

	public Shader gorillaUnlitCutout;

	public Material[] dayNightSupportedMaterials;

	public Material[] dayNightSupportedMaterialsCutout;

	public string[] dayNightLightmapNames;

	public string[] dayNightWeatherLightmapNames;

	public Texture2D[] dayNightSkyboxTextures;

	public Texture2D[] cloudsDayNightSkyboxTextures;

	public Texture2D[] beachDayNightSkyboxTextures;

	public Texture2D[] dayNightWeatherSkyboxTextures;

	public float[] standardUnlitColor;

	public float[] standardUnlitColorWithPremadeColorDarker;

	public float currentLerp;

	public float currentTimestep;

	public Season currentSeason;

	public double[] summerTimeOfDayRange;

	public double[] winterTimeOfDayRange;

	public double timeMultiplier;

	private float lastTime;

	private double currentTime;

	private double totalHours;

	private double totalSeconds;

	private float colorFrom;

	private float colorTo;

	private float colorFromDarker;

	private float colorToDarker;

	public int currentTimeIndex;

	public int currentWeatherIndex;

	private int lastIndex;

	private double currentIndexSeconds;

	private double baseSeconds;

	private bool computerInit;

	public int mySeed;

	public System.Random randomNumberGenerator = new System.Random();

	public WeatherType[] weatherCycle;

	public bool overrideWeather;

	public WeatherType overrideWeatherType;

	public float rainChance = 0.3f;

	public int maxRainDuration = 5;

	private int rainDuration;

	private float remainingSeconds;

	private long initialDayCycles;

	private long gameEpochDay;

	private int currentWeatherCycle;

	private int fromWeatherIndex;

	private int toWeatherIndex;

	private Texture2D fromSky;

	private Texture2D fromSky2;

	private Texture2D fromSky3;

	private Texture2D toSky;

	private Texture2D toSky2;

	private Texture2D toSky3;

	public AddCollidersToParticleSystemTriggers[] weatherSystems;

	public List<Collider> collidersToAddToWeatherSystems = new List<Collider>();

	private float lastTimeChecked;

	private Func<int, int> timeIndexOverrideFunc;

	public int overrideIndex = -1;

	[OnEnterPlay_Clear]
	private static readonly Dictionary<int, ScheduledEvent> scheduledEvents = new Dictionary<int, ScheduledEvent>(256);

	public TimeSettings currentSetting;

	private ShaderHashId _GT_DayCycleTimeProgress = "_GT_DayCycleTimeProgress";

	private ShaderHashId _GT_DayCycleBrightnessOption1_Id = "_GT_DayCycleBrightnessOption1";

	private ShaderHashId _GT_DayCycleBrightnessOption2_Id = "_GT_DayCycleBrightnessOption2";

	private ShaderHashId _GlobalDayNightLerpValue = "_GlobalDayNightLerpValue";

	private ShaderHashId _GlobalDayNightSkyTex1 = "_GlobalDayNightSkyTex1";

	private ShaderHashId _GlobalDayNightSkyTex2 = "_GlobalDayNightSkyTex2";

	private ShaderHashId _GlobalDayNightSky2Tex1 = "_GlobalDayNightSky2Tex1";

	private ShaderHashId _GlobalDayNightSky2Tex2 = "_GlobalDayNightSky2Tex2";

	private ShaderHashId _GlobalDayNightSky3Tex1 = "_GlobalDayNightSky3Tex1";

	private ShaderHashId _GlobalDayNightSky3Tex2 = "_GlobalDayNightSky3Tex2";

	private bool shouldRepopulate;

	private Coroutine animatingLightFlash;

	public double[] timeOfDayRange
	{
		get
		{
			if (currentSeason == Season.Winter)
			{
				return winterTimeOfDayRange;
			}
			return summerTimeOfDayRange;
		}
	}

	public string currentTimeOfDay { get; private set; }

	public float NormalizedTimeOfDay => Mathf.Clamp01((float)((baseSeconds + (double)Time.realtimeSinceStartup * timeMultiplier) % totalSeconds / totalSeconds));

	double ITimeOfDaySystem.currentTimeInSeconds => currentTime;

	double ITimeOfDaySystem.totalTimeInSeconds => totalSeconds;

	public static void Register(PerSceneRenderData data)
	{
		allScenesRenderData.Add(data);
	}

	public static void Unregister(PerSceneRenderData data)
	{
		allScenesRenderData.Remove(data);
	}

	public void OnEnable()
	{
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
		if (instance == null)
		{
			instance = this;
		}
		else if (instance != this)
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
		currentLerp = 0f;
		totalHours = 0.0;
		for (int i = 0; i < timeOfDayRange.Length; i++)
		{
			totalHours += timeOfDayRange[i];
		}
		totalSeconds = totalHours * 60.0 * 60.0;
		currentTimeIndex = 0;
		baseSeconds = 0.0;
		computerInit = false;
		randomNumberGenerator = new System.Random(mySeed);
		GenerateWeatherEventTimes();
		ChangeMaps(0, 1);
		StartCoroutine(InitialUpdate());
	}

	public void OnDisable()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	public void UpdateTimeOfDay()
	{
		if (Time.time < lastTimeChecked + currentTimestep)
		{
			return;
		}
		lastTimeChecked = Time.time;
		if (animatingLightFlash != null)
		{
			return;
		}
		try
		{
			if (!computerInit && GorillaComputer.instance != null && GorillaComputer.instance.startupMillis != 0L)
			{
				computerInit = true;
				initialDayCycles = (long)(TimeSpan.FromMilliseconds(GorillaComputer.instance.startupMillis).TotalSeconds * timeMultiplier / totalSeconds);
				currentWeatherIndex = (int)(initialDayCycles * dayNightLightmapNames.Length) % weatherCycle.Length;
				baseSeconds = TimeSpan.FromMilliseconds(GorillaComputer.instance.startupMillis).TotalSeconds * timeMultiplier % totalSeconds;
				currentTime = (baseSeconds + (double)Time.realtimeSinceStartup * timeMultiplier) % totalSeconds;
				currentIndexSeconds = 0.0;
				for (int i = 0; i < timeOfDayRange.Length; i++)
				{
					currentIndexSeconds += timeOfDayRange[i] * 3600.0;
					if (currentIndexSeconds > currentTime)
					{
						currentTimeIndex = i;
						break;
					}
				}
				currentWeatherIndex += currentTimeIndex;
			}
			else if (!computerInit && baseSeconds == 0.0)
			{
				initialDayCycles = (long)(TimeSpan.FromTicks(DateTime.UtcNow.Ticks).TotalSeconds * timeMultiplier / totalSeconds);
				currentWeatherIndex = (int)(initialDayCycles * dayNightLightmapNames.Length) % weatherCycle.Length;
				baseSeconds = TimeSpan.FromTicks(DateTime.UtcNow.Ticks).TotalSeconds * timeMultiplier % totalSeconds;
				currentTime = baseSeconds % totalSeconds;
				currentIndexSeconds = 0.0;
				for (int j = 0; j < timeOfDayRange.Length; j++)
				{
					currentIndexSeconds += timeOfDayRange[j] * 3600.0;
					if (currentIndexSeconds > currentTime)
					{
						currentTimeIndex = j;
						break;
					}
				}
				currentWeatherIndex += currentTimeIndex - 1;
				if (currentWeatherIndex < 0)
				{
					currentWeatherIndex = weatherCycle.Length - 1;
				}
			}
			currentTime = ((currentSetting == TimeSettings.Normal) ? ((baseSeconds + (double)Time.realtimeSinceStartup * timeMultiplier) % totalSeconds) : currentTime);
			currentIndexSeconds = 0.0;
			for (int k = 0; k < timeOfDayRange.Length; k++)
			{
				currentIndexSeconds += timeOfDayRange[k] * 3600.0;
				if (currentIndexSeconds > currentTime)
				{
					currentTimeIndex = k;
					break;
				}
			}
			if (timeIndexOverrideFunc != null)
			{
				currentTimeIndex = timeIndexOverrideFunc(currentTimeIndex);
			}
			if (currentTimeIndex != lastIndex)
			{
				currentWeatherIndex = (currentWeatherIndex + 1) % weatherCycle.Length;
				ChangeMaps(currentTimeIndex, (currentTimeIndex + 1) % timeOfDayRange.Length);
			}
			currentLerp = (float)(1.0 - (currentIndexSeconds - currentTime) / (timeOfDayRange[currentTimeIndex] * 3600.0));
			Shader.SetGlobalFloat(_GT_DayCycleTimeProgress, NormalizedTimeOfDay);
			ChangeLerps(currentLerp);
			lastIndex = currentTimeIndex;
			currentTimeOfDay = dayNightLightmapNames[currentTimeIndex];
		}
		catch (Exception ex)
		{
			Debug.LogError("Error in BetterDayNightManager: " + ex, this);
		}
		gameEpochDay = (long)((baseSeconds + (double)Time.realtimeSinceStartup * timeMultiplier) / totalSeconds + (double)initialDayCycles);
		foreach (ScheduledEvent value in scheduledEvents.Values)
		{
			if (value.lastDayCalled != gameEpochDay && value.hour == currentTimeIndex)
			{
				value.lastDayCalled = gameEpochDay;
				value.action();
			}
		}
	}

	private void ChangeLerps(float newLerp)
	{
		Shader.SetGlobalFloat(_GlobalDayNightLerpValue, newLerp);
		Shader.SetGlobalFloat(_GT_DayCycleBrightnessOption1_Id, Mathf.Lerp(colorFrom, colorTo, newLerp));
		Shader.SetGlobalFloat(_GT_DayCycleBrightnessOption2_Id, Mathf.Lerp(colorFromDarker, colorToDarker, newLerp));
	}

	private void ChangeMaps(int fromIndex, int toIndex)
	{
		fromWeatherIndex = currentWeatherIndex;
		toWeatherIndex = (currentWeatherIndex + 1) % weatherCycle.Length;
		if (weatherCycle[fromWeatherIndex] == WeatherType.Raining)
		{
			fromSky = dayNightWeatherSkyboxTextures[fromIndex];
		}
		else
		{
			fromSky = dayNightSkyboxTextures[fromIndex];
		}
		fromSky2 = cloudsDayNightSkyboxTextures[fromIndex];
		fromSky3 = beachDayNightSkyboxTextures[fromIndex];
		if (weatherCycle[toWeatherIndex] == WeatherType.Raining)
		{
			toSky = dayNightWeatherSkyboxTextures[toIndex];
		}
		else
		{
			toSky = dayNightSkyboxTextures[toIndex];
		}
		toSky2 = cloudsDayNightSkyboxTextures[toIndex];
		toSky3 = beachDayNightSkyboxTextures[toIndex];
		PopulateAllLightmaps(fromIndex, toIndex);
		Shader.SetGlobalTexture(_GlobalDayNightSkyTex1, fromSky);
		Shader.SetGlobalTexture(_GlobalDayNightSkyTex2, toSky);
		Shader.SetGlobalTexture(_GlobalDayNightSky2Tex1, fromSky2);
		Shader.SetGlobalTexture(_GlobalDayNightSky2Tex2, toSky2);
		Shader.SetGlobalTexture(_GlobalDayNightSky3Tex1, fromSky3);
		Shader.SetGlobalTexture(_GlobalDayNightSky3Tex2, toSky3);
		colorFrom = standardUnlitColor[fromIndex];
		colorTo = standardUnlitColor[toIndex];
		colorFromDarker = standardUnlitColorWithPremadeColorDarker[fromIndex];
		colorToDarker = standardUnlitColorWithPremadeColorDarker[toIndex];
	}

	public void SliceUpdate()
	{
		if (!shouldRepopulate)
		{
			foreach (PerSceneRenderData allScenesRenderDatum in allScenesRenderData)
			{
				if (allScenesRenderDatum.CheckShouldRepopulate())
				{
					shouldRepopulate = true;
				}
			}
		}
		if (shouldRepopulate)
		{
			PopulateAllLightmaps();
			shouldRepopulate = false;
		}
		UpdateTimeOfDay();
	}

	private IEnumerator InitialUpdate()
	{
		yield return null;
		SliceUpdate();
	}

	public void RequestRepopulateLightmaps()
	{
		shouldRepopulate = true;
	}

	public void PopulateAllLightmaps()
	{
		PopulateAllLightmaps(currentTimeIndex, (currentTimeIndex + 1) % timeOfDayRange.Length);
	}

	public void PopulateAllLightmaps(int fromIndex, int toIndex)
	{
		string fromTimeOfDay = ((weatherCycle[fromWeatherIndex] != WeatherType.Raining) ? dayNightLightmapNames[fromIndex] : dayNightWeatherLightmapNames[fromIndex]);
		string toTimeOfDay = ((weatherCycle[toWeatherIndex] != WeatherType.Raining) ? dayNightLightmapNames[toIndex] : dayNightWeatherLightmapNames[toIndex]);
		LightmapData[] lightmaps = LightmapSettings.lightmaps;
		foreach (PerSceneRenderData allScenesRenderDatum in allScenesRenderData)
		{
			allScenesRenderDatum.PopulateLightmaps(fromTimeOfDay, toTimeOfDay, lightmaps);
		}
		LightmapSettings.lightmaps = lightmaps;
	}

	public WeatherType CurrentWeather()
	{
		if (!overrideWeather)
		{
			return weatherCycle[currentWeatherIndex];
		}
		return overrideWeatherType;
	}

	public WeatherType NextWeather()
	{
		if (!overrideWeather)
		{
			return weatherCycle[(currentWeatherIndex + 1) % weatherCycle.Length];
		}
		return overrideWeatherType;
	}

	public WeatherType LastWeather()
	{
		if (!overrideWeather)
		{
			return weatherCycle[(currentWeatherIndex - 1) % weatherCycle.Length];
		}
		return overrideWeatherType;
	}

	private void GenerateWeatherEventTimes()
	{
		weatherCycle = new WeatherType[100 * dayNightLightmapNames.Length];
		rainChance = rainChance * 2f / (float)maxRainDuration;
		for (int i = 1; i < weatherCycle.Length; i++)
		{
			weatherCycle[i] = (((float)randomNumberGenerator.Next(100) < rainChance * 100f) ? WeatherType.Raining : WeatherType.None);
			if (weatherCycle[i] != WeatherType.Raining)
			{
				continue;
			}
			rainDuration = randomNumberGenerator.Next(1, maxRainDuration + 1);
			for (int j = 1; j < rainDuration; j++)
			{
				if (i + j < weatherCycle.Length)
				{
					weatherCycle[i + j] = WeatherType.Raining;
				}
			}
			i += rainDuration - 1;
		}
	}

	public static int RegisterScheduledEvent(int hour, Action action)
	{
		int i;
		for (i = (int)(DateTime.Now.Ticks % int.MaxValue); scheduledEvents.ContainsKey(i); i++)
		{
		}
		scheduledEvents.Add(i, new ScheduledEvent
		{
			lastDayCalled = -1L,
			hour = hour,
			action = action
		});
		return i;
	}

	public static void UnregisterScheduledEvent(int id)
	{
		scheduledEvents.Remove(id);
	}

	public void SetTimeIndexOverrideFunction(Func<int, int> overrideFunction)
	{
		timeIndexOverrideFunc = overrideFunction;
	}

	public void UnsetTimeIndexOverrideFunction()
	{
		timeIndexOverrideFunc = null;
	}

	public void SetOverrideIndex(int index)
	{
		overrideIndex = index;
		currentWeatherIndex = overrideIndex;
		currentTimeIndex = overrideIndex;
		currentTimeOfDay = dayNightLightmapNames[currentTimeIndex];
		ChangeMaps(currentTimeIndex, (currentTimeIndex + 1) % timeOfDayRange.Length);
	}

	public void AnimateLightFlash(int index, float fadeInDuration, float holdDuration, float fadeOutDuration)
	{
		if (animatingLightFlash != null)
		{
			StopCoroutine(animatingLightFlash);
		}
		animatingLightFlash = StartCoroutine(AnimateLightFlashCo(index, fadeInDuration, holdDuration, fadeOutDuration));
	}

	private IEnumerator AnimateLightFlashCo(int index, float fadeInDuration, float holdDuration, float fadeOutDuration)
	{
		int startMap = ((currentLerp < 0.5f) ? currentTimeIndex : ((currentTimeIndex + 1) % timeOfDayRange.Length));
		ChangeMaps(startMap, index);
		float endTimestamp = Time.time + fadeInDuration;
		while (Time.time < endTimestamp)
		{
			ChangeLerps(1f - (endTimestamp - Time.time) / fadeInDuration);
			yield return null;
		}
		ChangeMaps(index, index);
		ChangeLerps(0f);
		endTimestamp = Time.time + fadeInDuration;
		while (Time.time < endTimestamp)
		{
			yield return null;
		}
		ChangeMaps(index, startMap);
		endTimestamp = Time.time + fadeOutDuration;
		while (Time.time < endTimestamp)
		{
			ChangeLerps(1f - (endTimestamp - Time.time) / fadeInDuration);
			yield return null;
		}
		ChangeMaps(currentTimeIndex, (currentTimeIndex + 1) % timeOfDayRange.Length);
		ChangeLerps(currentLerp);
		animatingLightFlash = null;
	}

	public void SetTimeOfDay(int timeIndex)
	{
		double num = 0.0;
		for (int i = 0; i < timeIndex; i++)
		{
			num += timeOfDayRange[i];
		}
		currentTime = num * 3600.0;
		currentSetting = TimeSettings.Static;
	}

	public void FastForward(float seconds)
	{
		baseSeconds += seconds;
	}

	public void SetFixedWeather(WeatherType weather)
	{
		overrideWeather = true;
		overrideWeatherType = weather;
	}

	public void ClearFixedWeather()
	{
		overrideWeather = false;
	}
}
