using System;
using ExitGames.Client.Photon;
using UnityEngine;

namespace GorillaNetworking.ScheduledEvents;

public static class ScheduledEventMatchmaking
{
	public const string StateRegular = "regular";

	public const string StateInProgress = "event-in-progress";

	public const string StatePostEvent = "post-event";

	public static bool GracePeriodEnded(ScheduledEventInfo e, DateTime serverNow)
	{
		if (!e.isActive)
		{
			return true;
		}
		return serverNow > e.scheduledStart + TimeSpan.FromMinutes(15.0);
	}

	public static string ResolveCreateState(ScheduledEventInfo e, DateTime serverNow, bool creatorSeenRecently)
	{
		if (!e.isActive || GracePeriodEnded(e, serverNow))
		{
			return "regular";
		}
		DateTime dateTime = e.scheduledStart + TimeSpan.FromMinutes(15.0);
		bool flag = serverNow > dateTime - TimeSpan.FromMinutes(5.0);
		if (!(serverNow < e.scheduledStart) && (creatorSeenRecently || flag))
		{
			return "post-event";
		}
		return "regular";
	}

	public static string ResolveSearchState(ScheduledEventInfo e, DateTime serverNow, bool joinerSeenRecently)
	{
		if (!e.isActive || GracePeriodEnded(e, serverNow))
		{
			return "regular";
		}
		if (!joinerSeenRecently)
		{
			return "regular";
		}
		return "post-event";
	}

	public static bool HasSeenScheduledEventRecently(DateTime serverNow)
	{
		string text = PlayerPrefs.GetString("lastSawScheduledEventTime", null);
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		if (!long.TryParse(text, out var result))
		{
			return false;
		}
		DateTime dateTime = new DateTime(result, DateTimeKind.Utc);
		return (serverNow.ToUniversalTime() - dateTime).TotalHours < 12.0;
	}

	public static void MarkSeenScheduledEventNow(DateTime serverNow)
	{
		PlayerPrefs.SetString("lastSawScheduledEventTime", serverNow.ToUniversalTime().Ticks.ToString());
	}

	public static void ApplyScheduledEventStateToHashes(Hashtable createProps, out Hashtable searchFilter)
	{
		searchFilter = null;
	}
}
