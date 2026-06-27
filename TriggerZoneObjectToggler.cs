using UnityEngine;
using UnityEngine.Events;

public class TriggerZoneObjectToggler : MonoBehaviour
{
	public string TriggerName = "Trigger";

	public GameObject ToggleObject;

	public UnityEvent OnEnter;

	public UnityEvent OnExit;

	private bool _inTriggerZone;

	private void Awake()
	{
		ToggleObject.SetActive(value: false);
	}

	private void OnDisable()
	{
		if (!ApplicationQuittingState.IsQuitting)
		{
			HandleExit();
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (IsMatchingTrigger(other))
		{
			HandleEnter();
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (IsMatchingTrigger(other))
		{
			HandleExit();
		}
	}

	private void HandleEnter()
	{
		if (!_inTriggerZone)
		{
			_inTriggerZone = true;
			ToggleObject.SetActive(value: true);
			OnEnter?.Invoke();
		}
	}

	private void HandleExit()
	{
		if (_inTriggerZone)
		{
			_inTriggerZone = false;
			ToggleObject.SetActive(value: false);
			OnExit?.Invoke();
		}
	}

	private bool IsMatchingTrigger(Collider other)
	{
		NamedTriggerZone component = other.GetComponent<NamedTriggerZone>();
		if ((object)component != null)
		{
			return component.TriggerName == TriggerName;
		}
		return false;
	}
}
