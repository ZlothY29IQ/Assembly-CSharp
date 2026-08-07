using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace GorillaNetworking.ScheduledEvents;

public class ScheduledEventPhaseListener : MonoBehaviour
{
	[SerializeField]
	private UnityEvent<float> _onBefore;

	[SerializeField]
	private UnityEvent<float> _onDuring;

	[SerializeField]
	private UnityEvent<float> _onAfter;

	[SerializeField]
	private UnityEvent<float> _onNoEvent;

	private void OnEnable()
	{
		if (ScheduledEventManager.Instance == null)
		{
			StartCoroutine(OnEnableDefered());
		}
		else
		{
			ScheduledEventManager.Instance.OnPhaseChanged += OnPhaseChange;
		}
	}

	private IEnumerator OnEnableDefered()
	{
		while (ScheduledEventManager.Instance == null)
		{
			yield return null;
		}
		OnEnable();
	}

	private void OnPhaseChange(ScheduledEventPhase phase)
	{
		switch (phase)
		{
		case ScheduledEventPhase.Before:
			_onBefore?.Invoke((float)ScheduledEventManager.Instance.SecondsUntilEventStart);
			break;
		case ScheduledEventPhase.During:
			_onDuring?.Invoke((float)ScheduledEventManager.Instance.SecondsUntilEventStart);
			break;
		case ScheduledEventPhase.After:
			_onAfter?.Invoke((float)ScheduledEventManager.Instance.SecondsUntilEventStart);
			break;
		case ScheduledEventPhase.NoEvent:
			_onNoEvent?.Invoke((float)ScheduledEventManager.Instance.SecondsUntilEventStart);
			break;
		}
	}

	private void OnDisable()
	{
		ScheduledEventManager.Instance.OnPhaseChanged -= OnPhaseChange;
	}

	public void DebugStartCountdown()
	{
		ScheduledEventManager.Instance.DebugStartCountdown();
	}
}
