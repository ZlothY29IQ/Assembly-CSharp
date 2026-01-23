using System;
using System.Threading.Tasks;
using GorillaNetworking;
using PlayFab;
using TMPro;
using UnityEngine;

public class Clock3x3 : ObservableBehavior
{
	[SerializeField]
	private string titleDataKey;

	[SerializeField]
	private TMP_Text display;

	[SerializeField]
	private Gradient color;

	private string formatString;

	private bool initialized;

	[SerializeField]
	private string[] headings;

	protected override void ObservableSliceUpdate()
	{
		DateTime serverTime = GorillaComputer.instance.GetServerTime();
		DateTime now = DateTime.Now;
		if (serverTime.Year >= 2000)
		{
			display.text = string.Format(formatString, headings[0], headings[1], serverTime.ToString("hh:mm:sstt"), DateTime.Now.ToString("hh:mm:sstt"), HexColor(color.Evaluate(((float)serverTime.Hour + (float)serverTime.Minute / 60f) / 24f)), HexColor(color.Evaluate(((float)now.Hour + (float)now.Minute / 60f) / 24f)));
		}
	}

	public string HexColor(Color color)
	{
		return "#" + Mathf.FloorToInt(Mathf.Clamp01(color.r) * 255f).ToString("X2") + Mathf.FloorToInt(Mathf.Clamp01(color.g) * 255f).ToString("X2") + Mathf.FloorToInt(Mathf.Clamp01(color.b) * 255f).ToString("X2");
	}

	protected override void OnBecameObservable()
	{
		display.gameObject.SetActive(value: true);
		Initialize();
	}

	private async void Initialize()
	{
		if (initialized)
		{
			return;
		}
		initialized = true;
		formatString = display.text;
		display.text = string.Empty;
		if (!titleDataKey.IsNullOrEmpty())
		{
			while (PlayFabTitleDataCache.Instance == null)
			{
				await Task.Yield();
			}
			PlayFabTitleDataCache.Instance.GetTitleData(titleDataKey, onTD, onTDError);
		}
	}

	private void onTD(string s)
	{
		headings = s.Split(";");
	}

	private void onTDError(PlayFabError error)
	{
		Debug.LogError($"Clock3x3 :: onTDError :: {titleDataKey} :: {error}");
	}

	protected override void OnLostObservable()
	{
		display.gameObject.SetActive(value: false);
	}
}
