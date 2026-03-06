using System;
using System.Collections.Generic;
using GorillaNetworking;
using Newtonsoft.Json;
using PlayFab;
using UnityEngine;

public class RotatingQuestsManager : MonoBehaviour, ITickSystemTick, GorillaQuestManager
{
	[Serializable]
	public class RotatingQuestGroup
	{
		public int selectCount;

		public string name;

		public List<RotatingQuest> quests;
	}

	[Serializable]
	public class RotatingQuestList
	{
		public List<RotatingQuestGroup> DailyQuests;

		public List<RotatingQuestGroup> WeeklyQuests;

		public void Init()
		{
			SetIsDaily(DailyQuests, isDaily: true);
			SetIsDaily(WeeklyQuests, isDaily: false);
			static void SetIsDaily(List<RotatingQuestGroup> questList, bool isDaily)
			{
				foreach (RotatingQuestGroup quest in questList)
				{
					foreach (RotatingQuest quest2 in quest.quests)
					{
						quest2.isDailyQuest = isDaily;
					}
				}
			}
		}

		public RotatingQuest GetQuest(int questID)
		{
			RotatingQuest rotatingQuest = null;
			rotatingQuest = GetQuestFrom(DailyQuests);
			if (rotatingQuest == null)
			{
				rotatingQuest = GetQuestFrom(WeeklyQuests);
			}
			return rotatingQuest;
			RotatingQuest GetQuestFrom(List<RotatingQuestGroup> list)
			{
				foreach (RotatingQuestGroup item in list)
				{
					foreach (RotatingQuest quest in item.quests)
					{
						if (quest.questID == questID)
						{
							return quest;
						}
					}
				}
				return null;
			}
		}
	}

	private bool hasQuest;

	[SerializeField]
	private bool useTestLocalQuests;

	[SerializeField]
	private string localQuestPath = "TestingRotatingQuests";

	public static int LastQuestChange;

	public static int LastQuestDailyID;

	public RotatingQuestList quests;

	public int dailyQuestSetID;

	public int weeklyQuestSetID;

	[SerializeField]
	private bool _playQuestSounds;

	private AudioSource _questAudio;

	private DateTime nextQuestUpdateTime;

	private const string kDailyQuestSetIDKey = "Rotating_Quest_Daily_SetID_Key";

	private const string kDailyQuestSaveCountKey = "Rotating_Quest_Daily_SaveCount_Key";

	private const string kDailyQuestIDKey = "Rotating_Quest_Daily_ID_Key";

	private const string kDailyQuestProgressKey = "Rotating_Quest_Daily_Progress_Key";

	private const string kWeeklyQuestSetIDKey = "Rotating_Quest_Weekly_SetID_Key";

	private const string kWeeklyQuestSaveCountKey = "Rotating_Quest_Weekly_SaveCount_Key";

	private const string kWeeklyQuestIDKey = "Rotating_Quest_Weekly_ID_Key";

	private const string kWeeklyQuestProgressKey = "Rotating_Quest_Weekly_Progress_Key";

	public bool TickRunning { get; set; }

	public DateTime DailyQuestCountdown { get; private set; }

	public DateTime WeeklyQuestCountdown { get; private set; }

	private void Start()
	{
		_questAudio = GetComponent<AudioSource>();
		RequestQuestsFromTitleData();
	}

	private void OnEnable()
	{
		TickSystem<object>.AddTickCallback(this);
	}

	private void OnDisable()
	{
		TickSystem<object>.RemoveTickCallback(this);
	}

	public void Tick()
	{
		if (hasQuest && nextQuestUpdateTime < DateTime.UtcNow)
		{
			SetupQuests();
		}
	}

	private void ProcessAllQuests(Action<RotatingQuest> action)
	{
		ProcessAllQuestsInList(quests.DailyQuests);
		ProcessAllQuestsInList(quests.WeeklyQuests);
		void ProcessAllQuestsInList(List<RotatingQuestGroup> questGroups)
		{
			foreach (RotatingQuestGroup questGroup in questGroups)
			{
				foreach (RotatingQuest quest in questGroup.quests)
				{
					action(quest);
				}
			}
		}
	}

	private void QuestLoadPostProcess(RotatingQuest quest)
	{
		if (quest.requiredZones.Count == 1 && quest.requiredZones[0] == GTZone.none)
		{
			quest.requiredZones.Clear();
		}
	}

