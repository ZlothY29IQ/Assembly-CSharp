using System;
using System.Collections.Generic;
using GorillaNetworking;
using UnityEngine;

public class ServerTimeEvent : TimeEvent
{
	[Serializable]
	public struct EventTime(int h, int m)
	{
		public int hour = h;

		public int minute = m;
	}

	[SerializeField]
	private EventTime[] times;

	[SerializeField]
	private float queryTime = 60f;

	private float lastQueryTime;

	private HashSet<EventTime> eventTimes;

	private void Awake()
	{
		eventTimes = new HashSet<EventTime>(times);
	}

	private void Update()
	{
		if (!(GorillaComputer.instance == null) && !(Time.time - lastQueryTime < queryTime))
		{
			EventTime item = new EventTime(GorillaComputer.instance.GetServerTime().Hour, GorillaComputer.instance.GetServerTime().Minute);
			bool flag = eventTimes.Contains(item);
			if (!_ongoing && flag)
			{
				StartEvent();
			}
			if (_ongoing && !flag)
			{
				StopEvent();
			}
			lastQueryTime = Time.time;
		}
	}
}
