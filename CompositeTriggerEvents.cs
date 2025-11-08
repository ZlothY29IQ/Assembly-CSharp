using System.Collections.Generic;
using GorillaExtensions;
using UnityEngine;

public class CompositeTriggerEvents : MonoBehaviour
{
	public delegate void TriggerEvent(Collider collider);

	[SerializeField]
	private List<Collider> individualTriggerColliders = new List<Collider>();

	private List<TriggerEventNotifier> triggerEventNotifiers = new List<TriggerEventNotifier>();

	private Dictionary<Collider, int> overlapMask = new Dictionary<Collider, int>();

	private Dictionary<Collider, int> CollderMasks => overlapMask;

	public event TriggerEvent CompositeTriggerEnter;

	public event TriggerEvent CompositeTriggerExit;

	private void Awake()
	{
		if (individualTriggerColliders.Count > 32)
		{
			Debug.LogError("The max number of triggers was exceeded in this composite trigger event sender on GameObject: " + base.gameObject.name + ".");
		}
		for (int i = 0; i < individualTriggerColliders.Count; i++)
		{
			TriggerEventNotifier triggerEventNotifier = individualTriggerColliders[i].gameObject.AddComponent<TriggerEventNotifier>();
			triggerEventNotifier.maskIndex = i;
			triggerEventNotifier.TriggerEnterEvent += TriggerEnterReceiver;
			triggerEventNotifier.TriggerExitEvent += TriggerExitReceiver;
			triggerEventNotifiers.Add(triggerEventNotifier);
		}
	}

	public void AddCollider(Collider colliderToAdd)
	{
		if (individualTriggerColliders.Count >= 32)
		{
			Debug.LogError("The max number of triggers are already present in this composite trigger event sender on GameObject: " + base.gameObject.name + ".");
			return;
		}
		individualTriggerColliders.Add(colliderToAdd);
		TriggerEventNotifier triggerEventNotifier = colliderToAdd.gameObject.AddComponent<TriggerEventNotifier>();
		triggerEventNotifier.maskIndex = GetNextMaskIndex();
		triggerEventNotifier.TriggerEnterEvent += TriggerEnterReceiver;
		triggerEventNotifier.TriggerExitEvent += TriggerExitReceiver;
		triggerEventNotifiers.Add(triggerEventNotifier);
		triggerEventNotifiers.Sort((TriggerEventNotifier a, TriggerEventNotifier b) => a.maskIndex.CompareTo(b.maskIndex));
	}

	public void RemoveCollider(Collider colliderToRemove)
	{
		TriggerEventNotifier component = colliderToRemove.gameObject.GetComponent<TriggerEventNotifier>();
		if (component.IsNotNull())
		{
			foreach (KeyValuePair<Collider, int> item in new Dictionary<Collider, int>(overlapMask))
			{
				TriggerExitReceiver(component, item.Key);
			}
			component.maskIndex = -1;
			component.TriggerEnterEvent -= TriggerEnterReceiver;
			component.TriggerExitEvent -= TriggerExitReceiver;
			triggerEventNotifiers.Remove(component);
		}
		individualTriggerColliders.Remove(colliderToRemove);
	}

	public void ResetColliders(bool sendExitEvent = true)
	{
		individualTriggerColliders.Clear();
		for (int num = triggerEventNotifiers.Count - 1; num >= 0; num--)
		{
			if (triggerEventNotifiers[num].IsNull())
			{
				triggerEventNotifiers.RemoveAt(num);
			}
			else
			{
				triggerEventNotifiers[num].maskIndex = -1;
				triggerEventNotifiers[num].TriggerEnterEvent -= TriggerEnterReceiver;
				triggerEventNotifiers[num].TriggerExitEvent -= TriggerExitReceiver;
				triggerEventNotifiers.RemoveAt(num);
			}
		}
		if (sendExitEvent)
		{
			foreach (KeyValuePair<Collider, int> item in overlapMask)
			{
				this.CompositeTriggerExit?.Invoke(item.Key);
			}
		}
		overlapMask.Clear();
	}

	public int GetNumColliders()
	{
		return individualTriggerColliders.Count;
	}

	public int GetNextMaskIndex()
	{
		if (individualTriggerColliders.Count >= 32)
		{
			Debug.LogError("The max number of triggers are already present in this composite trigger event sender on GameObject: " + base.gameObject.name + ".");
			return -1;
		}
		int num = 0;
		for (int i = 0; i < triggerEventNotifiers.Count && triggerEventNotifiers[i].maskIndex == num; i++)
		{
			num++;
		}
		return num;
	}

	private void OnDestroy()
	{
		for (int i = 0; i < triggerEventNotifiers.Count; i++)
		{
			if (triggerEventNotifiers[i] != null)
			{
				triggerEventNotifiers[i].TriggerEnterEvent -= TriggerEnterReceiver;
				triggerEventNotifiers[i].TriggerExitEvent -= TriggerExitReceiver;
			}
		}
	}

	public void TriggerEnterReceiver(TriggerEventNotifier notifier, Collider other)
	{
		if (overlapMask.TryGetValue(other, out var value))
		{
			value = SetMaskIndexTrue(value, notifier.maskIndex);
			overlapMask[other] = value;
		}
		else
		{
			int value2 = SetMaskIndexTrue(0, notifier.maskIndex);
			overlapMask.Add(other, value2);
			this.CompositeTriggerEnter?.Invoke(other);
		}
	}

	public void TriggerExitReceiver(TriggerEventNotifier notifier, Collider other)
	{
		if (overlapMask.TryGetValue(other, out var value))
		{
			value = SetMaskIndexFalse(value, notifier.maskIndex);
			if (value == 0)
			{
				overlapMask.Remove(other);
				this.CompositeTriggerExit?.Invoke(other);
			}
			else
			{
				overlapMask[other] = value;
			}
		}
	}

	public void ResetColliderMask(Collider other)
	{
		if (overlapMask.TryGetValue(other, out var value))
		{
			if (value != 0)
			{
				this.CompositeTriggerExit?.Invoke(other);
			}
			overlapMask.Remove(other);
		}
	}

	public void CompositeTriggerEnterReceiver(Collider other)
	{
		this.CompositeTriggerEnter?.Invoke(other);
	}

	public void CompositeTriggerExitReceiver(Collider other)
	{
		this.CompositeTriggerExit?.Invoke(other);
	}

	private bool TestMaskIndex(int mask, int index)
	{
		return (mask & (1 << index)) != 0;
	}

	private int SetMaskIndexTrue(int mask, int index)
	{
		return mask | (1 << index);
	}

	private int SetMaskIndexFalse(int mask, int index)
	{
		return mask & ~(1 << index);
	}

	private string MaskToString(int mask)
	{
		string text = "";
		for (int num = 31; num >= 0; num--)
		{
			text += (TestMaskIndex(mask, num) ? "1" : "0");
		}
		return text;
	}
}
