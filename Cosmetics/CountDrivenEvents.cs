using System;
using System.Collections.Generic;
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

		[Tooltip("Should this trigger fire every time the count passes through this value, or only once?")]
		public bool triggerOnce;

		[NonSerialized]
		public bool hasTriggered;
	}

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

	[Header("General Events")]
	public UnityEvent<int> onCountChanged;

	public UnityEvent<int> onCountIncreased;

	public UnityEvent<int> onCountDecreased;

	public UnityEvent onCountResetToZero;

	public UnityEvent onReachedMaxTrigger;

	[Header("Debug - Counter Settings")]
	[SerializeField]
	private int currentCount;

	public int CurrentCount => currentCount;

	private void OnEnable()
	{
		if (evaluateOnEnable)
		{
			CheckTriggers(currentCount, currentCount);
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
			currentCount = newCount;
			onCountChanged?.Invoke(currentCount);
			if (currentCount > num)
			{
				onCountIncreased?.Invoke(currentCount);
			}
			else if (currentCount < num)
			{
				onCountDecreased?.Invoke(currentCount);
			}
			CheckTriggers(num, currentCount);
			if (currentCount == 0)
			{
				onCountResetToZero?.Invoke();
			}
			int highestTriggerCount2 = GetHighestTriggerCount();
			if (highestTriggerCount2 > 0 && currentCount == highestTriggerCount2)
			{
				onReachedMaxTrigger?.Invoke();
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
				if (countTrigger.triggerOnce)
				{
					countTrigger.hasTriggered = true;
				}
			}
		}
	}
}
