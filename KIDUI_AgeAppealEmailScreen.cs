using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

public class KIDUI_AgeAppealEmailScreen : MonoBehaviour
{
	[SerializeField]
	private KIDUIButton _confirmButton;

	[SerializeField]
	private KIDUI_AgeAppealEmailConfirmation _confirmationScreen;

	[SerializeField]
	private TMP_Text _enterEmailText;

	[SerializeField]
	private TMP_InputField _emailText;

	[SerializeField]
	private GameObject _parentPermissionNotice;

	private string PARENT_EMAIL_DESCRIPTION = "Enter your parent or guardian's email address below.";

	private string VERIFY_AGE_EMAIL_DESCRIPTION = "Enter your email address below";

	private bool hasChallenge = true;

	private int newAgeToAppeal;

	public void ShowAgeAppealEmailScreen(bool receivedChallenge, int newAge)
	{
		newAgeToAppeal = newAge;
		base.gameObject.SetActive(value: true);
		hasChallenge = receivedChallenge;
		_enterEmailText.text = (hasChallenge ? PARENT_EMAIL_DESCRIPTION : VERIFY_AGE_EMAIL_DESCRIPTION);
		if ((bool)_parentPermissionNotice)
		{
			_parentPermissionNotice.SetActive(hasChallenge);
		}
		OnInputChanged(_emailText.text);
		TelemetryData telemetryData = new TelemetryData
		{
			EventName = "kid_age_appeal_enter_email",
			CustomTags = new string[3]
			{
				"kid_age_appeal",
				KIDTelemetry.GameVersionCustomTag,
				KIDTelemetry.GameEnvironment
			},
			BodyData = new Dictionary<string, string> { 
			{
				"email_type",
				hasChallenge ? "under_dac" : "over_dac"
			} }
		};
		GorillaTelemetry.EnqueueTelemetryEvent(telemetryData.EventName, telemetryData.BodyData, telemetryData.CustomTags);
	}

	public void OnInputChanged(string newVal)
	{
		bool flag = !string.IsNullOrEmpty(newVal);
		if (flag)
		{
			flag = Regex.IsMatch(newVal, "^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$");
		}
		_confirmButton.interactable = flag;
	}

	public void OnConfirmPressed()
	{
		if (string.IsNullOrEmpty(_emailText.text))
		{
			Debug.LogError("[KID::UI::APPEAL_AGE_EMAIL] Age Appeal Email Text is empty");
			return;
		}
		_confirmationScreen.ShowAgeAppealConfirmationScreen(hasChallenge, newAgeToAppeal, _emailText.text);
		base.gameObject.SetActive(value: false);
	}

	public void OnDisable()
	{
		KIDAudioManager.Instance?.PlaySoundWithDelay(KIDAudioManager.KIDSoundType.PageTransition);
	}
}
