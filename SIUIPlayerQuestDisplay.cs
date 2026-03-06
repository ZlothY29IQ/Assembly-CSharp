using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class SIUIPlayerQuestDisplay : MonoBehaviour, IGorillaSliceableSimple
{
	public TextMeshProUGUI playerName;

	[FormerlySerializedAs("playerTestPoints")]
	public TextMeshProUGUI playerTechPoints;

	public TextMeshProUGUI stashedQuestCount;

	public TextMeshProUGUI stashedBonusPointCount;

	public Image displayBackground;

	public Image smallDisplayBackground;

	public Image monkeIdolIcon;

	public Color localPlayerColor;

	public Color remotePlayerColor;

	public Color noPlayerColor;

	public SIUIPlayerQuestEntry[] questEntries;

	public GameObject collectBonusButton;

	public GameObject bonusPointsInProgress;

	public GameObject bonusPointsCompleted;

	public SIUIProgressBar sharedProgress;

	public GameObject activePlayer;

	public GameObject waitingForPlayer;

	public int activePlayerActorNumber;

	private string lastNickName;

	private int lastStashedQuests = -1;

	private int lastStashedBonusPoints = -1;

	private int lastTechPoints = -1;

	private int lastBonusProgress = -1;

	public void RefreshDisplay()
	{
		SIPlayer sIPlayer = SIPlayer.Get(activePlayerActorNumber);
		bool flag = sIPlayer != null && sIPlayer.gamePlayer != null && sIPlayer.gamePlayer.rig != null && sIPlayer.gamePlayer.rig.Creator != null && activePlayerActorNumber > 0;
		if (!flag || !SIProgression.Instance.ClientReady)
		{
			if (activePlayer.activeSelf)
			{
				activePlayer.SetActive(value: false);
			}
			if (!waitingForPlayer.activeSelf)
			{
				waitingForPlayer.SetActive(value: true);
			}
			displayBackground.color = noPlayerColor;
			smallDisplayBackground.color = noPlayerColor;
			return;
		}
		if (activePlayer.activeSelf != flag)
		{
			activePlayer.SetActive(flag);
		}
		if (waitingForPlayer.activeSelf == flag)
		{
			waitingForPlayer.SetActive(!flag);
		}
		if (!flag)
		{
			displayBackground.color = noPlayerColor;
			smallDisplayBackground.color = noPlayerColor;
			return;
		}
		Color color = ((sIPlayer == SIPlayer.LocalPlayer) ? localPlayerColor : remotePlayerColor);
		displayBackground.color = color;
		smallDisplayBackground.color = color;
		string sanitizedNickName = sIPlayer.gamePlayer.rig.Creator.SanitizedNickName;
		if (lastNickName != sanitizedNickName)
		{
			playerName.text = sanitizedNickName;
		}
		lastNickName = sanitizedNickName;
		int num = sIPlayer.CurrentProgression.resourceArray[0];
		if (lastTechPoints != num)
		{
			playerTechPoints.text = $"TECH POINTS: {num}";
		}
		lastTechPoints = num;
		bool flag2 = sIPlayer.HasLimitedResourceBeenDeposited(SIResource.LimitedDepositType.MonkeIdol);
		if (flag2 != monkeIdolIcon.enabled)
		{
			monkeIdolIcon.enabled = flag2;
		}
		int stashedQuests = sIPlayer.CurrentProgression.stashedQuests;
		if (lastStashedQuests != stashedQuests)
		{
			stashedQuestCount.text = $"STASHED QUESTS: {Mathf.Max(0, stashedQuests - 3)}/{6}";
		}
		lastStashedQuests = stashedQuests;
		int stashedBonusPoints = sIPlayer.CurrentProgression.stashedBonusPoints;
		if (lastStashedBonusPoints != stashedBonusPoints)
		{
			stashedBonusPointCount.text = $"STASHED BONUS: {Mathf.Max(0, stashedBonusPoints - 1)}/{2}";
		}
		lastStashedBonusPoints = stashedBonusPoints;
		int bonusProgress = sIPlayer.CurrentProgression.bonusProgress;
		if (lastBonusProgress != bonusProgress)
		{
			float num2 = Mathf.Clamp01((float)bonusProgress / 4f);
			sharedProgress.UpdateFillPercent(num2);
			sharedProgress.progressText.text = $"{num2 * 100f:F0}%";
		}
		lastBonusProgress = bonusProgress;
		bool flag3 = sIPlayer.CurrentProgression.stashedBonusPoints > 0;
		if (bonusPointsInProgress.activeSelf != flag3)
		{
			bonusPointsInProgress.SetActive(flag3);
		}
		if (bonusPointsCompleted.activeSelf == flag3)
		{
			bonusPointsCompleted.SetActive(!flag3);
		}
		bool flag4 = sIPlayer.CurrentProgression.bonusProgress >= 4;
		if (collectBonusButton.activeSelf != flag4)
		{
			collectBonusButton.SetActive(flag4);
		}
		if (questEntries != null && sIPlayer.CurrentProgression.currentQuestIds != null && sIPlayer.CurrentProgression.currentQuestProgresses != null)
		{
			for (int i = 0; i < questEntries.Length; i++)
			{
				ProcessQuestEntry(questEntries[i], sIPlayer.CurrentProgression.currentQuestIds[i], sIPlayer.CurrentProgression.currentQuestProgresses[i]);
			}
		}
	}

	public void ProcessQuestEntry(SIUIPlayerQuestEntry entry, int questId, int questProgress)
	{
		if (SIProgression.Instance.questSourceList == null)
		{
			if (entry.questInfo.activeSelf)
			{
				entry.questInfo.SetActive(value: false);
			}
			if (!entry.noQuestAvailable.activeSelf)
			{
				entry.noQuestAvailable.SetActive(value: true);
			}
			if (entry.completeOverlay.activeSelf)
			{
				entry.completeOverlay.SetActive(value: false);
			}
			entry.lastQuestId = -1;
			entry.lastQuestProgress = -1;
			return;
		}
		RotatingQuest questById = SIProgression.Instance.questSourceList.GetQuestById(questId);
		bool flag = questId != -1 && questById != null;
		if (entry.completeOverlay.activeSelf && !flag)
		{
			entry.completeOverlay.SetActive(value: false);
		}
		if (entry.questInfo.activeSelf != flag)
		{
			entry.questInfo.SetActive(flag);
		}
		if (entry.noQuestAvailable.activeSelf == flag)
		{
			entry.noQuestAvailable.SetActive(!flag);
		}
		if (!flag)
		{
			entry.lastQuestId = -1;
			return;
		}
		if (questId != entry.lastQuestId)
		{
			entry.questDescription.text = questById.GetTextDescription();
		}
		if (entry.lastQuestProgress != questProgress || questId != entry.lastQuestId)
		{
			entry.progress.UpdateFillPercent((float)questProgress / (float)questById.requiredOccurenceCount);
			entry.progress.progressText.text = questProgress + "/" + questById.requiredOccurenceCount;
		}
		if (entry.lastQuestId != -1 && entry.lastQuestId != questById.questID)
		{
			entry.newQuestTag.SetActive(value: true);
		}
		entry.lastQuestId = questById.questID;
		entry.lastQuestProgress = questProgress;
		bool flag2 = questProgress >= questById.requiredOccurenceCount;
		if (entry.completeOverlay.activeSelf != flag2)
		{
			entry.completeOverlay.SetActive(flag2);
		}
	}

	public void BonusPointCollectButtonPress()
	{
		if (activePlayerActorNumber == SIPlayer.LocalPlayer.ActorNr)
		{
			SIProgression.Instance.AttemptRedeemBonusPoint();
		}
	}

	public void QuestPointCollectButtonPress(int questIndex)
	{
		if (activePlayerActorNumber == SIPlayer.LocalPlayer.ActorNr && SIPlayer.LocalPlayer.QuestAvailableToClaim(questIndex))
		{
			SIProgression.Instance.AttemptRedeemCompletedQuest(questIndex);
		}
	}

	void IGorillaSliceableSimple.SliceUpdate()
	{
		RefreshDisplay();
	}

	public void OnEnable()
	{
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	public void OnDisable()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}
}
