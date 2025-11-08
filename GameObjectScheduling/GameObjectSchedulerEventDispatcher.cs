using UnityEngine;
using UnityEngine.Events;

namespace GameObjectScheduling;

public class GameObjectSchedulerEventDispatcher : MonoBehaviour
{
	[SerializeField]
	private UnityEvent onScheduledActivation;

	[SerializeField]
	private UnityEvent onScheduledDeactivation;

	public UnityEvent OnScheduledActivation => onScheduledActivation;

	public UnityEvent OnScheduledDeactivation => onScheduledDeactivation;
}
