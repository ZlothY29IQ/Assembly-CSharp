using System;
using UnityEngine;

public class TimeOfDayEvent : TimeEvent
{
	[SerializeField]
	[Range(0f, 1f)]
	private float _timeStart;

	[SerializeField]
	[Range(0f, 1f)]
	private float _timeEnd = 1f;

	[SerializeField]
	private float _currentTime = -1f;

	[Space]
	[SerializeField]
	private double _currentSeconds = -1.0;

	[SerializeField]
	private double _totalSecondsInRange = -1.0;

	[NonSerialized]
	private float _elapsed = -1f;

	[SerializeField]
	private BetterDayNightManager _dayNightManager;

	public float currentTime => _currentTime;

	public float timeStart
	{
		get
		{
			return _timeStart;
		}
		set
		{
			_timeStart = Mathf.Clamp01(value);
		}
	}

	public float timeEnd
	{
		get
		{
			return _timeEnd;
		}
		set
		{
			_timeEnd = Mathf.Clamp01(value);
		}
	}

	public bool isOngoing => _ongoing;

	private void Start()
	{
		if (!_dayNightManager)
		{
			_dayNightManager = BetterDayNightManager.instance;
		}
		if ((bool)_dayNightManager)
		{
			for (int i = 0; i < _dayNightManager.timeOfDayRange.Length; i++)
			{
				_totalSecondsInRange += _dayNightManager.timeOfDayRange[i] * 3600.0;
			}
			_totalSecondsInRange = Math.Floor(_totalSecondsInRange);
		}
	}

	private void Update()
	{
		_elapsed += Time.deltaTime;
		if (!(_elapsed < 1f))
		{
			_elapsed = 0f;
			UpdateTime();
		}
	}

	private void UpdateTime()
	{
		_currentSeconds = ((ITimeOfDaySystem)_dayNightManager).currentTimeInSeconds;
		_currentSeconds = Math.Floor(_currentSeconds);
		_currentTime = (float)(_currentSeconds / _totalSecondsInRange);
		bool flag = _currentTime >= 0f && _currentTime >= _timeStart && _currentTime <= _timeEnd;
		if (!_ongoing && flag)
		{
			StartEvent();
		}
		if (_ongoing && !flag)
		{
			StopEvent();
		}
	}

	public static implicit operator bool(TimeOfDayEvent ev)
	{
		if ((bool)(UnityEngine.Object)ev)
		{
			return ev.isOngoing;
		}
		return false;
	}
}
