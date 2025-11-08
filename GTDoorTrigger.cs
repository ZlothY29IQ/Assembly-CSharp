using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;

public class GTDoorTrigger : MonoBehaviour
{
	[Tooltip("Optional timeline to play to animate the thing getting activated, play sound, particles, etc...")]
	public PlayableDirector timeline;

	private int lastTriggeredFrame = -1;

	private List<Collider> overlappingColliders = new List<Collider>(20);

	internal UnityEvent TriggeredEvent = new UnityEvent();

	public int overlapCount => overlappingColliders.Count;

	public bool TriggeredThisFrame => lastTriggeredFrame == Time.frameCount;

	public void ValidateOverlappingColliders()
	{
		for (int num = overlappingColliders.Count - 1; num >= 0; num--)
		{
			if (overlappingColliders[num] == null || !overlappingColliders[num].gameObject.activeInHierarchy || !overlappingColliders[num].enabled)
			{
				overlappingColliders.RemoveAt(num);
			}
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!overlappingColliders.Contains(other))
		{
			overlappingColliders.Add(other);
		}
		lastTriggeredFrame = Time.frameCount;
		TriggeredEvent.Invoke();
		if (timeline != null && (timeline.time == 0.0 || timeline.time >= timeline.duration))
		{
			timeline.Play();
		}
	}

	private void OnTriggerExit(Collider other)
	{
		overlappingColliders.Remove(other);
	}
}
