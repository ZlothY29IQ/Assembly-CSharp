using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

public class KIDUI_SetupScreen : MonoBehaviour
{
	[SerializeField]
	private TMP_InputField _emailInputField;

	[SerializeField]
	private KIDUIButton _confirmButton;

	[SerializeField]
	private KIDUI_ConfirmScreen _confirmScreen;

	[SerializeField]
	private KIDUI_MainScreen _mainScreen;

	[SerializeField]
	private TMP_Text _riftKeyboardMessage;

	private string _emailStr = string.Empty;

	private TouchScreenKeyboard _keyboard;

	private void Awake()
	{
		if (_emailInputField == null)
		{
			Debug.LogErrorFormat("[KID::UI::Setup] Email Input Field is NULL");
		}
		else if (_confirmScreen == null)
		{
			Debug.LogErrorFormat("[KID::UI::Setup] Confirm Screen is NULL");
		}
		else if (_mainScreen == null)
		{
			Debug.LogErrorFormat("[KID::UI::Setup] Main Screen is NULL");
		}
	}

	private void OnEnable()
	{
		string text = PlayerPrefs.GetString(KIDManager.GetEmailForUserPlayerPrefRef, "");
		_emailInputField.text = text;
		_confirmButton.ResetButton();
		OnInputChanged(text);
	}

	private void OnDisable()
	{
		if (_keyboard != null)
		{
			_keyboard.active = false;
		}
	}

	public void OnStartSetup()
	{
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
			BodyData = new Dictionary<string, string> { { "screen", "enter_email" } }
		};
		GorillaTelemetry.EnqueueTelemetryEvent(telemetryData.EventName, telemetryData.BodyData, telemetryData.CustomTags);
	}

	public void OnInputSelected()
	{
		Debug.LogFormat("[KID::UI::SETUP] Email Input Selected!");
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

	public void OnSubmitEmailPressed()
	{
		PlayerPrefs.SetString(KIDManager.GetEmailForUserPlayerPrefRef, _emailInputField.text);
		PlayerPrefs.Save();
		base.gameObject.SetActive(value: false);
		_confirmScreen.OnEmailSubmitted(_emailInputField.text);
	}

	public void OnBackPressed()
	{
		PlayerPrefs.SetString(KIDManager.GetEmailForUserPlayerPrefRef, _emailInputField.text);
		PlayerPrefs.Save();
		base.gameObject.SetActive(value: false);
		_mainScreen.ShowMainScreen(EMainScreenStatus.Previous);
	}
}
