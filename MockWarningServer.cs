using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GorillaNetworking;
using UnityEngine;

internal class MockWarningServer : WarningsServer
{
	public struct ButtonSetup
	{
		public string buttonText;

		public WarningButtonResult buttonResult;

		public ButtonSetup(string txt, WarningButtonResult result)
		{
			buttonText = txt;
			buttonResult = result;
		}
	}

	private const string SHOWN_SCREEN_PREFIX = "screen-shown-";

	private const string KID_WARNING_TITLE_KEY = "KID_WARNING_TITLE";

	private const string KID_WARNING_CONTINUE_KEY = "KID_WARNING_CONTINUE";

	private const string KID_WARNING_PHASE_THREE_IN_COHORT_KEY = "KID_WARNING_PHASE_THREE_IN_COHORT";

	private const string KID_WARNING_PHASE_FOUR_RETURNING_PLAYER_KEY = "KID_WARNING_PHASE_FOUR_RETURNING_PLAYER";

	private const string KID_WARNING_OPT_IN_FOLLOW_MESSAGE_KEY = "KID_WARNING_OPT_IN_FOLLOW_MESSAGE";

	private const string KID_WARNING_FOLLOW_UP_YAY_KEY = "KID_WARNING_FOLLOW_UP_YAY";

	public static string ShownScreenPlayerPref => "screen-shown-" + PlayFabAuthenticator.instance.GetPlayFabPlayerId();

	private void Awake()
	{
		if (WarningsServer.Instance == null)
		{
			WarningsServer.Instance = this;
		}
		else
		{
			UnityEngine.Object.Destroy(this);
		}
	}

	private PlayerAgeGateWarningStatus CreateWarningStatus(string header, string body, ButtonSetup? leftButtonSetup, ButtonSetup? rightButtonSetup, EImageVisibility showImage, Action leftButtonCallback, Action rightButtonCallback)
	{
		PlayerAgeGateWarningStatus result = default(PlayerAgeGateWarningStatus);
		result.header = header;
		result.body = body;
		result.leftButtonText = string.Empty;
		result.rightButtonText = string.Empty;
		result.leftButtonResult = WarningButtonResult.None;
		result.rightButtonResult = WarningButtonResult.None;
		result.noWarningResult = WarningButtonResult.None;
		result.showImage = showImage;
		result.onLeftButtonPressedAction = leftButtonCallback;
		result.onRightButtonPressedAction = rightButtonCallback;
		if (leftButtonSetup.HasValue)
		{
			result.leftButtonText = leftButtonSetup.Value.buttonText;
			result.leftButtonResult = leftButtonSetup.Value.buttonResult;
		}
		if (rightButtonSetup.HasValue)
		{
			result.rightButtonText = rightButtonSetup.Value.buttonText;
			result.rightButtonResult = rightButtonSetup.Value.buttonResult;
		}
		return result;
	}

