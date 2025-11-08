using System;
using GorillaTag;
using UnityEngine;

public class LocalActivateOnDateRange : MonoBehaviour
{
	[Header("Activation Date and Time (UTC)")]
	public int activationYear = 2023;

	public int activationMonth = 4;

	public int activationDay = 1;

	public int activationHour = 7;

	public int activationMinute;

	public int activationSecond;

	[Header("Deactivation Date and Time (UTC)")]
	public int deactivationYear = 2023;

	public int deactivationMonth = 4;

	public int deactivationDay = 2;

	public int deactivationHour = 7;

	public int deactivationMinute;

	public int deactivationSecond;

	public GameObject[] gameObjectsToActivate;

	private bool isActive;

	private DateTime activationTime;

	private DateTime deactivationTime;

	[DebugReadout]
	public double dbgTimeUntilActivation;

	[DebugReadout]
	public double dbgTimeUntilDeactivation;

	private void Awake()
	{
		GameObject[] array = gameObjectsToActivate;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetActive(value: false);
		}
	}

	private void OnEnable()
	{
		InitActiveTimes();
	}

	private void InitActiveTimes()
	{
		activationTime = new DateTime(activationYear, activationMonth, activationDay, activationHour, activationMinute, activationSecond, DateTimeKind.Utc);
		deactivationTime = new DateTime(deactivationYear, deactivationMonth, deactivationDay, deactivationHour, deactivationMinute, deactivationSecond, DateTimeKind.Utc);
	}

	private void LateUpdate()
	{
		DateTime utcNow = DateTime.UtcNow;
		dbgTimeUntilActivation = (activationTime - utcNow).TotalSeconds;
		dbgTimeUntilDeactivation = (deactivationTime - utcNow).TotalSeconds;
		bool flag = utcNow >= activationTime && utcNow <= deactivationTime;
		if (flag != isActive)
		{
			GameObject[] array = gameObjectsToActivate;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].SetActive(flag);
			}
			isActive = flag;
		}
	}
}
