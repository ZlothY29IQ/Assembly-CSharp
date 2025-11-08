using UnityEngine;
using UnityEngine.Events;

public class LifeCycleEventTrigger : MonoBehaviour
{
	[SerializeField]
	private UnityEvent _onAwake;

	[SerializeField]
	private UnityEvent _onStart;

	[SerializeField]
	private UnityEvent _onEnable;

	[SerializeField]
	private UnityEvent _onDisable;

	[SerializeField]
	private UnityEvent _onDestroy;

	private void Awake()
	{
		_onAwake?.Invoke();
	}

	private void Start()
	{
		_onStart?.Invoke();
	}

	private void OnEnable()
	{
		_onEnable?.Invoke();
	}

	private void OnDisable()
	{
		_onDisable?.Invoke();
	}

	private void OnDestroy()
	{
		_onDestroy?.Invoke();
	}
}
