using UnityEngine;

namespace GorillaNetworking.ScheduledEvents;

public class ScheduledEventControlledObject : MonoBehaviour
{
	[Tooltip("Active while waiting for the event to start (also the default state during initial scene load / sync).")]
	public bool enableBefore;

	[Tooltip("Active while the event is playing in this room.")]
	public bool enableDuring;

	[Tooltip("Active after the event has finished in this room, or in post-event rooms where the player missed it.")]
	public bool enableAfter;

	[Tooltip("Active when no scheduled event is configured at all (manager has no titleDataKey). Use for the ordinary stage look.")]
	public bool enableIfNoEvent;

	private void Start()
	{
		ScheduledEventManager.Instance?.Register(this);
	}

	private void OnDestroy()
	{
		if (ScheduledEventManager.Instance != null)
		{
			ScheduledEventManager.Instance.Unregister(this);
		}
	}

	public bool MatchesPhase(ScheduledEventPhase phase)
	{
		return phase switch
		{
			ScheduledEventPhase.Before => enableBefore, 
			ScheduledEventPhase.During => enableDuring, 
			ScheduledEventPhase.After => enableAfter, 
			ScheduledEventPhase.NoEvent => enableIfNoEvent, 
			_ => false, 
		};
	}
}
