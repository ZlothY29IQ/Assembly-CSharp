using System;
using System.Collections.Generic;
using GorillaExtensions;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;

namespace Cosmetics;

public class CountDrivenEvents : MonoBehaviour
{
	[Serializable]
	public class CountTrigger
	{
		[Tooltip("The count value that triggers this event")]
		public int triggerCount;

		[Tooltip("Events to invoke when count reaches this value")]
		public UnityEvent onCountReached;

		public UnityEvent onCountReachedShared;

		[Tooltip("Should this trigger fire every time the count passes through this value, or only once?")]
		public bool triggerOnce;

		[NonSerialized]
		public bool hasTriggered;
	}

	[Header("Network")]
	[SerializeField]
	private bool syncAllEvents;

	[Header("General Settings")]
	[Tooltip("If true, triggers will be evaluated once on enable using the initial count.")]
	[SerializeField]
	private bool evaluateOnEnable;

	[Tooltip("If enabled, the counter value will loop between 0 and the highest triggerCount.")]
	[SerializeField]
	private bool wrapCount;

	[Header("Count Triggers")]
	[SerializeField]
	private List<CountTrigger> triggers = new List<CountTrigger>();

	[Header("Local and Networked Events")]
	public UnityEvent<int> onCountChanged;

	public UnityEvent<int> onCountChangedShared;

	public UnityEvent<int> onCountIncreased;

	public UnityEvent<int> onCountIncreasedShared;

	public UnityEvent<int> onCountDecreased;

	public UnityEvent<int> onCountDecreasedShared;

	public UnityEvent onCountResetToZero;

	public UnityEvent onCountResetToZeroShared;

	public UnityEvent onReachedMaxTrigger;

	public UnityEvent onReachedMaxTriggerShared;

	[Header("Debug - Counter Settings")]
	[SerializeField]
	private int currentCount;

	private RubberDuckEvents _events;

	private VRRig myRig;

	private CallLimiter callLimiter = new CallLimiter(10, 1f);

	public int CurrentCount => currentCount;

	private void OnEnable()
	{
		if (myRig == null)
		{
			myRig = GetComponentInParent<VRRig>();
		}
		if (_events == null)
		{
			_events = base.gameObject.GetOrAddComponent<RubberDuckEvents>();
		}
		NetPlayer netPlayer = ((myRig != null) ? (myRig.creator ?? NetworkSystem.Instance.LocalPlayer) : NetworkSystem.Instance.LocalPlayer);
		if (netPlayer != null)
		{
			_events.Init(netPlayer);
		}
		if (_events != null)
		{
			_events.Activate.reliable = true;
			_events.Deactivate.reliable = true;
			_events.Activate += new Action<int, int, object[], PhotonMessageInfoWrapped>(OnCountChanged_SharedEvent);
			_events.Deactivate += new Action<int, int, object[], PhotonMessageInfoWrapped>(OnCountReached_SharedEvent);
		}
		if (evaluateOnEnable)
		{
			CheckTriggers(currentCount, currentCount);
		}
	}

	private void OnDisable()
	{
		if (_events != null)
		{
			_events.Activate -= new Action<int, int, object[], PhotonMessageInfoWrapped>(OnCountChanged_SharedEvent);
			_events.Deactivate -= new Action<int, int, object[], PhotonMessageInfoWrapped>(OnCountReached_SharedEvent);
			_events.Dispose();
			_events = null;
		}
	}

	private void OnValidate()
	{
		if (triggers == null)
		{
			return;
		}
		for (int i = 0; i < triggers.Count; i++)
		{
			if (triggers[i].triggerCount < 0)
			{
				triggers[i].triggerCount = 0;
			}
		}
	}

	public void Increment()
	{
		SetCount(currentCount + 1);
	}

	public void Decrement()
	{
		SetCount(currentCount - 1);
	}

