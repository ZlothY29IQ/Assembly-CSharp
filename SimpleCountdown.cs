using System;
using System.Threading.Tasks;
using GorillaNetworking;
using GorillaNetworking.ScheduledEvents;
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
		ScheduledEvent
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
	private string date;

	[SerializeField]
	private ServerTimeSyncRule timeSyncRule;

	[SerializeField]
	private Vector2 hourRange = new Vector2(float.MinValue, float.MaxValue);

	private DateTime dt;

	private TextMeshPro tmp;

	private DateTime overrideDt = DateTime.MinValue;

	public Action ManualCountdownComplete;

	private async void Start()
	{
		tmp = GetComponent<TextMeshPro>();
		switch (mode)
		{
		case Mode.TitleData:
			while (PlayFabTitleDataCache.Instance == null)
			{
				await Task.Yield();
			}
			PlayFabTitleDataCache.Instance.GetTitleData(titleDataKey, onTD, onTDError);
			break;
		case Mode.FixedDate:
			ParseDateTime();
			break;
		case Mode.TimeSync:
			if (GorillaComputer.instance != null)
			{
				DateTime serverTime = GorillaComputer.instance.GetServerTime();
				dt = timeSyncRule.GetPrevious(serverTime);
			}
			break;
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
				double value = ((ScheduledEventManager.Instance != null) ? ScheduledEventManager.Instance.SecondsUntilEventStart : 0.0);
				dt = serverTime.AddSeconds(value);
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
