using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

[Serializable]
public class RotatingQuest
{
	public bool disable;

	public int questID;

	public float weight = 1f;

	public QuestCategory category;

	public string questName = "UNNAMED QUEST";

	public QuestType questType;

	public string questOccurenceFilter;

	public int requiredOccurenceCount = 1;

	[JsonProperty(ItemConverterType = typeof(StringEnumConverter))]
	public List<GTZone> requiredZones;

	[NonSerialized]
	[Space]
	public bool isQuestActive;

	[NonSerialized]
	public bool isQuestComplete;

	[NonSerialized]
	public bool isDailyQuest;

	[NonSerialized]
	public int lastChange;

	[NonSerialized]
	public int occurenceCount;

	private float moveDistance;

	[NonSerialized]
	public GorillaQuestManager questManager;

	[JsonIgnore]
	public bool IsMovementQuest
	{
		get
		{
			if (questType != QuestType.moveDistance)
			{
				return questType == QuestType.swimDistance;
			}
			return true;
		}
	}

	[JsonIgnore]
	public GTZone RequiredZone { get; private set; } = GTZone.none;

	public void SetRequiredZone()
	{
		RequiredZone = ((requiredZones.Count > 0) ? requiredZones[UnityEngine.Random.Range(0, requiredZones.Count)] : GTZone.none);
	}

	public void AddEventListener()
	{
		if (!isQuestComplete)
		{
			switch (questType)
			{
			case QuestType.gameModeObjective:
				PlayerGameEvents.OnGameModeObjectiveTrigger += OnGameEventOccurence;
				break;
			case QuestType.gameModeRound:
				PlayerGameEvents.OnGameModeCompleteRound += OnGameEventOccurence;
				break;
			case QuestType.grabObject:
				PlayerGameEvents.OnGrabbedObject += OnGameEventOccurence;
				break;
			case QuestType.dropObject:
				PlayerGameEvents.OnDroppedObject += OnGameEventOccurence;
				break;
			case QuestType.eatObject:
				PlayerGameEvents.OnEatObject += OnGameEventOccurence;
				break;
			case QuestType.tapObject:
				PlayerGameEvents.OnTapObject += OnGameEventOccurence;
				break;
			case QuestType.launchedProjectile:
				PlayerGameEvents.OnLaunchedProjectile += OnGameEventOccurence;
				break;
			case QuestType.moveDistance:
				PlayerGameEvents.OnPlayerMoved += OnGameMoveEvent;
				break;
			case QuestType.swimDistance:
				PlayerGameEvents.OnPlayerSwam += OnGameMoveEvent;
				break;
			case QuestType.triggerHandEffect:
				PlayerGameEvents.OnTriggerHandEffect += OnGameEventOccurence;
				break;
			case QuestType.enterLocation:
				PlayerGameEvents.OnEnterLocation += OnGameEventOccurence;
				break;
			case QuestType.misc:
				PlayerGameEvents.OnMiscEvent += OnGameEventOccurence;
				break;
			case QuestType.critter:
				PlayerGameEvents.OnCritterEvent += OnGameEventOccurence;
				break;
			}
		}
	}

	public void RemoveEventListener()
	{
		switch (questType)
		{
		case QuestType.gameModeObjective:
			PlayerGameEvents.OnGameModeObjectiveTrigger -= OnGameEventOccurence;
			break;
		case QuestType.gameModeRound:
			PlayerGameEvents.OnGameModeCompleteRound -= OnGameEventOccurence;
			break;
		case QuestType.grabObject:
			PlayerGameEvents.OnGrabbedObject -= OnGameEventOccurence;
			break;
		case QuestType.dropObject:
			PlayerGameEvents.OnDroppedObject -= OnGameEventOccurence;
			break;
		case QuestType.eatObject:
			PlayerGameEvents.OnEatObject -= OnGameEventOccurence;
			break;
		case QuestType.tapObject:
			PlayerGameEvents.OnTapObject -= OnGameEventOccurence;
			break;
		case QuestType.launchedProjectile:
			PlayerGameEvents.OnLaunchedProjectile -= OnGameEventOccurence;
			break;
		case QuestType.moveDistance:
			PlayerGameEvents.OnPlayerMoved -= OnGameMoveEvent;
			break;
		case QuestType.swimDistance:
			PlayerGameEvents.OnPlayerSwam -= OnGameMoveEvent;
			break;
		case QuestType.triggerHandEffect:
			PlayerGameEvents.OnTriggerHandEffect -= OnGameEventOccurence;
			break;
		case QuestType.enterLocation:
			PlayerGameEvents.OnEnterLocation -= OnGameEventOccurence;
			break;
		case QuestType.misc:
			PlayerGameEvents.OnMiscEvent -= OnGameEventOccurence;
			break;
		case QuestType.critter:
			PlayerGameEvents.OnCritterEvent -= OnGameEventOccurence;
			break;
		}
	}

