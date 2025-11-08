using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace GorillaTag.Cosmetics;

[RequireComponent(typeof(Collider))]
public class OnCollisionEventsCosmetic : MonoBehaviour
{
	[Serializable]
	public class Listener
	{
		[Tooltip("Only collisions with objects on these layers will be considered.")]
		public LayerMask collisionLayerMask;

		[Tooltip("Optional tag whitelist. If non-empty, collisions must match at least one of these tags.")]
		public List<string> collisionTagsList = new List<string>();

		[Tooltip("Choose which collision phase triggers this listener: Enter, Stay, or Exit.")]
		public EventType eventType;

		public UnityEvent<bool, Collision> listenerComponent;

		public UnityEvent<Vector3> listenerComponentContactPoint;

		public UnityEvent<VRRig> onCollidedVRRig;

		[Tooltip("If true, fire for everyone in the room. If false, only fire when this item is owned locally (offline rig).")]
		public bool syncForEveryoneInRoom = true;

		[Tooltip("If true, only fire while this item is held. Requires a TransferrableObject on this object or a parent.")]
		public bool fireOnlyWhileHeld = true;

		[NonSerialized]
		public HashSet<string> tagSet;
	}

	public enum EventType
	{
		CollisionEnter,
		CollisionStay,
		CollisionExit
	}

	[Tooltip("List of per-condition listeners. Each entry specifies when (Enter/Stay/Exit), what to collide with (layers/tags), and which UnityEvents to fire.")]
	public Listener[] eventListeners = new Listener[0];

	private Listener[] enterListeners = Array.Empty<Listener>();

	private Listener[] stayListeners = Array.Empty<Listener>();

	private Listener[] exitListeners = Array.Empty<Listener>();

	private Collider myCollider;

	private VRRig rig;

	private TransferrableObject parentTransferable;

	private bool IsMyItem()
	{
		if (rig != null)
		{
			return rig.isOfflineVRRig;
		}
		return false;
	}

	private void Awake()
	{
		myCollider = GetComponent<Collider>();
		if (myCollider == null)
		{
			Debug.LogError("OnCollisionEventsCosmetic requires a Collider on the same GameObject.");
			base.enabled = false;
			return;
		}
		if (myCollider.isTrigger)
		{
			Debug.LogWarning("OnCollisionEventsCosmetic: Collider is set to Trigger. OnCollision will not fire. Set it to non-trigger for collisions.");
		}
		rig = GetComponentInParent<VRRig>();
		parentTransferable = GetComponentInParent<TransferrableObject>();
		List<Listener> list = new List<Listener>();
		List<Listener> list2 = new List<Listener>();
		List<Listener> list3 = new List<Listener>();
		if (eventListeners != null)
		{
			for (int i = 0; i < eventListeners.Length; i++)
			{
				Listener listener = eventListeners[i];
				if (listener.tagSet == null)
				{
					if (listener.collisionTagsList != null && listener.collisionTagsList.Count > 0)
					{
						listener.tagSet = new HashSet<string>(listener.collisionTagsList);
					}
					else
					{
						listener.tagSet = new HashSet<string>();
					}
				}
				if (listener.eventType == EventType.CollisionEnter)
				{
					list.Add(listener);
				}
				else if (listener.eventType == EventType.CollisionStay)
				{
					list2.Add(listener);
				}
				else if (listener.eventType == EventType.CollisionExit)
				{
					list3.Add(listener);
				}
			}
		}
		enterListeners = ((list.Count > 0) ? list.ToArray() : Array.Empty<Listener>());
		stayListeners = ((list2.Count > 0) ? list2.ToArray() : Array.Empty<Listener>());
		exitListeners = ((list3.Count > 0) ? list3.ToArray() : Array.Empty<Listener>());
	}

	private void OnCollisionEnter(Collision collision)
	{
		if (IsCollisionUsable(collision))
		{
			Dispatch(enterListeners, collision);
		}
	}

	private void OnCollisionStay(Collision collision)
	{
		if (IsCollisionUsable(collision))
		{
			Dispatch(stayListeners, collision);
		}
	}

	private void OnCollisionExit(Collision collision)
	{
		if (IsCollisionUsable(collision))
		{
			Dispatch(exitListeners, collision);
		}
	}

	private static bool IsCollisionUsable(Collision collision)
	{
		if (collision == null)
		{
			return false;
		}
		Collider collider = collision.collider;
		if (collider == null)
		{
			return false;
		}
		GameObject gameObject = collider.gameObject;
		if (gameObject == null || !gameObject.activeInHierarchy)
		{
			return false;
		}
		return true;
	}

	private void Dispatch(Listener[] listeners, Collision collision)
	{
		if (listeners == null || listeners.Length == 0)
		{
			return;
		}
		Collider collider = collision.collider;
		GameObject gameObject = ((collider != null) ? collider.gameObject : null);
		if (gameObject == null)
		{
			return;
		}
		int layer = gameObject.layer;
		bool arg = (bool)parentTransferable && parentTransferable.InLeftHand();
		Vector3 position = ((myCollider != null) ? myCollider.bounds.center : base.transform.position);
		Vector3 arg2 = ((collision.contactCount <= 0) ? collider.ClosestPoint(position) : collision.GetContact(0).point);
		foreach (Listener listener in listeners)
		{
			if ((listener.syncForEveryoneInRoom || IsMyItem()) && (!listener.fireOnlyWhileHeld || !parentTransferable || parentTransferable.InHand()) && (listener.tagSet == null || listener.tagSet.Count <= 0 || CompareTagAny(gameObject, listener.tagSet)) && ((1 << layer) & listener.collisionLayerMask.value) != 0)
			{
				if (listener.listenerComponent != null)
				{
					listener.listenerComponent.Invoke(arg, collision);
				}
				if (listener.listenerComponentContactPoint != null)
				{
					listener.listenerComponentContactPoint.Invoke(arg2);
				}
				VRRig componentInParent = gameObject.GetComponentInParent<VRRig>();
				if (componentInParent != null && listener.onCollidedVRRig != null)
				{
					listener.onCollidedVRRig.Invoke(componentInParent);
				}
			}
		}
	}

	private static bool CompareTagAny(GameObject go, HashSet<string> tagSet)
	{
		if (tagSet == null || tagSet.Count == 0)
		{
			return true;
		}
		foreach (string item in tagSet)
		{
			if (!string.IsNullOrEmpty(item) && go.CompareTag(item))
			{
				return true;
			}
		}
		return false;
	}

	private bool IsTagValid(GameObject obj, Listener listener)
	{
		if (listener == null)
		{
			return true;
		}
		if (listener.tagSet == null || listener.tagSet.Count == 0)
		{
			return true;
		}
		return CompareTagAny(obj, listener.tagSet);
	}
}
