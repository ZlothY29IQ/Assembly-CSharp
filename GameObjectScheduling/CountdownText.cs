using System;
using System.Collections;
using System.Globalization;
using GorillaNetworking;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.SmartFormat.PersistentVariables;

namespace GameObjectScheduling;

public class CountdownText : MonoBehaviour
{
	private enum TimeChunk
	{
		DAY,
		HOUR,
		MINUTE,
		SECOND
	}

	[SerializeField]
	private CountdownTextDate CountdownTo;

	[SerializeField]
	private bool updateDisplay;

	[SerializeField]
	private bool useExternalTime;

	[SerializeField]
	private bool shouldLocalize = true;

	private TMP_Text displayText;

	private string displayTextFormat;

	private DateTime targetTime;

	private TimeSpan countdownTime;

	private Coroutine monitor;

	private Coroutine displayRefresh;

	private LocalizedText _locTextComp;

	private LocalizedString _countdownLocStr;

	private IntVariable _timeCountdownVar;

	private IntVariable _timescaleCountdownVar;

	private BoolVariable _isValidVar;

	private bool ShouldLocalize
	{
		get
		{
			if (shouldLocalize)
			{
				if (_locTextComp != null && _countdownLocStr != null && _timeCountdownVar != null && _timescaleCountdownVar != null)
				{
					return _isValidVar != null;
				}
				return false;
			}
			return false;
		}
	}

	public CountdownTextDate Countdown
	{
		get
		{
			return CountdownTo;
		}
		set
		{
			CountdownTo = value;
			if (CountdownTo.FormatString.Length > 0)
			{
				displayTextFormat = CountdownTo.FormatString;
			}
			displayText.text = CountdownTo.DefaultString;
			if (base.gameObject.activeInHierarchy && !useExternalTime && monitor == null && CountdownTo != null)
			{
				monitor = StartCoroutine(MonitorTime());
			}
		}
	}

	private void Awake()
	{
		displayText = GetComponent<TMP_Text>();
		displayTextFormat = string.Empty;
		displayText.text = string.Empty;
		if (CountdownTo == null)
		{
			return;
		}
		if (displayTextFormat.Length == 0 && CountdownTo.FormatString.Length > 0)
		{
			displayTextFormat = CountdownTo.FormatString;
		}
		displayText.text = CountdownTo.DefaultString;
		if (!shouldLocalize)
		{
			return;
		}
		_locTextComp = GetComponent<LocalizedText>();
		if (_locTextComp == null)
		{
			Debug.LogError("[LOCALIZATION::COUNTDOWN_TEXT] There is no [LocalizedText] component on [" + base.name + "]!", this);
			return;
		}
		_countdownLocStr = _locTextComp.StringReference;
		if (_locTextComp.StringReference == null || _locTextComp.StringReference.IsEmpty)
		{
			Debug.LogError("[LOCALIZATION::COUNTDOWN_TEXT] There is no [StringReference] assigned on [" + base.name + "]!", this);
			return;
		}
		_timeCountdownVar = _countdownLocStr["time-value"] as IntVariable;
		_timescaleCountdownVar = _countdownLocStr["timescale-index"] as IntVariable;
		_isValidVar = _countdownLocStr["is-valid"] as BoolVariable;
	}

	private void OnEnable()
	{
		if (!(CountdownTo == null) && monitor == null && !useExternalTime)
		{
			monitor = StartCoroutine(MonitorTime());
		}
	}

	private void OnDisable()
	{
		StopMonitorTime();
		StopDisplayRefresh();
	}

	private IEnumerator MonitorTime()
	{
		while (GorillaComputer.instance == null || GorillaComputer.instance.startupMillis == 0L)
		{
			yield return null;
		}
		monitor = null;
		targetTime = TryParseDateTime();
		if (updateDisplay)
		{
			StartDisplayRefresh();
		}
		else
		{
			RefreshDisplay();
		}
	}

	private IEnumerator MonitorExternalTime(DateTime countdown)
	{
		while (GorillaComputer.instance == null || GorillaComputer.instance.startupMillis == 0L)
		{
			yield return null;
		}
		monitor = null;
		targetTime = countdown;
		if (updateDisplay)
		{
			StartDisplayRefresh();
		}
		else
		{
			RefreshDisplay();
		}
	}

	private void StopMonitorTime()
	{
		if (monitor != null)
		{
			StopCoroutine(monitor);
		}
		monitor = null;
	}

	public void SetCountdownTime(DateTime countdown)
	{
		StopMonitorTime();
		StopDisplayRefresh();
		monitor = StartCoroutine(MonitorExternalTime(countdown));
	}

