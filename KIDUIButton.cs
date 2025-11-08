using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

public class KIDUIButton : Button, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	[SerializeField]
	private Image _borderImage;

	[SerializeField]
	private RectTransform _fillImageRef;

	[SerializeField]
	private TMP_Text _buttonText;

	[Header("Transition States")]
	[Header("Normal")]
	[SerializeField]
	[ColorUsage(true, false)]
	private Color _normalBorderColor;

	[SerializeField]
	[ColorUsage(true, false)]
	private Color _normalTextColor;

	[SerializeField]
	private float _normalBorderSize;

	[Header("Highlighted")]
	[SerializeField]
	[ColorUsage(true, false)]
	private Color _highlightedBorderColor;

	[SerializeField]
	[ColorUsage(true, false)]
	private Color _highlightedTextColor;

	[SerializeField]
	private float _highlightedBorderSize;

	[SerializeField]
	private float _highlightedVibrationStrength = 0.1f;

	[SerializeField]
	private float _highlightedVibrationDuration = 0.1f;

	[Header("Pressed")]
	[SerializeField]
	[ColorUsage(true, false)]
	private Color _pressedBorderColor;

	[SerializeField]
	[ColorUsage(true, false)]
	private Color _pressedTextColor;

	[SerializeField]
	private float _pressedBorderSize;

	[SerializeField]
	private float _pressedVibrationStrength = 0.5f;

	[SerializeField]
	private float _pressedVibrationDuration = 0.1f;

	[Header("Selected")]
	[SerializeField]
	[ColorUsage(true, false)]
	private Color _selectedBorderColor;

	[SerializeField]
	[ColorUsage(true, false)]
	private Color _selectedTextColor;

	[SerializeField]
	private float _selectedBorderSize;

	[Header("Disabled")]
	[SerializeField]
	[ColorUsage(true, false)]
	private Color _disabledBorderColor;

	[SerializeField]
	[ColorUsage(true, false)]
	private Color _disabledTextColor;

	[SerializeField]
	private float _disabledBorderSize;

	[Header("Audio")]
	[SerializeField]
	private KIDAudioManager.KIDSoundType onClickSound;

	[Header("Icon Swap Settings")]
	[SerializeField]
	private GameObject _normalIcon;

	[SerializeField]
	private GameObject _highlightedIcon;

	[Header("Steam Settings")]
	[SerializeField]
	private UXSettings _cbUXSettings;

	private bool inside;

	private static bool _triggeredThisFrame = false;

	private static bool _canTrigger = true;

	private XRUIInputModule InputModule => EventSystem.current.currentInputModule as XRUIInputModule;

	protected override void OnEnable()
	{
		base.OnEnable();
		if ((bool)ControllerBehaviour.Instance)
		{
			ControllerBehaviour.Instance.OnAction += PostUpdate;
		}
	}

	private void PostUpdate()
	{
		if (!_canTrigger)
		{
			_canTrigger = !ControllerBehaviour.Instance.TriggerDown;
		}
		if (base.interactable && inside && _canTrigger && (bool)ControllerBehaviour.Instance && ControllerBehaviour.Instance.TriggerDown && !_triggeredThisFrame)
		{
			string text = "[" + base.transform.parent.parent.parent.name + "." + base.transform.parent.parent.name + "." + base.transform.parent.name + "." + base.transform.name + "]";
			Debug.Log("[KID::UIBUTTON::DEBUG] " + text + " - STEAM - OnClick is pressed. Time: [" + Time.time + "]", this);
			base.onClick?.Invoke();
			_triggeredThisFrame = true;
			_canTrigger = false;
		}
	}

	private void LateUpdate()
	{
		if (_triggeredThisFrame)
		{
			string text = "[" + base.transform.parent.parent.parent.name + "." + base.transform.parent.parent.name + "." + base.transform.parent.name + "." + base.transform.name + "]";
			Debug.Log("[KID::UIBUTTON::DEBUG] " + text + " - STEAM - OnLateUpdate triggered and Triggered Frame Reset. Time: [" + Time.time + "]", this);
		}
		_triggeredThisFrame = false;
	}

	public override void OnPointerExit(PointerEventData eventData)
	{
		base.OnPointerExit(eventData);
		inside = false;
	}

	public void ResetButton()
	{
		inside = false;
		_triggeredThisFrame = false;
	}

	protected override void OnDisable()
	{
		FixStuckPressedState();
		if ((bool)ControllerBehaviour.Instance)
		{
			ControllerBehaviour.Instance.OnAction -= PostUpdate;
		}
	}

	private void FixStuckPressedState()
	{
		InstantClearState();
		_buttonText.color = (base.interactable ? _normalTextColor : _disabledTextColor);
		inside = false;
		_triggeredThisFrame = false;
	}

	protected override void DoStateTransition(SelectionState state, bool instant)
	{
		base.DoStateTransition(state, instant);
		switch (state)
		{
		default:
			_buttonText.color = _normalTextColor;
			SetIcons(normalEnabled: true, highlightedEnabled: false);
			break;
		case SelectionState.Disabled:
			_buttonText.color = _disabledTextColor;
			SetIcons(normalEnabled: true, highlightedEnabled: false);
			break;
		case SelectionState.Highlighted:
			_buttonText.color = _highlightedTextColor;
			SetIcons(normalEnabled: false, highlightedEnabled: true);
			break;
		case SelectionState.Pressed:
			_buttonText.color = _pressedTextColor;
			SetIcons(normalEnabled: true, highlightedEnabled: false);
			break;
		case SelectionState.Selected:
			_buttonText.color = _selectedTextColor;
			SetIcons(normalEnabled: true, highlightedEnabled: false);
			break;
		}
	}

	private void SetIcons(bool normalEnabled, bool highlightedEnabled)
	{
		if (!(_normalIcon == null) && !(_highlightedIcon == null))
		{
			_normalIcon?.SetActive(normalEnabled);
			_highlightedIcon?.SetActive(highlightedEnabled);
		}
	}

	public override void OnPointerEnter(PointerEventData eventData)
	{
		base.OnPointerEnter(eventData);
		inside = true;
		if (IsInteractable() && IsActive())
		{
			KIDAudioManager.Instance?.PlaySound(KIDAudioManager.KIDSoundType.Hover);
			Debug.Log("[KID::UIBUTTON::KIDAudioManager] Hover played");
			XRRayInteractor xRRayInteractor = InputModule.GetInteractor(eventData.pointerId) as XRRayInteractor;
			if ((bool)xRRayInteractor)
			{
				xRRayInteractor.xrController.SendHapticImpulse(_highlightedVibrationStrength, _highlightedVibrationDuration);
			}
		}
	}

	public override void OnPointerDown(PointerEventData eventData)
	{
		base.OnPointerDown(eventData);
		inside = false;
		if (IsInteractable() && IsActive())
		{
			KIDAudioManager.Instance?.PlaySound(onClickSound);
			XRRayInteractor xRRayInteractor = InputModule.GetInteractor(eventData.pointerId) as XRRayInteractor;
			if ((bool)xRRayInteractor)
			{
				xRRayInteractor.xrController.SendHapticImpulse(_pressedVibrationStrength, _pressedVibrationDuration);
			}
		}
	}

	public void SetText(string text)
	{
		_buttonText.SetText(text);
	}

	public void SetFont(TMP_FontAsset font)
	{
		_buttonText.font = font;
	}

	public string GetText()
	{
		return _buttonText.text;
	}

	public void SetBorderImage(Sprite newImg)
	{
		_borderImage.sprite = newImg;
	}
}
