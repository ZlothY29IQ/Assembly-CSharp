using System.Collections;
using System.Collections.Generic;
using GameObjectScheduling;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MonkeBusinessStation : MonoBehaviourPunCallbacks
{
	[SerializeField]
	private RectTransform _questContainerParent;

	[SerializeField]
	private RectTransform _dailyQuestContainer;

	[SerializeField]
	private RectTransform _weeklyQuestContainer;

	[SerializeField]
	private QuestDisplay _questDisplayPrefab;

	[SerializeField]
	private List<QuestDisplay> _quests;

	[SerializeField]
	private ProgressDisplay _weeklyProgress;

	[SerializeField]
	private TMP_Text _unclaimedPoints;

	[SerializeField]
	private GorillaPressableButton _claimButton;

	[SerializeField]
	private AudioSource _audioSource;

	[SerializeField]
	private GameObject _claimablePointsObject;

	[SerializeField]
	private GameObject _noClaimablePointsObject;

	[SerializeField]
	private Transform _claimablePointsBadgePosition;

	[SerializeField]
	private Transform _noClaimablePointsBadgePosition;

	[SerializeField]
	private Transform _badgeMount;

	[Space]
	[SerializeField]
	private float _claimDelayPerPoint = 0.12f;

	[SerializeField]
	private AudioClip _claimPointDefaultSFX;

	[SerializeField]
	private AudioClip _claimPointFinalSFX;

	[Header("Quest Timers")]
	[SerializeField]
	private CountdownText _dailyCountdown;

	[SerializeField]
	private CountdownText _weeklyCountdown;

	private RotatingQuestsManager _questManager;

	private int _lastQuestChange = -1;

	private int _lastQuestDailyID = -1;

	private bool _isUpdatingPointCount;

	private int _tempUnclaimedPoints;

	private int _tempTotalPoints;

	private bool _hasBuiltQuestList;

	private Dictionary<NetPlayer, Coroutine> perPlayerRedemptionSequence = new Dictionary<NetPlayer, Coroutine>();

	public override void OnEnable()
	{
		base.OnEnable();
		FindQuestManager();
		ProgressionController.OnQuestSelectionChanged += OnQuestSelectionChanged;
		ProgressionController.OnProgressEvent += OnProgress;
		ProgressionController.RequestProgressUpdate();
		UpdateCountdownTimers();
	}

	public override void OnDisable()
	{
		base.OnDisable();
		ProgressionController.OnQuestSelectionChanged -= OnQuestSelectionChanged;
		ProgressionController.OnProgressEvent -= OnProgress;
	}

	private void FindQuestManager()
	{
		if (!_questManager)
		{
			_questManager = Object.FindAnyObjectByType<RotatingQuestsManager>();
		}
	}

	private void UpdateCountdownTimers()
	{
		_dailyCountdown.SetCountdownTime(_questManager.DailyQuestCountdown);
		_weeklyCountdown.SetCountdownTime(_questManager.WeeklyQuestCountdown);
	}

	private void OnQuestSelectionChanged()
	{
		UpdateCountdownTimers();
	}

	private void OnProgress()
	{
		UpdateQuestStatus();
		UpdateProgressDisplays();
	}

	private void UpdateProgressDisplays()
	{
		var (progress, num, _) = ProgressionController.GetProgressionData();
		_weeklyProgress.SetProgress(progress, ProgressionController.WeeklyCap);
		if (!_isUpdatingPointCount)
		{
			_unclaimedPoints.text = num.ToString();
			_claimButton.isOn = num > 0;
		}
		bool flag = num > 0;
		_claimablePointsObject.SetActive(flag);
		_noClaimablePointsObject.SetActive(!flag);
		_badgeMount.position = (flag ? _claimablePointsBadgePosition.position : _noClaimablePointsBadgePosition.position);
		_claimButton.gameObject.SetActive(flag);
	}

	private void UpdateQuestStatus()
	{
		if (_lastQuestChange >= RotatingQuestsManager.LastQuestChange)
		{
			return;
		}
		FindQuestManager();
		if (_quests.Count == 0 || _lastQuestDailyID != RotatingQuestsManager.LastQuestDailyID)
		{
			BuildQuestList();
		}
		foreach (QuestDisplay quest in _quests)
		{
			if (quest.IsChanged)
			{
				quest.UpdateDisplay();
			}
		}
		_lastQuestChange = Time.frameCount;
		_lastQuestDailyID = RotatingQuestsManager.LastQuestDailyID;
	}

	public void RedeemProgress()
	{
		if (_claimButton.isOn)
		{
			_isUpdatingPointCount = true;
			(int weekly, int unclaimed, int total) progressionData = ProgressionController.GetProgressionData();
			int item = progressionData.unclaimed;
			int item2 = progressionData.total;
			_tempUnclaimedPoints = item;
			_tempTotalPoints = item2;
			_claimButton.isOn = false;
			ProgressionController.RedeemProgress();
			if (PhotonNetwork.InRoom)
			{
				base.photonView.RPC("BroadcastRedeemQuestPoints", RpcTarget.Others, _tempUnclaimedPoints);
			}
			StartCoroutine(PerformPointRedemptionSequence());
		}
	}

	private IEnumerator PerformPointRedemptionSequence()
	{
		while (_tempUnclaimedPoints > 0)
		{
			_tempUnclaimedPoints--;
			_tempTotalPoints++;
			_unclaimedPoints.text = _tempUnclaimedPoints.ToString();
			if (_tempUnclaimedPoints == 0)
			{
				_audioSource.PlayOneShot(_claimPointFinalSFX);
			}
			else
			{
				_audioSource.PlayOneShot(_claimPointDefaultSFX);
			}
			yield return new WaitForSeconds(_claimDelayPerPoint);
		}
		_isUpdatingPointCount = false;
		UpdateProgressDisplays();
	}

	[PunRPC]
	private void BroadcastRedeemQuestPoints(int redeemedPointCount, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "BroadcastRedeemQuestPoints");
		if (new PhotonMessageInfoWrapped(info).Sender == null || !VRRigCache.Instance.TryGetVrrig(info.Sender, out var playerRig) || !FXSystem.CheckCallSpam(playerRig.Rig.fxSettings, 10, Time.unscaledTime))
		{
			return;
		}
		redeemedPointCount = Mathf.Min(redeemedPointCount, 50);
		if (perPlayerRedemptionSequence.TryGetValue(info.Sender, out var value))
		{
			if (value != null)
			{
				StopCoroutine(value);
			}
			perPlayerRedemptionSequence.Remove(info.Sender);
		}
		if (base.gameObject.activeInHierarchy)
		{
			Coroutine value2 = StartCoroutine(PerformRemotePointRedemptionSequence(info.Sender, redeemedPointCount));
			perPlayerRedemptionSequence.Add(info.Sender, value2);
		}
	}

	private IEnumerator PerformRemotePointRedemptionSequence(NetPlayer player, int redeemedPointCount)
	{
		while (redeemedPointCount > 0)
		{
			redeemedPointCount--;
			if (redeemedPointCount == 0)
			{
				_audioSource.PlayOneShot(_claimPointFinalSFX);
			}
			else
			{
				_audioSource.PlayOneShot(_claimPointDefaultSFX);
			}
			yield return new WaitForSeconds(_claimDelayPerPoint);
		}
		perPlayerRedemptionSequence.Remove(player);
	}

	private void BuildQuestList()
	{
		DestroyQuestList();
		RotatingQuestsManager.RotatingQuestList quests = _questManager.quests;
		foreach (RotatingQuestsManager.RotatingQuestGroup dailyQuest in quests.DailyQuests)
		{
			foreach (RotatingQuest quest in dailyQuest.quests)
			{
				if (quest.isQuestActive)
				{
					QuestDisplay questDisplay = Object.Instantiate(_questDisplayPrefab, _dailyQuestContainer);
					questDisplay.quest = quest;
					_quests.Add(questDisplay);
				}
			}
		}
		foreach (RotatingQuestsManager.RotatingQuestGroup weeklyQuest in quests.WeeklyQuests)
		{
			foreach (RotatingQuest quest2 in weeklyQuest.quests)
			{
				if (quest2.isQuestActive)
				{
					QuestDisplay questDisplay2 = Object.Instantiate(_questDisplayPrefab, _weeklyQuestContainer);
					questDisplay2.quest = quest2;
					_quests.Add(questDisplay2);
				}
			}
		}
		foreach (QuestDisplay quest3 in _quests)
		{
			quest3.UpdateDisplay();
		}
		if (!_hasBuiltQuestList)
		{
			LayoutRebuilder.ForceRebuildLayoutImmediate(_questContainerParent);
			_hasBuiltQuestList = true;
		}
		else
		{
			LayoutRebuilder.MarkLayoutForRebuild(_questContainerParent);
		}
	}

	private void DestroyQuestList()
	{
		DestroyChildren(_dailyQuestContainer);
		DestroyChildren(_weeklyQuestContainer);
		_quests.Clear();
		static void DestroyChildren(Transform parent)
		{
			for (int num = parent.childCount - 1; num >= 0; num--)
			{
				Object.Destroy(parent.GetChild(num).gameObject);
			}
		}
	}
}