	public void SetFixedText(string text)
	{
		StopMonitorTime();
		StopDisplayRefresh();
		displayText.text = text;
	}

	private void StartDisplayRefresh()
	{
		StopDisplayRefresh();
		displayRefresh = StartCoroutine(WaitForDisplayRefresh());
	}

	private void StopDisplayRefresh()
	{
		if (displayRefresh != null)
		{
			StopCoroutine(displayRefresh);
		}
		displayRefresh = null;
	}

	private IEnumerator WaitForDisplayRefresh()
	{
		while (true)
		{
			RefreshDisplay();
			TimeSpan timeSpan;
			if (countdownTime.Days > 0)
			{
				timeSpan = countdownTime - TimeSpan.FromDays(countdownTime.Days);
			}
			else if (countdownTime.Hours > 0)
			{
				timeSpan = countdownTime - TimeSpan.FromHours(countdownTime.Hours);
			}
			else if (countdownTime.Minutes > 0)
			{
				timeSpan = countdownTime - TimeSpan.FromMinutes(countdownTime.Minutes);
			}
			else
			{
				if (countdownTime.Seconds <= 0)
				{
					break;
				}
				timeSpan = countdownTime - TimeSpan.FromSeconds(countdownTime.Seconds);
			}
			yield return new WaitForSeconds((float)timeSpan.TotalSeconds);
		}
	}

	private void RefreshDisplay()
	{
		countdownTime = targetTime.Subtract(GorillaComputer.instance.GetServerTime());
		var (text, value, value2, value3) = GetTimeDisplay(countdownTime, displayTextFormat, CountdownTo.DaysThreshold, string.Empty, CountdownTo.DefaultString);
		if (!ShouldLocalize)
		{
			displayText.text = text;
			return;
		}
		_timescaleCountdownVar.Value = value;
		_timeCountdownVar.Value = value2;
		_isValidVar.Value = value3;
	}

	public static string GetTimeDisplay(TimeSpan ts, string format)
	{
		return GetTimeDisplay(ts, format, int.MaxValue, string.Empty, string.Empty).msg;
	}

	public static (string msg, int timescaleVar, int countdownVar, bool valid) GetTimeDisplay(TimeSpan ts, string format, int maxDaysToDisplay, string elapsedString, string overMaxString)
	{
		string item = overMaxString;
		int item2 = 0;
		int item3 = ts.Days;
		bool item4 = false;
		if (ts.TotalSeconds < 0.0)
		{
			item = elapsedString;
			return (msg: item, timescaleVar: item2, countdownVar: item3, valid: item4);
		}
		if (ts.TotalDays < (double)maxDaysToDisplay)
		{
			if (ts.Days > 0)
			{
				item2 = 3;
				item3 = ts.Days;
				item4 = true;
				item = string.Format(format, ts.Days, getTimeChunkString(TimeChunk.DAY, ts.Days));
			}
			else if (ts.Hours > 0)
			{
				item2 = 2;
				item3 = ts.Hours;
				item4 = true;
				item = string.Format(format, ts.Hours, getTimeChunkString(TimeChunk.HOUR, ts.Hours));
			}
			else if (ts.Minutes > 0)
			{
				item2 = 1;
				item3 = ts.Minutes;
				item4 = true;
				item = string.Format(format, ts.Minutes, getTimeChunkString(TimeChunk.MINUTE, ts.Minutes));
			}
			else if (ts.Seconds > 0)
			{
				item2 = 0;
				item3 = ts.Seconds;
				item4 = true;
				item = string.Format(format, ts.Seconds, getTimeChunkString(TimeChunk.SECOND, ts.Seconds));
			}
		}
		return (msg: item, timescaleVar: item2, countdownVar: item3, valid: item4);
	}

	private static string getTimeChunkString(TimeChunk chunk, int n)
	{
		switch (chunk)
		{
		case TimeChunk.MINUTE:
			if (n == 1)
			{
				return "MINUTE";
			}
			return "MINUTES";
		case TimeChunk.HOUR:
			if (n == 1)
			{
				return "HOUR";
			}
			return "HOURS";
		case TimeChunk.DAY:
			if (n == 1)
			{
				return "DAY";
			}
			return "DAYS";
		case TimeChunk.SECOND:
			if (n == 1)
			{
				return "SECOND";
			}
			return "SECONDS";
		default:
			return string.Empty;
		}
	}

	private DateTime TryParseDateTime()
	{
		try
		{
			return DateTime.Parse(CountdownTo.CountdownTo, CultureInfo.InvariantCulture);
		}
		catch
		{
			return DateTime.MinValue;
		}
	}
}
