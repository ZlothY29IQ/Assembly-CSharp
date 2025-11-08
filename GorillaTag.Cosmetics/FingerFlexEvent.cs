using System;
using UnityEngine;
using UnityEngine.Events;

namespace GorillaTag.Cosmetics;

public class FingerFlexEvent : MonoBehaviourTick
{
	[Serializable]
	public class Listener
	{
		public EventType eventType;

		public UnityEvent<bool, float> listenerComponent;

		public float fingerFlexValue = 0.75f;

		public float fingerReleaseValue = 0.01f;

		[Tooltip("How many frames should pass to fire a finger flex stayed event")]
		public int frameInterval = 20;

		[Tooltip("This event will be fired for everyone in the room (synced) by default unless you uncheck this box so that it will be fired only for the local player.")]
		public bool syncForEveryoneInRoom = true;

		[Tooltip("Fire these events only when the item is held in hand, only works if there is a transferable component somewhere on the object or its parent.")]
		public bool fireOnlyWhileHeld = true;

		[Tooltip("Whether to check the left hand or the right hand, only works if \"ignoreTransferable\" is true.")]
		public bool checkLeftHand;

		internal int frameCounter;

		internal float fingerRightLastValue;

		internal float fingerLeftLastValue;
	}

	public enum EventType
	{
		OnFingerFlexed,
		OnFingerReleased,
		OnFingerFlexStayed
	}

	private enum FingerType
	{
		Thumb,
		Index,
		Middle,
		IndexAndMiddleMin
	}

	[SerializeField]
	public bool ignoreTransferable;

	[SerializeField]
	private FingerType fingerType = FingerType.Index;

	public Listener[] eventListeners = new Listener[0];

	private VRRig _rig;

	private TransferrableObject parentTransferable;

	private void Awake()
	{
		_rig = GetComponentInParent<VRRig>();
		parentTransferable = GetComponentInParent<TransferrableObject>();
	}

	private bool IsMyItem()
	{
		if (_rig != null)
		{
			return _rig.isOfflineVRRig;
		}
		return false;
	}

	public override void Tick()
	{
		for (int i = 0; i < eventListeners.Length; i++)
		{
			Listener listener = eventListeners[i];
			FireEvents(listener);
		}
	}

	private void FireEvents(Listener listener)
	{
		if (!listener.syncForEveryoneInRoom && !IsMyItem())
		{
			return;
		}
		if (!ignoreTransferable && listener.fireOnlyWhileHeld && (bool)parentTransferable && !parentTransferable.InHand() && listener.eventType == EventType.OnFingerReleased)
		{
			if (listener.fingerRightLastValue > listener.fingerReleaseValue)
			{
				listener.listenerComponent?.Invoke(arg0: false, 0f);
				listener.fingerRightLastValue = 0f;
			}
			if (listener.fingerLeftLastValue > listener.fingerReleaseValue)
			{
				listener.listenerComponent?.Invoke(arg0: true, 0f);
				listener.fingerLeftLastValue = 0f;
			}
		}
		if (ignoreTransferable || !parentTransferable || !listener.fireOnlyWhileHeld || parentTransferable.InHand())
		{
			switch (fingerType)
			{
			case FingerType.Index:
			{
				float calcT5 = _rig.leftIndex.calcT;
				float calcT6 = _rig.rightIndex.calcT;
				FireEvents(listener, calcT5, calcT6);
				break;
			}
			case FingerType.Middle:
			{
				float calcT3 = _rig.leftMiddle.calcT;
				float calcT4 = _rig.rightMiddle.calcT;
				FireEvents(listener, calcT3, calcT4);
				break;
			}
			case FingerType.Thumb:
			{
				float calcT = _rig.leftThumb.calcT;
				float calcT2 = _rig.rightThumb.calcT;
				FireEvents(listener, calcT, calcT2);
				break;
			}
			case FingerType.IndexAndMiddleMin:
			{
				float leftFinger = Mathf.Min(_rig.leftIndex.calcT, _rig.leftMiddle.calcT);
				float rightFinger = Mathf.Min(_rig.rightIndex.calcT, _rig.rightMiddle.calcT);
				FireEvents(listener, leftFinger, rightFinger);
				break;
			}
			}
		}
	}

	private void FireEvents(Listener listener, float leftFinger, float rightFinger)
	{
		if ((ignoreTransferable && listener.checkLeftHand) || ((bool)parentTransferable && FingerFlexValidation(isLeftHand: true)))
		{
			CheckFingerValue(listener, leftFinger, isLeft: true, ref listener.fingerLeftLastValue);
			return;
		}
		if ((ignoreTransferable && !listener.checkLeftHand) || ((bool)parentTransferable && FingerFlexValidation(isLeftHand: false)))
		{
			CheckFingerValue(listener, rightFinger, isLeft: false, ref listener.fingerRightLastValue);
			return;
		}
		CheckFingerValue(listener, leftFinger, isLeft: true, ref listener.fingerLeftLastValue);
		CheckFingerValue(listener, rightFinger, isLeft: false, ref listener.fingerRightLastValue);
	}

	private void CheckFingerValue(Listener listener, float fingerValue, bool isLeft, ref float lastValue)
	{
		if (fingerValue > listener.fingerFlexValue)
		{
			listener.frameCounter++;
		}
		switch (listener.eventType)
		{
		case EventType.OnFingerFlexed:
			if (fingerValue > listener.fingerFlexValue && lastValue < listener.fingerFlexValue)
			{
				listener.listenerComponent?.Invoke(isLeft, fingerValue);
			}
			break;
		case EventType.OnFingerReleased:
			if (fingerValue <= listener.fingerReleaseValue && lastValue > listener.fingerReleaseValue)
			{
				listener.listenerComponent?.Invoke(isLeft, fingerValue);
				listener.frameCounter = 0;
			}
			break;
		case EventType.OnFingerFlexStayed:
			if (fingerValue > listener.fingerFlexValue && lastValue >= listener.fingerFlexValue && listener.frameCounter % listener.frameInterval == 0)
			{
				listener.listenerComponent?.Invoke(isLeft, fingerValue);
				listener.frameCounter = 0;
			}
			break;
		}
		lastValue = fingerValue;
	}

	private bool FingerFlexValidation(bool isLeftHand)
	{
		if (parentTransferable.InLeftHand() && !isLeftHand)
		{
			return false;
		}
		if (!parentTransferable.InLeftHand() && isLeftHand)
		{
			return false;
		}
		return true;
	}
}
