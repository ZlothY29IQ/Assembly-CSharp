using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class KIDUI_AgeAppealEmailConfirmation : MonoBehaviour
{
	[SerializeField]
	private TMP_Text _confirmText;

	[SerializeField]
	private TMP_Text _emailText;

	private string CONFIRM_PARENT_EMAIL = "Please confirm your parent or guardian's email address.";

	private string CONFIRM_YOUR_EMAIL = "Please confirm your email address.";

	private bool hasChallenge = true;

	private int newAgeToAppeal;

	private bool _hasCompletedSendEmailRequest;

	[SerializeField]
	private KIDUI_EmailSuccess _successScreen;

	[SerializeField]
	private KIDUI_AgeAppealEmailError _errorScreen;

	[SerializeField]
	private KIDUI_AgeAppealEmailScreen _ageAppealEmailScreen;

	[SerializeField]
	private int _minimumDelay = 1000;

	private void OnEnable()
	{
		KIDManager.onEmailResultReceived = (KIDManager.OnEmailResultReceived)Delegate.Combine(KIDManager.onEmailResultReceived, new KIDManager.OnEmailResultReceived(NotifyOfEmailResult));
	}

	private void OnDisable()
	{
		KIDManager.onEmailResultReceived = (KIDManager.OnEmailResultReceived)Delegate.Remove(KIDManager.onEmailResultReceived, new KIDManager.OnEmailResultReceived(NotifyOfEmailResult));
		KIDAudioManager.Instance?.PlaySoundWithDelay(KIDAudioManager.KIDSoundType.PageTransition);
	}

	public void ShowAgeAppealConfirmationScreen(bool hasChallenge, int newAge, string emailToConfirm)
	{
		this.hasChallenge = hasChallenge;
		newAgeToAppeal = newAge;
		_confirmText.text = (this.hasChallenge ? CONFIRM_PARENT_EMAIL : CONFIRM_YOUR_EMAIL);
		_emailText.text = emailToConfirm;
		base.gameObject.SetActive(value: true);
	}

	public void OnConfirmPressed()
	{
		TelemetryData telemetryData = new TelemetryData
		{
			EventName = "kid_age_appeal_confirm_email",
			CustomTags = new string[3]
			{
				"kid_age_appeal",
				KIDTelemetry.GameVersionCustomTag,
				KIDTelemetry.GameEnvironment
			},
			BodyData = new Dictionary<string, string>
			{
				{
					"email_type",
					hasChallenge ? "under_dac" : "over_dac"
				},
				{ "button_pressed", "confirm" }
			}
		};
		GorillaTelemetry.EnqueueTelemetryEvent(telemetryData.EventName, telemetryData.BodyData, telemetryData.CustomTags);
		if (hasChallenge)
		{
			StartAgeAppealChallengeEmail();
		}
		else
		{
			StartAgeAppealEmail();
		}
	}

	public void OnBackPressed()
	{
		TelemetryData telemetryData = new TelemetryData
		{
			EventName = "kid_age_appeal_confirm_email",
			CustomTags = new string[3]
			{
				"kid_age_appeal",
				KIDTelemetry.GameVersionCustomTag,
				KIDTelemetry.GameEnvironment
			},
			BodyData = new Dictionary<string, string>
			{
				{
					"email_type",
					hasChallenge ? "under_dac" : "over_dac"
				},
				{ "button_pressed", "go_back" }
			}
		};
		GorillaTelemetry.EnqueueTelemetryEvent(telemetryData.EventName, telemetryData.BodyData, telemetryData.CustomTags);
		base.gameObject.SetActive(value: false);
		_ageAppealEmailScreen.ShowAgeAppealEmailScreen(hasChallenge, newAgeToAppeal);
	}

	private async void StartAgeAppealChallengeEmail()
	{
		(bool success, string message) result = await KIDManager.SetAndSendEmail(_emailText.text);
		Debug.Log($"[KID::UI::APPEAL_AGE_EMAIL] Email has been sent, awaiting minimum duration {_minimumDelay}");
		do
		{
			await Task.Yield();
		}
		while (!_hasCompletedSendEmailRequest);
		if (_minimumDelay > 0)
		{
			await Task.Delay(_minimumDelay);
		}
		if (!result.success)
		{
			base.gameObject.SetActive(value: false);
			return;
		}
		Debug.Log("[KID::UI::APPEAL_AGE_EMAIL] Minimum duration passed, result is successful. Proceeding to Success screen");
		base.gameObject.SetActive(value: false);
		_successScreen.ShowSuccessScreenAppeal(_emailText.text);
	}

	private async Task StartAgeAppealEmail()
	{
		if (!(await KIDManager.TryAppealAge(_emailText.text, newAgeToAppeal)))
		{
			base.gameObject.SetActive(value: false);
			_errorScreen.ShowAgeAppealEmailErrorScreen(hasChallenge, newAgeToAppeal, _emailText.text);
		}
		else
		{
			Debug.Log("[KID::UI::APPEAL_AGE_EMAIL] Age appeal succesful for [" + _emailText.text + "]. Proceeding tu Success screen");
			base.gameObject.SetActive(value: false);
			_successScreen.ShowSuccessScreenAppeal(_emailText.text);
		}
	}

	private void NotifyOfEmailResult(bool success)
	{
		if (_successScreen == null)
		{
			Debug.LogError("[KID::AGE_APPEAL_EMAIL] _successScreen has not been set yet and is NULL. Cannot inform of result");
			return;
		}
		_hasCompletedSendEmailRequest = true;
		if (success)
		{
			base.gameObject.SetActive(value: false);
			_successScreen.ShowSuccessScreenAppeal(_emailText.text);
		}
	}

	private void ShowErrorScreen()
	{
		Debug.LogErrorFormat("[KID::UI::Setup] K-ID Confirmation Failed - Failed to send email");
		base.gameObject.SetActive(value: false);
		_errorScreen.ShowAgeAppealEmailErrorScreen(hasChallenge, newAgeToAppeal, _emailText.text);
	}
}
