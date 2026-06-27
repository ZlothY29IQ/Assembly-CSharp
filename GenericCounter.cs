using UnityEngine;
using UnityEngine.Events;

public class GenericCounter : MonoBehaviour
{
	[SerializeField]
	private int Threshold;

	[SerializeField]
	private UnityEvent whenLessThan;

	[SerializeField]
	private UnityEvent whenEqual;

	[SerializeField]
	private UnityEvent whenGreaterThan;

	private int currentCount;

	public void CountUp()
	{
		currentCount++;
		DoCallbacks();
	}

	public void CountDown()
	{
		currentCount--;
		DoCallbacks();
	}

	private void DoCallbacks()
	{
		if (currentCount < Threshold)
		{
			whenLessThan.Invoke();
		}
		else if (currentCount == Threshold)
		{
			whenEqual.Invoke();
		}
		else
		{
			whenGreaterThan.Invoke();
		}
	}

	public void ResetCounter()
	{
		currentCount = 0;
	}
}
