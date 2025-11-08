using System.Collections;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;

namespace GorillaTagScripts;

public class GorillaTimer : MonoBehaviourPun
{
	[SerializeField]
	private float timerDuration;

	[SerializeField]
	private bool useRandomDuration;

	[SerializeField]
	private float randTimeMin;

	[SerializeField]
	private float randTimeMax;

	private float passedTime;

	private bool startTimer;

	private bool resetTimer;

	public UnityEvent<GorillaTimer> onTimerStarted;

	public UnityEvent<GorillaTimer> onTimerStopped;

	private void Awake()
	{
		ResetTimer();
	}

	public void StartTimer()
	{
		startTimer = true;
		onTimerStarted?.Invoke(this);
	}

	public IEnumerator DelayedReStartTimer(float delayTime)
	{
		yield return new WaitForSeconds(delayTime);
		RestartTimer();
	}

	private void StopTimer()
	{
		startTimer = false;
		onTimerStopped?.Invoke(this);
	}

	private void ResetTimer()
	{
		passedTime = 0f;
	}

	public void RestartTimer()
	{
		if (useRandomDuration)
		{
			SetTimerDuration(Random.Range(randTimeMin, randTimeMax));
		}
		ResetTimer();
		StartTimer();
	}

	public void SetTimerDuration(float timer)
	{
		timerDuration = timer;
	}

	public void InvokeUpdate()
	{
		if (startTimer)
		{
			passedTime += Time.deltaTime;
		}
		if (startTimer && passedTime >= timerDuration)
		{
			StopTimer();
			ResetTimer();
		}
	}

	public float GetPassedTime()
	{
		return passedTime;
	}

	public void SetPassedTime(float time)
	{
		passedTime = time;
	}

	public float GetRemainingTime()
	{
		return timerDuration - passedTime;
	}

	public void OnEnable()
	{
		GorillaTimerManager.RegisterGorillaTimer(this);
	}

	public void OnDisable()
	{
		GorillaTimerManager.UnregisterGorillaTimer(this);
	}
}
