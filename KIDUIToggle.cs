using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class KIDUIToggle : Slider
{
	[Header("Toggle Setup")]
	[SerializeField]
	[Range(0f, 1f)]
	private float _initValue;

	[SerializeField]
	private Image _borderImg;

	[SerializeField]
	private float _borderHeightRatio = 2f;

	[SerializeField]
	private Image _fillImg;

	[SerializeField]
	private Image _fillInactiveImg;

	[SerializeField]
	private Image _handleImg;

	[SerializeField]
	private Image _lockIcon;

	[SerializeField]
	private Image _unlockIcon;

	[SerializeField]
	private Image _handleLockIcon;

	[SerializeField]
	private Image _handleUnlockIcon;

	[SerializeField]
	private Color _lockActiveColor;

	[SerializeField]
	private Color _lockInactiveColor;

	[SerializeField]
	private RectTransform _borderImgRef;

	[Header("Steam Settings")]
	[SerializeField]
	private UXSettings _cbUXSettings;

	[Header("Animation")]
	[SerializeField]
	private float _animationDuration = 0.15f;

	[SerializeField]
	private AnimationCurve _toggleEase = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	[Header("Fill Colors")]
	[SerializeField]
	private ColorBlock _fillColors;

	[Header("Border Colors")]
	[SerializeField]
	private ColorBlock _borderColors;

	[Header("Borders")]
	[SerializeField]
	private float _normalBorderSize = 1f;

	[SerializeField]
	private float _disabledBorderSize = 1f;

	[SerializeField]
	private float _highlightedBorderSize = 1f;

	[SerializeField]
	private float _pressedBorderSize = 1f;

	[SerializeField]
	private float _selectedBorderSize = 1f;

	[Header("Handle Colors")]
	[SerializeField]
	private ColorBlock _handleColors;

	[Header("Events")]
	[SerializeField]
	private UnityEvent _onToggleOn;

	[SerializeField]
	private UnityEvent _onToggleOff;

	[SerializeField]
	private UnityEvent _onToggleChanged;

	private bool _previousValue;

	private bool _isDisabled;

	private Coroutine _animationCoroutine;

	private bool inside;

	private static bool _triggeredThisFrame = false;

	private static bool _canTrigger = true;

	public bool CurrentValue { get; private set; }

	public bool IsOn => CurrentValue;

	protected override void Awake()
	{
		base.Awake();
		SetupToggleComponent();
	}

	protected override void Start()
	{
		base.Start();
		base.interactable = false;
	}

	protected override void OnEnable()
	{
		base.OnEnable();
		base.interactable = false;
		if ((bool)ControllerBehaviour.Instance)
		{
			ControllerBehaviour.Instance.OnAction += PostUpdate;
		}
	}

	public override void OnPointerDown(PointerEventData eventData)
	{
		base.OnPointerDown(eventData);
		Toggle();
	}

	public override void OnPointerEnter(PointerEventData pointerEventData)
	{
		SetHighlighted();
		inside = true;
	}

	public override void OnPointerExit(PointerEventData pointerEventData)
	{
		SetNormal();
		inside = false;
	}

	protected virtual void SetupToggleComponent()
	{
		SetupSliderComponent();
		base.handleRect.anchorMin = new Vector2(0f, 0.5f);
		base.handleRect.anchorMax = new Vector3(0f, 0.5f);
		base.handleRect.pivot = new Vector2(0f, 0.5f);
		base.handleRect.sizeDelta = new Vector2(base.handleRect.sizeDelta.x, base.handleRect.sizeDelta.x);
	}

	protected virtual void SetupSliderComponent()
	{
		base.interactable = false;
		ColorBlock colorBlock = base.colors;
		colorBlock.disabledColor = Color.white;
		SetColors();
		base.transition = Transition.None;
	}

	public void RegisterOnChangeEvent(Action onChange)
	{
		_onToggleChanged.AddListener(delegate
		{
			onChange?.Invoke();
		});
	}

	public void UnregisterOnChangeEvent(Action onChange)
	{
		_onToggleChanged.RemoveListener(delegate
		{
			onChange?.Invoke();
		});
	}

	public void RegisterToggleOnEvent(Action onToggle)
	{
		_onToggleOn.AddListener(delegate
		{
			onToggle?.Invoke();
		});
	}

	public void UnregisterToggleOnEvent(Action onToggle)
	{
		_onToggleOn.RemoveListener(delegate
		{
			onToggle?.Invoke();
		});
	}

	public void RegisterToggleOffEvent(Action onToggle)
	{
		_onToggleOff.AddListener(delegate
		{
			onToggle?.Invoke();
		});
	}

	public void UnregisterToggleOffEvent(Action onToggle)
	{
		_onToggleOff.RemoveListener(delegate
		{
			onToggle?.Invoke();
		});
	}

	private void SetColors()
	{
		base.colors = _fillColors;
	}

	private void Toggle()
	{
		if (!_isDisabled)
		{
			SetStateAndStartAnimation(!CurrentValue);
		}
	}

	public void SetValue(bool newValue)
	{
		if (newValue != CurrentValue)
		{
			SetStateAndStartAnimation(newValue);
		}
	}

	private void SetStateAndStartAnimation(bool state, bool skipAnim = false)
	{
		if (CurrentValue == state)
		{
			Debug.Log("IS SAME STATE, WILL NOT CHANGE");
			return;
		}
		CurrentValue = state;
		_onToggleChanged?.Invoke();
		if (CurrentValue)
		{
			_onToggleOn?.Invoke();
			KIDAudioManager.Instance.PlaySound(KIDAudioManager.KIDSoundType.Success);
		}
		else
		{
			_onToggleOff?.Invoke();
			KIDAudioManager.Instance.PlaySound(KIDAudioManager.KIDSoundType.TurnOffPermission);
		}
		if (_animationCoroutine != null)
		{
			StopCoroutine(_animationCoroutine);
		}
		_handleUnlockIcon.gameObject.SetActive(CurrentValue);
		_handleLockIcon.gameObject.SetActive(!CurrentValue);
		if (_animationDuration == 0f || skipAnim)
		{
			Debug.Log("[KID::UI::SetStateAndStartAnimation] Skipping animation. Setting value to " + (CurrentValue ? "1f" : "0f"));
			value = (CurrentValue ? 1f : 0f);
		}
		else
		{
			_animationCoroutine = StartCoroutine(AnimateSlider());
		}
	}

	private IEnumerator AnimateSlider()
	{
		Debug.Log($"[KID::UI::TOGGLE] Toggle: [{base.name}] is {CurrentValue}");
		float startValue = (CurrentValue ? 0f : 1f);
		float endValue = (CurrentValue ? 1f : 0f);
		Debug.Log($"[KID::UI::TOGGLE] Toggle: [{base.name}] Start: {startValue}, End: {endValue}, Value: {value}");
		float time = 0f;
		while (time < _animationDuration)
		{
			time += Time.deltaTime;
			float t = _toggleEase.Evaluate(time / _animationDuration);
			value = Mathf.Lerp(startValue, endValue, t);
			yield return null;
		}
		value = endValue;
	}

	private void PostUpdate()
	{
		if (inside && (bool)ControllerBehaviour.Instance)
		{
			if (ControllerBehaviour.Instance.TriggerDown && _canTrigger)
			{
				string text = "[" + base.transform.parent.parent.parent.name + "." + base.transform.parent.parent.name + "." + base.transform.parent.name + "." + base.transform.name + "]";
				Debug.Log("[KID::UIBUTTON::DEBUG] " + text + " - STEAM - OnClick is pressed. Time: [" + Time.time + "]", this);
				Toggle();
				_triggeredThisFrame = true;
				_canTrigger = false;
			}
			else if (!ControllerBehaviour.Instance.TriggerDown)
			{
				_canTrigger = true;
			}
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

	protected new void OnDisable()
	{
		if ((bool)ControllerBehaviour.Instance)
		{
			ControllerBehaviour.Instance.OnAction -= PostUpdate;
		}
		inside = false;
	}

	private void SetDisabled(bool isLockedButEnabled)
	{
		SetSwitchColors(_borderColors.disabledColor, _handleColors.disabledColor, _fillColors.disabledColor);
		SetBorderSize(_disabledBorderSize);
		SetBackgroundActive(isActive: false);
	}

	private void SetNormal()
	{
		if (!_isDisabled)
		{
			SetSwitchColors(_borderColors.normalColor, _handleColors.normalColor, _fillColors.normalColor);
			SetBorderSize(_normalBorderSize);
			SetBackgroundActive(isActive: false);
		}
	}

	private void SetSelected()
	{
		if (!_isDisabled)
		{
			SetSwitchColors(_borderColors.selectedColor, _handleColors.selectedColor, _fillColors.selectedColor);
			SetBorderSize(_selectedBorderSize);
			SetBackgroundActive(isActive: true);
		}
	}

	private void SetHighlighted()
	{
		if (!_isDisabled)
		{
			SetSwitchColors(_borderColors.highlightedColor, _handleColors.highlightedColor, _fillColors.highlightedColor);
			SetBorderSize(_highlightedBorderSize);
			SetBackgroundActive(isActive: true);
		}
	}

	private void SetPressed()
	{
		if (!_isDisabled)
		{
			SetSwitchColors(_borderColors.pressedColor, _handleColors.pressedColor, _fillColors.pressedColor);
			SetBorderSize(_pressedBorderSize);
			SetBackgroundActive(isActive: true);
		}
	}

	private void SetSwitchColors(Color borderColor, Color handleColor, Color fillColor)
	{
		_borderImg.color = borderColor;
		_handleImg.color = handleColor;
	}

	private void SetBorderSize(float borderScale)
	{
		_borderImgRef.offsetMin = new Vector2(0f - borderScale, (0f - borderScale) * _borderHeightRatio);
		_borderImgRef.offsetMax = new Vector2(borderScale, borderScale * _borderHeightRatio);
	}

	private void SetBackgroundActive(bool isActive)
	{
		_fillImg.gameObject.SetActive(isActive);
		_fillInactiveImg.gameObject.SetActive(!isActive);
		SetBackgroundLocksActive(isActive);
	}

	private void SetBackgroundLocksActive(bool isActive)
	{
		Color color = (isActive ? _lockActiveColor : _lockInactiveColor);
		_lockIcon.color = color;
		_unlockIcon.color = color;
	}
}
