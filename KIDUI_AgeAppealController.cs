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
			TelemetryData telemetryData = new TelemetryData
			{
				EventName = "kid_age_appeal",
				CustomTags = new string[3]
				{
					"kid_age_appeal",
					KIDTelemetry.GameVersionCustomTag,
					KIDTelemetry.GameEnvironment
				},
				BodyData = new Dictionary<string, string> { 
				{
					"submitted_age",
					ageType.ToString()
				} }
			};
			GorillaTelemetry.EnqueueTelemetryEvent(telemetryData.EventName, telemetryData.BodyData, telemetryData.CustomTags);
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
		TelemetryData telemetryData = new TelemetryData
		{
			EventName = "kid_screen_shown",
			CustomTags = new string[3]
			{
				"kid_age_appeal",
				KIDTelemetry.GameVersionCustomTag,
				KIDTelemetry.GameEnvironment
			},
			BodyData = new Dictionary<string, string> { { "screen", "blocked" } }
		};
		GorillaTelemetry.EnqueueTelemetryEvent(telemetryData.EventName, telemetryData.BodyData, telemetryData.CustomTags);
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
