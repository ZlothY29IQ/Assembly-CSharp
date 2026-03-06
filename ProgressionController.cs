using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using GorillaNetworking;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

public class ProgressionController : MonoBehaviour
{
	[Serializable]
	private class GetQuestsStatusRequest
	{
		public string PlayFabId;

		public string PlayFabTicket;

		public string MothershipId;

		public string MothershipToken;
	}

	[Serializable]
	public class GetQuestStatusResponse
	{
		public UserQuestsStatus result;
	}

	public class UserQuestsStatus
	{
		public Dictionary<string, int> dailyPoints;

		public Dictionary<int, int> weeklyPoints;

		public int userPointsTotal;

		public int GetWeeklyPoints()
		{
			int num = 0;
			if (dailyPoints != null)
			{
				foreach (KeyValuePair<string, int> dailyPoint in dailyPoints)
				{
					num += dailyPoint.Value;
				}
			}
			if (weeklyPoints != null)
			{
				foreach (KeyValuePair<int, int> weeklyPoint in weeklyPoints)
				{
					num += weeklyPoint.Value;
				}
			}
			return Mathf.Min(num, WeeklyCap);
		}
	}

	[Serializable]
	private class SetQuestCompleteRequest
	{
		public string PlayFabId;

		public string PlayFabTicket;

		public string MothershipId;

		public string MothershipToken;

		public int QuestId;

		public string ClientVersion;
	}

	[Serializable]
	public class SetQuestCompleteResponse
	{
		public UserQuestsStatus result;
	}

	private static ProgressionController _gInstance;

	[SerializeField]
	private RotatingQuestsManager _questManager;

	private int weeklyPoints;

	private int totalPointsRaw;

	private int unclaimedPoints;

	private bool _progressReportPending;

	private (int weeklyPoints, int unclaimedPoints, int totalPointsRaw) _lastProgressReport;

	private bool _isFetchingStatus;

	private bool _statusReceived;

	private bool _isSendingQuestComplete;

	private int _fetchStatusRetryCount;

	private int _sendQuestCompleteRetryCount;

	private int _maxRetriesOnFail = 3;

	private List<int> _queuedDailyCompletedQuests = new List<int>();

	private List<int> _queuedWeeklyCompletedQuests = new List<int>();

	private int _currentlyProcessingQuest = -1;

	private const string kUnclaimedPointKey = "Claimed_Points_Key";

	private const string kQueuedDailyQuestSetIDKey = "Queued_Quest_Daily_SetID_Key";

	private const string kQueuedDailyQuestSaveCountKey = "Queued_Quest_Daily_SaveCount_Key";

	private const string kQueuedDailyQuestIDKey = "Queued_Quest_Daily_ID_Key";

	private const string kQueuedWeeklyQuestSetIDKey = "Queued_Quest_Weekly_SetID_Key";

	private const string kQueuedWeeklyQuestSaveCountKey = "Queued_Quest_Weekly_SaveCount_Key";

	private const string kQueuedWeeklyQuestIDKey = "Queued_Quest_Weekly_ID_Key";

	public static int WeeklyCap { get; private set; } = 25;

	public static int TotalPoints => _gInstance.totalPointsRaw - _gInstance.unclaimedPoints;

	public static event Action OnQuestSelectionChanged;

	public static event Action OnProgressEvent;

	public static void ReportQuestChanged(bool initialLoad)
	{
		_gInstance.OnQuestProgressChanged(initialLoad);
	}

	public static void ReportQuestSelectionChanged()
	{
		_gInstance.LoadCompletedQuestQueue();
		ProgressionController.OnQuestSelectionChanged?.Invoke();
	}

	public static void ReportQuestComplete(int questId, bool isDaily)
	{
		_gInstance.OnQuestComplete(questId, isDaily);
	}

	public static void RedeemProgress()
	{
		_gInstance.RequestProgressRedemption(_gInstance.OnProgressRedeemed);
	}

	public static (int weekly, int unclaimed, int total) GetProgressionData()
	{
		return _gInstance.GetProgress();
	}

	public static void RequestProgressUpdate()
	{
		_gInstance?.ReportProgress();
	}

	private void Awake()
	{
		if ((bool)_gInstance)
		{
			Debug.LogError("Duplicate ProgressionController detected. Destroying self.", base.gameObject);
			UnityEngine.Object.Destroy(this);
			return;
		}
		_gInstance = this;
		unclaimedPoints = PlayerPrefs.GetInt("Claimed_Points_Key", 0);
		RequestStatus();
		LoadCompletedQuestQueue();
	}

