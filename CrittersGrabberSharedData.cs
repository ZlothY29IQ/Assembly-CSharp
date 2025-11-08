using System.Collections.Generic;
using UnityEngine;

public static class CrittersGrabberSharedData
{
	public static List<CrittersActor> enteredCritterActor;

	public static List<CapsuleCollider> triggerCollidersToCheck;

	public static List<CrittersActor> heldActor;

	public static List<CrittersActorGrabber> actorGrabbers;

	private static bool initialized;

	public static void Initialize()
	{
		if (!initialized)
		{
			initialized = true;
			enteredCritterActor = new List<CrittersActor>();
			triggerCollidersToCheck = new List<CapsuleCollider>();
			heldActor = new List<CrittersActor>();
			actorGrabbers = new List<CrittersActorGrabber>();
		}
	}

	public static void AddEnteredActor(CrittersActor actor)
	{
		Initialize();
		if (!enteredCritterActor.Contains(actor))
		{
			enteredCritterActor.Add(actor);
		}
	}

	public static void RemoveEnteredActor(CrittersActor actor)
	{
		Initialize();
		if (enteredCritterActor.Contains(actor))
		{
			enteredCritterActor.Remove(actor);
		}
	}

	public static void AddTrigger(CapsuleCollider trigger)
	{
		Initialize();
		if (!triggerCollidersToCheck.Contains(trigger))
		{
			triggerCollidersToCheck.Add(trigger);
		}
	}

	public static void RemoveTrigger(CapsuleCollider trigger)
	{
		Initialize();
		if (triggerCollidersToCheck.Contains(trigger))
		{
			triggerCollidersToCheck.Remove(trigger);
		}
	}

	public static void AddActorGrabber(CrittersActorGrabber grabber)
	{
		Initialize();
		if (!actorGrabbers.Contains(grabber))
		{
			actorGrabbers.Add(grabber);
		}
	}

	public static void RemoveActorGrabber(CrittersActorGrabber grabber)
	{
		Initialize();
		if (actorGrabbers.Contains(grabber))
		{
			actorGrabbers.Remove(grabber);
		}
	}

	public static void DisableEmptyGrabberJoints()
	{
		Initialize();
		for (int i = 0; i < actorGrabbers.Count; i++)
		{
			if (actorGrabbers[i].grabber != null && actorGrabbers[i].actorsStillPresent.Count == 0)
			{
				for (int j = 0; j < actorGrabbers[i].grabber.grabbedActors.Count; j++)
				{
					actorGrabbers[i].grabber.grabbedActors[j].DisconnectJoint();
				}
			}
		}
	}
}
