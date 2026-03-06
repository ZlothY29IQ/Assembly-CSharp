using System;
using GorillaTag.Audio;
using UnityEngine;
using UnityEngine.Events;

public class SITouchscreenButton : MonoBehaviour, IClickable
{
	public enum ButtonMode
	{
		Normal,
		Toggle
	}

	public enum SITouchscreenButtonType
	{
		Back,
		Next,
		Exit,
		Help,
		Select,
		Dispense,
		Research,
		Collect,
		Debug,
		PageSelect,
		Purchase,
		Confirm,
		Cancel,
		OverrideFailure,
		None,
		Subscribe
	}

	public ButtonMode buttonMode;

	public SITouchscreenButtonType buttonType;

	public int data;

	[SerializeField]
	private AudioClip _pressSound;

	[SerializeField]
	private float _pressSoundVolume = 0.1f;

	[SerializeField]
	private bool _isToggledOn;

	[SerializeField]
	private bool _startToggledOn;

	public UnityEvent<SITouchscreenButtonType, int, int> buttonPressed;

	public UnityEvent<SITouchscreenButtonType, int, int, bool> buttonToggled;

	private SIScreenRegion _screenRegion;

	private const float DEBOUNCE_TIME = 0.2f;

	private float _enableTime;

	[NonSerialized]
	public bool isUsable = true;

	private bool IsReady
	{
		get
		{
			bool flag = Time.time - _enableTime >= 0.2f;
			if ((bool)_screenRegion)
			{
				flag = flag && !_screenRegion.HasPressedButton;
			}
			return flag;
		}
	}

	public bool IsToggledOn => _isToggledOn;

	private void Awake()
	{
		ITouchScreenStation componentInParent = GetComponentInParent<ITouchScreenStation>();
		if (componentInParent != null)
		{
			_screenRegion = componentInParent.ScreenRegion;
		}
		if (buttonMode == ButtonMode.Toggle)
		{
			_isToggledOn = _startToggledOn;
		}
	}

	private void OnEnable()
	{
		_enableTime = Time.time;
	}

	private void OnTriggerEnter(Collider other)
	{
		GorillaTriggerColliderHandIndicator componentInParent = other.GetComponentInParent<GorillaTriggerColliderHandIndicator>();
		if ((bool)componentInParent)
		{
			PressButton();
			GorillaTagger.Instance.StartVibration(componentInParent.isLeftHand, GorillaTagger.Instance.tapHapticStrength / 2f, GorillaTagger.Instance.tapHapticDuration);
		}
	}

	public void PressButton()
	{
		if (IsReady && isUsable)
		{
			if ((bool)_screenRegion)
			{
				_screenRegion.RegisterButtonPress();
			}
			if (buttonMode == ButtonMode.Normal)
			{
				buttonPressed.Invoke(buttonType, data, NetworkSystem.Instance.LocalPlayer.ActorNumber);
			}
			else if (buttonMode == ButtonMode.Toggle)
			{
				bool arg = !_isToggledOn;
				buttonToggled.Invoke(buttonType, data, NetworkSystem.Instance.LocalPlayer.ActorNumber, arg);
			}
			if (_pressSound != null)
			{
				GTAudioOneShot.Play(_pressSound, base.transform.position, _pressSoundVolume);
			}
		}
	}

	public void SetToggleState(bool state, bool invokeEvent = false)
	{
		if (buttonMode == ButtonMode.Toggle)
		{
			bool flag = _isToggledOn != state;
			_isToggledOn = state;
			if (invokeEvent && flag)
			{
				buttonToggled.Invoke(buttonType, data, NetworkSystem.Instance.LocalPlayer.ActorNumber, _isToggledOn);
			}
		}
	}

	public void Click(bool leftHand = false)
	{
		PressButton();
	}
}
