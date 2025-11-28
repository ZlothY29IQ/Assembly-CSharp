using System;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class WarningScreens : MonoBehaviour
{
	private static WarningScreens _activeReference;

	[SerializeField]
	private MessageBox _messageBox;

	[SerializeField]
	private GameObject _imageContainerAfter;

	[SerializeField]
	private GameObject _imageContainerBefore;

	[SerializeField]
	private TMP_Text _withImageTextBefore;

	[SerializeField]
	private TMP_Text _withImageTextAfter;

	[SerializeField]
	private TMP_Text _noImageText;

	private Action _onLeftButtonPressedAction;

	private Action _onRightButtonPressedAction;

	private static WarningButtonResult _result;

	private static WarningButtonResult _leftButtonResult;

	private static WarningButtonResult _rightButtonResult;

	private static bool _closedMessageBox;

	private void Awake()
	{
		if (_activeReference == null)
		{
			_activeReference = this;
			return;
		}
		Debug.LogError("[WARNINGS] WarningScreens already exists. Destroying this instance.");
		UnityEngine.Object.Destroy(this);
	}

	private async Task<WarningButtonResult> StartWarningScreenInternal(CancellationToken cancellationToken)
	{
		_closedMessageBox = false;
		_result = WarningButtonResult.CloseWarning;
		PlayerAgeGateWarningStatus? playerAgeGateWarningStatus = await WarningsServer.Instance.FetchPlayerData(cancellationToken);
		if (cancellationToken.IsCancellationRequested || !playerAgeGateWarningStatus.HasValue)
		{
			return WarningButtonResult.None;
		}
		PlayerAgeGateWarningStatus value = playerAgeGateWarningStatus.Value;
		if (value.header.IsNullOrEmpty() || value.body.IsNullOrEmpty())
		{
			Debug.Log("[WARNINGS] Not showing warning screen.");
			return value.noWarningResult;
		}
		_messageBox.Header = value.header;
		_messageBox.Body = value.body;
		_messageBox.LeftButton = value.leftButtonText;
		_messageBox.RightButton = value.rightButtonText;
		_leftButtonResult = value.leftButtonResult;
		_rightButtonResult = value.rightButtonResult;
		_onLeftButtonPressedAction = value.onLeftButtonPressedAction;
		_onRightButtonPressedAction = value.onRightButtonPressedAction;
		if ((bool)_imageContainerAfter && (bool)_withImageTextBefore && (bool)_imageContainerBefore && (bool)_withImageTextAfter && (bool)_noImageText)
		{
			_imageContainerAfter.SetActive(value.showImage == EImageVisibility.AfterBody);
			_imageContainerBefore.SetActive(value.showImage == EImageVisibility.BeforeBody);
			_withImageTextBefore.text = value.body;
			_withImageTextBefore.gameObject.SetActive(value.showImage == EImageVisibility.AfterBody);
			_withImageTextAfter.text = value.body;
			_withImageTextAfter.gameObject.SetActive(value.showImage == EImageVisibility.BeforeBody);
			_noImageText.gameObject.SetActive(value.showImage == EImageVisibility.None);
		}
		_messageBox.gameObject.SetActive(value: true);
		GameObject canvas = _messageBox.GetCanvas();
		PrivateUIRoom.AddUI(canvas.transform);
		HandRayController.Instance.EnableHandRays();
		await WaitForResponse(cancellationToken);
		HandRayController.Instance.DisableHandRays();
		PrivateUIRoom.RemoveUI(canvas.transform);
		_messageBox.gameObject.SetActive(value: false);
		return _result;
	}

	private async Task<WarningButtonResult> StartOptInFollowUpScreenInternal(CancellationToken cancellationToken)
	{
		_closedMessageBox = false;
		_result = WarningButtonResult.CloseWarning;
		PlayerAgeGateWarningStatus? playerAgeGateWarningStatus = await WarningsServer.Instance.GetOptInFollowUpMessage(cancellationToken);
		if (cancellationToken.IsCancellationRequested || !playerAgeGateWarningStatus.HasValue)
		{
			return WarningButtonResult.None;
		}
		Debug.Log("[KID::WARNING_SCREEN] Body: " + playerAgeGateWarningStatus.Value.body);
		_messageBox.Header = playerAgeGateWarningStatus.Value.header;
		_messageBox.Body = playerAgeGateWarningStatus.Value.body;
		_messageBox.LeftButton = playerAgeGateWarningStatus.Value.leftButtonText;
		_messageBox.RightButton = playerAgeGateWarningStatus.Value.rightButtonText;
		_leftButtonResult = playerAgeGateWarningStatus.Value.leftButtonResult;
		_rightButtonResult = playerAgeGateWarningStatus.Value.rightButtonResult;
		_onLeftButtonPressedAction = playerAgeGateWarningStatus.Value.onLeftButtonPressedAction;
		_onRightButtonPressedAction = playerAgeGateWarningStatus.Value.onRightButtonPressedAction;
		if ((bool)_imageContainerAfter && (bool)_withImageTextBefore && (bool)_imageContainerBefore && (bool)_withImageTextAfter && (bool)_noImageText)
		{
			_imageContainerAfter.SetActive(playerAgeGateWarningStatus.Value.showImage == EImageVisibility.AfterBody);
			_imageContainerBefore.SetActive(playerAgeGateWarningStatus.Value.showImage == EImageVisibility.BeforeBody);
			_withImageTextBefore.text = playerAgeGateWarningStatus.Value.body;
			_withImageTextBefore.gameObject.SetActive(playerAgeGateWarningStatus.Value.showImage == EImageVisibility.AfterBody);
			_withImageTextAfter.text = playerAgeGateWarningStatus.Value.body;
			_withImageTextAfter.gameObject.SetActive(playerAgeGateWarningStatus.Value.showImage == EImageVisibility.BeforeBody);
			_noImageText.gameObject.SetActive(playerAgeGateWarningStatus.Value.showImage == EImageVisibility.None);
		}
		_messageBox.gameObject.SetActive(value: true);
		GameObject canvas = _messageBox.GetCanvas();
		PrivateUIRoom.AddUI(canvas.transform);
		HandRayController.Instance.EnableHandRays();
		await WaitForResponse(cancellationToken);
		HandRayController.Instance.DisableHandRays();
		PrivateUIRoom.RemoveUI(canvas.transform);
		_messageBox.gameObject.SetActive(value: false);
		return _result;
	}

	public static async Task<WarningButtonResult> StartWarningScreen(CancellationToken cancellationToken)
	{
		return await _activeReference.StartWarningScreenInternal(cancellationToken);
	}

	public static async Task<WarningButtonResult> StartOptInFollowUpScreen(CancellationToken cancellationToken)
	{
		return await _activeReference.StartOptInFollowUpScreenInternal(cancellationToken);
	}

	private static async Task WaitForResponse(CancellationToken cancellationToken)
	{
		while (!_closedMessageBox && !cancellationToken.IsCancellationRequested)
		{
			await Task.Yield();
		}
	}

	public void OnDisable()
	{
		KIDAudioManager.Instance?.PlaySoundWithDelay(KIDAudioManager.KIDSoundType.PageTransition);
	}

	public static void OnLeftButtonClicked()
	{
		_result = _leftButtonResult;
		_closedMessageBox = true;
		_activeReference?._onLeftButtonPressedAction?.Invoke();
	}

	public static void OnRightButtonClicked()
	{
		_result = _rightButtonResult;
		_closedMessageBox = true;
		_activeReference?._onRightButtonPressedAction?.Invoke();
	}
}