	private async void RequestStatus()
	{
		if (ShouldFetchStatus())
		{
			_isFetchingStatus = true;
			await WaitForSessionToken();
			FetchStatus();
		}
		else
		{
			Debug.LogError("RequestStatus triggered multiple times.  That's probably not good.");
		}
		bool ShouldFetchStatus()
		{
			if (!_isFetchingStatus)
			{
				return !_statusReceived;
			}
			return false;
		}
	}

	private async Task WaitForSessionToken()
	{
		while (!PlayFabAuthenticator.instance || PlayFabAuthenticator.instance.GetPlayFabPlayerId().IsNullOrEmpty() || PlayFabAuthenticator.instance.GetPlayFabSessionTicket().IsNullOrEmpty())
		{
			await Task.Yield();
			await Task.Delay(1000);
		}
	}

	private void FetchStatus()
	{
		StartCoroutine(DoFetchStatus(new GetQuestsStatusRequest
		{
			PlayFabId = PlayFabAuthenticator.instance.GetPlayFabPlayerId(),
			PlayFabTicket = PlayFabAuthenticator.instance.GetPlayFabSessionTicket(),
			MothershipId = "",
			MothershipToken = ""
		}, OnFetchStatusResponse));
	}

	private IEnumerator DoFetchStatus(GetQuestsStatusRequest data, Action<GetQuestStatusResponse> callback)
	{
		UnityWebRequest request = new UnityWebRequest(PlayFabAuthenticatorSettings.DailyQuestsApiBaseUrl + "/api/GetQuestStatus", "POST");
		byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(data));
		bool retry = false;
		request.uploadHandler = new UploadHandlerRaw(bytes);
		request.downloadHandler = new DownloadHandlerBuffer();
		request.SetRequestHeader("Content-Type", "application/json");
		yield return request.SendWebRequest();
		if (request.result == UnityWebRequest.Result.Success)
		{
			GetQuestStatusResponse obj = JsonConvert.DeserializeObject<GetQuestStatusResponse>(request.downloadHandler.text);
			callback(obj);
		}
		else
		{
			long responseCode = request.responseCode;
			if (responseCode >= 500 && responseCode < 600)
			{
				retry = true;
			}
			else if (request.result == UnityWebRequest.Result.ConnectionError)
			{
				retry = true;
			}
		}
		if (retry)
		{
			if (_fetchStatusRetryCount < _maxRetriesOnFail)
			{
				int num = (int)Mathf.Pow(2f, _fetchStatusRetryCount + 1);
				_fetchStatusRetryCount++;
				yield return new WaitForSecondsRealtime(num);
				FetchStatus();
			}
			else
			{
				GTDev.LogError("Maximum FetchStatus retries attempted. Please check your network connection.");
				_fetchStatusRetryCount = 0;
				callback(null);
			}
		}
	}

	private void OnFetchStatusResponse([CanBeNull] GetQuestStatusResponse response)
	{
		_isFetchingStatus = false;
		_statusReceived = false;
		if (response != null)
		{
			SetProgressionValues(response.result.GetWeeklyPoints(), unclaimedPoints, response.result.userPointsTotal);
			ReportProgress();
		}
		else
		{
			GTDev.LogError("Error: Could not fetch status!");
		}
	}

	private void SendQuestCompleted(int questId)
	{
		if (!_isSendingQuestComplete)
		{
			_isSendingQuestComplete = true;
			StartSendQuestComplete(questId);
		}
	}

	private void StartSendQuestComplete(int questId)
	{
		StartCoroutine(DoSendQuestComplete(new SetQuestCompleteRequest
		{
			PlayFabId = PlayFabAuthenticator.instance.GetPlayFabPlayerId(),
			PlayFabTicket = PlayFabAuthenticator.instance.GetPlayFabSessionTicket(),
			MothershipId = "",
			MothershipToken = "",
			QuestId = questId,
			ClientVersion = MothershipClientApiUnity.DeploymentId
		}, OnSendQuestCompleteSuccess));
	}

	private IEnumerator DoSendQuestComplete(SetQuestCompleteRequest data, Action<SetQuestCompleteResponse> callback)
	{
		UnityWebRequest request = new UnityWebRequest(PlayFabAuthenticatorSettings.DailyQuestsApiBaseUrl + "/api/SetQuestComplete", "POST");
		byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(data));
		bool retry = false;
		request.uploadHandler = new UploadHandlerRaw(bytes);
		request.downloadHandler = new DownloadHandlerBuffer();
		request.SetRequestHeader("Content-Type", "application/json");
		yield return request.SendWebRequest();
		if (request.result == UnityWebRequest.Result.Success)
		{
			SetQuestCompleteResponse obj = JsonConvert.DeserializeObject<SetQuestCompleteResponse>(request.downloadHandler.text);
			callback(obj);
			ProcessQuestSubmittedSuccess();
		}
		else
		{
			long responseCode = request.responseCode;
			if (responseCode >= 500 && responseCode < 600)
			{
				retry = true;
			}
			else if (request.responseCode == 403)
			{
				GTDev.LogWarning("User already reached the max number of completion points for this time period!");
				callback(null);
				ClearQuestQueue();
			}
			else if (request.result == UnityWebRequest.Result.ConnectionError)
			{
				retry = true;
			}
		}
		if (retry)
		{
			if (_sendQuestCompleteRetryCount < _maxRetriesOnFail)
			{
				int num = (int)Mathf.Pow(2f, _sendQuestCompleteRetryCount + 1);
				_sendQuestCompleteRetryCount++;
				yield return new WaitForSecondsRealtime(num);
				StartSendQuestComplete(data.QuestId);
			}
			else
			{
				GTDev.LogError("Maximum SendQuestComplete retries attempted. Please check your network connection.");
				_sendQuestCompleteRetryCount = 0;
				callback(null);
				ProcessQuestSubmittedFail();
			}
		}
		else
		{
			_isSendingQuestComplete = false;
		}
	}

	private void OnSendQuestCompleteSuccess([CanBeNull] SetQuestCompleteResponse response)
	{
		_isSendingQuestComplete = false;
		if (response != null)
		{
			UpdateProgressionValues(response.result.GetWeeklyPoints(), response.result.userPointsTotal);
			ReportProgress();
		}
	}

	private void OnQuestProgressChanged(bool initialLoad)
	{
		ReportProgress();
	}

	private void OnQuestComplete(int questId, bool isDaily)
	{
		QueueQuestCompletion(questId, isDaily);
	}

	private void QueueQuestCompletion(int questId, bool isDaily)
	{
		if (isDaily)
		{
			_queuedDailyCompletedQuests.Add(questId);
		}
		else
		{
			_queuedWeeklyCompletedQuests.Add(questId);
		}
		SaveCompletedQuestQueue();
		SubmitNextQuestInQueue();
	}

	private void SubmitNextQuestInQueue()
	{
		if (_currentlyProcessingQuest == -1 && AreCompletedQuestsQueued())
		{
			int num = -1;
			if (_queuedWeeklyCompletedQuests.Count > 0)
			{
				num = _queuedWeeklyCompletedQuests[0];
			}
			else if (_queuedDailyCompletedQuests.Count > 0)
			{
				num = _queuedDailyCompletedQuests[0];
			}
			_currentlyProcessingQuest = num;
			SendQuestCompleted(num);
		}
	}

	private void ClearQuestQueue()
	{
		_currentlyProcessingQuest = -1;
		_queuedDailyCompletedQuests.Clear();
		_queuedWeeklyCompletedQuests.Clear();
		SaveCompletedQuestQueue();
	}

	private void ProcessQuestSubmittedSuccess()
	{
		if (_currentlyProcessingQuest == -1)
		{
			return;
		}
		if (AreCompletedQuestsQueued())
		{
			if (_queuedWeeklyCompletedQuests.Remove(_currentlyProcessingQuest))
			{
				SaveCompletedQuestQueue();
			}
			else if (_queuedDailyCompletedQuests.Remove(_currentlyProcessingQuest))
			{
				SaveCompletedQuestQueue();
			}
		}
		_currentlyProcessingQuest = -1;
		SubmitNextQuestInQueue();
	}

	private void ProcessQuestSubmittedFail()
	{
		_currentlyProcessingQuest = -1;
	}

	private bool AreCompletedQuestsQueued()
	{
		if (_queuedDailyCompletedQuests.Count <= 0)
		{
			return _queuedWeeklyCompletedQuests.Count > 0;
		}
		return true;
	}

	private void SaveCompletedQuestQueue()
	{
		int num = 0;
		for (int i = 0; i < _queuedDailyCompletedQuests.Count; i++)
		{
			PlayerPrefs.SetInt(string.Format("{0}{1}", "Queued_Quest_Daily_ID_Key", num), _queuedDailyCompletedQuests[i]);
			num++;
		}
		int dailyQuestSetID = _questManager.dailyQuestSetID;
		PlayerPrefs.SetInt("Queued_Quest_Daily_SetID_Key", dailyQuestSetID);
		PlayerPrefs.SetInt("Queued_Quest_Daily_SaveCount_Key", num);
		int num2 = 0;
		for (int j = 0; j < _queuedWeeklyCompletedQuests.Count; j++)
		{
			PlayerPrefs.SetInt(string.Format("{0}{1}", "Queued_Quest_Weekly_ID_Key", num2), _queuedWeeklyCompletedQuests[j]);
			num2++;
		}
		int weeklyQuestSetID = _questManager.weeklyQuestSetID;
		PlayerPrefs.SetInt("Queued_Quest_Weekly_SetID_Key", weeklyQuestSetID);
		PlayerPrefs.SetInt("Queued_Quest_Weekly_SaveCount_Key", num2);
	}

	private void LoadCompletedQuestQueue()
	{
		_queuedDailyCompletedQuests.Clear();
		int num = PlayerPrefs.GetInt("Queued_Quest_Daily_SetID_Key", -1);
		int num2 = PlayerPrefs.GetInt("Queued_Quest_Daily_SaveCount_Key", -1);
		int dailyQuestSetID = _questManager.dailyQuestSetID;
		if (num == dailyQuestSetID)
		{
			for (int i = 0; i < num2; i++)
			{
				int num3 = PlayerPrefs.GetInt(string.Format("{0}{1}", "Queued_Quest_Daily_ID_Key", i), -1);
				if (num3 != -1)
				{
					_queuedDailyCompletedQuests.Add(num3);
				}
			}
		}
		_queuedWeeklyCompletedQuests.Clear();
		int num4 = PlayerPrefs.GetInt("Queued_Quest_Weekly_SetID_Key", -1);
		int num5 = PlayerPrefs.GetInt("Queued_Quest_Weekly_SaveCount_Key", -1);
		int weeklyQuestSetID = _questManager.weeklyQuestSetID;
		if (num4 == weeklyQuestSetID)
		{
			for (int j = 0; j < num5; j++)
			{
				int num6 = PlayerPrefs.GetInt(string.Format("{0}{1}", "Queued_Quest_Weekly_ID_Key", j), -1);
				if (num6 != -1)
				{
					_queuedWeeklyCompletedQuests.Add(num6);
				}
			}
		}
		SubmitNextQuestInQueue();
	}

	private async void RequestProgressRedemption(Action onComplete)
	{
		await Task.Yield();
		onComplete?.Invoke();
	}

	private void OnProgressRedeemed()
	{
		unclaimedPoints = 0;
		PlayerPrefs.SetInt("Claimed_Points_Key", unclaimedPoints);
		ReportProgress();
	}

	private void AddPoints(int points)
	{
		if (weeklyPoints < WeeklyCap)
		{
			int num = Mathf.Clamp(points, 0, WeeklyCap - weeklyPoints);
			SetProgressionValues(weeklyPoints + num, unclaimedPoints + num, totalPointsRaw + num);
		}
	}

	private void UpdateProgressionValues(int weekly, int totalRaw)
	{
		int num = totalRaw - totalPointsRaw;
		unclaimedPoints += num;
		SetProgressionValues(weekly, unclaimedPoints, totalRaw);
	}

	private void SetProgressionValues(int weekly, int unclaimed, int totalRaw)
	{
		weeklyPoints = weekly;
		unclaimedPoints = unclaimed;
		totalPointsRaw = totalRaw;
		ReportScoreChange();
		PlayerPrefs.SetInt("Claimed_Points_Key", unclaimed);
	}

	private async void ReportProgress()
	{
		try
		{
			if (!_progressReportPending)
			{
				_progressReportPending = true;
				await Task.Yield();
				_progressReportPending = false;
				ProgressionController.OnProgressEvent?.Invoke();
				ReportScoreChange();
			}
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
		}
	}

	private void ReportScoreChange()
	{
		(int, int, int) tuple = (weeklyPoints, unclaimedPoints, totalPointsRaw);
		(int, int, int) lastProgressReport = _lastProgressReport;
		(int, int, int) tuple2 = tuple;
		if (lastProgressReport.Item1 != tuple2.Item1 || lastProgressReport.Item2 != tuple2.Item2 || lastProgressReport.Item3 != tuple2.Item3)
		{
			if ((bool)VRRig.LocalRig)
			{
				VRRig.LocalRig.SetQuestScore(TotalPoints);
			}
			_lastProgressReport = tuple;
		}
	}

	private (int weekly, int unclaimed, int total) GetProgress()
	{
		return (weekly: weeklyPoints, unclaimed: unclaimedPoints, total: totalPointsRaw - unclaimedPoints);
	}
}
