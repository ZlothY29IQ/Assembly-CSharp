using System;
using System.Threading.Tasks;
using GorillaNetworking;
using GorillaNetworking.ScheduledEvents;
using Newtonsoft.Json;
using PlayFab;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshPro))]
public class SimpleCountdown : ObservableBehavior
{
	private enum Mode
	{
		None,
		TitleData,
		FixedDate,
		TimeSync,
		ScheduledEvent,
		EventStart,
		EventEnd
	}

	private enum DisplayFormat
	{
		DD_HH_MM_SS,
		HH_MM_SS,
		DD_HH_MM,
		HH_MM,
		MM_SS
	}

	[SerializeField]
	private DisplayFormat displayFormat;

	[SerializeField]
	private Mode mode = Mode.TitleData;

	[SerializeField]
	private string titleDataKey;

	[SerializeField]
	private string titleDataObjectID;

	[SerializeField]
	private string date;

	[SerializeField]
	private ServerTimeSyncRule timeSyncRule;

	[SerializeField]
	private Vector2 hourRange = new Vector2(float.MinValue, float.MaxValue);

	private DateTime dt;

	private TitleDataActivation.TitleDataObjectActivationData activationData;

	private TextMeshPro tmp;

	private DateTime overrideDt = DateTime.MinValue;

	public Action ManualCountdownComplete;

	private async void Start()
	{
		tmp = GetComponent<TextMeshPro>();
		switch (mode)
		{
		default:
			return;
		case Mode.TitleData:
			while (PlayFabTitleDataCache.Instance == null)
			{
				await Task.Yield();
			}
			PlayFabTitleDataCache.Instance.GetTitleData(titleDataKey, onTD, onTDError);
			return;
		case Mode.FixedDate:
			ParseDateTime();
			return;
		case Mode.TimeSync:
			if (GorillaComputer.instance != null)
			{
				DateTime serverTime = GorillaComputer.instance.GetServerTime();
				dt = timeSyncRule.GetPrevious(serverTime);
			}
			return;
		case Mode.EventStart:
		case Mode.EventEnd:
			break;
		case Mode.ScheduledEvent:
			return;
		}
		while (PlayFabTitleDataCache.Instance == null || !TitleDataActivation.UpdatedReferenceDateFromTitleData)
		{
			await Task.Yield();
		}
		PlayFabTitleDataCache.Instance.GetTitleData(titleDataKey, onEventTD, onEventTDError);
	}

	private void onEventTD(string s)
	{
		TitleDataActivation.TitleDataActivationData titleDataActivationData;
		try
		{
			titleDataActivationData = JsonConvert.DeserializeObject<TitleDataActivation.TitleDataActivationData>(s);
		}
		catch (Exception ex)
		{
			Debug.LogError("SimpleCountdown :: onEventTD ::" + ex.Message + " string was " + s);
			return;
		}
		int num = 0;
		while (titleDataActivationData != null && titleDataActivationData.Data != null && num < titleDataActivationData.Data.Length)
		{
			if (titleDataActivationData.Data[num].TitleDataObjectID == titleDataObjectID)
			{
				activationData = titleDataActivationData.Data[num];
				break;
			}
			num++;
		}
	}

	private void onEventTDError(PlayFabError error)
	{
		Debug.LogError("SimpleCountdown component on " + base.name + " failed to get '" + titleDataKey + "' from title data :: " + error.ErrorMessage);
	}

	private DateTime GetEventWindowDateTime(DateTime now)
	{
		if (activationData == null)
		{
			return now;
		}
		bool found = false;
		DateTime target = DateTime.MinValue;
		TitleDataActivation.AbsoluteDateTimeWindow[] absoluteDateTimeWindow = activationData.AbsoluteDateTimeWindow;
		int num = 0;
		while (absoluteDateTimeWindow != null && num < absoluteDateTimeWindow.Length)
		{
			ConsiderTime((mode == Mode.EventStart) ? absoluteDateTimeWindow[num].StartDate : absoluteDateTimeWindow[num].EndDate, now, ref found, ref target);
			num++;
		}
		TitleDataActivation.RelativeDateTimeWindow[] relativeDateTimeWindow = activationData.RelativeDateTimeWindow;
		int num2 = 0;
		while (relativeDateTimeWindow != null && num2 < relativeDateTimeWindow.Length)
		{
			ConsiderTime((mode == Mode.EventStart) ? relativeDateTimeWindow[num2].StartDate : relativeDateTimeWindow[num2].EndDate, now, ref found, ref target);
			num2++;
		}
		if (!found)
		{
			return now;
		}
		return target;
	}

