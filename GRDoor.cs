using System;
using UnityEngine;

[Serializable]
public class GRDoor
{
	public enum DoorState
	{
		Closed,
		Open
	}

	public DoorState doorState;

	public Animation animation;

	public AnimationClip openAnim;

	public AnimationClip closeAnim;

	public AbilitySound openDoorSound;

	public AbilitySound closeDoorSound;

	public void Setup()
	{
		doorState = DoorState.Closed;
	}

	public void SetDoorState(DoorState newState)
	{
		if (newState != doorState)
		{
			doorState = newState;
			if (doorState == DoorState.Closed)
			{
				animation.clip = closeAnim;
				animation.Play();
				closeDoorSound.Play(null);
			}
			else
			{
				animation.clip = openAnim;
				animation.Play();
				openDoorSound.Play(null);
			}
		}
	}
}
