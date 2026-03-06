using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GorillaNetworking;
using Newtonsoft.Json;
using PlayFab;
using UnityEngine;

public class KIDMessagingController : MonoBehaviour
{
	private const string SHOWN_CONFIRMATION_SCREEN_PREFIX = "hasShownKIDConfirmationScreen-";

	private const string CONFIRMATION_HEADER = "Thank you";

	private const string CONFIRMATION_BODY = "k-ID setup is now complete. Thanks and have fun in Gorilla World!";

	private const string CONFIRMATION_BUTTON = "Continue";

	private const string KID_SETUP_CONFIRMATION_TITLE_KEY = "KID_SETUP_CONFIRMATION_TITLE";

	private const string KID_SETUP_CONFIRMATION_BODY_KEY = "KID_SETUP_CONFIRMATION_BODY";

	private const string KID_SETUP_CONFIRMATION_BUTTON_KEY = "KID_SETUP_CONFIRMATION_BUTTON";

	private static KIDMessagingController instance;

	[SerializeField]
	private MessageBox messageBox;

	private const string CONNECTION_ERROR_HEADER = "Connection Error";

	private const string CONNECTION_ERROR_BODY = "Unable to connect to the internet. Please restart the game and try again.";

	private const string CONNECTION_ERROR_BUTTON = "Quit";

	private bool _closeMessageBox;

	private static string HasShownConfirmationScreenPlayerPref => "hasShownKIDConfirmationScreen-" + PlayFabAuthenticator.instance.GetPlayFabPlayerId();

	public void OnConfirmPressed()
	{
		_closeMessageBox = true;
	}

	private void Awake()
	{
		if (instance != null)
		{
			Debug.LogError("[KID::MESSAGING_CONTROLLER] Trying to start a new [KIDMessagingController] but one already exists");
			Object.Destroy(this);
		}
		else
		{
			instance = this;
		}
	}

	private bool ShouldShowConfirmationScreen()
	{
		if (KIDManager.CurrentSession.IsDefault)
		{
			return false;
		}
		return true;
	}

	private async Task StartKIDConfirmationScreenInternal(CancellationToken token)
	{
		if (messageBox == null)
		{
			Debug.LogError("[KID::MESSAGING_CONTROLLER] Trying to show confirmation screen but [messageBox] is null");
			return;
		}
		string text = await GetSetupConfirmationMessage();
		if (string.IsNullOrEmpty(text))
		{
			text = "k-ID setup is now complete. Thanks and have fun in Gorilla World!";
		}
		if (!LocalisationManager.TryGetKeyForCurrentLocale("KID_SETUP_CONFIRMATION_TITLE", out var result, "Thank you"))
		{
			Debug.LogError("[LOCALIZATION::KID_MESSAGING_CONTROLLER] Failed to get key for k-ID localization [KID_SETUP_CONFIRMATION_TITLE]");
		}
		messageBox.Header = result;
		if (!LocalisationManager.TryGetKeyForCurrentLocale("KID_SETUP_CONFIRMATION_BODY", out result, text))
		{
			Debug.LogError("[LOCALIZATION::KID_MESSAGING_CONTROLLER] Failed to get key for k-ID localization [KID_SETUP_CONFIRMATION_BODY]");
		}
		messageBox.Body = result;
		messageBox.LeftButton = string.Empty;
		if (!LocalisationManager.TryGetKeyForCurrentLocale("KID_SETUP_CONFIRMATION_BUTTON", out result, "Continue"))
		{
			Debug.LogError("[LOCALIZATION::KID_MESSAGING_CONTROLLER] Failed to get key for k-ID localization [KID_SETUP_CONFIRMATION_BUTTON]");
		}
		messageBox.RightButton = result;
		messageBox.gameObject.SetActive(value: true);
		HandRayController.Instance.EnableHandRays();
		PrivateUIRoom.AddUI(base.transform);
		do
		{
			if (token.IsCancellationRequested)
			{
				return;
			}
			await Task.Yield();
		}
		while (!_closeMessageBox);
		PrivateUIRoom.RemoveUI(base.transform);
		HandRayController.Instance.DisableHandRays();
		messageBox.gameObject.SetActive(value: false);
		await KIDManager.TrySetHasConfirmedStatus();
	}

