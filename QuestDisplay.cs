using System;
using TMPro;
using UnityEngine;

public class QuestDisplay : MonoBehaviour
{
	[SerializeField]
	private ProgressDisplay progressDisplay;

	[SerializeField]
	private TMP_Text text;

	[SerializeField]
	private TMP_Text statusText;

	[SerializeField]
	private GameObject dailyIncompleteIndicator;

	[SerializeField]
	private GameObject dailyCompleteIndicator;

	[SerializeField]
	private GameObject weeklyIncompleteIndicator;

	[SerializeField]
	private GameObject weeklyCompleteIndicator;

	[NonSerialized]
	public RotatingQuest quest;

	private int _lastUpdate = -1;

	public bool IsChanged => quest.lastChange > _lastUpdate;

	public void UpdateDisplay()
	{
		text.text = quest.GetTextDescription();
		if (quest.isQuestComplete)
		{
			progressDisplay.SetVisible(visible: false);
		}
		else if (quest.requiredOccurenceCount > 1)
		{
			progressDisplay.SetProgress(quest.occurenceCount, quest.requiredOccurenceCount);
			progressDisplay.SetVisible(visible: true);
		}
		else
		{
			progressDisplay.SetVisible(visible: false);
		}
		UpdateCompletionIndicator();
		_lastUpdate = Time.frameCount;
	}

	private void UpdateCompletionIndicator()
	{
		bool isQuestComplete = quest.isQuestComplete;
		bool flag = !isQuestComplete && quest.requiredOccurenceCount == 1;
		dailyIncompleteIndicator.SetActive(quest.isDailyQuest && flag);
		dailyCompleteIndicator.SetActive(quest.isDailyQuest && isQuestComplete);
		weeklyIncompleteIndicator.SetActive(!quest.isDailyQuest && flag);
		weeklyCompleteIndicator.SetActive(!quest.isDailyQuest && isQuestComplete);
	}
}