	private void QuestSavePreProcess(RotatingQuest quest)
	{
		if (quest.requiredZones.Count == 0)
		{
			quest.requiredZones.Add(GTZone.none);
		}
	}

	public void LoadTestQuestsFromFile()
	{
		TextAsset textAsset = Resources.Load<TextAsset>(localQuestPath);
		LoadQuestsFromJson(textAsset.text);
	}

	public void RequestQuestsFromTitleData()
	{
		PlayFabTitleDataCache.Instance.GetTitleData("AllActiveQuests", delegate(string data)
		{
			LoadQuestsFromJson(data);
		}, delegate(PlayFabError e)
		{
			Debug.LogError($"Error getting AllActiveQuests data: {e}");
		});
	}

	public void LoadQuestsFromJson(string jsonString)
	{
		quests = JsonConvert.DeserializeObject<RotatingQuestList>(jsonString);
		ProcessAllQuests(QuestLoadPostProcess);
		if (quests == null)
		{
			Debug.LogError("Error: Quests failed to parse!");
			return;
		}
		hasQuest = true;
		quests.Init();
		if (Application.isPlaying)
		{
			SetupQuests();
		}
	}

	private void SetupQuests()
	{
		ClearAllQuestEventListeners();
		SelectActiveQuests();
		LoadQuestProgress();
		HandleQuestProgressChanged(initialLoad: true);
		SetupAllQuestEventListeners();
		nextQuestUpdateTime = DailyQuestCountdown;
		nextQuestUpdateTime = nextQuestUpdateTime.AddMinutes(1.0);
	}

