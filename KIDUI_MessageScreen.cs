using TMPro;
using UnityEngine;

public class KIDUI_MessageScreen : MonoBehaviour
{
	[SerializeField]
	private KIDUI_MainScreen _mainScreen;

	[SerializeField]
	private TMP_Text _errorTxt;

	public void Show(string errorMessage)
	{
		base.gameObject.SetActive(value: true);
		if (errorMessage != null && errorMessage.Length > 0)
		{
			_errorTxt.text = errorMessage;
		}
	}

	public void OnClose()
	{
		base.gameObject.SetActive(value: false);
		_mainScreen.ShowMainScreen(EMainScreenStatus.Pending);
	}

	public void OnDisable()
	{
		KIDAudioManager.Instance?.PlaySoundWithDelay(KIDAudioManager.KIDSoundType.PageTransition);
	}
}
