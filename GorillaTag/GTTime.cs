using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using GorillaNetworking;
using TMPro;
using UnityEngine;

namespace GorillaTag;

public static class GTTime
{
	private const string preLog = "[GTTime]  ";

	private const string preErr = "[GTTime]  ERROR!!!  ";

	private static bool _isInitialized;

	public static TimeZoneInfo timeZoneInfoLA { get; private set; }

	public static bool usingServerTime { get; private set; }

	static GTTime()
	{
		_Init();
	}

	[RuntimeInitializeOnLoadMethod]
	private static void _Init()
	{
		if (_isInitialized)
		{
			return;
		}
		try
		{
			timeZoneInfoLA = TimeZoneInfo.FindSystemTimeZoneById("America/Los_Angeles");
		}
		catch
		{
			try
			{
				timeZoneInfoLA = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
			}
			catch
			{
				if (_TryCreateCustomPST(out var out_tz))
				{
					timeZoneInfoLA = out_tz;
					Debug.Log("[GTTime]  _Init: Could not get US Pacific Time Zone, so using manual created Pacific time zone instead.");
				}
				else
				{
					Debug.LogError("[GTTime]  ERROR!!!  _Init: Could not get US Pacific Time Zone and manual Pacific time zone creation failed. Using UTC instead.");
					timeZoneInfoLA = TimeZoneInfo.Utc;
				}
			}
		}
		finally
		{
			_isInitialized = true;
		}
	}

	private static bool _TryCreateCustomPST(out TimeZoneInfo out_tz)
	{
		TimeZoneInfo.AdjustmentRule[] adjustmentRules = new TimeZoneInfo.AdjustmentRule[1] { TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(new DateTime(2007, 1, 1), DateTime.MaxValue.Date, TimeSpan.FromHours(1.0), TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 2, 0, 0), 3, 2, DayOfWeek.Sunday), TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 2, 0, 0), 11, 1, DayOfWeek.Sunday)) };
		try
		{
			out_tz = TimeZoneInfo.CreateCustomTimeZone("Custom/America_Los_Angeles", TimeSpan.FromHours(-8.0), "(UTC-08:00) Pacific Time (US & Canada)", "Pacific Standard Time", "Pacific Daylight Time", adjustmentRules, disableDaylightSavingTime: false);
			return true;
		}
		catch (Exception ex)
		{
			Debug.LogError("[GTTime]  ERROR!!!  _TryCreateCustomPST: Encountered exception: " + ex.Message);
			out_tz = null;
			return false;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static long GetServerStartupTimeAsMilliseconds()
	{
		return GorillaComputer.instance.startupMillis;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static long GetDeviceStartupTimeAsMilliseconds()
	{
		return (long)(TimeSpan.FromTicks(DateTime.UtcNow.Ticks).TotalMilliseconds - Time.realtimeSinceStartupAsDouble * 1000.0);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static long GetStartupTimeAsMilliseconds()
	{
		usingServerTime = true;
		long num = 0L;
		if (GorillaComputer.hasInstance)
		{
			num = GetServerStartupTimeAsMilliseconds();
		}
		if (num == 0L)
		{
			usingServerTime = false;
			num = GetDeviceStartupTimeAsMilliseconds();
		}
		return num;
	}

	public static long TimeAsMilliseconds()
	{
		return GetStartupTimeAsMilliseconds() + (long)(Time.realtimeSinceStartupAsDouble * 1000.0);
	}

	public static double TimeAsDouble()
	{
		return (double)GetStartupTimeAsMilliseconds() / 1000.0 + Time.realtimeSinceStartupAsDouble;
	}

	public static DateTime GetAAxiomDateTime()
	{
		return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZoneInfoLA);
	}

	public static string GetAAxiomDateTimeAsStringForDisplay()
	{
		return GetAAxiomDateTime().ToString("yyyy-MM-dd HH:mm:ss.fff");
	}

	public static string GetAAxiomDateTimeAsStringForFilename()
	{
		return GetAAxiomDateTime().ToString("yyyy-MM-dd_HH-mm-ss-fff");
	}

	public static long GetAAxiomDateTimeAsHumanReadableLong()
	{
		return long.Parse(GetAAxiomDateTime().ToString("yyyyMMddHHmmssfff00"));
	}

	public static DateTime ConvertDateTimeHumanReadableLongToDateTime(long humanReadableLong)
	{
		return DateTime.ParseExact(humanReadableLong.ToString(), "yyyyMMddHHmmssfff'00'", CultureInfo.InvariantCulture);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryUpdateTimeText(TMP_Text textComponent, TimeSpan timeSpan, char[] chars, int index, ref int ref_lastUpdateSeconds)
	{
		int num = (int)timeSpan.TotalSeconds;
		if (ref_lastUpdateSeconds == num)
		{
			return false;
		}
		ref_lastUpdateSeconds = num;
		int num2 = Mathf.Clamp((int)timeSpan.TotalHours, 0, 99);
		int minutes = timeSpan.Minutes;
		int seconds = timeSpan.Seconds;
		chars[index] = (char)(48 + num2 / 10);
		chars[index + 1] = (char)(48 + num2 % 10);
		chars[index + 3] = (char)(48 + minutes / 10);
		chars[index + 4] = (char)(48 + minutes % 10);
		chars[index + 6] = (char)(48 + seconds / 10);
		chars[index + 7] = (char)(48 + seconds % 10);
		textComponent.SetCharArray(chars);
		return true;
	}
}
