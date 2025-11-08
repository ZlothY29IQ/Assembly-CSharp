using System;
using GorillaTag.Cosmetics;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;

namespace GorillaTagScripts;

public class GorillaIntervalTimer : MonoBehaviourPun
{
	private enum TimeUnit
	{
		Seconds,
		Minutes,
		Hours
	}

	private enum RandomDistribution
	{
		Uniform,
		Normal,
		Exponential
	}

	private enum IntervalSource
	{
		LocalRandom,
		NetworkedRandom
	}

	private enum RunLength
	{
		Infinite,
		Finite
	}

	[Header("Scheduling")]
	[Tooltip("If true, the timer will automatically start when this component is enabled.")]
	[SerializeField]
	private bool runOnEnable = true;

	[Tooltip("If true, apply an initial delay before the first interval is fired.")]
	[SerializeField]
	private bool useInitialDelay;

	[Tooltip("Delay (in seconds or minutes depending on Unit) before the first fire if 'Use Initial Delay' is enabled.")]
	[SerializeField]
	private float initialDelay;

	[Header("Interval")]
	[Tooltip("Unit of time for Fixed Interval, Min and Max values.")]
	[SerializeField]
	private TimeUnit unit;

	[Tooltip("Distribution type used for generating random intervals when Interval Source = LocalRandom.")]
	[SerializeField]
	private RandomDistribution distribution;

	[Tooltip("Fixed interval duration (interpreted by Unit) when Use Random Duration = false.")]
	[SerializeField]
	private float fixedInterval = 1f;

	[Space]
	[Tooltip("If false, 'Fixed Interval' is used. If true, a random interval is sampled each cycle.")]
	[SerializeField]
	private bool useRandomDuration;

	[Tooltip("Minimum interval time (in selected Unit).")]
	[SerializeField]
	private float randTimeMin = 0.5f;

	[Tooltip("Maximum interval time (in selected Unit).")]
	[SerializeField]
	private float randTimeMax = 2f;

	[Tooltip("Determines whether to use a local random generator or a networked random source.")]
	[SerializeField]
	private IntervalSource intervalSource;

	[Header("Networked Interval (optional)")]
	[Tooltip("If Interval Source = NetworkedRandom, the timer queries this component for the next interval")]
	[SerializeField]
	private NetworkedRandomProvider networkProvider;

	[Space]
	[Tooltip("If true, wait this additional delay after onIntervalFired() before starting the next interval.")]
	[SerializeField]
	private bool usePostIntervalDelay;

	[Tooltip("Additional delay (in selected Unit) to wait after onIntervalFired(), before the next interval begins.")]
	[SerializeField]
	private float postIntervalDelay;

	[Header("Run Length")]
	[Tooltip("Infinite runs forever. Finite stops after Max Fires Per Run.")]
	[SerializeField]
	private RunLength runLength;

	[Tooltip("Number of times the timer fires before the run completes (when Run Length = Finite).")]
	[SerializeField]
	private int maxFiresPerRun = 3;

	[Tooltip("If true, the timer stops at the end of a finite run and requires ResetRun() / StartTimer() to continue. If false, the run counter auto-resets and continues.")]
	[SerializeField]
	private bool requireManualReset = true;

	[Header("Events")]
	public UnityEvent onIntervalFired;

	public UnityEvent onTimerStarted;

	public UnityEvent onTimerStopped;

	private const float minIntervalEpsilon = 0.001f;

	private float currentIntervalSeconds = 1f;

	private float elapsed;

	private bool isRunning;

	private bool isPaused;

	private bool isRegistered;

	private int runFiredSoFar;

	private bool isInPostFireDelay;

	private void Awake()
	{
		if (networkProvider == null)
		{
			networkProvider = GetComponentInParent<NetworkedRandomProvider>();
		}
		ResetElapsed();
		ResetRun();
	}

	private void OnEnable()
	{
		if (runOnEnable)
		{
			if (!isRegistered)
			{
				GorillaIntervalTimerManager.RegisterGorillaTimer(this);
				isRegistered = true;
			}
			StartTimer();
		}
	}

	private void OnDisable()
	{
		if (isRegistered)
		{
			GorillaIntervalTimerManager.UnregisterGorillaTimer(this);
			isRegistered = false;
		}
		StopTimer();
	}

	public void StartTimer()
	{
		if (!isRegistered)
		{
			GorillaIntervalTimerManager.RegisterGorillaTimer(this);
			isRegistered = true;
		}
		ResetRun();
		elapsed = 0f;
		isInPostFireDelay = false;
		if (useInitialDelay && initialDelay > 0f)
		{
			currentIntervalSeconds = Mathf.Max(0.001f, ToSeconds(initialDelay));
		}
		else
		{
			RollNextInterval();
		}
		isRunning = true;
		isPaused = false;
		onTimerStarted?.Invoke();
	}

	public void StopTimer()
	{
		isRunning = false;
		isPaused = false;
		elapsed = 0f;
		isInPostFireDelay = false;
		onTimerStopped?.Invoke();
		if (isRegistered)
		{
			GorillaIntervalTimerManager.UnregisterGorillaTimer(this);
			isRegistered = false;
		}
	}

