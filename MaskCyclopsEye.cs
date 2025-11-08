using UnityEngine;
using UnityEngine.Events;

public class MaskCyclopsEye : MonoBehaviour
{
	[Tooltip("Invoked when it's time to trigger a blink (e.g., play animation one-shot).")]
	public UnityEvent OnBlink;

	[Tooltip("Minimum time in seconds between blinks.")]
	[SerializeField]
	private float minWaitTime = 3f;

	[Tooltip("Maximum time in seconds between blinks.")]
	[SerializeField]
	private float maxWaitTime = 5f;

	private float nextBlinkTime;

	private void OnEnable()
	{
		ScheduleNextBlink();
	}

	private void OnDisable()
	{
	}

	public void Update()
	{
		if (Time.time >= nextBlinkTime)
		{
			OnBlink?.Invoke();
			ScheduleNextBlink();
		}
	}

	public void Tick()
	{
		if (Time.time >= nextBlinkTime)
		{
			OnBlink?.Invoke();
			ScheduleNextBlink();
		}
	}

	private void ScheduleNextBlink()
	{
		float num = Random.Range(minWaitTime, maxWaitTime);
		nextBlinkTime = Time.time + num;
	}
}
