using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(TransferrableObject))]
public class UnityEventOnGrab : MonoBehaviour
{
	[SerializeField]
	private UnityEvent onGrab;

	[SerializeField]
	private UnityEvent onRelease;

	private void Awake()
	{
		TransferrableObject componentInParent = GetComponentInParent<TransferrableObject>();
		Behaviour[] behavioursEnabledOnlyWhileHeld = componentInParent.behavioursEnabledOnlyWhileHeld;
		Behaviour[] array = new Behaviour[behavioursEnabledOnlyWhileHeld.Length + 1];
		for (int i = 0; i < behavioursEnabledOnlyWhileHeld.Length; i++)
		{
			array[i] = behavioursEnabledOnlyWhileHeld[i];
		}
		array[behavioursEnabledOnlyWhileHeld.Length] = this;
		componentInParent.behavioursEnabledOnlyWhileHeld = array;
	}

	private void OnEnable()
	{
		onGrab?.Invoke();
	}

	private void OnDisable()
	{
		onRelease?.Invoke();
	}
}