	public void Pause()
	{
		isPaused = true;
	}

	public void Resume()
	{
		isPaused = false;
	}

	public void SetFixedIntervalSeconds(float seconds)
	{
		useRandomDuration = false;
		fixedInterval = Mathf.Max(0f, seconds);
		currentIntervalSeconds = Mathf.Max(0.001f, ToSeconds(fixedInterval));
		elapsed = 0f;
	}

	public void OverrideNextIntervalSeconds(float seconds)
	{
		currentIntervalSeconds = Mathf.Max(0.001f, seconds);
		elapsed = 0f;
	}

	public void ResetRun()
	{
		runFiredSoFar = 0;
	}

	public void InvokeUpdate()
	{
		if (!isRunning || isPaused)
		{
			return;
		}
		elapsed += Time.deltaTime;
		if (!(elapsed >= currentIntervalSeconds))
		{
			return;
		}
		if (isInPostFireDelay)
		{
			isInPostFireDelay = false;
			elapsed = 0f;
			RollNextInterval();
			return;
		}
		onIntervalFired?.Invoke();
		runFiredSoFar++;
		if (runLength == RunLength.Finite && runFiredSoFar >= Mathf.Max(1, maxFiresPerRun))
		{
			if (requireManualReset)
			{
				StopTimer();
				return;
			}
			runFiredSoFar = 0;
		}
		if (usePostIntervalDelay && postIntervalDelay > 0f)
		{
			isInPostFireDelay = true;
			elapsed = 0f;
			currentIntervalSeconds = Mathf.Max(0.001f, ToSeconds(postIntervalDelay));
		}
		else
		{
			elapsed = 0f;
			RollNextInterval();
		}
	}

	private void ResetElapsed()
	{
		elapsed = 0f;
	}

	private void RollNextInterval()
	{
		if (!useRandomDuration)
		{
			currentIntervalSeconds = Mathf.Max(0.001f, ToSeconds(fixedInterval));
			return;
		}
		float num = Mathf.Max(0f, ToSeconds(randTimeMin));
		float num2 = Mathf.Max(num, ToSeconds(randTimeMax));
		if (intervalSource == IntervalSource.NetworkedRandom && networkProvider != null)
		{
			float b;
			switch (distribution)
			{
			default:
				b = networkProvider.NextFloat(num, num2);
				break;
			case RandomDistribution.Normal:
			{
				double d2 = Math.Max(double.Epsilon, 1.0 - networkProvider.NextDouble(0.0, 1.0));
				double num5 = Math.Max(double.Epsilon, 1.0 - (double)networkProvider.NextFloat01());
				double num6 = Math.Sqrt(-2.0 * Math.Log(d2)) * Math.Sin(Math.PI * 2.0 * num5);
				float num7 = 0.5f * (num + num2);
				float num8 = (num2 - num) / 6f;
				b = Mathf.Clamp(num7 + (float)(num6 * (double)num8), num, num2);
				break;
			}
			case RandomDistribution.Exponential:
			{
				double d = Math.Max(double.Epsilon, 1.0 - networkProvider.NextDouble(0.0, 1.0));
				double num3 = 0.5 * (double)(num + num2);
				double num4 = ((num3 > 0.0) ? (1.0 / num3) : 1.0);
				b = Mathf.Clamp((float)((0.0 - Math.Log(d)) / num4), num, num2);
				break;
			}
			}
			currentIntervalSeconds = Mathf.Max(0.001f, b);
		}
		else
		{
			float b;
			switch (distribution)
			{
			default:
				b = UnityEngine.Random.Range(num, num2);
				break;
			case RandomDistribution.Normal:
			{
				float f = Mathf.Max(float.Epsilon, 1f - UnityEngine.Random.value);
				float num11 = 1f - UnityEngine.Random.value;
				float num12 = Mathf.Sqrt(-2f * Mathf.Log(f)) * Mathf.Sin(MathF.PI * 2f * num11);
				float num13 = 0.5f * (num + num2);
				float num14 = (num2 - num) / 6f;
				b = Mathf.Clamp(num13 + num12 * num14, num, num2);
				break;
			}
			case RandomDistribution.Exponential:
			{
				float num9 = 0.5f * (num + num2);
				float num10 = ((num9 > 0f) ? (1f / num9) : 1f);
				b = Mathf.Clamp((0f - Mathf.Log(Mathf.Max(float.Epsilon, 1f - UnityEngine.Random.value))) / num10, num, num2);
				break;
			}
			}
			currentIntervalSeconds = Mathf.Max(0.001f, b);
		}
	}

	private float ToSeconds(float value)
	{
		return unit switch
		{
			TimeUnit.Minutes => value * 60f, 
			TimeUnit.Hours => value * 3600f, 
			_ => value, 
		};
	}

	public void RestartTimer()
	{
		ResetElapsed();
		RollNextInterval();
		StartTimer();
	}

	public float GetPassedTime()
	{
		return elapsed;
	}

	public float GetRemainingTime()
	{
		return Mathf.Max(0f, currentIntervalSeconds - elapsed);
	}
}
