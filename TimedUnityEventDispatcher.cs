using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GorillaNetworking;
using GorillaNetworking.ScheduledEvents;
using PlayFab;
using UnityEngine;
using UnityEngine.Events;

public class TimedUnityEventDispatcher : MonoBehaviour, IGorillaSliceableSimple
{
	private enum TimedUnityEventDispatcherMode
	{
		FIXED,
		TITLE_DATA,
		SCHEDULED_EVENT
	}

	private enum ReadyState
	{
		None,
		Initializing,
		Ready,
		Crashed
	}

	[Serializable]
	private class TimedUnityEventDispatcherNode : IComparable<TimedUnityEventDispatcherNode>
	{
		[SerializeField]
		private int subphase;

		[SerializeField]
		private bool afterEvent;

		[SerializeField]
		private int hrs;

		[SerializeField]
		private int min;

		[SerializeField]
		private int sec;

		[SerializeField]
		private UnityEvent payload;

		[SerializeField]
		private UnityEvent<float> persistantPayload;

		public int SubphaseOrder => subphase;

		public DateTime ActivationTime { get; private set; }

		public TimeSpan ActivationDelay { get; private set; }

		public void Initialize(DateTime refTime)
		{
			Initialize();
			ActivationTime = refTime + ActivationDelay;
		}

		public void Initialize()
		{
			ActivationDelay = new TimeSpan(hrs, min, sec);
			if (afterEvent)
			{
				subphase = int.MaxValue;
			}
		}

		public void Activate(DateTime now)
		{
			float late = (float)(now - ActivationTime).TotalSeconds;
			Activate(late);
		}

		public void Activate()
		{
			Activate(0f);
		}

		private void Activate(float late)
		{
			if (late < 1f)
			{
				payload?.Invoke();
			}
			persistantPayload?.Invoke(late);
		}

		public void ActivatePersistent(float late)
		{
			persistantPayload?.Invoke(late);
		}

		int IComparable<TimedUnityEventDispatcherNode>.CompareTo(TimedUnityEventDispatcherNode other)
		{
			if (SubphaseOrder == other.SubphaseOrder)
			{
				return (hrs * 3600 + min * 60 + sec).CompareTo(other.hrs * 3600 + other.min * 60 + other.sec);
			}
			return SubphaseOrder.CompareTo(other.SubphaseOrder);
		}
	}

	[SerializeField]
	private TimedUnityEventDispatcherMode mode;

	[SerializeField]
	private string dateTime;

	[SerializeField]
	private string titleDataKey;

	[SerializeField]
	private TimedUnityEventDispatcherNode[] nodes;

	private ReadyState readyState;

	private List<TimedUnityEventDispatcherNode> nodeList = new List<TimedUnityEventDispatcherNode>();

	private int activeNodeIndex;

	private async void Initialize()
	{
		if (readyState == ReadyState.Initializing || readyState == ReadyState.Ready)
		{
			return;
		}
		readyState = ReadyState.Initializing;
		switch (mode)
		{
		case TimedUnityEventDispatcherMode.FIXED:
			onDateRetrieved(dateTime);
			break;
		case TimedUnityEventDispatcherMode.TITLE_DATA:
			while (PlayFabTitleDataCache.Instance == null)
			{
				await Task.Yield();
			}
			PlayFabTitleDataCache.Instance.GetTitleData(titleDataKey, onDateRetrieved, onTDError);
			break;
		case TimedUnityEventDispatcherMode.SCHEDULED_EVENT:
		{
			nodeList.Clear();
			for (int i = 0; i < nodes.Length; i++)
			{
				nodes[i].Initialize();
				nodeList.Add(nodes[i]);
			}
			nodeList.Sort();
			activeNodeIndex = 0;
			readyState = ReadyState.Ready;
			break;
		}
		}
	}

	private void onDateRetrieved(string s)
	{
		try
		{
			setStartDate(DateTime.Parse(s));
			readyState = ReadyState.Ready;
		}
		catch (Exception ex)
		{
			Debug.LogError("TimedUnityEventDispatcher :: onDateRetrieved :: " + ex.Message + " :: " + ex.StackTrace);
			readyState = ReadyState.Crashed;
		}
	}

	public void StartNow(float delay)
	{
		throw new Exception("Oops! Function not available in production builds.");
	}

	private void setStartDate(DateTime d)
	{
		nodeList.Clear();
		for (int i = 0; i < nodes.Length; i++)
		{
			nodes[i].Initialize(d);
			nodeList.Add(nodes[i]);
		}
		nodeList.Sort();
		activeNodeIndex = 0;
	}

	private void onTDError(PlayFabError error)
	{
		Debug.LogError($"TitleDataDateRefActivation :: onTDError :: {error}");
		readyState = ReadyState.Crashed;
	}

	private void OnEnable()
	{
		Initialize();
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.LateUpdate);
	}

	private void OnDisable()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.LateUpdate);
	}

	void IGorillaSliceableSimple.SliceUpdate()
	{
		if (readyState != ReadyState.Ready)
		{
			return;
		}
		switch (mode)
		{
		case TimedUnityEventDispatcherMode.FIXED:
		case TimedUnityEventDispatcherMode.TITLE_DATA:
			if (activeNodeIndex < nodeList.Count)
			{
				DateTime serverTime = GorillaComputer.instance.GetServerTime();
				if (serverTime.Year >= 2000 && nodeList[activeNodeIndex].ActivationTime <= serverTime)
				{
					nodeList[activeNodeIndex].Activate(serverTime);
					activeNodeIndex++;
				}
			}
			break;
		case TimedUnityEventDispatcherMode.SCHEDULED_EVENT:
		{
			ScheduledEventManager instance = ScheduledEventManager.Instance;
			if (activeNodeIndex >= nodeList.Count || !instance.DataReady)
			{
				break;
			}
			ScheduledEventPhase currentPhase = instance.CurrentPhase;
			if (currentPhase != ScheduledEventPhase.During && currentPhase != ScheduledEventPhase.After)
			{
				break;
			}
			int num = ((currentPhase == ScheduledEventPhase.After) ? int.MaxValue : instance.EventSubphase);
			TimeSpan timeSpan = instance.EventSubphaseStartTime - instance.PreviousEventSubphaseStartTime;
			while (activeNodeIndex < nodeList.Count && nodeList[activeNodeIndex].SubphaseOrder < num)
			{
				TimedUnityEventDispatcherNode timedUnityEventDispatcherNode = nodeList[activeNodeIndex];
				timedUnityEventDispatcherNode.ActivatePersistent((float)(timeSpan - timedUnityEventDispatcherNode.ActivationDelay).TotalSeconds);
				activeNodeIndex++;
			}
			if (activeNodeIndex < nodeList.Count)
			{
				TimedUnityEventDispatcherNode timedUnityEventDispatcherNode2 = nodeList[activeNodeIndex];
				if (timedUnityEventDispatcherNode2.SubphaseOrder == num && timedUnityEventDispatcherNode2.ActivationDelay <= DateTime.Now - instance.EventSubphaseStartTime)
				{
					timedUnityEventDispatcherNode2.Activate();
					activeNodeIndex++;
				}
			}
			break;
		}
		}
	}
}
