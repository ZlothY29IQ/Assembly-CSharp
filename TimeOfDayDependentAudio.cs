using UnityEngine;

public class TimeOfDayDependentAudio : MonoBehaviour, IGorillaSliceableSimple, IBuildValidation
{
	public AudioSource[] audioSources;

	public float[] volumes;

	public float currentVolume;

	public float stepTime;

	public BetterDayNightManager.WeatherType myWeather;

	public GameObject dependentStuff;

	public GameObject timeOfDayDependent;

	public bool includesAudio;

	public ParticleSystem myParticleSystem;

	private float startingEmissionRate;

	private ParticleSystem.MinMaxCurve newCurve;

	private ParticleSystem.EmissionModule myEmissionModule;

	private float newRate;

	public float positionMultiplierSet;

	public float positionMultiplier = 1f;

	public bool isModified;

	private void Awake()
	{
		stepTime = 1f;
		if (myParticleSystem != null)
		{
			myEmissionModule = myParticleSystem.emission;
			startingEmissionRate = myEmissionModule.rateOverTime.constant;
		}
		if (isModified)
		{
			positionMultiplier = positionMultiplierSet;
		}
		else
		{
			positionMultiplier = 1f;
		}
		if (volumes == null)
		{
			volumes = new float[10];
		}
	}

	public void OnEnable()
	{
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.FixedUpdate);
	}

	public void OnDisable()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.FixedUpdate);
	}

	public void SliceUpdate()
	{
		isModified = false;
		UpdateTimeOfDay();
	}

	private void UpdateTimeOfDay()
	{
		if (BetterDayNightManager.instance == null)
		{
			return;
		}
		BetterDayNightManager.WeatherType weatherType = BetterDayNightManager.instance.CurrentWeather();
		BetterDayNightManager.WeatherType weatherType2 = BetterDayNightManager.instance.NextWeather();
		bool num = myWeather == BetterDayNightManager.WeatherType.All || myWeather == weatherType || myWeather == weatherType2;
		bool flag = myWeather != BetterDayNightManager.WeatherType.All && weatherType != weatherType2;
		int currentTimeIndex = BetterDayNightManager.instance.currentTimeIndex;
		int num2 = (currentTimeIndex + 1) % BetterDayNightManager.instance.timeOfDayRange.Length;
		int num3 = (currentTimeIndex - 1) % BetterDayNightManager.instance.timeOfDayRange.Length;
		if (num3 < 0)
		{
			num3 = BetterDayNightManager.instance.timeOfDayRange.Length - 1;
		}
		float currentLerp = BetterDayNightManager.instance.currentLerp;
		if (!num)
		{
			if (dependentStuff.activeSelf)
			{
				dependentStuff.SetActive(value: false);
			}
			return;
		}
		if (!dependentStuff.activeSelf && (!includesAudio || dependentStuff != timeOfDayDependent))
		{
			dependentStuff.SetActive(value: true);
		}
		if (includesAudio && timeOfDayDependent != null)
		{
			bool flag2 = volumes[currentTimeIndex] != 0f;
			if (timeOfDayDependent.activeSelf != flag2)
			{
				timeOfDayDependent.SetActive(flag2);
			}
		}
		if (!flag)
		{
			newRate = startingEmissionRate;
			currentVolume = Mathf.Lerp(volumes[num3], volumes[currentTimeIndex], Mathf.Clamp(currentLerp * 20f, 0f, 1f));
		}
		else if (myWeather == weatherType2)
		{
			float t = Mathf.Clamp(currentLerp * 2f - 1f, 0f, 1f);
			newRate = Mathf.Lerp(0f, startingEmissionRate, t);
			currentVolume = Mathf.Lerp(0f, volumes[num2], currentLerp);
		}
		else
		{
			float t2 = Mathf.Clamp(currentLerp * 2f, 0f, 1f);
			newRate = Mathf.Lerp(startingEmissionRate, 0f, t2);
			currentVolume = Mathf.Lerp(volumes[currentTimeIndex], 0f, currentLerp);
		}
		if (myParticleSystem != null)
		{
			myEmissionModule = myParticleSystem.emission;
			myEmissionModule.rateOverTime = newRate;
			bool flag3 = newRate != 0f;
			if (myParticleSystem.gameObject.activeSelf != flag3)
			{
				myParticleSystem.gameObject.SetActive(flag3);
			}
		}
		if (!includesAudio)
		{
			return;
		}
		for (int i = 0; i < audioSources.Length; i++)
		{
			MusicSource component = audioSources[i].gameObject.GetComponent<MusicSource>();
			if (!(component != null) || !component.VolumeOverridden)
			{
				audioSources[i].volume = currentVolume * positionMultiplier;
				audioSources[i].enabled = currentVolume != 0f;
			}
		}
	}

	public bool BuildValidationCheck()
	{
		for (int i = 0; i < audioSources.Length; i++)
		{
			if (audioSources[i] == null)
			{
				Debug.LogError("audio source array contains null references", this);
				return false;
			}
		}
		if (volumes.Length != 10)
		{
			Debug.LogError("volumes array is the wrong length! you'll pay for this!", this);
			return false;
		}
		return true;
	}
}
