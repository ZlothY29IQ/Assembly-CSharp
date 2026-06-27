using GorillaLocomotion;
using UnityEngine;

public abstract class PlayerTrigger : MonoBehaviour
{
	protected bool isPlayerCollided;

	protected Collider playerCollider;

	[SerializeField]
	private CompositeTriggerEvents triggerCollisionEvents;

	protected virtual void Awake()
	{
		triggerCollisionEvents.CompositeTriggerEnter += OnCompositeTriggerEnter;
		triggerCollisionEvents.CompositeTriggerExit += OnCompositeTriggerExit;
	}

	private void OnCompositeTriggerEnter(Collider collider)
	{
		if (!isPlayerCollided && collider == GTPlayer.Instance.bodyCollider)
		{
			playerCollider = collider;
			PlayerEnter();
		}
	}

	private void OnCompositeTriggerExit(Collider collider)
	{
		if (isPlayerCollided && collider == playerCollider)
		{
			PlayerExit();
		}
	}

	protected virtual void PlayerEnter()
	{
		isPlayerCollided = true;
	}

	protected virtual void PlayerExit()
	{
		playerCollider = null;
		isPlayerCollided = false;
	}
}
