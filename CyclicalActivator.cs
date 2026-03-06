using System;
using GorillaNetworking;
using UnityEngine;

public class CyclicalActivator : MonoBehaviour, IGorillaSliceableSimple
{
	[Serializable]
	private class CyclicalActivatorObjectScheduleNode
	{
		public Vector2 secondsActiveRange;
	}

	[Serializable]
	private class CyclicalActivatorObjectSchedule
	{
		[Range(10f, 3599f)]
		public int totalSeconds = 60;

		public CyclicalActivatorObjectScheduleNode[] schedule;

		public bool CheckTime(float nowSeconds)
		{
			nowSeconds %= (float)totalSeconds;
			for (int i = 0; i < schedule.Length; i++)
			{
				if (schedule[i].secondsActiveRange.x <= nowSeconds && schedule[i].secondsActiveRange.y > nowSeconds)
				{
					return true;
				}
			}
			return false;
		}
	}

	[Serializable]
	private class CyclicalActivatorObject
	{
		public GameObject gameObject;

		public CyclicalActivatorObjectSchedule schedule;
	}

	[SerializeField]
	private CyclicalActivatorObject[] objects;

	private void OnEnable()
	{
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.LateUpdate);
	}

	private void OnDisable()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.LateUpdate);
	}

	void IGorillaSliceableSimple.SliceUpdate()
	{
		if (!(GorillaComputer.instance == null) && GorillaComputer.instance.GetServerTime().Year >= 2000)
		{
			DateTime serverTime = GorillaComputer.instance.GetServerTime();
			float nowSeconds = (float)(serverTime.Minute * 60) + ((float)serverTime.Second + (float)serverTime.Millisecond * 0.001f);
			for (int i = 0; i < objects.Length; i++)
			{
				objects[i].gameObject.SetActive(objects[i].schedule.CheckTime(nowSeconds));
			}
		}
	}
}
