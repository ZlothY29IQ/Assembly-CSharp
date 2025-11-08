using System;
using System.Collections;
using System.Collections.Generic;
using GorillaNetworking;
using UnityEngine;

public class LightningManager : MonoBehaviour
{
	public int lightMapIndex;

	public float minTimeBetweenFlashes;

	public float maxTimeBetweenFlashes;

	public float flashFadeInDuration;

	public float flashHoldDuration;

	public float flashFadeOutDuration;

	private AudioSource lightningAudio;

	private SRand rng;

	private long currentHourlySeed;

	private List<float> lightningTimestampsRealtime = new List<float>();

	private int nextLightningTimestampIndex;

	public AudioClip regularLightning;

	public AudioClip muffledLightning;

	private Coroutine lightningRunner;

	private void Start()
	{
		lightningAudio = GetComponent<AudioSource>();
		GorillaComputer instance = GorillaComputer.instance;
		instance.OnServerTimeUpdated = (Action)Delegate.Combine(instance.OnServerTimeUpdated, new Action(OnTimeChanged));
	}

	private void OnTimeChanged()
	{
		InitializeRng();
		if (lightningRunner != null)
		{
			StopCoroutine(lightningRunner);
		}
		lightningRunner = StartCoroutine(LightningEffectRunner());
	}

	private void GetHourStart(out long seed, out float timestampRealtime)
	{
		DateTime serverTime = GorillaComputer.instance.GetServerTime();
		DateTime dateTime = new DateTime(serverTime.Year, serverTime.Month, serverTime.Day, serverTime.Hour, 0, 0);
		timestampRealtime = Time.realtimeSinceStartup - (float)(serverTime - dateTime).TotalSeconds;
		seed = dateTime.Ticks;
	}

	private void InitializeRng()
	{
		GetHourStart(out var seed, out var timestampRealtime);
		currentHourlySeed = seed;
		rng = new SRand(seed);
		lightningTimestampsRealtime.Clear();
		nextLightningTimestampIndex = -1;
		float num = timestampRealtime;
		float num2 = 0f;
		float realtimeSinceStartup = Time.realtimeSinceStartup;
		while (num2 < 3600f)
		{
			float num3 = rng.NextFloat(minTimeBetweenFlashes, maxTimeBetweenFlashes);
			num2 += num3;
			num += num3;
			if (nextLightningTimestampIndex == -1 && num > realtimeSinceStartup)
			{
				nextLightningTimestampIndex = lightningTimestampsRealtime.Count;
			}
			lightningTimestampsRealtime.Add(num);
		}
		lightningTimestampsRealtime[lightningTimestampsRealtime.Count - 1] = timestampRealtime + 3605f;
	}

	internal void DoLightningStrike()
	{
		BetterDayNightManager.instance.AnimateLightFlash(lightMapIndex, flashFadeInDuration, flashHoldDuration, flashFadeOutDuration);
		lightningAudio.clip = (ZoneManagement.IsInZone(GTZone.cave) ? muffledLightning : regularLightning);
		lightningAudio.GTPlay();
	}

	private IEnumerator LightningEffectRunner()
	{
		while (true)
		{
			if (lightningTimestampsRealtime.Count <= nextLightningTimestampIndex)
			{
				InitializeRng();
			}
			if (lightningTimestampsRealtime.Count > nextLightningTimestampIndex)
			{
				yield return new WaitForSecondsRealtime(lightningTimestampsRealtime[nextLightningTimestampIndex] - Time.realtimeSinceStartup);
				float num = lightningTimestampsRealtime[nextLightningTimestampIndex];
				nextLightningTimestampIndex++;
				if (Time.realtimeSinceStartup - num < 1f && lightningTimestampsRealtime.Count > nextLightningTimestampIndex)
				{
					DoLightningStrike();
				}
			}
			yield return null;
		}
	}
}
