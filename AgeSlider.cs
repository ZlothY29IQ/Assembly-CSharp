using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class AgeSlider : MonoBehaviour, IBuildValidation
{
	[Serializable]
	public class SliderHeldEvent : UnityEvent<int>
	{
	}

	private const int MIN_AGE = 13;

	[SerializeField]
	private SliderHeldEvent m_OnHoldComplete = new SliderHeldEvent();

	[SerializeField]
	private int _maxAge = 99;

	[SerializeField]
	private TMP_Text _ageValueTxt;

	[SerializeField]
	private GameObject _confirmButton;

	[SerializeField]
	private float holdTime = 5f;

	[SerializeField]
	private LineRenderer progressBar;

	private int _currentAge;

	private static bool _ageGateActive;

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

	private void OnEnable()
	{
		if ((bool)ControllerBehaviour.Instance)
		{
			ControllerBehaviour.Instance.OnAction += PostUpdate;
		}
	}

	private void OnDisable()
	{
		if ((bool)ControllerBehaviour.Instance)
		{
			ControllerBehaviour.Instance.OnAction -= PostUpdate;
		}
	}

	protected void Update()
	{
		if (!_ageGateActive)
		{
			return;
		}
		if (ControllerBehaviour.Instance.ButtonDown && _confirmButton.activeInHierarchy)
		{
			progress += Time.deltaTime / holdTime;
			progressBar.transform.localScale = new Vector3(Mathf.Clamp01(progress), 1f, 1f);
			progressBar.textureScale = new Vector2(Mathf.Clamp01(progress), -1f);
			if (progress >= 1f)
			{
				m_OnHoldComplete.Invoke(_currentAge);
			}
		}
		else
		{
			progress = 0f;
			progressBar.transform.localScale = new Vector3(Mathf.Clamp01(progress), 1f, 1f);
			progressBar.textureScale = new Vector2(Mathf.Clamp01(progress), -1f);
		}
	}

	private void PostUpdate()
	{
		if (_ageGateActive)
		{
			if (ControllerBehaviour.Instance.IsLeftStick || ControllerBehaviour.Instance.IsUpStick)
			{
				_currentAge = Mathf.Clamp(_currentAge - 1, 0, _maxAge);
				_ageValueTxt.text = ((_currentAge > 0) ? _currentAge.ToString() : "?");
				_confirmButton.SetActive(_currentAge > 0);
			}
			if (ControllerBehaviour.Instance.IsRightStick || ControllerBehaviour.Instance.IsDownStick)
			{
				_currentAge = Mathf.Clamp(_currentAge + 1, 0, _maxAge);
				_ageValueTxt.text = ((_currentAge > 0) ? _currentAge.ToString() : "?");
				_confirmButton.SetActive(_currentAge > 0);
			}
		}
	}

	public static void ToggleAgeGate(bool state)
	{
		_ageGateActive = state;
	}

	public bool BuildValidationCheck()
	{
		if (_confirmButton == null)
		{
			Debug.LogError("[KID] Object [_confirmButton] is NULL. Must be assigned in editor");
			return false;
		}
		return true;
	}
}
