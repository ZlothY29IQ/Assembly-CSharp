using UnityEngine;
using UnityEngine.Events;

public class GeodeAtmTrigger : MonoBehaviour
{
	[SerializeField]
	private UnityEvent OnTrigger;

	private void OnTriggerEnter(Collider other)
	{
		if (OnTrigger != null)
		{
			OnTrigger.Invoke();
		}
	}
}
