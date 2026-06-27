using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace GorillaNetworking;

public class SubCosmeticSignalReceiver : MonoBehaviour
{
	[Serializable]
	public class SignalTrigger
	{
		[Tooltip("Integer signal value that fires this trigger")]
		public int signal;

		[Tooltip("Events invoked when this signal is received")]
		public UnityEvent onSignalReceived;

		[Tooltip("Fire only once per session, ignoring subsequent broadcasts of the same signal")]
		public bool triggerOnce;

		[NonSerialized]
		public bool hasTriggered;
	}

	[Header("Signal Triggers")]
	[SerializeField]
	private List<SignalTrigger> triggers = new List<SignalTrigger>();

	[Header("Catch-All")]
	[Tooltip("[Optional] event fired for every received signal regardless of value, after any specific triggers above run")]
	public UnityEvent<int> onAnySignal;

	public void ReceiveSignal(int signal)
	{
		for (int i = 0; i < triggers.Count; i++)
		{
			SignalTrigger signalTrigger = triggers[i];
			if (signalTrigger != null && signalTrigger.signal == signal && (!signalTrigger.triggerOnce || !signalTrigger.hasTriggered))
			{
				signalTrigger.onSignalReceived?.Invoke();
				if (signalTrigger.triggerOnce)
				{
					signalTrigger.hasTriggered = true;
				}
			}
		}
		onAnySignal?.Invoke(signal);
	}

	[Tooltip("Resets all 'triggerOnce' flags so single-fire triggers can fire again.")]
	public void ResetTriggers()
	{
		for (int i = 0; i < triggers.Count; i++)
		{
			if (triggers[i] != null)
			{
				triggers[i].hasTriggered = false;
			}
		}
	}
}
