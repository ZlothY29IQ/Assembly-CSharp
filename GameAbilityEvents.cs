using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GameAbilityEvents
{
	public GameAbilityEvent startEvent;

	public GameAbilityEvent stopEvent;

	public List<GameAbilityEvent> events;

	public void Reset()
	{
		for (int i = 0; i < events.Count; i++)
		{
			events[i].Reset();
		}
	}

	public void OnAbilityStart(float abilityTime, AudioSource audioSource)
	{
		startEvent.TryPlay(abilityTime, (startEvent.sound.audioSource == null) ? audioSource : startEvent.sound.audioSource);
	}

	public void OnAbilityStop(float abilityTime, AudioSource audioSource)
	{
		stopEvent.TryPlay(abilityTime, (stopEvent.sound.audioSource == null) ? audioSource : stopEvent.sound.audioSource);
	}

	public void TryPlay(float abilityTime, AudioSource audioSource)
	{
		for (int i = 0; i < events.Count; i++)
		{
			events[i].TryPlay(abilityTime, (events[i].sound.audioSource == null) ? audioSource : events[i].sound.audioSource);
		}
	}
}