	public override async Task<PlayerAgeGateWarningStatus?> FetchPlayerData(CancellationToken token)
	{
		int num = await KIDManager.CheckKIDPhase();
		if (token.IsCancellationRequested)
		{
			return null;
		}
		bool flag = GorillaServer.Instance.CheckIsInKIDOptInCohort();
		bool flag2 = GorillaServer.Instance.CheckIsInKIDRequiredCohort();
		if (!ShouldShowWarningScreen(num, flag))
		{
			return CreateWarningStatus("", "", null, null, EImageVisibility.None, null, null);
		}
		Debug.Log($"[KID::WARNING_SERVER] Phase Is: [{num}]");
		PlayerAgeGateWarningStatus value2;
		switch (num)
		{
		case 1:
		{
			ButtonSetup value3 = new ButtonSetup("Continue", WarningButtonResult.CloseWarning);
			value2 = CreateWarningStatus("IMPORTANT NEWS", "We're working to make Gorilla Tag a better, more age-appropriate experience in our next update. To learn more, please check out our Discord.", null, value3, EImageVisibility.None, null, null);
			break;
		}
		default:
			return CreateWarningStatus("", "", null, null, EImageVisibility.None, null, null);
		case 5:
			value2 = CreateWarningStatus("", "", null, null, EImageVisibility.None, null, null);
			value2.noWarningResult = WarningButtonResult.Continue;
			break;
		case 2:
			if (flag)
			{
				ButtonSetup value4 = new ButtonSetup("Do This Later", WarningButtonResult.CloseWarning);
				value2 = CreateWarningStatus(rightButtonSetup: new ButtonSetup("Opt-In", WarningButtonResult.OptIn), header: "IMPORTANT NEWS", body: "We have partnered with k-ID to create a better, more age-appropriate experience. Opt-in early and get 500 Shiny Rocks as our way of saying \"Thanks!\"", leftButtonSetup: value4, showImage: EImageVisibility.AfterBody, leftButtonCallback: delegate
				{
					TelemetryData telemetryData3 = default(TelemetryData);
					telemetryData3.EventName = "kid_phase2_incohort";
					telemetryData3.CustomTags = new string[4]
					{
						"kid_warning_screen",
						"kid_phase_2",
						KIDTelemetry.GameVersionCustomTag,
						KIDTelemetry.GameEnvironment
					};
					telemetryData3.BodyData = new Dictionary<string, string> { { "opt_in_choice", "skip" } };
					TelemetryData telemetryData4 = telemetryData3;
					GorillaTelemetry.EnqueueTelemetryEvent(telemetryData4.EventName, telemetryData4.BodyData, telemetryData4.CustomTags);
				}, rightButtonCallback: delegate
				{
					TelemetryData telemetryData5 = default(TelemetryData);
					telemetryData5.EventName = "kid_phase2_incohort";
					telemetryData5.CustomTags = new string[4]
					{
						"kid_warning_screen",
						"kid_phase_2",
						KIDTelemetry.GameVersionCustomTag,
						KIDTelemetry.GameEnvironment
					};
					telemetryData5.BodyData = new Dictionary<string, string> { { "opt_in_choice", "sign_up" } };
					TelemetryData telemetryData6 = telemetryData5;
					GorillaTelemetry.EnqueueTelemetryEvent(telemetryData6.EventName, telemetryData6.BodyData, telemetryData6.CustomTags);
				});
			}
			else
			{
				ButtonSetup value5 = new ButtonSetup("Continue", WarningButtonResult.CloseWarning);
				value2 = CreateWarningStatus("IMPORTANT NEWS", "We're working to make Gorilla Tag a better, more age-appropriate experience in the coming days. To learn more, please check out our Discord.", null, value5, EImageVisibility.None, null, null);
				TelemetryData telemetryData7 = default(TelemetryData);
				telemetryData7.EventName = "kid_screen_shown";
				telemetryData7.CustomTags = new string[4]
				{
					"kid_warning_screen",
					"kid_phase_2",
					KIDTelemetry.GameVersionCustomTag,
					KIDTelemetry.GameEnvironment
				};
				telemetryData7.BodyData = new Dictionary<string, string> { { "screen", "phase2_nocohort" } };
				TelemetryData telemetryData8 = telemetryData7;
				GorillaTelemetry.EnqueueTelemetryEvent(telemetryData8.EventName, telemetryData8.BodyData, telemetryData8.CustomTags);
			}
			break;
		case 3:
		{
			if (flag2)
			{
				if (!LocalisationManager.TryGetKeyForCurrentLocale("KID_WARNING_CONTINUE", out var result4, "Continue"))
				{
					Debug.LogError("[LOCALIZATION::ATM_MANAGER] Failed to get key for [KID_WARNING_CONTINUE]");
				}
				ButtonSetup value6 = new ButtonSetup(result4, WarningButtonResult.OptIn);
				if (!LocalisationManager.TryGetKeyForCurrentLocale("KID_WARNING_TITLE", out var result5, "IMPORTANT NEWS"))
				{
					Debug.LogError("[LOCALIZATION::ATM_MANAGER] Failed to get key for [KID_WARNING_TITLE]");
				}
				string defaultResult2 = "We have partnered with k-ID to create a better, more age-appropriate experience. Confirm your age and get 500 Shiny Rocks as our way of saying \"Thanks!\"";
				if (!LocalisationManager.TryGetKeyForCurrentLocale("KID_WARNING_PHASE_THREE_IN_COHORT", out var result6, defaultResult2))
				{
					Debug.LogError("[LOCALIZATION::ATM_MANAGER] Failed to get key for [KID_WARNING_PHASE_THREE_IN_COHORT]");
				}
				value2 = CreateWarningStatus(result5, result6, value6, null, EImageVisibility.AfterBody, delegate
				{
					TelemetryData telemetryData9 = default(TelemetryData);
					telemetryData9.EventName = "kid_screen_shown";
					telemetryData9.CustomTags = new string[4]
					{
						"kid_warning_screen",
						"kid_phase_3",
						KIDTelemetry.GameVersionCustomTag,
						KIDTelemetry.GameEnvironment
					};
					telemetryData9.BodyData = new Dictionary<string, string> { { "screen", "phase3_required" } };
					TelemetryData telemetryData10 = telemetryData9;
					GorillaTelemetry.EnqueueTelemetryEvent(telemetryData10.EventName, telemetryData10.BodyData, telemetryData10.CustomTags);
				}, null);
				break;
			}
			ButtonSetup value7 = new ButtonSetup("Do This Later", WarningButtonResult.CloseWarning);
			value2 = CreateWarningStatus(rightButtonSetup: new ButtonSetup("Opt-In", WarningButtonResult.OptIn), header: "IMPORTANT NEWS", body: "We have partnered with k-ID to create a better, more age-appropriate experience. Opt-in early and get 500 Shiny Rocks as our way of saying \"Thanks!\"", leftButtonSetup: value7, showImage: EImageVisibility.AfterBody, leftButtonCallback: delegate
			{
				TelemetryData telemetryData11 = default(TelemetryData);
				telemetryData11.EventName = "kid_phase3_optional";
				telemetryData11.CustomTags = new string[4]
				{
					"kid_warning_screen",
					"kid_phase_3",
					KIDTelemetry.GameVersionCustomTag,
					KIDTelemetry.GameEnvironment
				};
				telemetryData11.BodyData = new Dictionary<string, string> { { "opt_in_choice", "skip" } };
				TelemetryData telemetryData12 = telemetryData11;
				GorillaTelemetry.EnqueueTelemetryEvent(telemetryData12.EventName, telemetryData12.BodyData, telemetryData12.CustomTags);
			}, rightButtonCallback: delegate
			{
				TelemetryData telemetryData13 = default(TelemetryData);
				telemetryData13.EventName = "kid_phase3_optional";
				telemetryData13.CustomTags = new string[4]
				{
					"kid_warning_screen",
					"kid_phase_3",
					KIDTelemetry.GameVersionCustomTag,
					KIDTelemetry.GameEnvironment
				};
				telemetryData13.BodyData = new Dictionary<string, string> { { "opt_in_choice", "sign_up" } };
				TelemetryData telemetryData14 = telemetryData13;
				GorillaTelemetry.EnqueueTelemetryEvent(telemetryData14.EventName, telemetryData14.BodyData, telemetryData14.CustomTags);
			});
			break;
		}
		case 4:
			if (PlayFabAuthenticator.instance.IsReturningPlayer)
			{
				if (!LocalisationManager.TryGetKeyForCurrentLocale("KID_WARNING_CONTINUE", out var result, "Continue"))
				{
					Debug.LogError("[LOCALIZATION::ATM_MANAGER] Failed to get key for [KID_WARNING_CONTINUE]");
				}
				ButtonSetup value = new ButtonSetup(result, WarningButtonResult.OptIn);
				if (!LocalisationManager.TryGetKeyForCurrentLocale("KID_WARNING_TITLE", out var result2, "IMPORTANT NEWS"))
				{
					Debug.LogError("[LOCALIZATION::ATM_MANAGER] Failed to get key for [KID_WARNING_TITLE]");
				}
				string defaultResult = "We have partnered with k-ID to create a better, more age-appropriate experience. Confirm your age and get 100 Shiny Rocks as our way of saying \"Thanks!\"";
				if (!LocalisationManager.TryGetKeyForCurrentLocale("KID_WARNING_PHASE_FOUR_RETURNING_PLAYER", out var result3, defaultResult))
				{
					Debug.LogError("[LOCALIZATION::ATM_MANAGER] Failed to get key for [KID_WARNING_PHASE_FOUR_RETURNING_PLAYER]");
				}
				value2 = CreateWarningStatus(result2, result3, null, value, EImageVisibility.AfterBody, delegate
				{
					TelemetryData telemetryData = default(TelemetryData);
					telemetryData.EventName = "kid_screen_shown";
					telemetryData.CustomTags = new string[4]
					{
						"kid_warning_screen",
						"kid_phase_4",
						KIDTelemetry.GameVersionCustomTag,
						KIDTelemetry.GameEnvironment
					};
					telemetryData.BodyData = new Dictionary<string, string> { { "screen", "phase4" } };
					TelemetryData telemetryData2 = telemetryData;
					GorillaTelemetry.EnqueueTelemetryEvent(telemetryData2.EventName, telemetryData2.BodyData, telemetryData2.CustomTags);
				}, null);
			}
			else
			{
				value2 = CreateWarningStatus("", "", null, null, EImageVisibility.None, null, null);
			}
			break;
		}
		PlayerPrefs.SetInt($"phase-{num}-{ShownScreenPlayerPref}", 1);
		PlayerPrefs.Save();
		return value2;
	}

