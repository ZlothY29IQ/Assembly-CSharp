using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class KIDAgeAppeal : MonoBehaviour
{
	[SerializeField]
	private TMP_Text _ageText;

	[SerializeField]
	private KIDUI_AgeAppealEmailScreen _ageAppealEmailScreen;

	[SerializeField]
	private GameObject _inputsContainer;

	[SerializeField]
	private GameObject _monkeLoader;

	private AgeSliderWithProgressBar _ageSlider;

	public void ShowAgeAppealScreen()
	{
		_ageSlider = GetComponentInChildren<AgeSliderWithProgressBar>(includeInactive: true);
		_ageSlider.ControllerActive = true;
		base.gameObject.SetActive(value: true);
		_inputsContainer.SetActive(value: true);
		_monkeLoader.SetActive(value: false);
	}

	public async void OnNewAgeConfirmed()
	{
		_inputsContainer.SetActive(value: false);
		_monkeLoader.SetActive(value: true);
		if (KIDManager.TryGetAgeStatusTypeFromAge(_ageSlider.CurrentAge, out var ageType))
		{
			TelemetryData telemetryData = new TelemetryData
			{
				EventName = "kid_age_appeal_age_gate",
				CustomTags = new string[3]
				{
					"kid_age_appeal",
					KIDTelemetry.GameVersionCustomTag,
					KIDTelemetry.GameEnvironment
				},
				BodyData = new Dictionary<string, string> { 
				{
					"correct_age",
					ageType.ToString()
				} }
			};
			GorillaTelemetry.EnqueueTelemetryEvent(telemetryData.EventName, telemetryData.BodyData, telemetryData.CustomTags);
		}
		AttemptAgeUpdateData attemptAgeUpdateData = await KIDManager.TryAttemptAgeUpdate(_ageSlider.CurrentAge);
		if (attemptAgeUpdateData.status == SessionStatus.PROHIBITED)
		{
			Debug.LogError("[KID::AGE-APPEAL] Age Appeal Status: PROHIBITED");
			base.gameObject.SetActive(value: false);
			KIDUI_AgeAppealController.Instance.StartTooYoungToPlayScreen();
		}
		else
		{
			_ageAppealEmailScreen.ShowAgeAppealEmailScreen(attemptAgeUpdateData.status == SessionStatus.CHALLENGE, _ageSlider.CurrentAge);
			_ageSlider.ControllerActive = false;
			base.gameObject.SetActive(value: false);
		}
	}
}
