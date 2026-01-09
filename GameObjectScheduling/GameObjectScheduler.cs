using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GorillaNetworking;
using UnityEngine;

namespace GameObjectScheduling;

public class GameObjectScheduler : MonoBehaviour, IGorillaSliceableSimple
{
	[SerializeField]
	private GameObjectSchedule schedule;

	private GameObject[] scheduledGameObject;

	private GameObjectSchedulerEventDispatcher dispatcher;

	private int currentNodeIndex = -1;

	private bool ready;

	private bool previousState;

	private int lastMinuteCheck = -1;

	public bool useSecondsFidelity;

	public bool debugTime;

	private async void Start()
	{
		schedule.Validate();
		List<GameObject> list = new List<GameObject>();
		for (int i = 0; i < base.transform.childCount; i++)
		{
			list.Add(base.transform.GetChild(i).gameObject);
		}
		scheduledGameObject = list.ToArray();
		for (int j = 0; j < scheduledGameObject.Length; j++)
		{
			scheduledGameObject[j].SetActive(value: false);
		}
		dispatcher = GetComponent<GameObjectSchedulerEventDispatcher>();
		while (GorillaComputer.instance == null || GorillaComputer.instance.startupMillis == 0L)
		{
			await Task.Yield();
		}
		SetInitialState();
		ready = true;
	}

	private void SetInitialState()
	{
		getActiveState(out previousState, out var totalSeconds);
		for (int i = 0; i < scheduledGameObject.Length; i++)
		{
			scheduledGameObject[i].SetActive(previousState);
			if (totalSeconds > 0.0)
			{
				Animator[] componentsInChildren = scheduledGameObject[i].GetComponentsInChildren<Animator>();
				for (int j = 0; j < componentsInChildren.Length; j++)
				{
					int fullPathHash = componentsInChildren[j].GetCurrentAnimatorStateInfo(0).fullPathHash;
					componentsInChildren[j].PlayInFixedTime(fullPathHash, 0, (float)totalSeconds);
				}
			}
		}
		lastMinuteCheck = getServerTime().Minute;
	}

	public void OnEnable()
	{
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
		if (ready)
		{
			SetInitialState();
		}
	}

	public void OnDisable()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	private void getActiveState(out bool state, out double totalSeconds)
	{
		DateTime serverTime = getServerTime();
		currentNodeIndex = schedule.GetCurrentNodeIndex(serverTime, out var _);
		if (currentNodeIndex == -1)
		{
			state = schedule.InitialState;
			totalSeconds = 0.0;
		}
		else if (currentNodeIndex < schedule.Nodes.Length)
		{
			state = schedule.Nodes[currentNodeIndex].ActiveState;
			totalSeconds = (serverTime - schedule.Nodes[currentNodeIndex].DateTime).TotalSeconds;
		}
		else
		{
			state = schedule.Nodes[schedule.Nodes.Length - 1].ActiveState;
			totalSeconds = (serverTime - schedule.Nodes[schedule.Nodes.Length - 1].DateTime).TotalSeconds;
		}
	}

	private DateTime getServerTime()
	{
		return GorillaComputer.instance.GetServerTime();
	}

	private void changeActiveState(bool state)
	{
		if (state)
		{
			for (int i = 0; i < scheduledGameObject.Length; i++)
			{
				scheduledGameObject[i].SetActive(value: true);
			}
			if (dispatcher != null && dispatcher.OnScheduledActivation != null)
			{
				dispatcher.OnScheduledActivation.Invoke();
			}
		}
		else if (dispatcher != null && dispatcher.OnScheduledDeactivation != null)
		{
			dispatcher.OnScheduledActivation.Invoke();
		}
		else
		{
			for (int j = 0; j < scheduledGameObject.Length; j++)
			{
				scheduledGameObject[j].SetActive(value: false);
			}
		}
	}

	public void SliceUpdate()
	{
		if (ready && (useSecondsFidelity || getServerTime().Minute != lastMinuteCheck))
		{
			getActiveState(out var state, out var _);
			if (previousState != state)
			{
				changeActiveState(state);
				previousState = state;
			}
			lastMinuteCheck = getServerTime().Minute;
		}
	}
}
