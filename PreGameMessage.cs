using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class PreGameMessage : MonoBehaviour
{
	[SerializeField]
	private GameObject _uiParent;

	[SerializeField]
	private TMP_Text _messageTitleTxt;

	[SerializeField]
	private TMP_Text _messageBodyTxt;

	[SerializeField]
	private GameObject _confirmButtonRoot;

	[SerializeField]
	private GameObject _multiButtonRoot;

	[SerializeField]
	private TMP_Text _messageConfirmationTxt;

	[SerializeField]
	private TMP_Text _messageAlternativeConfirmationTxt;

	[SerializeField]
	private TMP_Text _messageAlternativeButtonTxt;

	private Action _confirmationAction;

	private Action _alternativeAction;

	private bool _hasCompleted;

	private float progress;

	[SerializeField]
	private float holdTime;

	[SerializeField]
	private LineRenderer progressBar;

	[SerializeField]
	private LineRenderer progressBarL;

	[SerializeField]
	private LineRenderer progressBarR;

	private void OnEnable()
	{
		if ((bool)ControllerBehaviour.Instance)
		{
			ControllerBehaviour.Instance.OnAction += PostUpdate;
		}
	}

	private void OnDisable()
	{
		KIDAudioManager.Instance?.PlaySoundWithDelay(KIDAudioManager.KIDSoundType.PageTransition);
		if ((bool)ControllerBehaviour.Instance)
		{
			ControllerBehaviour.Instance.OnAction -= PostUpdate;
		}
	}

	public void ShowMessage(string messageTitle, string messageBody, string messageConfirmation, Action onConfirmationAction, float bodyFontSize = 0.5f, float buttonHideTimer = 0f)
	{
		_alternativeAction = null;
		_multiButtonRoot.SetActive(value: false);
		_messageTitleTxt.text = messageTitle;
		_messageBodyTxt.text = messageBody;
		_messageConfirmationTxt.text = messageConfirmation;
		_confirmationAction = onConfirmationAction;
		_messageBodyTxt.fontSize = bodyFontSize;
		_hasCompleted = false;
		if (_confirmationAction == null)
		{
			_confirmButtonRoot.SetActive(value: false);
		}
		else if (!string.IsNullOrEmpty(_messageConfirmationTxt.text))
		{
			_confirmButtonRoot.SetActive(value: true);
		}
		PrivateUIRoom.AddUI(_uiParent.transform);
	}

	public void ShowMessage(string messageTitle, string messageBody, string messageConfirmationButton, string messageAlternativeButton, Action onConfirmationAction, Action onAlternativeAction, float bodyFontSize = 0.5f)
	{
		_confirmButtonRoot.SetActive(value: false);
		_messageTitleTxt.text = messageTitle;
		_messageBodyTxt.text = messageBody;
		_messageAlternativeConfirmationTxt.text = messageConfirmationButton;
		_messageAlternativeButtonTxt.text = messageAlternativeButton;
		_confirmationAction = onConfirmationAction;
		_alternativeAction = onAlternativeAction;
		_messageBodyTxt.fontSize = bodyFontSize;
		_hasCompleted = false;
		if (_confirmationAction == null || _alternativeAction == null)
		{
			Debug.LogError("[KID] Trying to show a mesasge with multiple buttons, but one or both callbacks are null");
			_multiButtonRoot.SetActive(value: false);
		}
		else if (!string.IsNullOrEmpty(_messageAlternativeConfirmationTxt.text) && !string.IsNullOrEmpty(_messageAlternativeButtonTxt.text))
		{
			_multiButtonRoot.SetActive(value: true);
		}
		PrivateUIRoom.AddUI(_uiParent.transform);
	}

	public async Task ShowMessageWithAwait(string messageTitle, string messageBody, string messageConfirmation, Action onConfirmationAction, float bodyFontSize = 0.5f, float buttonHideTimer = 0f)
	{
		_alternativeAction = null;
		_multiButtonRoot.SetActive(value: false);
		_messageTitleTxt.text = messageTitle;
		_messageBodyTxt.text = messageBody;
		_messageConfirmationTxt.text = messageConfirmation;
		_confirmationAction = onConfirmationAction;
		_messageBodyTxt.fontSize = bodyFontSize;
		_hasCompleted = false;
		if (_confirmationAction == null)
		{
			_confirmButtonRoot.SetActive(value: false);
		}
		else if (!string.IsNullOrEmpty(_messageConfirmationTxt.text))
		{
			_confirmButtonRoot.SetActive(value: true);
		}
		PrivateUIRoom.AddUI(_uiParent.transform);
		await WaitForCompletion();
	}

	public void UpdateMessage(string newMessageBody, string newConfirmButton)
	{
		_messageBodyTxt.text = newMessageBody;
		_messageConfirmationTxt.text = newConfirmButton;
		if (string.IsNullOrEmpty(_messageConfirmationTxt.text))
		{
			_confirmButtonRoot.SetActive(value: false);
		}
		else if (_confirmationAction != null)
		{
			_confirmButtonRoot.SetActive(value: true);
		}
	}

	public void CloseMessage()
	{
		PrivateUIRoom.RemoveUI(_uiParent.transform);
	}

	private async Task WaitForCompletion()
	{
		do
		{
			await Task.Yield();
		}
		while (!_hasCompleted);
	}

	private void PostUpdate()
	{
		bool isLeftStick = ControllerBehaviour.Instance.IsLeftStick;
		bool isRightStick = ControllerBehaviour.Instance.IsRightStick;
		bool buttonDown = ControllerBehaviour.Instance.ButtonDown;
		if (_multiButtonRoot.activeInHierarchy)
		{
			if (isLeftStick)
			{
				progress += Time.deltaTime / holdTime;
				progressBarL.transform.localScale = new Vector3(0f, 1f, 1f);
				progressBarR.transform.localScale = new Vector3(Mathf.Clamp01(progress), 1f, 1f);
				progressBarR.textureScale = new Vector2(Mathf.Clamp01(progress), -1f);
				if (progress >= 1f)
				{
					OnConfirmedPressed();
				}
			}
			else if (isRightStick)
			{
				progress += Time.deltaTime / holdTime;
				progressBarR.transform.localScale = new Vector3(0f, 1f, 1f);
				progressBarL.transform.localScale = new Vector3(Mathf.Clamp01(progress), 1f, 1f);
				progressBarL.textureScale = new Vector2(Mathf.Clamp01(progress), -1f);
				if (progress >= 1f)
				{
					OnAlternativePressed();
				}
			}
			else
			{
				progress = 0f;
				progressBarR.transform.localScale = new Vector3(0f, 1f, 1f);
				progressBarL.transform.localScale = new Vector3(0f, 1f, 1f);
				progressBarL.textureScale = new Vector2(Mathf.Clamp01(progress), -1f);
			}
		}
		else
		{
			if (!_confirmButtonRoot.activeInHierarchy)
			{
				return;
			}
			if (buttonDown)
			{
				progress += Time.deltaTime / holdTime;
				progressBar.transform.localScale = new Vector3(Mathf.Clamp01(progress), 1f, 1f);
				progressBar.textureScale = new Vector2(Mathf.Clamp01(progress), -1f);
				if (progress >= 1f)
				{
					OnConfirmedPressed();
				}
			}
			else
			{
				progress = 0f;
				progressBar.transform.localScale = new Vector3(Mathf.Clamp01(progress), 1f, 1f);
				progressBar.textureScale = new Vector2(Mathf.Clamp01(progress), -1f);
			}
		}
	}

	private void OnConfirmedPressed()
	{
		PrivateUIRoom.RemoveUI(_uiParent.transform);
		_hasCompleted = true;
		_confirmationAction?.Invoke();
	}

	private void OnAlternativePressed()
	{
		PrivateUIRoom.RemoveUI(_uiParent.transform);
		_hasCompleted = true;
		_alternativeAction?.Invoke();
	}
}
