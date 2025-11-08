using System;
using System.Globalization;
using GameObjectScheduling;
using UnityEngine;

[CreateAssetMenu(fileName = "New Game Object Schedule Generator", menuName = "Game Object Scheduling/Game Object Schedule Generator")]
public class GameObjectScheduleGenerator : ScriptableObject
{
	private enum ScheduleType
	{
		DailyShuffle
	}

	[SerializeField]
	private GameObjectSchedule[] schedules;

	[SerializeField]
	private string scheduleStart = "1/1/0001 00:00:00";

	[SerializeField]
	private string scheduleEnd = "1/1/0001 00:00:00";

	[SerializeField]
	private ScheduleType scheduleType;

	private void GenerateSchedule()
	{
		DateTime startDate;
		try
		{
			startDate = DateTime.Parse(scheduleStart, CultureInfo.InvariantCulture);
		}
		catch
		{
			Debug.LogError("Don't understand Start Date " + scheduleStart);
			return;
		}
		DateTime endDate;
		try
		{
			endDate = DateTime.Parse(scheduleEnd, CultureInfo.InvariantCulture);
		}
		catch
		{
			Debug.LogError("Don't understand End Date " + scheduleEnd);
			return;
		}
		if (scheduleType == ScheduleType.DailyShuffle)
		{
			GameObjectSchedule.GenerateDailyShuffle(startDate, endDate, schedules);
		}
	}
}