	public void OnDisable()
	{
		KIDAudioManager.Instance?.PlaySoundWithDelay(KIDAudioManager.KIDSoundType.PageTransition);
	}

	public static async Task StartKIDConfirmationScreen(CancellationToken token)
	{
		KIDMessagingController kIDMessagingController = instance;
		if ((object)kIDMessagingController == null || kIDMessagingController.ShouldShowConfirmationScreen())
		{
			await instance.StartKIDConfirmationScreenInternal(token);
			TelemetryData telemetryData = new TelemetryData
			{
				EventName = "kid_screen_shown",
				CustomTags = new string[3]
				{
					"kid_setup",
					KIDTelemetry.GameVersionCustomTag,
					KIDTelemetry.GameEnvironment
				},
				BodyData = new Dictionary<string, string>
				{
					{ "screen", "setup_complete" },
					{
						"saw_game_settings",
						KIDUI_MainScreen.ShownSettingsScreen.ToString().ToLower() ?? ""
					}
				}
			};
			GorillaTelemetry.EnqueueTelemetryEvent(telemetryData.EventName, telemetryData.BodyData, telemetryData.CustomTags);
		}
	}

	private static async Task<string> GetSetupConfirmationMessage()
	{
		int state = 0;
		string bodyText = string.Empty;
		PlayFabTitleDataCache.Instance.GetTitleData("KIDData", delegate(string res)
		{
			state = 1;
			bodyText = GetConfirmMessageFromTitleDataJson(res);
		}, delegate(PlayFabError err)
		{
			state = -1;
			Debug.LogError("[KID_MANAGER] Something went wrong trying to get title data for key: [KIDData]. Error:\n" + err.ErrorMessage);
		});
		do
		{
			await Task.Yield();
		}
		while (state == 0);
		return bodyText;
	}

	private static string GetConfirmMessageFromTitleDataJson(string jsonTxt)
	{
		if (string.IsNullOrEmpty(jsonTxt))
		{
			Debug.LogError("[KID_MANAGER] Cannot get Confirmation Message. JSON is null or empty!");
			return null;
		}
		KIDMessagingTitleData kIDMessagingTitleData = JsonConvert.DeserializeObject<KIDMessagingTitleData>(jsonTxt);
		if (kIDMessagingTitleData == null)
		{
			Debug.LogError("[KID_MANAGER] Failed to parse json to [KIDMessagingTitleData]. Json: \n" + jsonTxt);
			return null;
		}
		if (string.IsNullOrEmpty(kIDMessagingTitleData.KIDSetupConfirmation))
		{
			Debug.LogError("[KID_MANAGER] Failed to parse json to [KIDMessagingTitleData] - [KIDSetupConfirmation] is null or empty. Json: \n" + jsonTxt);
			return null;
		}
		return kIDMessagingTitleData.KIDSetupConfirmation;
	}

	public static void ShowConnectionErrorScreen()
	{
		if (instance == null || instance.messageBox == null)
		{
			Debug.LogError("[KID::MESSAGING_CONTROLLER] No message box");
			return;
		}
		instance._closeMessageBox = false;
		instance.messageBox.Header = "Connection Error";
		instance.messageBox.Body = "Unable to connect to the internet. Please restart the game and try again.";
		instance.messageBox.RightButton = "Quit";
		instance.messageBox.ShowQuitButtonAsPrimary();
		instance.messageBox.RightButtonCallback.RemoveAllListeners();
		instance.messageBox.RightButtonCallback.AddListener(Application.Quit);
		instance.messageBox.gameObject.SetActive(value: true);
		HandRayController.Instance.EnableHandRays();
		PrivateUIRoom.AddUI(instance.transform);
	}
}
