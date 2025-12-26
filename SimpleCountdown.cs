using System;
using System.Threading.Tasks;
using GorillaNetworking;
using PlayFab;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshPro))]
public class SimpleCountdown : ObservableBehavior
{
	private enum DisplayFormat
	{
		DD_HH_MM_SS,
		HH_MM_SS,
		DD_HH_MM,
		HH_MM
	}

	[SerializeField]
	private DisplayFormat displayFormat;

	[SerializeField]
	private bool useTitleData = true;

	[SerializeField]
	private string titleDataKey;

	[SerializeField]
	private string date;

	[SerializeField]
	private Vector2 hourRange = new Vector2(float.MinValue, float.MaxValue);

	private DateTime dt;

	private TextMeshPro tmp;

	private async void Start()
	{
		tmp = GetComponent<TextMeshPro>();
		if (useTitleData)
		{
			while (PlayFabTitleDataCache.Instance == null)
			{
				await Task.Yield();
			}
			PlayFabTitleDataCache.Instance.GetTitleData(titleDataKey, onTD, onTDError);
		}
		else
		{
			ParseDateTime();
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
		if (!(GorillaComputer.instance == null))
		{
			_ = dt;
			TimeSpan timeSpan = dt - GorillaComputer.instance.GetServerTime();
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
			}
		}
	}

	protected override void OnBecameObservable()
	{
	}

	protected override void OnLostObservable()
	{
	}
}
