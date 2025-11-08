using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using GorillaNetworking;
using UnityEngine;

namespace GorillaTag;

public static class GTTime
{
	private const string preLog = "[GTTime]  ";

	private const string preErr = "[GTTime]  ERROR!!!  ";

	public static TimeZoneInfo timeZoneInfoLA { get; private set; }

	public static bool usingServerTime { get; private set; }

	static GTTime()
	{
		_Init();
	}

	[RuntimeInitializeOnLoadMethod]
	private static void _Init()
	{
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
				Debug.LogError("[GTTime]  ERROR!!!  Constructor: Could not get United States Pacific Time Zone (Los Angeles) so UTC will be used instead.");
				timeZoneInfoLA = TimeZoneInfo.Utc;
			}
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
}
