using System;
using System.Collections.Generic;
using GorillaExtensions;
using GorillaLocomotion;
using UnityEngine;
using UnityEngine.Events;

namespace GorillaTag.Cosmetics;

[RequireComponent(typeof(Collider))]
public class OnTriggerEventsCosmetic : MonoBehaviour
{
	[Serializable]
	public class Listener
	{
		[Tooltip("Only trigger interactions with objects on these layers.")]
		public LayerMask triggerLayerMask;

		[Tooltip("Optional tag whitelist. If non-empty, triggers must match at least one of these tags.")]
		public List<string> triggerTagsList = new List<string>();

		[Tooltip("Choose which trigger phase invokes this listener: Enter, Stay, or Exit.")]
		public EventType eventType;

		public UnityEvent<bool, Collider> listenerComponent;

		public UnityEvent<Vector3> listenerComponentContactPoint;

		public UnityEvent<VRRig> onTriggeredVRRig;

		[Tooltip("If true, fire for everyone in the room. If false, only fire when this item is owned locally (offline rig).")]
		public bool syncForEveryoneInRoom = true;

		[Tooltip("If true, only fire while this item is held. Requires a TransferrableObject on this object or a parent.")]
		public bool fireOnlyWhileHeld = true;

		[NonSerialized]
		public HashSet<string> tagSet;
	}

	public enum EventType
	{
		TriggerEnter,
		TriggerStay,
		TriggerExit
	}

	[Tooltip("List of per-condition listeners. Each entry specifies when (Enter/Stay/Exit), what to trigger with (layers/tags), and which UnityEvents to fire.")]
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
		Collider[] components = GetComponents<Collider>();
		if (components == null || components.Length == 0)
		{
			Debug.LogError("OnTriggerEventsCosmetic requires at least one Collider on the same GameObject.");
			base.enabled = false;
			return;
		}
		bool flag = false;
		Collider[] array = components;
		foreach (Collider collider in array)
		{
			if (collider != null && (collider.isTrigger || collider.attachedRigidbody != null))
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			Debug.LogWarning("OnTriggerEventsCosmetic: Collider is not set to Trigger. OnTrigger will not fire. Path=" + base.transform.GetPathQ(), base.transform);
		}
		rig = GetComponentInParent<VRRig>();
		if (rig == null && base.gameObject.GetComponentInParent<GTPlayer>() != null)
		{
			rig = GorillaTagger.Instance.offlineVRRig;
		}
		parentTransferable = GetComponentInParent<TransferrableObject>();
		List<Listener> list = new List<Listener>();
		List<Listener> list2 = new List<Listener>();
		List<Listener> list3 = new List<Listener>();
		if (eventListeners != null)
		{
			for (int j = 0; j < eventListeners.Length; j++)
			{
				Listener listener = eventListeners[j];
				if (listener.tagSet == null)
				{
					if (listener.triggerTagsList != null && listener.triggerTagsList.Count > 0)
					{
						listener.tagSet = new HashSet<string>(listener.triggerTagsList);
					}
					else
					{
						listener.tagSet = new HashSet<string>();
					}
				}
				if (listener.eventType == EventType.TriggerEnter)
				{
					list.Add(listener);
				}
				else if (listener.eventType == EventType.TriggerStay)
				{
					list2.Add(listener);
				}
				else if (listener.eventType == EventType.TriggerExit)
				{
					list3.Add(listener);
				}
			}
		}
		enterListeners = ((list.Count > 0) ? list.ToArray() : Array.Empty<Listener>());
		stayListeners = ((list2.Count > 0) ? list2.ToArray() : Array.Empty<Listener>());
		exitListeners = ((list3.Count > 0) ? list3.ToArray() : Array.Empty<Listener>());
	}

	private void OnTriggerEnter(Collider other)
	{
		if (IsOtherUsable(other))
		{
			Dispatch(enterListeners, other);
		}
	}

	private void OnTriggerStay(Collider other)
	{
		if (IsOtherUsable(other))
		{
			Dispatch(stayListeners, other);
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (IsOtherUsable(other))
		{
			Dispatch(exitListeners, other);
		}
	}

	private static bool IsOtherUsable(Collider other)
	{
		if (other == null)
		{
			return false;
		}
		GameObject gameObject = other.gameObject;
		if (gameObject == null || !gameObject.activeInHierarchy)
		{
			return false;
		}
		return true;
	}

	private void Dispatch(Listener[] listeners, Collider other)
	{
		if (listeners == null || listeners.Length == 0)
		{
			return;
		}
		int layer = other.gameObject.layer;
		bool arg = (bool)parentTransferable && parentTransferable.InLeftHand();
		Vector3 position = ((myCollider != null) ? myCollider.bounds.center : base.transform.position);
		foreach (Listener listener in listeners)
		{
			if ((listener.syncForEveryoneInRoom || IsMyItem()) && (!listener.fireOnlyWhileHeld || !parentTransferable || parentTransferable.InHand()) && (listener.tagSet == null || listener.tagSet.Count <= 0 || CompareTagAny(other.gameObject, listener.tagSet)) && ((1 << layer) & listener.triggerLayerMask.value) != 0)
			{
				listener.listenerComponent?.Invoke(arg, other);
				Vector3 arg2 = other.ClosestPoint(position);
				listener.listenerComponentContactPoint?.Invoke(arg2);
				VRRig componentInParent = other.GetComponentInParent<VRRig>();
				if (componentInParent != null)
				{
					listener.onTriggeredVRRig?.Invoke(componentInParent);
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
