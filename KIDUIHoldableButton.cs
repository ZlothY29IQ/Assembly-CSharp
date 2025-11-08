using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class KIDUIHoldableButton : MonoBehaviour, IPointerDownHandler, IEventSystemHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
	[Serializable]
	public class ButtonHoldCompleteEvent : UnityEvent
	{
	}

	[Serializable]
	public class ButtonHoldStartEvent : UnityEvent
	{
	}

	[Serializable]
	public class ButtonHoldReleaseEvent : UnityEvent
	{
	}

	public KIDUIButton _button;

	[SerializeField]
	private float _holdDuration;

	[SerializeField]
	private Image _holdProgressFill;

	[Header("Steam Settings")]
	[SerializeField]
	private UXSettings _cbUXSettings;

	[SerializeField]
	private ButtonHoldCompleteEvent m_OnHoldComplete = new ButtonHoldCompleteEvent();

	[SerializeField]
	private ButtonHoldStartEvent m_OnHoldStart = new ButtonHoldStartEvent();

	[SerializeField]
	private ButtonHoldReleaseEvent m_OnHoldRelease = new ButtonHoldReleaseEvent();

	private bool _isHoldingButton;

	private float _elapsedTime;

	private bool inside;

	private bool _isHoldingMouse;

	private static bool _triggeredThisFrame = false;

	private static bool _canTrigger = true;

	public ButtonHoldCompleteEvent onHoldComplete
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

	public float HoldPercentage => _elapsedTime / _holdDuration;

	private void OnEnable()
	{
		_holdProgressFill.rectTransform.localScale = new Vector3(0f, 1f, 1f);
		if ((bool)ControllerBehaviour.Instance)
		{
			ControllerBehaviour.Instance.OnAction += PostUpdate;
		}
	}

	private void Update()
	{
		ManageButtonInteraction();
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		_isHoldingMouse = true;
		ToggleHoldingButton(isPointerDown: true);
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		_isHoldingMouse = false;
		ManageButtonInteraction(isPointerUp: true);
		ToggleHoldingButton(isPointerDown: false);
	}

	private void ToggleHoldingButton(bool isPointerDown)
	{
		_isHoldingButton = isPointerDown && _button.interactable;
		_holdProgressFill.rectTransform.localScale = new Vector3(0f, 1f, 1f);
		if (isPointerDown)
		{
			_elapsedTime = 0f;
			m_OnHoldStart?.Invoke();
			KIDAudioManager.Instance.StartButtonHeldSound();
		}
		else
		{
			m_OnHoldRelease?.Invoke();
			KIDAudioManager.Instance.StopButtonHeldSound();
		}
	}

	private void ManageButtonInteraction(bool isPointerUp = false)
	{
		if (!_isHoldingButton || isPointerUp)
		{
			return;
		}
		if (_holdDuration <= 0f)
		{
			HoldComplete();
			return;
		}
		_elapsedTime += Time.deltaTime;
		bool num = _elapsedTime > _holdDuration;
		float num2 = _elapsedTime / _holdDuration;
		_holdProgressFill.rectTransform.localScale = new Vector3(num2, 1f, 1f);
		HandRayController.Instance.PulseActiveHandray(num2, 0.1f);
		if (num)
		{
			HoldComplete();
		}
	}

	private void HoldComplete()
	{
		ToggleHoldingButton(isPointerDown: false);
		m_OnHoldComplete?.Invoke();
		Debug.Log("[HOLD_BUTTON " + base.name + " ]: Hold Complete");
		ResetButton();
	}

	private void ResetButton()
	{
		_elapsedTime = 0f;
		inside = false;
		_triggeredThisFrame = false;
		_button.ResetButton();
	}

	protected void Awake()
	{
		if (!(_button != null))
		{
			_button = GetComponentInChildren<KIDUIButton>();
			if (_button == null)
			{
				Debug.LogError("[KID::UI_BUTTON] Could not find [KIDUIButton] in children, trying to create a new one.");
			}
		}
	}

	private void PostUpdate()
	{
		if (!_canTrigger)
		{
			_canTrigger = !ControllerBehaviour.Instance.TriggerDown;
		}
		if (!_button.interactable || !_canTrigger || !ControllerBehaviour.Instance)
		{
			return;
		}
		if (ControllerBehaviour.Instance.TriggerDown && inside)
		{
			if (!_isHoldingButton)
			{
				string text = "[" + base.transform.parent.parent.parent.name + "." + base.transform.parent.parent.name + "." + base.transform.parent.name + "." + base.transform.name + "]";
				Debug.Log("[KID::UIBUTTON::DEBUG] " + text + " - STEAM - OnClick is pressed. Time: [" + Time.time + "]", this);
				ToggleHoldingButton(isPointerDown: true);
				_triggeredThisFrame = true;
				_canTrigger = false;
			}
		}
		else if (_isHoldingButton && !_isHoldingMouse)
		{
			ToggleHoldingButton(isPointerDown: false);
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

	public void OnPointerEnter(PointerEventData eventData)
	{
		inside = true;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		inside = false;
	}

	protected void OnDisable()
	{
		if ((bool)ControllerBehaviour.Instance)
		{
			ControllerBehaviour.Instance.OnAction -= PostUpdate;
		}
		inside = false;
	}
}