	public void SetCount(int newCount)
	{
		if (myRig != null && !myRig.isLocal)
		{
			return;
		}
		int num = currentCount;
		if (wrapCount)
		{
			int highestTriggerCount = GetHighestTriggerCount();
			if (highestTriggerCount > 0)
			{
				int num2 = highestTriggerCount + 1;
				newCount = (newCount % num2 + num2) % num2;
			}
			else if (newCount < 0)
			{
				newCount = 0;
			}
		}
		else if (newCount < 0)
		{
			newCount = 0;
		}
		if (newCount != num)
		{
			bool flag = false;
			currentCount = newCount;
			onCountChanged?.Invoke(currentCount);
			onCountChangedShared?.Invoke(currentCount);
			if (currentCount > num)
			{
				onCountIncreased?.Invoke(currentCount);
				onCountIncreasedShared?.Invoke(currentCount);
				flag = true;
			}
			else if (currentCount < num)
			{
				onCountDecreased?.Invoke(currentCount);
				onCountDecreasedShared?.Invoke(currentCount);
			}
			CheckTriggers(num, currentCount);
			if (currentCount == 0)
			{
				onCountResetToZero?.Invoke();
				onCountResetToZeroShared?.Invoke();
			}
			int highestTriggerCount2 = GetHighestTriggerCount();
			if (highestTriggerCount2 > 0 && currentCount == highestTriggerCount2)
			{
				onReachedMaxTrigger?.Invoke();
				onReachedMaxTriggerShared?.Invoke();
			}
			if (syncAllEvents && PhotonNetwork.InRoom && _events != null && _events.Activate != null)
			{
				object[] args = new object[2] { flag, currentCount };
				_events.Activate.RaiseOthers(args);
			}
		}
	}

	[Tooltip("Resets all 'triggerOnce' flags, allowing one-time triggers to fire again.\n\nUse this when restarting a sequence, resetting an object,\nor testing trigger behavior multiple times in play mode.")]
	public void ResetTriggers()
	{
		for (int i = 0; i < triggers.Count; i++)
		{
			triggers[i].hasTriggered = false;
		}
	}

	private int GetHighestTriggerCount()
	{
		int num = 0;
		for (int i = 0; i < triggers.Count; i++)
		{
			if (triggers[i].triggerCount > num)
			{
				num = triggers[i].triggerCount;
			}
		}
		return num;
	}

	private void CheckTriggers(int oldCount, int newCount)
	{
		if (myRig != null && !myRig.isLocal)
		{
			return;
		}
		for (int i = 0; i < triggers.Count; i++)
		{
			CountTrigger countTrigger = triggers[i];
			if (countTrigger.triggerOnce && countTrigger.hasTriggered)
			{
				continue;
			}
			bool flag = false;
			if (wrapCount)
			{
				if (newCount == countTrigger.triggerCount)
				{
					flag = true;
				}
			}
			else if (oldCount < countTrigger.triggerCount && newCount >= countTrigger.triggerCount)
			{
				flag = true;
			}
			else if (oldCount > countTrigger.triggerCount && newCount <= countTrigger.triggerCount)
			{
				flag = true;
			}
			else if (oldCount == newCount && newCount == countTrigger.triggerCount)
			{
				flag = true;
			}
			if (flag)
			{
				countTrigger.onCountReached?.Invoke();
				countTrigger.onCountReachedShared?.Invoke();
				if (syncAllEvents && PhotonNetwork.InRoom && _events != null && _events.Deactivate != null)
				{
					object[] args = new object[1] { i };
					_events.Deactivate.RaiseOthers(args);
				}
				if (countTrigger.triggerOnce)
				{
					countTrigger.hasTriggered = true;
				}
			}
		}
	}

	private void OnCountChanged_SharedEvent(int sender, int target, object[] args, PhotonMessageInfoWrapped info)
	{
		if (sender != target || info.senderID != myRig.creator.ActorNumber)
		{
			return;
		}
		MonkeAgent.IncrementRPCCall(info, "OnCountChanged_SharedEvent");
		if (callLimiter.CheckCallTime(Time.time) && args.Length == 2 && args[0] is bool flag && args[1] is int num)
		{
			onCountChangedShared?.Invoke(num);
			if (flag)
			{
				onCountIncreasedShared?.Invoke(num);
			}
			else
			{
				onCountDecreasedShared?.Invoke(num);
			}
			int highestTriggerCount = GetHighestTriggerCount();
			if (num == 0)
			{
				onCountResetToZeroShared?.Invoke();
			}
			else if (highestTriggerCount > 0 && num == highestTriggerCount)
			{
				onReachedMaxTriggerShared?.Invoke();
			}
		}
	}

	private void OnCountReached_SharedEvent(int sender, int target, object[] args, PhotonMessageInfoWrapped info)
	{
		if (sender == target && info.senderID == myRig.creator.ActorNumber)
		{
			MonkeAgent.IncrementRPCCall(info, "OnCountReached_SharedEvent");
			if (callLimiter.CheckCallTime(Time.time) && args.Length == 1 && args[0] is int num && num >= 0 && num < triggers.Count)
			{
				triggers[num].onCountReachedShared?.Invoke();
			}
		}
	}
}
