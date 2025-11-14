using System;
using UnityEngine;

namespace GorillaTag.Cosmetics;

public class ContinuousPropertyTimeline : MonoBehaviour, ITickSystemTick
{
	private enum TimelineEndBehavior
	{
		Stop,
		Loop,
		PingPong
	}

	[Flags]
	private enum TimelineEvent
	{
		OnReachedEnd = 1,
		OnReachedBeginning = 2,
		OnEnable = 4,
		OnDisable = 8
	}

	[SerializeField]
	private float durationSeconds = 1f;

	[SerializeField]
	private float backwardDuration = 1f;

	[Tooltip("If true, the the timeline can move at a different speed when playing backwards.")]
	[SerializeField]
	private bool separateBackwardDuration;

	[Tooltip("When this object is enabled for the first time, should it immediately start playing from the beginning?")]
	[SerializeField]
	private bool startPlaying;

	[Tooltip("Determine what happens when the timeline reaches the end (or beginning while playing backwards).")]
	[SerializeField]
	private TimelineEndBehavior endBehavior;

	[SerializeField]
	private ContinuousPropertyArray continuousProperties;

	[SerializeField]
	private FlagEvents<TimelineEvent> events;

	private float time;

	private float inverseDuration;

	private float backwardDeltaMult;

	private bool IsForward = true;

	private bool IsPlaying;

	private bool IsBackward
	{
		get
		{
			return !IsForward;
		}
		set
		{
			IsForward = !value;
		}
	}

	private bool IsPaused
	{
		get
		{
			return !IsPlaying;
		}
		set
		{
			IsPlaying = !value;
		}
	}

	public bool TickRunning { get; set; }

	public void TimelinePlay()
	{
		IsPlaying = true;
		TickSystem<object>.AddTickCallback(this);
	}

	public void TimelinePause()
	{
		IsPaused = true;
		TickSystem<object>.RemoveTickCallback(this);
	}

	public void TimelineToggleDirection()
	{
		IsForward = !IsForward;
	}

	public void TimelineTogglePlay()
	{
		if (IsPlaying)
		{
			TimelinePause();
		}
		else
		{
			TimelinePlay();
		}
	}

	public void TimelinePlayForward()
	{
		IsForward = true;
		TimelinePlay();
	}

	public void TimelinePlayBackward()
	{
		IsBackward = true;
		TimelinePlay();
	}

	public void TimelinePlayFromBeginning()
	{
		time = 0f;
		TimelinePlayForward();
		OnReachedBeginning();
	}

	public void TimelinePlayFromEnd()
	{
		time = durationSeconds;
		TimelinePlayBackward();
		OnReachedEnd();
	}

	public void TimelineScrubToTime(float t)
	{
		if (t <= 0f)
		{
			time = 0f;
			OnReachedBeginning();
		}
		else if (t >= durationSeconds)
		{
			time = durationSeconds;
			OnReachedEnd();
		}
		else
		{
			time = t;
		}
	}

	public void TimelineScrubToFraction(float f)
	{
		TimelineScrubToTime(f * durationSeconds);
	}

	public void TimelineSetDuration(float d)
	{
		durationSeconds = d;
		inverseDuration = 1f / durationSeconds;
		backwardDeltaMult = durationSeconds / backwardDuration;
	}

	public void TimelineSetBackwardDuration(float d)
	{
		separateBackwardDuration = true;
		backwardDuration = d;
		backwardDeltaMult = durationSeconds / backwardDuration;
	}

	private void Awake()
	{
		IsPlaying = startPlaying;
	}

	private void OnEnable()
	{
		inverseDuration = 1f / durationSeconds;
		backwardDeltaMult = durationSeconds / backwardDuration;
		events.InvokeAll(TimelineEvent.OnEnable);
		if (IsPlaying)
		{
			TickSystem<object>.AddTickCallback(this);
		}
	}

	private void OnDisable()
	{
		events.InvokeAll(TimelineEvent.OnDisable);
		TickSystem<object>.RemoveTickCallback(this);
	}

	private void OnReachedEnd()
	{
		if (IsForward)
		{
			switch (endBehavior)
			{
			case TimelineEndBehavior.Stop:
				TimelinePause();
				time = durationSeconds;
				break;
			case TimelineEndBehavior.Loop:
				TimelinePlayFromBeginning();
				break;
			case TimelineEndBehavior.PingPong:
				IsBackward = true;
				time = durationSeconds;
				break;
			}
		}
		continuousProperties.ApplyAll(1f);
		events.InvokeAll(TimelineEvent.OnReachedEnd);
	}

	private void OnReachedBeginning()
	{
		if (IsBackward)
		{
			switch (endBehavior)
			{
			case TimelineEndBehavior.Stop:
				TimelinePause();
				time = 0f;
				break;
			case TimelineEndBehavior.Loop:
				TimelinePlayFromEnd();
				break;
			case TimelineEndBehavior.PingPong:
				IsForward = true;
				time = 0f;
				break;
			}
		}
		continuousProperties.ApplyAll(0f);
		events.InvokeAll(TimelineEvent.OnReachedBeginning);
	}

	private void InBetween()
	{
		float f = time * inverseDuration;
		continuousProperties.ApplyAll(f);
	}

	public void Tick()
	{
		if (IsForward)
		{
			time += Time.deltaTime;
			if (time >= durationSeconds)
			{
				OnReachedEnd();
			}
			else
			{
				InBetween();
			}
		}
		else
		{
			time -= Time.deltaTime * backwardDeltaMult;
			if (time <= 0f)
			{
				OnReachedBeginning();
			}
			else
			{
				InBetween();
			}
		}
	}
}
