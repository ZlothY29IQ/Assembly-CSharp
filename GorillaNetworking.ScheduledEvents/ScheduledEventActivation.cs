using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace GorillaNetworking.ScheduledEvents;

public class ScheduledEventActivation : MonoBehaviour
{
	[Serializable]
	private class ScheduledEventActivationTarget
	{
		[SerializeField]
		private GameObject gameObject;

		[SerializeField]
		private bool enableBefore;

		[SerializeField]
		private bool enableDuring;

		[SerializeField]
		private bool enableAfter;

		[SerializeField]
		private bool enableIfNoEvent;

		[Tooltip("Subphases (ScheduledEventManager.EventSubphase) in which this object is active during the event. Leave empty to be active for the entire During phase regardless of subphase.")]
		[SerializeField]
		private int[] duringSubphases;

		[SerializeField]
		private UnityEvent onActivate;

		[SerializeField]
		private UnityEvent onDeactivate;

		public bool MatchesPhase(ScheduledEventPhase phase)
		{
			return phase switch
			{
				ScheduledEventPhase.Before => enableBefore, 
				ScheduledEventPhase.During => enableDuring, 
				ScheduledEventPhase.After => enableAfter, 
				ScheduledEventPhase.NoEvent => enableIfNoEvent, 
				_ => false, 
			};
		}

		public bool IsActive(ScheduledEventPhase phase, int subphase)
		{
			if (!MatchesPhase(phase))
			{
				return false;
			}
			if (phase == ScheduledEventPhase.During && duringSubphases != null && duringSubphases.Length != 0)
			{
				return Array.IndexOf(duringSubphases, subphase) >= 0;
			}
			return true;
		}

		public void Apply(ScheduledEventPhase phase, int subphase)
		{
			if (gameObject == null)
			{
				return;
			}
			bool flag = IsActive(phase, subphase);
			if (gameObject.activeSelf != flag)
			{
				gameObject.SetActive(flag);
				if (flag)
				{
					onActivate?.Invoke();
				}
				else
				{
					onDeactivate?.Invoke();
				}
			}
		}
	}

	[SerializeField]
	private ScheduledEventActivationTarget[] nodes;

	private bool subscribed;

	private ScheduledEventPhase currentPhase = ScheduledEventPhase.None;

	private int currentSubphase = -1;

	private void OnEnable()
	{
		if (ScheduledEventManager.Instance == null)
		{
			StartCoroutine(SubscribeWhenReady());
		}
		else
		{
			Subscribe();
		}
	}

	private IEnumerator SubscribeWhenReady()
	{
		while (ScheduledEventManager.Instance == null)
		{
			yield return null;
		}
		if (base.isActiveAndEnabled)
		{
			Subscribe();
		}
	}

	private void Subscribe()
	{
		if (!subscribed)
		{
			subscribed = true;
			ScheduledEventManager.Instance.OnPhaseChanged += OnPhaseChanged;
			ScheduledEventManager.Instance.OnSubphaseChanged += OnSubphaseChanged;
			currentPhase = ScheduledEventManager.Instance.CurrentPhase;
			currentSubphase = ScheduledEventManager.Instance.EventSubphase;
			ApplyAll();
		}
	}

	private void OnDisable()
	{
		if (subscribed && ScheduledEventManager.Instance != null)
		{
			ScheduledEventManager.Instance.OnPhaseChanged -= OnPhaseChanged;
			ScheduledEventManager.Instance.OnSubphaseChanged -= OnSubphaseChanged;
		}
		subscribed = false;
	}

	private void OnPhaseChanged(ScheduledEventPhase phase)
	{
		currentPhase = phase;
		ApplyAll();
	}

	private void OnSubphaseChanged(int subphase)
	{
		currentSubphase = subphase;
		if (currentPhase == ScheduledEventPhase.During)
		{
			ApplyAll();
		}
	}

	private void ApplyAll()
	{
		if (nodes != null)
		{
			for (int i = 0; i < nodes.Length; i++)
			{
				nodes[i].Apply(currentPhase, currentSubphase);
			}
		}
	}
}