	private void SelectActiveQuests()
	{
		DateTime dateTime = new DateTime(2025, 1, 10, 18, 0, 0, DateTimeKind.Utc);
		TimeSpan timeSpan = TimeSpan.FromHours(-8.0);
		DateTime dateStart = new DateTime(1, 1, 1, 0, 0, 0);
		DateTime dateEnd = new DateTime(2006, 12, 31, 0, 0, 0);
		TimeSpan daylightDelta = TimeSpan.FromHours(1.0);
		TimeZoneInfo.TransitionTime daylightTransitionStart = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 2, 0, 0), 4, 1, DayOfWeek.Sunday);
		TimeZoneInfo.TransitionTime daylightTransitionEnd = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 2, 0, 0), 10, 5, DayOfWeek.Sunday);
		DateTime dateStart2 = new DateTime(2007, 1, 1, 0, 0, 0);
		DateTime dateEnd2 = new DateTime(9999, 12, 31, 0, 0, 0);
		TimeSpan daylightDelta2 = TimeSpan.FromHours(1.0);
		TimeZoneInfo.TransitionTime daylightTransitionStart2 = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 2, 0, 0), 3, 2, DayOfWeek.Sunday);
		TimeZoneInfo.TransitionTime daylightTransitionEnd2 = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 2, 0, 0), 11, 1, DayOfWeek.Sunday);
		TimeZoneInfo timeZoneInfo = TimeZoneInfo.CreateCustomTimeZone("Pacific Standard Time", timeSpan, "Pacific Standard Time", "Pacific Standard Time", "Pacific Standard Time", new TimeZoneInfo.AdjustmentRule[2]
		{
			TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(dateStart, dateEnd, daylightDelta, daylightTransitionStart, daylightTransitionEnd),
			TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(dateStart2, dateEnd2, daylightDelta2, daylightTransitionStart2, daylightTransitionEnd2)
		});
		if (timeZoneInfo != null && timeZoneInfo.IsDaylightSavingTime(DateTime.UtcNow - timeSpan))
		{
			dateTime -= TimeSpan.FromHours(1.0);
		}
		TimeSpan timeSpan2 = DateTime.UtcNow - dateTime;
		RemoveDisabledQuests();
		weeklyQuestSetID = (dailyQuestSetID = timeSpan2.Days) / 7;
		LastQuestDailyID = dailyQuestSetID;
		DailyQuestCountdown = dateTime + TimeSpan.FromDays(dailyQuestSetID + 1);
		WeeklyQuestCountdown = dateTime + TimeSpan.FromDays((weeklyQuestSetID + 1) * 7);
		UnityEngine.Random.InitState(dailyQuestSetID);
		foreach (RotatingQuestGroup dailyQuest in quests.DailyQuests)
		{
			int num = Math.Min(dailyQuest.selectCount, dailyQuest.quests.Count);
			float num2 = 0f;
			List<(int, float)> list = new List<(int, float)>(dailyQuest.quests.Count);
			for (int i = 0; i < dailyQuest.quests.Count; i++)
			{
				dailyQuest.quests[i].isQuestActive = false;
				num2 += dailyQuest.quests[i].weight;
				list.Add((i, dailyQuest.quests[i].weight));
			}
			for (int j = 0; j < num; j++)
			{
				float num3 = UnityEngine.Random.Range(0f, num2);
				for (int k = 0; k < list.Count; k++)
				{
					float item = list[k].Item2;
					if (num3 <= item || k == list.Count - 1)
					{
						num2 -= item;
						int item2 = list[k].Item1;
						list.RemoveAt(k);
						dailyQuest.quests[item2].isQuestActive = true;
						dailyQuest.quests[item2].SetRequiredZone();
						break;
					}
					num3 -= item;
				}
			}
		}
		UnityEngine.Random.InitState(weeklyQuestSetID);
		foreach (RotatingQuestGroup weeklyQuest in quests.WeeklyQuests)
		{
			int num4 = Math.Min(weeklyQuest.selectCount, weeklyQuest.quests.Count);
			float num5 = 0f;
			List<(int, float)> list2 = new List<(int, float)>(weeklyQuest.quests.Count);
			for (int l = 0; l < weeklyQuest.quests.Count; l++)
			{
				weeklyQuest.quests[l].isQuestActive = false;
				num5 += weeklyQuest.quests[l].weight;
				list2.Add((l, weeklyQuest.quests[l].weight));
			}
			for (int m = 0; m < num4; m++)
			{
				float num6 = UnityEngine.Random.Range(0f, num5);
				for (int n = 0; n < list2.Count; n++)
				{
					float item3 = list2[n].Item2;
					if (num6 <= item3 || n == list2.Count - 1)
					{
						num5 -= item3;
						int item4 = list2[n].Item1;
						list2.RemoveAt(n);
						weeklyQuest.quests[item4].isQuestActive = true;
						weeklyQuest.quests[item4].SetRequiredZone();
						break;
					}
					num6 -= item3;
				}
			}
		}
		ProgressionController.ReportQuestSelectionChanged();
	}

	private void RemoveDisabledQuests()
	{
		RemoveDisabledQuestsFromGroupList(quests.DailyQuests);
		RemoveDisabledQuestsFromGroupList(quests.WeeklyQuests);
		static void RemoveDisabledQuestsFromGroupList(List<RotatingQuestGroup> questList)
		{
			foreach (RotatingQuestGroup quest in questList)
			{
				for (int num = quest.quests.Count - 1; num >= 0; num--)
				{
					if (quest.quests[num].disable)
					{
						quest.quests.RemoveAt(num);
					}
				}
			}
		}
	}

	public void LoadQuestProgress()
	{
		int num = PlayerPrefs.GetInt("Rotating_Quest_Daily_SetID_Key", -1);
		int num2 = PlayerPrefs.GetInt("Rotating_Quest_Daily_SaveCount_Key", -1);
		if (num == dailyQuestSetID)
		{
			for (int i = 0; i < num2; i++)
			{
				int num3 = PlayerPrefs.GetInt(string.Format("{0}{1}", "Rotating_Quest_Daily_ID_Key", i), -1);
				int progress = PlayerPrefs.GetInt(string.Format("{0}{1}", "Rotating_Quest_Daily_Progress_Key", i), -1);
				if (num3 == -1)
				{
					continue;
				}
				for (int j = 0; j < quests.DailyQuests.Count; j++)
				{
					for (int k = 0; k < quests.DailyQuests[j].quests.Count; k++)
					{
						RotatingQuest rotatingQuest = quests.DailyQuests[j].quests[k];
						if (rotatingQuest.questID == num3)
						{
							rotatingQuest.ApplySavedProgress(progress);
							break;
						}
					}
				}
			}
		}
		int num4 = PlayerPrefs.GetInt("Rotating_Quest_Weekly_SetID_Key", -1);
		int num5 = PlayerPrefs.GetInt("Rotating_Quest_Weekly_SaveCount_Key", -1);
		if (num4 != weeklyQuestSetID)
		{
			return;
		}
		for (int l = 0; l < num5; l++)
		{
			int num6 = PlayerPrefs.GetInt(string.Format("{0}{1}", "Rotating_Quest_Weekly_ID_Key", l), -1);
			int progress2 = PlayerPrefs.GetInt(string.Format("{0}{1}", "Rotating_Quest_Weekly_Progress_Key", l), -1);
			if (num6 == -1)
			{
				continue;
			}
			for (int m = 0; m < quests.WeeklyQuests.Count; m++)
			{
				for (int n = 0; n < quests.WeeklyQuests[m].quests.Count; n++)
				{
					RotatingQuest rotatingQuest2 = quests.WeeklyQuests[m].quests[n];
					if (rotatingQuest2.questID == num6)
					{
						rotatingQuest2.ApplySavedProgress(progress2);
						break;
					}
				}
			}
		}
	}

	public void SaveQuestProgress()
	{
		int num = 0;
		for (int i = 0; i < quests.DailyQuests.Count; i++)
		{
			for (int j = 0; j < quests.DailyQuests[i].quests.Count; j++)
			{
				RotatingQuest rotatingQuest = quests.DailyQuests[i].quests[j];
				int progress = rotatingQuest.GetProgress();
				if (progress > 0)
				{
					PlayerPrefs.SetInt(string.Format("{0}{1}", "Rotating_Quest_Daily_ID_Key", num), rotatingQuest.questID);
					PlayerPrefs.SetInt(string.Format("{0}{1}", "Rotating_Quest_Daily_Progress_Key", num), progress);
					num++;
				}
			}
		}
		if (num > 0)
		{
			PlayerPrefs.SetInt("Rotating_Quest_Daily_SetID_Key", dailyQuestSetID);
			PlayerPrefs.SetInt("Rotating_Quest_Daily_SaveCount_Key", num);
		}
		int num2 = 0;
		for (int k = 0; k < quests.WeeklyQuests.Count; k++)
		{
			for (int l = 0; l < quests.WeeklyQuests[k].quests.Count; l++)
			{
				RotatingQuest rotatingQuest2 = quests.WeeklyQuests[k].quests[l];
				int progress2 = rotatingQuest2.GetProgress();
				if (progress2 > 0)
				{
					PlayerPrefs.SetInt(string.Format("{0}{1}", "Rotating_Quest_Weekly_ID_Key", num2), rotatingQuest2.questID);
					PlayerPrefs.SetInt(string.Format("{0}{1}", "Rotating_Quest_Weekly_Progress_Key", num2), progress2);
					num2++;
				}
			}
		}
		if (num2 > 0)
		{
			PlayerPrefs.SetInt("Rotating_Quest_Weekly_SetID_Key", weeklyQuestSetID);
			PlayerPrefs.SetInt("Rotating_Quest_Weekly_SaveCount_Key", num2);
		}
		PlayerPrefs.Save();
	}

	public void SetupAllQuestEventListeners()
	{
		foreach (RotatingQuestGroup dailyQuest in quests.DailyQuests)
		{
			foreach (RotatingQuest quest in dailyQuest.quests)
			{
				quest.questManager = this;
				if (quest.isQuestActive && !quest.isQuestComplete)
				{
					quest.AddEventListener();
				}
			}
		}
		foreach (RotatingQuestGroup weeklyQuest in quests.WeeklyQuests)
		{
			foreach (RotatingQuest quest2 in weeklyQuest.quests)
			{
				quest2.questManager = this;
				if (quest2.isQuestActive && !quest2.isQuestComplete)
				{
					quest2.AddEventListener();
				}
			}
		}
	}

	public void ClearAllQuestEventListeners()
	{
		foreach (RotatingQuestGroup dailyQuest in quests.DailyQuests)
		{
			foreach (RotatingQuest quest in dailyQuest.quests)
			{
				quest.RemoveEventListener();
			}
		}
		foreach (RotatingQuestGroup weeklyQuest in quests.WeeklyQuests)
		{
			foreach (RotatingQuest quest2 in weeklyQuest.quests)
			{
				quest2.RemoveEventListener();
			}
		}
	}

	public void HandleQuestCompleted(int questID)
	{
		RotatingQuest quest = quests.GetQuest(questID);
		if (quest != null)
		{
			ProgressionController.ReportQuestComplete(questID, quest.isDailyQuest);
			if (_playQuestSounds)
			{
				_questAudio?.GTPlay();
			}
		}
	}

	public void HandleQuestProgressChanged(bool initialLoad)
	{
		if (!initialLoad)
		{
			SaveQuestProgress();
		}
		LastQuestChange = Time.frameCount;
		ProgressionController.ReportQuestChanged(initialLoad);
	}
}
