using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class KIDUI_ConfirmScreen : MonoBehaviour
{
	[SerializeField]
	private TMP_Text _emailToConfirmTxt;

	[SerializeField]
	private KIDUI_MainScreen _mainScreen;

	[SerializeField]
	private KIDUI_SetupScreen _setupScreen;

	[SerializeField]
	private KIDUI_ErrorScreen _errorScreen;

	[SerializeField]
	private KIDUI_EmailSuccess _successScreen;

	[SerializeField]
	private KIDUI_AnimatedEllipsis _animatedEllipsis;

	[SerializeField]
	private KIDUIButton _confirmButton;

	[SerializeField]
	private KIDUIButton _backButton;

	[SerializeField]
	private int _minimumDelay = 1000;

	private string _submittedEmailAddress;

	private CancellationTokenSource _cancellationTokenSource;

	private bool _hasCompletedSendEmailRequest;

	private bool _emailRequestResult;

	private void Awake()
	{
		if (_emailToConfirmTxt == null)
		{
			Debug.LogErrorFormat("[KID::UI::Setup] Email To Confirm Field is NULL");
		}
		else if (_setupScreen == null)
		{
			Debug.LogErrorFormat("[KID::UI::Setup] Setup K-ID Screen is NULL");
		}
		else if (_mainScreen == null)
		{
			Debug.LogErrorFormat("[KID::UI::Setup] Main Screen is NULL");
		}
		else
		{
			_cancellationTokenSource = new CancellationTokenSource();
		}
	}

	private void OnEnable()
	{
		_confirmButton.interactable = true;
		_backButton.interactable = true;
	}

	public void OnEmailSubmitted(string emailAddress)
	{
		_submittedEmailAddress = emailAddress;
		_emailToConfirmTxt.text = _submittedEmailAddress;
		base.gameObject.SetActive(value: true);
	}

	public async void OnConfirmPressed()
	{
		TelemetryData telemetryData = new TelemetryData
		{
			EventName = "kid_email_confirm",
			CustomTags = new string[3]
			{
				"kid_setup",
				KIDTelemetry.GameVersionCustomTag,
				KIDTelemetry.GameEnvironment
			},
			BodyData = new Dictionary<string, string> { { "button_pressed", "confirm" } }
		};
		GorillaTelemetry.EnqueueTelemetryEvent(telemetryData.EventName, telemetryData.BodyData, telemetryData.CustomTags);
		_confirmButton.interactable = false;
		_backButton.interactable = false;
		await _animatedEllipsis.StartAnimation();
		Debug.Log("[KID::UI::CONFIRM_EMAIL] Ellipsis Animation Complete, proceeding to send email");
		(bool success, string message) result = await KIDManager.SetAndSendEmail(_submittedEmailAddress);
		do
		{
			await Task.Yield();
		}
		while (!_hasCompletedSendEmailRequest);
		Debug.Log($"[KID::UI::CONFIRM_EMAIL] Email has been sent, awaiting minimum duration {_minimumDelay}");
		if (_minimumDelay > 0)
		{
			await Task.Delay(_minimumDelay);
		}
		base.gameObject.SetActive(value: false);
		if (!result.success)
		{
			ShowErrorScreen(result.message);
			return;
		}
		Debug.Log("[KID::UI::CONFIRM_EMAIL] Minimum duration passed, result is successful. Proceeding to Success screen");
		KIDAudioManager.Instance?.PlaySoundWithDelay(KIDAudioManager.KIDSoundType.PageTransition);
		_mainScreen.OnConfirmedEmailAddress(_submittedEmailAddress);
		_successScreen.ShowSuccessScreen(_submittedEmailAddress);
	}

	public async void OnBackPressed()
	{
		TelemetryData telemetryData = new TelemetryData
		{
			EventName = "kid_email_confirm",
			CustomTags = new string[3]
			{
				"kid_setup",
				KIDTelemetry.GameVersionCustomTag,
				KIDTelemetry.GameEnvironment
			},
			BodyData = new Dictionary<string, string> { { "button_pressed", "go_back" } }
		};
		GorillaTelemetry.EnqueueTelemetryEvent(telemetryData.EventName, telemetryData.BodyData, telemetryData.CustomTags);
		_cancellationTokenSource.Cancel();
		await _animatedEllipsis.StopAnimation();
		base.gameObject.SetActive(value: false);
		_setupScreen.OnStartSetup();
	}

	public void NotifyOfResult(bool success)
	{
		_hasCompletedSendEmailRequest = true;
		_emailRequestResult = success;
	}

	private async void ShowErrorScreen(string errorMessage)
	{
		Debug.LogErrorFormat("[KID::UI::Setup] K-ID Confirmation Failed - Failed to send email");
		_cancellationTokenSource.Cancel();
		await _animatedEllipsis.StopAnimation();
		base.gameObject.SetActive(value: false);
		_errorScreen.ShowErrorScreen("Confirmation Error", _submittedEmailAddress, errorMessage);
	}

	public void OnDisable()
	{
		KIDAudioManager.Instance?.PlaySoundWithDelay(KIDAudioManager.KIDSoundType.PageTransition);
	}
}
