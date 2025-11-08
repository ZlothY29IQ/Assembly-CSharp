using GorillaTag.Audio;
using UnityEngine;
using UnityEngine.Events;

public class SITouchscreenButton : MonoBehaviour, IClickable
{
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
		None
	}

	public SITouchscreenButtonType buttonType;

	public int data;

	[SerializeField]
	private AudioClip _pressSound;

	[SerializeField]
	private float _pressSoundVolume = 0.1f;

	public UnityEvent<SITouchscreenButtonType, int, int> buttonPressed;

	private SIScreenRegion _screenRegion;

	private const float DEBOUNCE_TIME = 0.2f;

	private float _enableTime;

	private bool IsUsable
	{
		get
		{
			if (!_screenRegion)
			{
				return Time.time - _enableTime >= 0.2f;
			}
			return !_screenRegion.HasPressedButton;
		}
	}

	private void Awake()
	{
		ITouchScreenStation componentInParent = GetComponentInParent<ITouchScreenStation>();
		if (componentInParent != null)
		{
			_screenRegion = componentInParent.ScreenRegion;
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
		if (IsUsable)
		{
			if ((bool)_screenRegion)
			{
				_screenRegion.RegisterButtonPress();
			}
			buttonPressed.Invoke(buttonType, data, NetworkSystem.Instance.LocalPlayer.ActorNumber);
			if (_pressSound != null)
			{
				GTAudioOneShot.Play(_pressSound, base.transform.position, _pressSoundVolume);
			}
		}
	}

	public void Click(bool leftHand = false)
	{
		PressButton();
	}
}
