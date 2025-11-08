using System.Collections;
using UnityEngine;

public class ThrowableBugBeaconActivation : MonoBehaviour
{
	private enum ActivationMode
	{
		CALL,
		DISMISS,
		LOCK
	}

	[SerializeField]
	private float minCallTime = 1f;

	[SerializeField]
	private float maxCallTime = 5f;

	[SerializeField]
	private uint signalCount;

	[SerializeField]
	private ActivationMode mode;

	private ThrowableBugBeacon tbb;

	private void Awake()
	{
		tbb = GetComponent<ThrowableBugBeacon>();
	}

	private void OnEnable()
	{
		StartCoroutine(SendSignals());
	}

	private void OnDisable()
	{
		StopAllCoroutines();
	}

	private IEnumerator SendSignals()
	{
		for (uint count = 0u; signalCount == 0 || count < signalCount; count++)
		{
			yield return new WaitForSeconds(Random.Range(minCallTime, maxCallTime));
			switch (mode)
			{
			case ActivationMode.CALL:
				tbb.Call();
				break;
			case ActivationMode.DISMISS:
				tbb.Dismiss();
				break;
			case ActivationMode.LOCK:
				tbb.Lock();
				break;
			}
		}
	}
}
