using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class KIDAgeGate : MonoBehaviour
{
	private const string LEARN_MORE_URL = "https://whyagegate.com/";

	private const string DEFAULT_AGE_VALUE_STRING = "SET AGE";

	private const int MINIMUM_PLATFORM_AGE = 13;

	[Header("Age Gate Settings")]
	[SerializeField]
	private PreGameMessage _pregameMessageReference;

	[SerializeField]
	private KIDUI_AgeDiscrepancyScreen _ageDiscrepancyScreen;

	[SerializeField]
	private GameObject _uiParent;

	[SerializeField]
	private AgeSliderWithProgressBar _ageSlider;

	[SerializeField]
	private GameObject _confirmationUI;

	[SerializeField]
	private KIDAgeGateConfirmation _confirmationUIManager;

	[SerializeField]
	private TMP_Text _confirmationAgeText;

	[SerializeField]
	private GameObject _whyAgeGateScreen;

	private const string strBlockAccessTitle = "UNDER AGE";

	private const string strBlockAccessMessage = "Your VR platform requires a certain minimum age to play Gorilla Tag. Unfortunately, due to those age requirements, we cannot allow you to play Gorilla Tag at this time.\n\nIf you incorrectly submitted your age, please appeal.";

	private const string strBlockAccessConfirm = "Hold any face button to appeal";

	private const string strVerifyAgeTitle = "VERIFY AGE";

	private const string strVerifyAgeMessage = "GETTING ONE TIME PASSCODE. PLEASE WAIT.\n\nGIVE IT TO A PARENT/GUARDIAN TO ENTER IT AT: k-id.com/code";

	private const string strDiscrepancyMessage = "You entered {0} for your age,\nbut your Meta account says you should be {1}. You could be logged into the wrong Meta account on this device.\n\nWe will use the lowest age ({2})\nif you Continue.";

	private static KIDAgeGate _activeReference;

	private static GetRequirementsData _ageGateConfig;

	private static int _ageValue;

	private CancellationTokenSource requestCancellationSource = new CancellationTokenSource();

	private static bool _hasChosenAge;

	private bool _metrics_LearnMorePressed;

	public static int UserAge => _ageValue;

	public static bool DisplayedScreen { get; private set; }

	private void Awake()
	{
		if (_activeReference != null)
		{
			Debug.LogError("[KID::Age_Gate] Age Gate already exists, this is a duplicate, deleting the new one");
			Object.DestroyImmediate(base.gameObject);
		}
		else
		{
			_activeReference = this;
		}
	}

	private async void Start()
	{
	}

	private void OnDestroy()
	{
		requestCancellationSource.Cancel();
	}

	public static async Task BeginAgeGate()
	{
		if (_activeReference == null)
		{
			Debug.LogError("[KID::Age_Gate] Unable to start Age Gate. No active reference assigned. Has it initialised yet?");
			do
			{
				await Task.Yield();
			}
			while (_activeReference == null);
		}
		await _activeReference.StartAgeGate();
	}

	private async Task StartAgeGate()
	{
		await InitialiseAgeGate();
	}

	private async Task InitialiseAgeGate()
	{
		Debug.Log("[KID] Initialising Age-Gate");
		TelemetryData telemetryData = new TelemetryData
		{
			EventName = "kid_screen_shown",
			CustomTags = new string[4]
			{
				KIDTelemetry.Open_MetricActionCustomTag,
				"kid_age_gate",
				KIDTelemetry.GameVersionCustomTag,
				KIDTelemetry.GameEnvironment
			},
			BodyData = new Dictionary<string, string> { { "screen", "age_gate" } }
		};
		GorillaTelemetry.EnqueueTelemetryEvent(telemetryData.EventName, telemetryData.BodyData, telemetryData.CustomTags);
		bool flag;
		do
		{
			DisplayedScreen = true;
			_ageSlider.ControllerActive = true;
			PrivateUIRoom.AddUI(_uiParent.transform);
			HandRayController.Instance.EnableHandRays();
			await ProcessAgeGate();
			_ageSlider.ControllerActive = false;
			KIDAudioManager.Instance?.PlaySoundWithDelay(KIDAudioManager.KIDSoundType.PageTransition);
			PrivateUIRoom.RemoveUI(_uiParent.transform);
			if (requestCancellationSource.IsCancellationRequested)
			{
				return;
			}
			if (KIDManager.TryGetAgeStatusTypeFromAge(UserAge, out var ageType))
			{
				telemetryData = new TelemetryData
				{
					EventName = "kid_age_gate",
					CustomTags = new string[4]
					{
						KIDTelemetry.Closed_MetricActionCustomTag,
						"kid_age_gate",
						KIDTelemetry.GameVersionCustomTag,
						KIDTelemetry.GameEnvironment
					},
					BodyData = new Dictionary<string, string> { 
					{
						"age_declared",
						ageType.ToString()
					} }
				};
				GorillaTelemetry.EnqueueTelemetryEvent(telemetryData.EventName, telemetryData.BodyData, telemetryData.CustomTags);
			}
			_confirmationUIManager.Reset(_ageValue);
			PrivateUIRoom.AddUI(_confirmationUI.transform);
			flag = await ProcessAgeGateConfirmation();
			telemetryData = new TelemetryData
			{
				EventName = "kid_age_gate_confirm",
				CustomTags = new string[3]
				{
					"kid_age_gate",
					KIDTelemetry.GameVersionCustomTag,
					KIDTelemetry.GameEnvironment
				},
				BodyData = new Dictionary<string, string> { 
				{
					"button_pressed",
					flag ? "confirm" : "go_back"
				} }
			};
			GorillaTelemetry.EnqueueTelemetryEvent(telemetryData.EventName, telemetryData.BodyData, telemetryData.CustomTags);
			KIDAudioManager.Instance?.PlaySoundWithDelay(KIDAudioManager.KIDSoundType.PageTransition);
			PrivateUIRoom.RemoveUI(_confirmationUI.transform);
			HandRayController.Instance.DisableHandRays();
		}
		while (!flag);
		OnAgeGateCompleted();
		Debug.Log("[KID] Age Gate Complete");
	}

	private async Task ProcessAgeGate()
	{
		Debug.Log("[KID] Waiting for Age Confirmation");
		await WaitForAgeChoice();
	}

	private async Task<bool> ProcessAgeGateConfirmation()
	{
		while (_confirmationUIManager.Result == KidAgeConfirmationResult.None)
		{
			if (requestCancellationSource.IsCancellationRequested)
			{
				return false;
			}
			await Task.Yield();
		}
		return _confirmationUIManager.Result == KidAgeConfirmationResult.Confirm;
	}

	private async Task WaitForAgeChoice()
	{
		_hasChosenAge = false;
		do
		{
			if (requestCancellationSource.IsCancellationRequested)
			{
				return;
			}
			await Task.Yield();
		}
		while (!_hasChosenAge);
		_ageValue = _ageSlider.CurrentAge;
		string ageString = _ageSlider.GetAgeString();
		_confirmationAgeText.text = "You entered " + ageString + "\n\nPlease be sure to enter your real age so we can customize your experience!";
	}

	public static void OnConfirmAgePressed(int currentAge)
	{
		_hasChosenAge = true;
	}

	private void OnAgeGateCompleted()
	{
		FinaliseAgeGateAndContinue();
	}

	private void FinaliseAgeGateAndContinue()
	{
		if (!requestCancellationSource.IsCancellationRequested)
		{
			Debug.Log("[KID::AGE_GATE] Age gate completed");
			Object.Destroy(base.gameObject);
		}
	}

	private void QuitGame()
	{
		Debug.Log("[KID] QUIT PRESSED");
		Application.Quit();
	}

	private async void AppealAge()
	{
		Debug.Log("[KID] APPEAL PRESSED");
		if (!KIDManager.InitialisationComplete)
		{
			Debug.LogError("[KID] [KIDManager] has not been Initialised yet. Unable to start appeals flow. Will wait until ready");
			do
			{
				await Task.Yield();
			}
			while (!KIDManager.InitialisationComplete);
		}
		if (KIDManager.InitialisationSuccessful)
		{
			string messageTitle = "VERIFY AGE";
			string messageBody = "GETTING ONE TIME PASSCODE. PLEASE WAIT.\n\nGIVE IT TO A PARENT/GUARDIAN TO ENTER IT AT: k-id.com/code";
			string empty = string.Empty;
			_pregameMessageReference.ShowMessage(messageTitle, messageBody, empty, RefreshChallengeStatus, 0.25f);
		}
		Debug.LogError("[KID::AGE_GATE] TODO: Refactor Age-Appeal flow");
	}

	private void AppealRejected()
	{
		Debug.Log("[KID] APPEAL REJECTED");
		string messageTitle = "UNDER AGE";
		string messageBody = "Your VR platform requires a certain minimum age to play Gorilla Tag. Unfortunately, due to those age requirements, we cannot allow you to play Gorilla Tag at this time.\n\nIf you incorrectly submitted your age, please appeal.";
		string messageConfirmation = "Hold any face button to appeal";
		_pregameMessageReference.ShowMessage(messageTitle, messageBody, messageConfirmation, AppealAge, 0.25f);
	}

	private void RefreshChallengeStatus()
	{
	}

	public static void SetAgeGateConfig(GetRequirementsData response)
	{
		_ageGateConfig = response;
	}

	public void OnWhyAgeGateButtonPressed()
	{
		TelemetryData telemetryData = new TelemetryData
		{
			EventName = "kid_screen_shown",
			CustomTags = new string[3]
			{
				"kid_age_gate",
				KIDTelemetry.GameVersionCustomTag,
				KIDTelemetry.GameEnvironment
			},
			BodyData = new Dictionary<string, string> { { "screen", "why_age_gate" } }
		};
		GorillaTelemetry.EnqueueTelemetryEvent(telemetryData.EventName, telemetryData.BodyData, telemetryData.CustomTags);
		_uiParent.SetActive(value: false);
		PrivateUIRoom.AddUI(_whyAgeGateScreen.transform);
		_whyAgeGateScreen.SetActive(value: true);
	}

	public void OnWhyAgeGateButtonBackPressed()
	{
		_uiParent.SetActive(value: true);
		PrivateUIRoom.RemoveUI(_whyAgeGateScreen.transform);
		_whyAgeGateScreen.SetActive(value: false);
	}

	public void OnLearnMoreAboutKIDPressed()
	{
		_metrics_LearnMorePressed = true;
		TelemetryData telemetryData = new TelemetryData
		{
			EventName = "kid_screen_shown",
			CustomTags = new string[3]
			{
				"kid_age_gate",
				KIDTelemetry.GameVersionCustomTag,
				KIDTelemetry.GameEnvironment
			},
			BodyData = new Dictionary<string, string> { { "screen", "learn_more_url" } }
		};
		GorillaTelemetry.EnqueueTelemetryEvent(telemetryData.EventName, telemetryData.BodyData, telemetryData.CustomTags);
		Application.OpenURL("https://whyagegate.com/");
	}
}
