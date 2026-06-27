using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace GorillaTagScripts.Subscription.AlarmClocks;

public sealed class AlarmClock : MonoBehaviour
{
	private const float TouchDebouncePeriod = 0.25f;

	[SerializeField]
	private string _key;

	[SerializeField]
	private GorillaPressableButton _button;

	[SerializeField]
	private GameObject _alarmClockOff;

	[SerializeField]
	private float _onTime = 1f;

	[SerializeField]
	private float _offTime = 0.2f;

	public UnityEvent OnActivate;

	public UnityEvent OnDeactivate;

	private float _lastTouchTime = float.MinValue;

	public string Key => _key;

	public bool Initialized { get; private set; }

	private void OnEnable()
	{
		_button.onPressButton.AddListener(OnButtonPressed);
		OnActivate.AddListener(OnActivateCallback);
		OnDeactivate.AddListener(OnDeactivateCallback);
		StartCoroutine(ActivateCoroutine());
	}

	private IEnumerator ActivateCoroutine()
	{
		while (AlarmClockManager.Instance == null || !AlarmClockManager.Instance.Initialized)
		{
			yield return null;
		}
		if (AlarmClockManager.Instance.ActiveKey == _key)
		{
			OnActivateCallback();
		}
		else
		{
			OnDeactivateCallback();
		}
		Initialized = true;
	}

	private void OnDisable()
	{
		_button.onPressButton.RemoveListener(OnButtonPressed);
		OnActivate.RemoveListener(OnActivateCallback);
		OnDeactivate.RemoveListener(OnDeactivateCallback);
		StopAllCoroutines();
	}

	private void OnButtonPressed()
	{
		if (Initialized && !(Time.time < _lastTouchTime + 0.25f) && SubscriptionManager.IsLocalSubscribed())
		{
			_lastTouchTime = Time.time;
			AlarmClockManager.ToggleAlarmClock(this);
		}
	}

	private void OnActivateCallback()
	{
		_alarmClockOff.SetActive(value: false);
		_button.buttonRenderer.material.color = Color.red;
	}

	private void OnDeactivateCallback()
	{
		_alarmClockOff.SetActive(value: true);
		_button.buttonRenderer.material.color = (SubscriptionManager.IsLocalSubscribed() ? Color.white : new Color(0.33f, 0.33f, 0.33f));
	}
}
