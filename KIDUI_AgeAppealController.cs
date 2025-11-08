using System.Collections.Generic;
using UnityEngine;

public class KIDUI_AgeAppealController : MonoBehaviour
{
	private static KIDUI_AgeAppealController _instance;

	[SerializeField]
	private KIDUI_RestrictedAccessScreen _firstAgeAppealScreen;

	[SerializeField]
	private KIDUI_TooYoungToPlay _tooYoungToPlayScreen;

	public static KIDUI_AgeAppealController Instance => _instance;

	private void Awake()
	{
		_instance = this;
		Debug.LogFormat("[KID::UI::AGEAPPEALCONTROLLER] Controller Initialised");
	}

	public void StartAgeAppealScreens(SessionStatus? sessionStatus)
	{
		Debug.LogFormat("[KID::UI::AGEAPPEALCONTROLLER] Showing k-ID Age Appeal Screens");
		HandRayController.Instance.EnableHandRays();
		PrivateUIRoom.AddUI(base.transform);
		_firstAgeAppealScreen.ShowRestrictedAccessScreen(sessionStatus);
		if (KIDManager.TryGetAgeStatusTypeFromAge(KIDAgeGate.UserAge, out var ageType))
		{
			TelemetryData telemetryData = default(TelemetryData);
			telemetryData.EventName = "kid_age_appeal";
			telemetryData.CustomTags = new string[3]
			{
				"kid_age_appeal",
				KIDTelemetry.GameVersionCustomTag,
				KIDTelemetry.GameEnvironment
			};
			telemetryData.BodyData = new Dictionary<string, string> { 
			{
				"submitted_age",
				ageType.ToString()
			} };
			TelemetryData telemetryData2 = telemetryData;
			GorillaTelemetry.EnqueueTelemetryEvent(telemetryData2.EventName, telemetryData2.BodyData, telemetryData2.CustomTags);
		}
	}

	public void CloseKIDScreens()
	{
		PrivateUIRoom.RemoveUI(base.transform);
		HandRayController.Instance.DisableHandRays();
		_firstAgeAppealScreen.gameObject.SetActive(value: false);
		Object.DestroyImmediate(base.gameObject);
	}

	public void StartTooYoungToPlayScreen()
	{
		Debug.LogFormat("[KID::UI::AGEAPPEALCONTROLLER] Showing k-ID Too Young to Play Screen");
		HandRayController.Instance.EnableHandRays();
		PrivateUIRoom.AddUI(base.transform);
		_tooYoungToPlayScreen.ShowTooYoungToPlayScreen();
		TelemetryData telemetryData = default(TelemetryData);
		telemetryData.EventName = "kid_screen_shown";
		telemetryData.CustomTags = new string[3]
		{
			"kid_age_appeal",
			KIDTelemetry.GameVersionCustomTag,
			KIDTelemetry.GameEnvironment
		};
		telemetryData.BodyData = new Dictionary<string, string> { { "screen", "blocked" } };
		TelemetryData telemetryData2 = telemetryData;
		GorillaTelemetry.EnqueueTelemetryEvent(telemetryData2.EventName, telemetryData2.BodyData, telemetryData2.CustomTags);
	}

	public void OnQuitGamePressed()
	{
		Application.Quit();
	}

	public void OnDisable()
	{
		KIDAudioManager.Instance?.PlaySoundWithDelay(KIDAudioManager.KIDSoundType.PageTransition);
	}
}