	public void ApplySavedProgress(int progress)
	{
		if (questType == QuestType.moveDistance || questType == QuestType.swimDistance)
		{
			moveDistance = progress;
			occurenceCount = Mathf.FloorToInt(moveDistance);
			isQuestComplete = occurenceCount >= requiredOccurenceCount;
		}
		else
		{
			occurenceCount = progress;
			isQuestComplete = occurenceCount >= requiredOccurenceCount;
		}
	}

	public int GetProgress()
	{
		if (questType == QuestType.moveDistance || questType == QuestType.swimDistance)
		{
			return Mathf.FloorToInt(moveDistance);
		}
		return occurenceCount;
	}

	private void OnGameEventOccurence(string eventName)
	{
		OnGameEventOccurence(eventName, 1);
	}

	private void OnGameEventOccurence(string eventName, int count)
	{
		if (RequiredZone == GTZone.none || ZoneManagement.IsInZone(RequiredZone))
		{
			string.IsNullOrEmpty(questOccurenceFilter);
			if (eventName.StartsWith(questOccurenceFilter))
			{
				SetProgress(occurenceCount + count);
			}
		}
	}

	private void OnGameMoveEvent(float distance, float speed)
	{
		if (RequiredZone != GTZone.none && !ZoneManagement.IsInZone(RequiredZone))
		{
			return;
		}
		if (questOccurenceFilter == "maxSpeed")
		{
			if (!(speed <= moveDistance))
			{
				moveDistance = speed;
				SetProgress(Mathf.FloorToInt(moveDistance));
			}
		}
		else
		{
			moveDistance += distance;
			SetProgress(Mathf.FloorToInt(moveDistance));
		}
	}

	private void SetProgress(int progress)
	{
		if (!isQuestComplete && occurenceCount != progress)
		{
			lastChange = Time.frameCount;
			occurenceCount = progress;
			if (questType == QuestType.moveDistance || questType == QuestType.swimDistance)
			{
				moveDistance = progress;
			}
			if (occurenceCount >= requiredOccurenceCount)
			{
				Complete();
			}
			questManager.HandleQuestProgressChanged(initialLoad: false);
		}
	}

	private void Complete()
	{
		if (!isQuestComplete)
		{
			isQuestComplete = true;
			RemoveEventListener();
			questManager.HandleQuestCompleted(questID);
		}
	}

	public string GetTextDescription()
	{
		return GetActionName().ToUpper() + GetLocationText().ToUpper();
		string GetActionName()
		{
			return questType switch
			{
				QuestType.none => "[UNDEFINED]", 
				QuestType.gameModeObjective => questName, 
				QuestType.gameModeRound => questName, 
				QuestType.grabObject => questName, 
				QuestType.dropObject => questName, 
				QuestType.eatObject => questName, 
				QuestType.launchedProjectile => questName, 
				QuestType.moveDistance => questName, 
				QuestType.swimDistance => questName, 
				QuestType.triggerHandEffect => questName, 
				QuestType.enterLocation => questName, 
				QuestType.misc => questName, 
				_ => questName, 
			};
		}
		string GetLocationText()
		{
			if (RequiredZone == GTZone.none)
			{
				return "";
			}
			return $" IN {RequiredZone}";
		}
	}

	public string GetProgressText()
	{
		if (!isQuestComplete)
		{
			return $"{occurenceCount}/{requiredOccurenceCount}";
		}
		return "[DONE]";
	}
}
