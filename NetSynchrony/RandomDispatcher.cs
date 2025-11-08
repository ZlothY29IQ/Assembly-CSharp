using System;
using System.Collections.Generic;
using UnityEngine;

namespace NetSynchrony;

[CreateAssetMenu(fileName = "RandomDispatcher", menuName = "NetSynchrony/RandomDispatcher", order = 0)]
public class RandomDispatcher : ScriptableObject
{
	public delegate void RandomDispatcherEvent(RandomDispatcher randomDispatcher);

	[SerializeField]
	private float minWaitTime = 1f;

	[SerializeField]
	private float maxWaitTime = 10f;

	[SerializeField]
	private float totalMinutes = 60f;

	private List<float> dispatchTimes;

	private int index = -1;

	public event RandomDispatcherEvent Dispatch;

	public void Init(double seconds)
	{
		seconds %= (double)(totalMinutes * 60f);
		index = 0;
		dispatchTimes = new List<float>();
		float num = 0f;
		float num2 = totalMinutes * 60f;
		UnityEngine.Random.InitState(StaticHash.Compute(Application.buildGUID));
		while (num < num2)
		{
			float num3 = UnityEngine.Random.Range(minWaitTime, maxWaitTime);
			num += num3;
			if ((double)num < seconds)
			{
				index = dispatchTimes.Count;
			}
			dispatchTimes.Add(num);
		}
		UnityEngine.Random.InitState((int)DateTime.Now.Ticks);
	}

	public void Sync(double seconds)
	{
		seconds %= (double)(totalMinutes * 60f);
		index = 0;
		for (int i = 0; i < dispatchTimes.Count; i++)
		{
			if ((double)dispatchTimes[i] < seconds)
			{
				index = i;
			}
		}
	}

	public void Tick(double seconds)
	{
		seconds %= (double)(totalMinutes * 60f);
		if ((double)dispatchTimes[index] < seconds)
		{
			index = (index + 1) % dispatchTimes.Count;
			if (this.Dispatch != null)
			{
				this.Dispatch(this);
			}
		}
	}
}
