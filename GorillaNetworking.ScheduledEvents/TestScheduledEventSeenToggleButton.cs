using System;
using UnityEngine;

namespace GorillaNetworking.ScheduledEvents;

public class TestScheduledEventSeenToggleButton : GorillaPressableButton
{
	private const float PollInterval = 1f;

	private float nextPollTime;

	public override void Start()
	{
		base.Start();
		RefreshFromPrefs();
		nextPollTime = Time.time + 1f;
	}

	private void Update()
	{
		if (!(Time.time < nextPollTime))
		{
			nextPollTime = Time.time + 1f;
			RefreshFromPrefs();
		}
	}

	public override void ButtonActivation()
	{
		DateTime serverNow = ((GorillaComputer.instance != null) ? GorillaComputer.instance.GetServerTime() : DateTime.UtcNow);
		if (ScheduledEventMatchmaking.HasSeenScheduledEventRecently(serverNow))
		{
			PlayerPrefs.DeleteKey("lastSawScheduledEventTime");
		}
		else
		{
			ScheduledEventMatchmaking.MarkSeenScheduledEventNow(serverNow);
		}
		RefreshFromPrefs();
	}

	private void RefreshFromPrefs()
	{
		DateTime serverNow = ((GorillaComputer.instance != null) ? GorillaComputer.instance.GetServerTime() : DateTime.UtcNow);
		isOn = ScheduledEventMatchmaking.HasSeenScheduledEventRecently(serverNow);
		UpdateColor();
	}
}
