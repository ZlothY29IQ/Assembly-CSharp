using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class AgeSliderWithProgressBar : MonoBehaviourTick
{
	[Serializable]
	public class SliderHeldEvent : UnityEvent<int>
	{
	}

	private const int MIN_AGE = 13;

	[SerializeField]
	private SliderHeldEvent m_OnHoldComplete = new SliderHeldEvent();

	[SerializeField]
	private bool _adjustAge;

	[SerializeField]
	private int _maxAge = 25;

	[SerializeField]
	private TMP_Text _ageValueTxt;

	[Tooltip("Optional game object that should hold the Progress Bar Fill. Disables Hold functionality if null.")]
	[SerializeField]
	private GameObject _progressBarContainer;

	[SerializeField]
	private float holdTime = 2.5f;

	[SerializeField]
	private Image progressBarFill;

	[SerializeField]
	private TMP_Text _messageText;

	[SerializeField]
	private float _stickVibrationStrength = 0.1f;

	[SerializeField]
	private float _stickVibrationDuration = 0.05f;

	[SerializeField]
	private KIDUIButton _confirmButton;

	private bool _ageSlidable = true;

	private bool _incrementButtonsLockingSlider;

	private bool controllerActive;

	[SerializeField]
	private string _lockMessage;

	private string _originalText;

	private int _currentAge;

	private float progress;

	public SliderHeldEvent onHoldComplete
	{
		get
		{
			return m_OnHoldComplete;
		}
		set
		{
			m_OnHoldComplete = value;
		}
	}

	public bool AdjustAge => _adjustAge;

	public bool ControllerActive
	{
		get
		{
			return controllerActive;
		}
		set
		{
			if (value)
			{
				ControllerBehaviour.Instance.OnAction += PostUpdate;
			}
			else
			{
				ControllerBehaviour.Instance.OnAction -= PostUpdate;
			}
			controllerActive = value;
		}
	}

	public string LockMessage
	{
		get
		{
			return _lockMessage;
		}
		set
		{
			_lockMessage = value;
		}
	}

	public int CurrentAge => _currentAge;

	private void Awake()
	{
		if ((bool)_messageText)
		{
			_originalText = _messageText.text;
		}
	}

	public void SetOriginalText(string text)
	{
		_originalText = text;
	}

	private new void OnEnable()
	{
		base.OnEnable();
		if (_progressBarContainer != null && progressBarFill != null)
		{
			progressBarFill.rectTransform.localScale = new Vector3(0f, 1f, 1f);
		}
		if ((bool)_ageValueTxt)
		{
			_ageValueTxt.text = ((_currentAge > 0) ? _currentAge.ToString() : "?");
		}
	}

	public override void Tick()
	{
		if (!_progressBarContainer || !ControllerActive)
		{
			return;
		}
		if (!_lockMessage.IsNullOrEmpty())
		{
			progress = 0f;
			if ((bool)_messageText)
			{
				_messageText.text = LockMessage;
			}
		}
		else
		{
			if ((bool)_messageText)
			{
				_messageText.text = _originalText;
			}
			if ((double)progress == 1.0)
			{
				m_OnHoldComplete.Invoke(_currentAge);
				progress = 0f;
			}
			if (ControllerBehaviour.Instance.ButtonDown && _progressBarContainer != null && (_currentAge > 0 || !AdjustAge))
			{
				progress += Time.deltaTime / holdTime;
				progress = Mathf.Clamp01(progress);
			}
			else
			{
				progress = 0f;
			}
		}
		if (_progressBarContainer != null)
		{
			progressBarFill.rectTransform.localScale = new Vector3(progress, 1f, 1f);
		}
	}

	private void PostUpdate()
	{
		if (ControllerActive && (bool)_ageValueTxt && _ageSlidable && !_incrementButtonsLockingSlider)
		{
			if (ControllerBehaviour.Instance.IsLeftStick)
			{
				_currentAge = Mathf.Clamp(_currentAge - 1, 0, _maxAge);
				if (_currentAge > 0 && _currentAge < _maxAge)
				{
					HandRayController.Instance.PulseActiveHandray(_stickVibrationStrength, _stickVibrationDuration);
				}
			}
			if (ControllerBehaviour.Instance.IsRightStick)
			{
				_currentAge = Mathf.Clamp(_currentAge + 1, 0, _maxAge);
				if (_currentAge > 0 && _currentAge < _maxAge)
				{
					HandRayController.Instance.PulseActiveHandray(_stickVibrationStrength, _stickVibrationDuration);
				}
			}
		}
		if ((bool)_ageValueTxt)
		{
			_ageValueTxt.text = GetAgeString();
			if (_progressBarContainer != null)
			{
				_progressBarContainer.SetActive(_currentAge > 0);
			}
		}
	}

	public void EnableEditing()
	{
		_ageSlidable = true;
	}

	public void DisableEditing()
	{
		_ageSlidable = false;
	}

	public string GetAgeString()
	{
		if ((bool)_confirmButton)
		{
			_confirmButton.interactable = true;
		}
		if (_currentAge == 0)
		{
			if ((bool)_confirmButton)
			{
				_confirmButton.interactable = false;
			}
			return "?";
		}
		if (_currentAge == _maxAge)
		{
			return _maxAge + "+";
		}
		return _currentAge.ToString();
	}

	public void ForceAddAge(int number)
	{
		_incrementButtonsLockingSlider = true;
		_currentAge = Math.Min(_currentAge + number, _maxAge);
	}

	public void ForceSubtractAge(int number)
	{
		_incrementButtonsLockingSlider = true;
		_currentAge = Math.Max(_currentAge - number, 1);
	}
}
