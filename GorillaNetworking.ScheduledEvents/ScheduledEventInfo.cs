using System;

namespace GorillaNetworking.ScheduledEvents;

public struct ScheduledEventInfo
{
	public bool isActive;

	public DateTime scheduledStart;

	public static ScheduledEventInfo None => new ScheduledEventInfo
	{
		isActive = false
	};
}