	private static void ConsiderTime(DateTime candidate, DateTime now, ref bool found, ref DateTime target)
	{
		if (!(candidate <= now) && (!found || !(candidate >= target)))
		{
			found = true;
			target = candidate;
		}
	}

	private void onTD(string s)
	{
		date = s;
		ParseDateTime();
	}

	private void onTDError(PlayFabError error)
	{
		Debug.Log("SimpleCountdown component on " + base.name + " failed to get '" + titleDataKey + "' from title data. Using Fallback: '" + date + "'");
		ParseDateTime();
	}

	private void ParseDateTime()
	{
		if (!DateTime.TryParse(date, out dt))
		{
			Debug.Log("SimpleCountdown component on " + base.name + " has an unparsable date string: '" + date + "'");
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	protected override void ObservableSliceUpdate()
	{
		if (GorillaComputer.instance == null)
		{
			return;
		}
		_ = dt;
		DateTime serverTime = GorillaComputer.instance.GetServerTime();
		TimeSpan timeSpan;
		if (overrideDt < serverTime)
		{
			if (overrideDt > DateTime.MinValue)
			{
				ManualCountdownComplete?.Invoke();
				overrideDt = DateTime.MinValue;
			}
			if (mode == Mode.TimeSync)
			{
				dt = timeSyncRule.GetNext(serverTime);
			}
			else if (mode == Mode.ScheduledEvent)
			{
				double value = ((ScheduledEventManager.Instance != null && ScheduledEventManager.Instance.SecondsUntilEventStart > 0.0) ? ScheduledEventManager.Instance.SecondsUntilEventStart : 0.0);
				dt = serverTime.AddSeconds(value);
			}
			else if (mode == Mode.EventStart || mode == Mode.EventEnd)
			{
				dt = GetEventWindowDateTime(serverTime);
			}
			timeSpan = dt - serverTime;
		}
		else
		{
			timeSpan = overrideDt - serverTime;
		}
		if (timeSpan.TotalHours <= (double)hourRange.x || timeSpan.TotalHours >= (double)hourRange.y)
		{
			timeSpan = timeSpan.Multiply(0.0);
		}
		switch (displayFormat)
		{
		case DisplayFormat.DD_HH_MM_SS:
			tmp.text = $"{timeSpan.Days:00}:{timeSpan.Hours:00}:{timeSpan.Minutes:00}:{timeSpan.Seconds:00}";
			break;
		case DisplayFormat.HH_MM_SS:
			tmp.text = $"{Math.Floor(timeSpan.TotalHours):00}:{timeSpan.Minutes:00}:{timeSpan.Seconds:00}";
			break;
		case DisplayFormat.DD_HH_MM:
			tmp.text = $"{timeSpan.Days:00}:{timeSpan.Hours:00}:{timeSpan.Minutes:00}";
			break;
		case DisplayFormat.HH_MM:
			tmp.text = $"{Math.Floor(timeSpan.TotalHours):00}:{timeSpan.Minutes:00}";
			break;
		case DisplayFormat.MM_SS:
			tmp.text = $"{Math.Floor(timeSpan.TotalMinutes):00}:{timeSpan.Seconds:00}";
			break;
		}
	}

	protected override void OnBecameObservable()
	{
	}

	protected override void OnLostObservable()
	{
	}

	public void StartCountdown(int seconds)
	{
		overrideDt = GorillaComputer.instance.GetServerTime().AddSeconds(seconds);
	}
}
