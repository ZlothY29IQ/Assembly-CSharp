using TMPro;
using UnityEngine;

public class KIDUI_ErrorScreen : MonoBehaviour
{
	[SerializeField]
	private TMP_Text _titleTxt;

	[SerializeField]
	private TMP_Text _emailTxt;

	[SerializeField]
	private TMP_Text _errorTxt;

	[SerializeField]
	private KIDUI_MainScreen _mainScreen;

	[SerializeField]
	private KIDUI_SetupScreen _setupScreen;

	public void ShowErrorScreen(string title, string email, string errorMessage)
	{
		_titleTxt.text = title;
		_emailTxt.text = email;
		_errorTxt.text = errorMessage;
		base.gameObject.SetActive(value: true);
	}

	public void OnClose()
	{
		base.gameObject.SetActive(value: false);
		_mainScreen.ShowMainScreen(EMainScreenStatus.None);
	}

	public void OnQuitGame()
	{
		Application.Quit();
	}

	public void OnBack()
	{
		base.gameObject.SetActive(value: false);
		_setupScreen.OnStartSetup();
	}
}
