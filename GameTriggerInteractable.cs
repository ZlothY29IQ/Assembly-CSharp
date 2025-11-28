using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GameEntity))]
public class GameTriggerInteractable : MonoBehaviour
{
	public GameEntity gameEntity;

	public Transform interactableCenter;

	public float interactableRadius;

	public bool interactableWhileGrabbed;

	public bool interactableWhileSnapped;

	public bool interactablePermanently;

	public bool interactableOnOthers;

	public bool triggerInteractionActive;

	public int handIndex = -1;

	public static List<GameTriggerInteractable> LocalInteractableTriggers = new List<GameTriggerInteractable>();

	private void OnEnable()
	{
		if (gameEntity == null)
		{
			gameEntity = GetComponent<GameEntity>();
		}
		if (interactableWhileGrabbed)
		{
			GameEntity obj = gameEntity;
			obj.OnGrabbed = (Action)Delegate.Combine(obj.OnGrabbed, new Action(StartHolding));
			GameEntity obj2 = gameEntity;
			obj2.OnReleased = (Action)Delegate.Combine(obj2.OnReleased, new Action(StopHolding));
		}
		if (interactableWhileSnapped)
		{
			GameEntity obj3 = gameEntity;
			obj3.OnSnapped = (Action)Delegate.Combine(obj3.OnSnapped, new Action(StartHolding));
			GameEntity obj4 = gameEntity;
			obj4.OnUnsnapped = (Action)Delegate.Combine(obj4.OnUnsnapped, new Action(StopHolding));
		}
	}

	public void StartHolding()
	{
		LocalInteractableTriggers.AddIfNew(this);
	}

	public void StopHolding()
	{
		LocalInteractableTriggers.RemoveIfContains(this);
	}

	public bool PointWithinInteractableArea(Vector3 point)
	{
		return (interactableCenter.position - point).magnitude < interactableRadius;
	}

	public void BeginTriggerInteraction(int _handIndex)
	{
		triggerInteractionActive = true;
		handIndex = _handIndex;
	}

	public void EndTriggerInteraction()
	{
		triggerInteractionActive = false;
		handIndex = -1;
	}
}
