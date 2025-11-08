using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class KIDUI_EmailSuccess : MonoBehaviour
{
	[SerializeField]
	private TMP_Text _emailTxt;

	[SerializeField]
	private KIDUI_MainScreen _mainScreen;

	public void ShowSuccessScreen(string email)
	{
		_emailTxt.text = email;
		base.gameObject.SetActive(value: true);
		TelemetryData telemetryData = default(TelemetryData);
		telemetryData.EventName = "kid_screen_shown";
		telemetryData.CustomTags = new string[3]
		{
			"kid_setup",
			KIDTelemetry.GameVersionCustomTag,
			KIDTelemetry.GameEnvironment
		};
		telemetryData.BodyData = new Dictionary<string, string> { { "screen", "email_sent" } };
		TelemetryData telemetryData2 = telemetryData;
		GorillaTelemetry.EnqueueTelemetryEvent(telemetryData2.EventName, telemetryData2.BodyData, telemetryData2.CustomTags);
	}

	public void ShowSuccessScreenAppeal(string email)
	{
		_emailTxt.text = email;
		base.gameObject.SetActive(value: true);
		TelemetryData telemetryData = default(TelemetryData);
		telemetryData.EventName = "kid_screen_shown";
		telemetryData.CustomTags = new string[3]
		{
			"kid_age_appeal",
			KIDTelemetry.GameVersionCustomTag,
			KIDTelemetry.GameEnvironment
		};
		telemetryData.BodyData = new Dictionary<string, string> { { "screen", "age_appeal_email_sent" } };
		TelemetryData telemetryData2 = telemetryData;
		GorillaTelemetry.EnqueueTelemetryEvent(telemetryData2.EventName, telemetryData2.BodyData, telemetryData2.CustomTags);
	}

	public void OnClose()
	{
		base.gameObject.SetActive(value: false);
		_mainScreen.ShowMainScreen(EMainScreenStatus.Pending);
	}

	public void OnCloseGame()
	{
		Application.Quit();
	}
}
