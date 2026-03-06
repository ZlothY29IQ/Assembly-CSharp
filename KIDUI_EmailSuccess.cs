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
		TelemetryData telemetryData = new TelemetryData
		{
			EventName = "kid_screen_shown",
			CustomTags = new string[3]
			{
				"kid_setup",
				KIDTelemetry.GameVersionCustomTag,
				KIDTelemetry.GameEnvironment
			},
			BodyData = new Dictionary<string, string> { { "screen", "email_sent" } }
		};
		GorillaTelemetry.EnqueueTelemetryEvent(telemetryData.EventName, telemetryData.BodyData, telemetryData.CustomTags);
	}

	public void ShowSuccessScreenAppeal(string email)
	{
		_emailTxt.text = email;
		base.gameObject.SetActive(value: true);
		TelemetryData telemetryData = new TelemetryData
		{
			EventName = "kid_screen_shown",
			CustomTags = new string[3]
			{
				"kid_age_appeal",
				KIDTelemetry.GameVersionCustomTag,
				KIDTelemetry.GameEnvironment
			},
			BodyData = new Dictionary<string, string> { { "screen", "age_appeal_email_sent" } }
		};
		GorillaTelemetry.EnqueueTelemetryEvent(telemetryData.EventName, telemetryData.BodyData, telemetryData.CustomTags);
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
