using UnityEngine;

public class CollisionEventNotifier : MonoBehaviour
{
	public delegate void CollisionEvent(CollisionEventNotifier notifier, Collision collision);

	public event CollisionEvent CollisionEnterEvent;

	public event CollisionEvent CollisionExitEvent;

	private void OnCollisionEnter(Collision collision)
	{
		this.CollisionEnterEvent?.Invoke(this, collision);
	}

	private void OnCollisionExit(Collision collision)
	{
		this.CollisionExitEvent?.Invoke(this, collision);
	}
}
