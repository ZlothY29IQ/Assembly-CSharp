using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class GameAbilityEvent
{
	public float time;

	public AbilitySound sound;

	public List<UnityEvent> triggerEvent;

	[NonSerialized]
	public bool played;

	public void Reset()
	{
		played = false;
	}

	public void TryPlay(float abilityTime, AudioSource audioSource)
	{
		if (!(abilityTime < time) && !played)
		{
			played = true;
			if (sound.IsValid())
			{
				sound.Play(audioSource);
			}
			for (int i = 0; i < triggerEvent.Count; i++)
			{
				triggerEvent[i].Invoke();
			}
		}
	}
}
