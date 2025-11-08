using TMPro;
using UnityEngine;

public class KIDUI_AgeAppealEmailError : MonoBehaviour
{
	[SerializeField]
	private KIDUI_AgeAppealEmailScreen _ageAppealEmailScreen;

	[SerializeField]
	private TMP_Text _emailText;

	private bool hasChallenge;

	private int newAge;

	public void ShowAgeAppealEmailErrorScreen(bool hasChallenge, int newAge, string email)
	{
		this.hasChallenge = hasChallenge;
		this.newAge = newAge;
		_emailText.text = email;
		base.gameObject.SetActive(value: true);
	}

	public void onBackPressed()
	{
		base.gameObject.SetActive(value: false);
		_ageAppealEmailScreen.ShowAgeAppealEmailScreen(hasChallenge, newAge);
	}
}
