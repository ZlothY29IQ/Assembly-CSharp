using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;
using Valve.VR;

public class KIDUI_InputFieldController : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	[Header("Haptics")]
	[SerializeField]
	private float _highlightedVibrationStrength = 0.1f;

	[SerializeField]
	private float _highlightedVibrationDuration = 0.1f;

	[Header("Steam Settings")]
	[SerializeField]
	private TMP_InputField _inputField;

	[SerializeField]
	private UXSettings _cbUXSettings;

	public bool testMinimal;

	public bool minimalMode;

	private bool inside;

	private bool keyboardShowing;

	private bool _canTrigger = true;

	private string _testStr = string.Empty;

	private string previousStr = string.Empty;

	private StringBuilder _inputStringBuilder = new StringBuilder(1024);

	private string _inputBuffer = "";

	private XRUIInputModule InputModule => EventSystem.current.currentInputModule as XRUIInputModule;

	protected void OnEnable()
	{
		if ((bool)ControllerBehaviour.Instance)
		{
			ControllerBehaviour.Instance.OnAction += PostUpdate;
		}
		SteamVR_Events.System(EVREventType.VREvent_KeyboardClosed).Listen(OnKeyboardClosed);
		SteamVR_Events.System(EVREventType.VREvent_KeyboardCharInput).Listen(OnChar);
	}

	protected void OnDisable()
	{
		if ((bool)ControllerBehaviour.Instance)
		{
			ControllerBehaviour.Instance.OnAction -= PostUpdate;
		}
		SteamVR_Events.System(EVREventType.VREvent_KeyboardClosed).Remove(OnKeyboardClosed);
		SteamVR_Events.System(EVREventType.VREvent_KeyboardCharInput).Remove(OnChar);
	}

	private void Update()
	{
		if (keyboardShowing)
		{
			SteamVR.instance.overlay.GetKeyboardText(_inputStringBuilder, 1024u);
			Debug.Log("[KID::INPUTFIELD_CONTROLLER] String BUilder Says: [" + _inputStringBuilder.ToString() + "]");
			_inputField.text = _inputBuffer;
			_inputField.stringPosition = _inputBuffer.Length;
		}
	}

	private void PostUpdate()
	{
		if (_inputField.interactable && inside && (bool)ControllerBehaviour.Instance && ControllerBehaviour.Instance.TriggerDown)
		{
			string text = "[" + base.transform.parent.parent.parent.name + "." + base.transform.parent.parent.name + "." + base.transform.parent.name + "." + base.transform.name + "]";
			Debug.Log("[KID::UIBUTTON::DEBUG] " + text + " - STEAM - OnClick is pressed. Time: [" + Time.time + "]", this);
			OnClickedInputField();
		}
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		inside = true;
		if (_inputField.IsInteractable() && _inputField.IsActive())
		{
			XRRayInteractor xRRayInteractor = InputModule.GetInteractor(eventData.pointerId) as XRRayInteractor;
			if ((bool)xRRayInteractor)
			{
				xRRayInteractor.xrController.SendHapticImpulse(_highlightedVibrationStrength, _highlightedVibrationDuration);
			}
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		inside = false;
	}

	private void OnClickedInputField(string _ = "")
	{
		if (!keyboardShowing)
		{
			Debug.Log("[KID::INPUT_FIELD_CONTROLLER] Selecting and Activating Input Field");
			EVROverlayError eVROverlayError = OpenVR.Overlay.ShowKeyboard(0, 0, 1u, "Enter Email", 1024u, _inputField.text ?? "", 0uL);
			if (eVROverlayError != EVROverlayError.None)
			{
				Debug.LogError("[KID::INPUT_FIELD_CONTROLLER] Failed to open keyboard. Resulted with error: [" + eVROverlayError.ToString() + "]");
				return;
			}
			_inputBuffer = _inputField.text ?? "";
			keyboardShowing = true;
			HandRayController.Instance.DisableHandRays();
		}
	}

	private void OnChar(VREvent_t ev)
	{
		if (keyboardShowing)
		{
			char c = ev.data.keyboard.cNewInput[0];
			if (c == '\b')
			{
				_inputBuffer = _inputBuffer.Remove(_inputBuffer.Length - 1, 1);
			}
			else if (!IsIllegalChar(c))
			{
				_inputBuffer += c;
			}
		}
	}

	private void OnKeyboardClosed(VREvent_t ev)
	{
		Debug.Log("[KID::INPUTFIELD_CONTROLLER] Trying to close Keyboard");
		if (keyboardShowing)
		{
			Debug.Log("[KID::INPUTFIELD_CONTROLLER] Closing Keyboard");
			OpenVR.Overlay.HideKeyboard();
			_inputField.text = _inputBuffer;
			_inputField.DeactivateInputField();
			HandRayController.Instance.EnableHandRays();
			keyboardShowing = false;
		}
	}

	private bool IsIllegalChar(char c)
	{
		if (c == '\t' || c == '\n')
		{
			return true;
		}
		return false;
	}
}
