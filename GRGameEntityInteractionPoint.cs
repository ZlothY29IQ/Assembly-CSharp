using System;
using UnityEngine;

public class GRGameEntityInteractionPoint : MonoBehaviour
{
	public GameEntity gameEntity;

	public float autoReleaseDistance = 0.1f;

	public Action OnGrabStart;

	public Action OnGrabContinue;

	public Action OnGrabEnd;

	public Transform targetParent;

	public void Start()
	{
		base.transform.parent = targetParent;
	}

	public void OnEnable()
	{
		GameEntity obj = gameEntity;
		obj.OnGrabbed = (Action)Delegate.Combine(obj.OnGrabbed, new Action(OnGrabbed));
		GameEntity obj2 = gameEntity;
		obj2.OnReleased = (Action)Delegate.Combine(obj2.OnReleased, new Action(OnReleased));
	}

	public void OnDisable()
	{
		GameEntity obj = gameEntity;
		obj.OnGrabbed = (Action)Delegate.Remove(obj.OnGrabbed, new Action(OnGrabbed));
		GameEntity obj2 = gameEntity;
		obj2.OnReleased = (Action)Delegate.Remove(obj2.OnReleased, new Action(OnReleased));
	}

	public void OnGrabbed()
	{
		GameEntity obj = gameEntity;
		obj.OnTick = (Action)Delegate.Combine(obj.OnTick, new Action(TickWhileHeld));
		OnGrabStart?.Invoke();
	}

	public void OnReleased()
	{
		GameEntity obj = gameEntity;
		obj.OnTick = (Action)Delegate.Remove(obj.OnTick, new Action(TickWhileHeld));
		gameEntity.transform.parent = targetParent;
		gameEntity.transform.localRotation = Quaternion.identity;
		gameEntity.transform.localPosition = Vector3.zero;
		OnGrabEnd();
	}

	public void TickWhileHeld()
	{
		if (targetParent != null)
		{
			Vector3 position = targetParent.transform.position;
			Vector3 position2 = base.transform.position;
			if (Vector3.Magnitude(position - position2) > autoReleaseDistance)
			{
				GamePlayer gamePlayer = GamePlayer.GetGamePlayer(gameEntity.heldByActorNumber);
				if (gamePlayer != null)
				{
					gamePlayer.ClearGrabbedIfHeld(gameEntity.id, gameEntity.manager);
				}
				if (gamePlayer != null && GamePlayerLocal.instance.gamePlayer == gamePlayer)
				{
					GamePlayerLocal.instance.ClearGrabbedIfHeld(gameEntity.id, gameEntity.manager);
				}
				OnReleased();
				return;
			}
		}
		OnGrabContinue?.Invoke();
	}
}
