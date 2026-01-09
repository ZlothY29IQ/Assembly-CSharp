using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace GameObjectScheduling;

[CreateAssetMenu(fileName = "New Game Object Schedule", menuName = "Game Object Scheduling/Game Object Schedule", order = 0)]
public class GameObjectSchedule : ScriptableObject
{
	[Serializable]
	public class GameObjectScheduleNode
	{
		[SerializeField]
		public string activeDateTime = "1/1/0001 00:00:00";

		[SerializeField]
		[Tooltip("Check to turn on. Uncheck to turn off.")]
		public bool activeState = true;

		private DateTime dateTime;

		public bool ActiveState => activeState;

		public DateTime DateTime => dateTime;

		public void Validate()
		{
			try
			{
				dateTime = DateTime.Parse(activeDateTime, CultureInfo.InvariantCulture);
			}
			catch
			{
				dateTime = DateTime.MinValue;
			}
		}
	}

	[SerializeField]
	private bool initialState;

	[SerializeField]
	private GameObjectScheduleNode[] nodes;

	[SerializeField]
	private SchedulingOptions options;

	private bool validated;

	public GameObjectScheduleNode[] Nodes => nodes;

	public bool InitialState => initialState;

	public int GetCurrentNodeIndex(DateTime currentDate, out DateTime startDate)
	{
		int i = -1;
		startDate = default(DateTime);
		for (; i < nodes.Length - 1; i++)
		{
			if (currentDate < nodes[i + 1].DateTime)
			{
				if (i >= 0)
				{
					startDate = nodes[i].DateTime;
				}
				return i;
			}
		}
		return int.MaxValue;
	}

	public void Validate()
	{
		if (!validated)
		{
			_validate();
			validated = true;
		}
	}

	private void _validate()
	{
		for (int i = 0; i < nodes.Length; i++)
		{
			nodes[i].Validate();
		}
		List<GameObjectScheduleNode> list = new List<GameObjectScheduleNode>(nodes);
		list.Sort((GameObjectScheduleNode e1, GameObjectScheduleNode e2) => e1.DateTime.CompareTo(e2.DateTime));
		nodes = list.ToArray();
	}

	public static void GenerateDailyShuffle(DateTime startDate, DateTime endDate, GameObjectSchedule[] schedules)
	{
		TimeSpan timeSpan = TimeSpan.FromDays(1.0);
		int num = schedules.Length - 1;
		int num2 = schedules.Length - 2;
		DateTime dateTime = startDate;
		List<GameObjectScheduleNode>[] array = new List<GameObjectScheduleNode>[schedules.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = new List<GameObjectScheduleNode>();
		}
		while (dateTime < endDate)
		{
			int num3 = UnityEngine.Random.Range(0, schedules.Length - 2);
			if (num <= num3)
			{
				num3++;
				if (num2 <= num3)
				{
					num3++;
				}
			}
			else if (num2 <= num3)
			{
				num3++;
				if (num <= num3)
				{
					num3++;
				}
			}
			array[num].Add(new GameObjectScheduleNode
			{
				activeDateTime = dateTime.ToString(),
				activeState = false
			});
			array[num3].Add(new GameObjectScheduleNode
			{
				activeDateTime = dateTime.ToString(),
				activeState = true
			});
			dateTime += timeSpan;
			num2 = num;
			num = num3;
		}
		array[num].Add(new GameObjectScheduleNode
		{
			activeDateTime = dateTime.ToString(),
			activeState = false
		});
		for (int j = 0; j < array.Length; j++)
		{
			schedules[j].nodes = array[j].ToArray();
		}
	}
}
