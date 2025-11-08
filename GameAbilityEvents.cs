using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GameAbilityEvents
{
	public List<GameAbilityEvent> events;

	public void Reset()
	{
		for (int i = 0; i < events.Count; i++)
		{
			events[i].Reset();
		}
	}

	public void TryPlay(float abilityTime, AudioSource audioSource)
	{
		for (int i = 0; i < events.Count; i++)
		{
			events[i].TryPlay(abilityTime, audioSource);
		}
	}
}
