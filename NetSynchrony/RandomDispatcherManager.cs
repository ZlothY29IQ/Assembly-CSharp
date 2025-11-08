using System;
using GorillaNetworking;
using UnityEngine;

namespace NetSynchrony;

public class RandomDispatcherManager : MonoBehaviour
{
	[SerializeField]
	private RandomDispatcher[] randomDispatchers;

	private static RandomDispatcherManager __instance;

	private double serverTime;

	private void OnDisable()
	{
		if (!ApplicationQuittingState.IsQuitting && GorillaComputer.instance != null)
		{
			GorillaComputer instance = GorillaComputer.instance;
			instance.OnServerTimeUpdated = (Action)Delegate.Remove(instance.OnServerTimeUpdated, new Action(OnTimeChanged));
		}
	}

	private void OnTimeChanged()
	{
		AdjustedServerTime();
		for (int i = 0; i < randomDispatchers.Length; i++)
		{
			randomDispatchers[i].Sync(serverTime);
		}
	}

	private void AdjustedServerTime()
	{
		DateTime dateTime = new DateTime(2020, 1, 1);
		long num = GorillaComputer.instance.GetServerTime().Ticks - dateTime.Ticks;
		serverTime = (float)num / 10000000f;
	}

	private void Start()
	{
		GorillaComputer instance = GorillaComputer.instance;
		instance.OnServerTimeUpdated = (Action)Delegate.Combine(instance.OnServerTimeUpdated, new Action(OnTimeChanged));
		for (int i = 0; i < randomDispatchers.Length; i++)
		{
			randomDispatchers[i].Init(serverTime);
		}
	}

	private void Update()
	{
		for (int i = 0; i < randomDispatchers.Length; i++)
		{
			randomDispatchers[i].Tick(serverTime);
		}
		serverTime += Time.deltaTime;
	}
}