	public override async Task<PlayerAgeGateWarningStatus?> GetOptInFollowUpMessage(CancellationToken token)
	{
		int num = await KIDManager.CheckKIDPhase();
		if (token.IsCancellationRequested)
		{
			return null;
		}
		PlayerAgeGateWarningStatus? result = null;
		string result2;
		switch (num)
		{
		case 2:
		{
			ButtonSetup value2 = new ButtonSetup("Yay!", WarningButtonResult.CloseWarning);
			result = CreateWarningStatus("", "Your shiny rocks have been granted!", null, value2, EImageVisibility.BeforeBody, null, null);
			break;
		}
		case 3:
		{
			if (!LocalisationManager.TryGetKeyForCurrentLocale("KID_WARNING_FOLLOW_UP_YAY", out result2, "Yay!"))
			{
				Debug.LogWarning("[KID::WARNING_SERVER] Missing localisation key: KID_WARNING_FOLLOW_UP_YAY");
			}
			ButtonSetup value3 = new ButtonSetup(result2, WarningButtonResult.CloseWarning);
			if (!LocalisationManager.TryGetKeyForCurrentLocale("KID_WARNING_OPT_IN_FOLLOW_MESSAGE", out result2, "Your shiny rocks have been granted!"))
			{
				Debug.LogWarning("[KID::WARNING_SERVER] Missing localisation key: KID_WARNING_OPT_IN_FOLLOW_MESSAGE");
			}
			result = CreateWarningStatus("", result2, null, value3, EImageVisibility.BeforeBody, null, null);
			break;
		}
		case 4:
			if (PlayFabAuthenticator.instance.IsReturningPlayer)
			{
				if (!LocalisationManager.TryGetKeyForCurrentLocale("KID_WARNING_FOLLOW_UP_YAY", out result2, "Yay!"))
				{
					Debug.LogWarning("[KID::WARNING_SERVER] Missing localisation key: KID_WARNING_FOLLOW_UP_YAY");
				}
				ButtonSetup value = new ButtonSetup(result2, WarningButtonResult.CloseWarning);
				if (!LocalisationManager.TryGetKeyForCurrentLocale("KID_WARNING_OPT_IN_FOLLOW_MESSAGE", out result2, "Your shiny rocks have been granted!"))
				{
					Debug.LogWarning("[KID::WARNING_SERVER] Missing localisation key: KID_WARNING_OPT_IN_FOLLOW_MESSAGE");
				}
				result = CreateWarningStatus("", result2, null, value, EImageVisibility.BeforeBody, null, null);
			}
			break;
		}
		return result;
	}

	private bool ShouldShowWarningScreen(int phase, bool inOptInCohort)
	{
		if (PlayerPrefs.GetInt($"phase-{phase}-{ShownScreenPlayerPref}", 0) == 0)
		{
			return true;
		}
		switch (phase)
		{
		default:
			return false;
		case 2:
			return inOptInCohort;
		case 3:
		case 4:
			return true;
		}
	}
}
