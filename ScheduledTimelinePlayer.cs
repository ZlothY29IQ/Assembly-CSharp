using UnityEngine;
using UnityEngine.Playables;

public class ScheduledTimelinePlayer : MonoBehaviour
{
	public PlayableDirector timeline;

	public int eventHour = 7;

	private int scheduledEventID;

	protected void OnEnable()
	{
		scheduledEventID = BetterDayNightManager.RegisterScheduledEvent(eventHour, HandleScheduledEvent);
	}

	protected void OnDisable()
	{
		BetterDayNightManager.UnregisterScheduledEvent(scheduledEventID);
	}

	private void HandleScheduledEvent()
	{
		timeline.Play();
	}
}
