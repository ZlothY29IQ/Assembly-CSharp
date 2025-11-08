using System;
using System.Collections;
using System.Collections.Generic;
using GorillaNetworking;
using UnityEngine;

namespace GameObjectScheduling;

public class GameObjectScheduler : MonoBehaviour
{
	[SerializeField]
	private GameObjectSchedule schedule;

	private GameObject[] scheduledGameObject;

	private GameObjectSchedulerEventDispatcher dispatcher;

	private int currentNodeIndex = -1;

	private Coroutine monitor;

	private void Start()
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
		monitor = StartCoroutine(MonitorTime());
	}

	private void OnEnable()
	{
		if (monitor == null && scheduledGameObject != null)
		{
			monitor = StartCoroutine(MonitorTime());
		}
	}

	private void OnDisable()
	{
		if (monitor != null)
		{
			StopCoroutine(monitor);
		}
		monitor = null;
	}

	private IEnumerator MonitorTime()
	{
		while (GorillaComputer.instance == null || GorillaComputer.instance.startupMillis == 0L)
		{
			yield return null;
		}
		bool previousState = getActiveState();
		for (int i = 0; i < scheduledGameObject.Length; i++)
		{
			scheduledGameObject[i].SetActive(previousState);
		}
		while (true)
		{
			yield return new WaitForSeconds(60f);
			bool activeState = getActiveState();
			if (previousState != activeState)
			{
				changeActiveState(activeState);
				previousState = activeState;
			}
		}
	}

	private bool getActiveState()
	{
		bool flag = false;
		currentNodeIndex = schedule.GetCurrentNodeIndex(getServerTime());
		if (currentNodeIndex == -1)
		{
			return schedule.InitialState;
		}
		if (currentNodeIndex < schedule.Nodes.Length)
		{
			return schedule.Nodes[currentNodeIndex].ActiveState;
		}
		return schedule.Nodes[schedule.Nodes.Length - 1].ActiveState;
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
}
