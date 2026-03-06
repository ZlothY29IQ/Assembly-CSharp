using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using GorillaNetworking;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Networking;

public class GorillaTagCompetitiveServerApi : MonoBehaviour
{
	public enum EPlatformType
	{
		PC,
		Quest,
		NumPlatforms
	}

	[Serializable]
	public class RankedModeRequestDataBase
	{
		public string mothershipId;

		public string mothershipToken;

		public string mothershipEnvId;
	}

	[Serializable]
	public class RankedModeRequestDataPlatformed : RankedModeRequestDataBase
	{
		public string platform;
	}

	[Serializable]
	public class RankedModeProgressionRequestData : RankedModeRequestDataPlatformed
	{
		public List<string> playfabIds;
	}

	[Serializable]
	public class RankedModeProgressionPlatformData
	{
		public string platform;

		public float elo;

		public int majorTier;

		public int minorTier;

		public float rankProgress;
	}

	[Serializable]
	public class RankedModePlayerProgressionData
	{
		public string playfabID;

		public RankedModeProgressionPlatformData[] platformData = new RankedModeProgressionPlatformData[2];
	}

	[Serializable]
	public class RankedModeProgressionData
	{
		public List<RankedModePlayerProgressionData> playerData;
	}

	[Serializable]
	public class RankedModeRequestDataWithMatchId : RankedModeRequestDataPlatformed
	{
		public string matchId;
	}

	[Serializable]
	public class RankedModeValidateMatchJoinResponseData
	{
		public bool validJoin;
	}

	[Serializable]
	public class RankedModePlayerScore
	{
		public string playfabId;

		public float gameScore;
	}

	[Serializable]
	public class RankedModeSubmitMatchScoresRequestData : RankedModeRequestDataBase
	{
		public string matchId;

		public string playfabId;

		public List<RankedModePlayerScore> playerScores;
	}

	[Serializable]
	public class RankedModeSetEloValueRequestData : RankedModeRequestDataPlatformed
	{
		public float elo;
	}

	[Serializable]
	public class RankedModeUnlockCompetitiveQueueRequestData : RankedModeRequestDataPlatformed
	{
		public bool unlocked;
	}

	public static GorillaTagCompetitiveServerApi Instance;

	public int MAX_SERVER_RETRIES = 3;

	private bool GetRankInformationInProgress;

	private int GetRankInformationRetryCount;

	private bool CreateMatchIdInProgress;

	private int CreateMatchIdRetryCount;

	private bool ValidateMatchJoinInProgress;

	private int ValidateMatchJoinRetryCount;

	private bool SubmitMatchScoresInProgress;

	private int SubmitMatchScoresRetryCount;

	private bool SetEloValueInProgress;

	private int SetEloValueRetryCount;

	private bool PingMatchInProgress;

	private int PingMatchRetryCount;

	private bool UnlockCompetitiveQueueInProgress;

	private int UnlockCompetitiveQueueRetryCount;

	private void Awake()
	{
		if ((bool)Instance)
		{
			GTDev.LogError("Duplicate GorillaTagCompetitiveServerApi detected. Destroying self.", base.gameObject);
			UnityEngine.Object.Destroy(this);
		}
		else
		{
			Instance = this;
		}
	}

	public void RequestGetRankInformation(List<string> playfabs, Action<RankedModeProgressionData> callback)
	{
		if (!MothershipClientContext.IsClientLoggedIn())
		{
			GTDev.LogWarning("GorillaTagCompetitiveServerApi RequestGetRankInformation Client Not Logged into Mothership");
			return;
		}
		if (GetRankInformationInProgress)
		{
			GTDev.LogWarning("GorillaTagCompetitiveServerApi RequestGetRankInformation already in progress");
			return;
		}
		GetRankInformationInProgress = true;
		string text = "Quest";
		text = "PC";
		StartCoroutine(GetRankInformation(new RankedModeProgressionRequestData
		{
			mothershipId = MothershipClientContext.MothershipId,
			mothershipToken = MothershipClientContext.Token,
			mothershipEnvId = MothershipClientApiUnity.EnvironmentId,
			platform = text,
			playfabIds = playfabs
		}, callback));
	}

	private IEnumerator GetRankInformation(RankedModeProgressionRequestData data, Action<RankedModeProgressionData> callback)
	{
		UnityWebRequest request = new UnityWebRequest(PlayFabAuthenticatorSettings.MmrApiBaseUrl + "/api/GetTier", "GET");
		byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(data));
		bool retry = false;
		request.uploadHandler = new UploadHandlerRaw(bytes);
		request.downloadHandler = new DownloadHandlerBuffer();
		request.SetRequestHeader("Content-Type", "application/json");
		yield return request.SendWebRequest();
		bool flag;
		if (request.result == UnityWebRequest.Result.Success)
		{
			OnCompleteGetRankInformation(request.downloadHandler.text, callback);
		}
		else
		{
			if (request.result == UnityWebRequest.Result.ProtocolError)
			{
				long responseCode = request.responseCode;
				if (responseCode >= 500)
				{
					if (responseCode < 600)
					{
						goto IL_0131;
					}
				}
				else if (responseCode == 408 || responseCode == 429)
				{
					goto IL_0131;
				}
				flag = false;
				goto IL_0139;
			}
			retry = true;
		}
		goto IL_0153;
		IL_0131:
		flag = true;
		goto IL_0139;
		IL_0153:
		if (retry)
		{
			if (GetRankInformationRetryCount < MAX_SERVER_RETRIES)
			{
				float time = UnityEngine.Random.Range(0.5f, Mathf.Pow(2f, GetRankInformationRetryCount + 1));
				GetRankInformationRetryCount++;
				yield return new WaitForSecondsRealtime(time);
				GetRankInformationInProgress = false;
				RequestGetRankInformation(data.playfabIds, callback);
			}
			else
			{
				GetRankInformationRetryCount = 0;
				OnCompleteGetRankInformation(null, callback);
			}
		}
		yield break;
		IL_0139:
		if (flag)
		{
			retry = true;
		}
		else
		{
			OnCompleteGetRankInformation(null, callback);
		}
		goto IL_0153;
	}

	private void OnCompleteGetRankInformation([CanBeNull] string response, Action<RankedModeProgressionData> callback)
	{
		GetRankInformationInProgress = false;
		GetRankInformationRetryCount = 0;
		if (!response.IsNullOrEmpty())
		{
			string text = "{ \"playerData\": " + response + " }";
			RankedModeProgressionData obj;
			try
			{
				obj = JsonUtility.FromJson<RankedModeProgressionData>(text);
			}
			catch (ArgumentException exception)
			{
				Debug.LogException(exception);
				Debug.LogError("[GT/GorillaTagCompetitiveServerApi]  ERROR!!!  OnCompleteGetRankInformation: Encountered ArgumentException above while trying to parse json string:\n" + text);
				return;
			}
			catch (Exception exception2)
			{
				Debug.LogException(exception2);
				Debug.LogError("[GT/GorillaTagCompetitiveServerApi]  ERROR!!!  OnCompleteGetRankInformation: Encountered exception above while trying to parse json string:\n" + text);
				return;
			}
			callback?.Invoke(obj);
		}
	}

	public void RequestCreateMatchId(Action<string> callback)
	{
		if (!MothershipClientContext.IsClientLoggedIn())
		{
			GTDev.LogWarning("GorillaTagCompetitiveServerApi RequestCreateMatchId Client Not Logged into Mothership");
			return;
		}
		if (CreateMatchIdInProgress)
		{
			GTDev.LogWarning("GorillaTagCompetitiveServerApi RequestCreateMatchId already in progress");
			return;
		}
		string text = "Quest";
		text = "PC";
		CreateMatchIdInProgress = true;
		StartCoroutine(CreateMatchId(new RankedModeRequestDataPlatformed
		{
			mothershipId = MothershipClientContext.MothershipId,
			mothershipToken = MothershipClientContext.Token,
			mothershipEnvId = MothershipClientApiUnity.EnvironmentId,
			platform = text
		}, callback));
	}

	private IEnumerator CreateMatchId(RankedModeRequestDataPlatformed data, Action<string> callback)
	{
		UnityWebRequest request = new UnityWebRequest(PlayFabAuthenticatorSettings.MmrApiBaseUrl + "/api/CreateMatchId", "POST");
		byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(data));
		bool retry = false;
		request.uploadHandler = new UploadHandlerRaw(bytes);
		request.downloadHandler = new DownloadHandlerBuffer();
		request.SetRequestHeader("Content-Type", "application/json");
		yield return request.SendWebRequest();
		bool flag;
		if (request.result == UnityWebRequest.Result.Success)
		{
			GTDev.Log("CreateMatchId Success: raw response: " + request.downloadHandler.text);
			OnCompleteCreateMatchId(request.downloadHandler.text, callback);
		}
		else
		{
			if (request.result == UnityWebRequest.Result.ProtocolError)
			{
				long responseCode = request.responseCode;
				if (responseCode >= 500)
				{
					if (responseCode < 600)
					{
						goto IL_0151;
					}
				}
				else if (responseCode == 408 || responseCode == 429)
				{
					goto IL_0151;
				}
				flag = false;
				goto IL_0159;
			}
			retry = true;
		}
		goto IL_0182;
		IL_0151:
		flag = true;
		goto IL_0159;
		IL_0182:
		if (retry)
		{
			if (CreateMatchIdRetryCount < MAX_SERVER_RETRIES)
			{
				float time = UnityEngine.Random.Range(0.5f, Mathf.Pow(2f, CreateMatchIdRetryCount + 1));
				CreateMatchIdRetryCount++;
				yield return new WaitForSecondsRealtime(time);
				CreateMatchIdInProgress = false;
				RequestCreateMatchId(callback);
			}
			else
			{
				CreateMatchIdRetryCount = 0;
				OnCompleteCreateMatchId(null, callback);
			}
		}
		yield break;
		IL_0159:
		if (flag)
		{
			retry = true;
		}
		else
		{
			OnCompleteCreateMatchId(request.downloadHandler.text, callback);
		}
		goto IL_0182;
	}

	private void OnCompleteCreateMatchId([CanBeNull] string response, Action<string> callback)
	{
		CreateMatchIdInProgress = false;
		CreateMatchIdRetryCount = 0;
		if (!response.IsNullOrEmpty())
		{
			callback?.Invoke(response);
		}
	}

	public void RequestValidateMatchJoin(string matchId, Action<bool> callback)
	{
		if (!MothershipClientContext.IsClientLoggedIn())
		{
			GTDev.LogWarning("GorillaTagCompetitiveServerApi RequestValidateMatchJoin Client Not Logged into Mothership");
			return;
		}
		if (ValidateMatchJoinInProgress)
		{
			GTDev.LogWarning("GorillaTagCompetitiveServerApi RequestValidateMatchJoin already in progress");
			return;
		}
		string text = "Quest";
		text = "PC";
		ValidateMatchJoinInProgress = true;
		StartCoroutine(ValidateMatchJoin(new RankedModeRequestDataWithMatchId
		{
			mothershipId = MothershipClientContext.MothershipId,
			mothershipToken = MothershipClientContext.Token,
			mothershipEnvId = MothershipClientApiUnity.EnvironmentId,
			platform = text,
			matchId = matchId
		}, callback));
	}

	private IEnumerator ValidateMatchJoin(RankedModeRequestDataWithMatchId data, Action<bool> callback)
	{
		UnityWebRequest request = new UnityWebRequest(PlayFabAuthenticatorSettings.MmrApiBaseUrl + "/api/ValidateMatchJoin", "POST");
		byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(data));
		bool retry = false;
		request.uploadHandler = new UploadHandlerRaw(bytes);
		request.downloadHandler = new DownloadHandlerBuffer();
		request.SetRequestHeader("Content-Type", "application/json");
		yield return request.SendWebRequest();
		bool flag;
		if (request.result == UnityWebRequest.Result.Success)
		{
			GTDev.Log("ValidateMatchJoin Success: raw response: " + request.downloadHandler.text);
			OnCompleteValidateMatchJoin(request.downloadHandler.text, callback);
		}
		else
		{
			if (request.result == UnityWebRequest.Result.ProtocolError)
			{
				long responseCode = request.responseCode;
				if (responseCode >= 500)
				{
					if (responseCode < 600)
					{
						goto IL_0151;
					}
				}
				else if (responseCode == 408 || responseCode == 429)
				{
					goto IL_0151;
				}
				flag = false;
				goto IL_0159;
			}
			retry = true;
		}
		goto IL_0182;
		IL_0151:
		flag = true;
		goto IL_0159;
		IL_0182:
		if (retry)
		{
			if (ValidateMatchJoinRetryCount < MAX_SERVER_RETRIES)
			{
				float time = UnityEngine.Random.Range(0.5f, Mathf.Pow(2f, ValidateMatchJoinRetryCount + 1));
				ValidateMatchJoinRetryCount++;
				yield return new WaitForSecondsRealtime(time);
				ValidateMatchJoinInProgress = false;
				RequestValidateMatchJoin(data.matchId, callback);
			}
			else
			{
				ValidateMatchJoinRetryCount = 0;
				OnCompleteValidateMatchJoin(null, callback);
			}
		}
		yield break;
		IL_0159:
		if (flag)
		{
			retry = true;
		}
		else
		{
			OnCompleteValidateMatchJoin(request.downloadHandler.text, callback);
		}
		goto IL_0182;
	}

	private void OnCompleteValidateMatchJoin([CanBeNull] string response, Action<bool> callback)
	{
		ValidateMatchJoinInProgress = false;
		ValidateMatchJoinRetryCount = 0;
		if (!response.IsNullOrEmpty())
		{
			RankedModeValidateMatchJoinResponseData rankedModeValidateMatchJoinResponseData = JsonUtility.FromJson<RankedModeValidateMatchJoinResponseData>(response);
			callback?.Invoke(rankedModeValidateMatchJoinResponseData.validJoin);
		}
	}

	public void RequestSubmitMatchScores(string matchId, List<RankedMultiplayerScore.PlayerScore> finalScores)
	{
		List<RankedModePlayerScore> list = new List<RankedModePlayerScore>();
		foreach (RankedMultiplayerScore.PlayerScore finalScore in finalScores)
		{
			NetPlayer player = NetworkSystem.Instance.GetPlayer(finalScore.PlayerId);
			list.Add(new RankedModePlayerScore
			{
				playfabId = player.UserId,
				gameScore = finalScore.GameScore
			});
		}
		RequestSubmitMatchScores(matchId, list);
	}

	private void RequestSubmitMatchScores(string matchId, List<RankedModePlayerScore> playerScores)
	{
		if (!MothershipClientContext.IsClientLoggedIn())
		{
			GTDev.LogWarning("GorillaTagCompetitiveServerApi RequestSubmitMatchScores Client Not Logged into Mothership");
			return;
		}
		if (SubmitMatchScoresInProgress)
		{
			GTDev.LogWarning("GorillaTagCompetitiveServerApi RequestSubmitMatchScores already in progress");
			return;
		}
		SubmitMatchScoresInProgress = true;
		StartCoroutine(SubmitMatchScores(new RankedModeSubmitMatchScoresRequestData
		{
			mothershipId = MothershipClientContext.MothershipId,
			mothershipToken = MothershipClientContext.Token,
			mothershipEnvId = MothershipClientApiUnity.EnvironmentId,
			matchId = matchId,
			playfabId = PlayFabAuthenticator.instance.GetPlayFabPlayerId(),
			playerScores = playerScores
		}));
	}

	private IEnumerator SubmitMatchScores(RankedModeSubmitMatchScoresRequestData data)
	{
		UnityWebRequest request = new UnityWebRequest(PlayFabAuthenticatorSettings.MmrApiBaseUrl + "/api/SubmitMatchScores", "POST");
		byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(data));
		bool retry = false;
		request.uploadHandler = new UploadHandlerRaw(bytes);
		request.downloadHandler = new DownloadHandlerBuffer();
		request.SetRequestHeader("Content-Type", "application/json");
		yield return request.SendWebRequest();
		bool flag;
		if (request.result == UnityWebRequest.Result.Success)
		{
			GTDev.Log("SubmitMatchScores Success: raw response: " + request.downloadHandler.text);
			OnCompleteSubmitMatchScores(request.downloadHandler.text);
		}
		else
		{
			if (request.result == UnityWebRequest.Result.ProtocolError)
			{
				long responseCode = request.responseCode;
				if (responseCode >= 500)
				{
					if (responseCode < 600)
					{
						goto IL_014b;
					}
				}
				else if (responseCode == 408 || responseCode == 429)
				{
					goto IL_014b;
				}
				flag = false;
				goto IL_0153;
			}
			retry = true;
		}
		goto IL_0176;
		IL_014b:
		flag = true;
		goto IL_0153;
		IL_0176:
		if (retry)
		{
			if (SubmitMatchScoresRetryCount < MAX_SERVER_RETRIES)
			{
				float time = UnityEngine.Random.Range(0.5f, Mathf.Pow(2f, SubmitMatchScoresRetryCount + 1));
				SubmitMatchScoresRetryCount++;
				yield return new WaitForSecondsRealtime(time);
				SubmitMatchScoresInProgress = false;
				RequestSubmitMatchScores(data.matchId, data.playerScores);
			}
			else
			{
				SubmitMatchScoresRetryCount = 0;
				OnCompleteSubmitMatchScores(null);
			}
		}
		yield break;
		IL_0153:
		if (flag)
		{
			retry = true;
		}
		else
		{
			OnCompleteSubmitMatchScores(request.downloadHandler.text);
		}
		goto IL_0176;
	}

	private void OnCompleteSubmitMatchScores([CanBeNull] string response)
	{
		SubmitMatchScoresInProgress = false;
		SubmitMatchScoresRetryCount = 0;
	}

	public void RequestSetEloValue(float desiredElo, Action callback)
	{
		if (!MothershipClientContext.IsClientLoggedIn())
		{
			GTDev.LogWarning("GorillaTagCompetitiveServerApi RequestSetEloValue Client Not Logged into Mothership");
			return;
		}
		if (SetEloValueInProgress)
		{
			GTDev.LogWarning("GorillaTagCompetitiveServerApi RequestSetEloValue already in progress");
			return;
		}
		string text = "Quest";
		text = "PC";
		SetEloValueInProgress = true;
		StartCoroutine(SetEloValue(new RankedModeSetEloValueRequestData
		{
			mothershipId = MothershipClientContext.MothershipId,
			mothershipToken = MothershipClientContext.Token,
			mothershipEnvId = MothershipClientApiUnity.EnvironmentId,
			platform = text,
			elo = desiredElo
		}, callback));
	}

	private IEnumerator SetEloValue(RankedModeSetEloValueRequestData data, Action callback)
	{
		GTDev.LogWarning("SetEloValue is for internal use only (Is Beta)");
		yield break;
	}

	private void OnCompleteSetEloValue([CanBeNull] string response, Action callback)
	{
		SetEloValueInProgress = false;
		SetEloValueRetryCount = 0;
		if (response != null)
		{
			callback?.Invoke();
		}
	}

	public void RequestPingRoom(string matchId, Action callback)
	{
		if (!MothershipClientContext.IsClientLoggedIn())
		{
			GTDev.LogWarning("GorillaTagCompetitiveServerApi RequestPingRoom Client Not Logged into Mothership");
			return;
		}
		if (SetEloValueInProgress)
		{
			GTDev.LogWarning("GorillaTagCompetitiveServerApi RequestPingRoom already in progress");
			return;
		}
		string text = "Quest";
		text = "PC";
		PingMatchInProgress = true;
		StartCoroutine(PingRoom(new RankedModeRequestDataWithMatchId
		{
			mothershipId = MothershipClientContext.MothershipId,
			mothershipToken = MothershipClientContext.Token,
			mothershipEnvId = MothershipClientApiUnity.EnvironmentId,
			platform = text,
			matchId = matchId
		}, callback));
	}

	private IEnumerator PingRoom(RankedModeRequestDataWithMatchId data, Action callback)
	{
		UnityWebRequest request = new UnityWebRequest(PlayFabAuthenticatorSettings.MmrApiBaseUrl + "/api/PingRoom", "POST");
		byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(data));
		bool retry = false;
		request.uploadHandler = new UploadHandlerRaw(bytes);
		request.downloadHandler = new DownloadHandlerBuffer();
		request.SetRequestHeader("Content-Type", "application/json");
		yield return request.SendWebRequest();
		bool flag;
		if (request.result == UnityWebRequest.Result.Success)
		{
			GTDev.Log("PingRoom Success: raw response: " + request.downloadHandler.text);
			OnCompletePingRoom(request.downloadHandler.text, callback);
		}
		else
		{
			if (request.result == UnityWebRequest.Result.ProtocolError)
			{
				long responseCode = request.responseCode;
				if (responseCode >= 500)
				{
					if (responseCode < 600)
					{
						goto IL_0151;
					}
				}
				else if (responseCode == 408 || responseCode == 429)
				{
					goto IL_0151;
				}
				flag = false;
				goto IL_0159;
			}
			retry = true;
		}
		goto IL_0182;
		IL_0151:
		flag = true;
		goto IL_0159;
		IL_0182:
		if (retry)
		{
			if (PingMatchRetryCount < MAX_SERVER_RETRIES)
			{
				float time = UnityEngine.Random.Range(0.5f, Mathf.Pow(2f, PingMatchRetryCount + 1));
				ValidateMatchJoinRetryCount++;
				yield return new WaitForSecondsRealtime(time);
				PingMatchInProgress = false;
				RequestPingRoom(data.matchId, callback);
			}
			else
			{
				PingMatchRetryCount = 0;
				OnCompletePingRoom(null, callback);
			}
		}
		yield break;
		IL_0159:
		if (flag)
		{
			retry = true;
		}
		else
		{
			OnCompletePingRoom(request.downloadHandler.text, callback);
		}
		goto IL_0182;
	}

	private void OnCompletePingRoom([CanBeNull] string response, Action callback)
	{
		GTDev.Log("PingRoom complete");
		PingMatchInProgress = false;
		PingMatchRetryCount = 0;
		if (response != null)
		{
			callback?.Invoke();
		}
	}

	public void RequestUnlockCompetitiveQueue(bool unlocked, Action callback)
	{
		if (!MothershipClientContext.IsClientLoggedIn())
		{
			GTDev.LogWarning("GorillaTagCompetitiveServerApi RequestUnlockCompetitiveQueue Client Not Logged into Mothership");
			return;
		}
		if (UnlockCompetitiveQueueInProgress)
		{
			GTDev.LogWarning("GorillaTagCompetitiveServerApi RequestUnlockCompetitiveQueue already in progress");
			return;
		}
		string text = "Quest";
		text = "PC";
		UnlockCompetitiveQueueInProgress = true;
		StartCoroutine(UnlockCompetitiveQueue(new RankedModeUnlockCompetitiveQueueRequestData
		{
			mothershipId = MothershipClientContext.MothershipId,
			mothershipToken = MothershipClientContext.Token,
			mothershipEnvId = MothershipClientApiUnity.EnvironmentId,
			platform = text,
			unlocked = unlocked
		}, callback));
	}

	private IEnumerator UnlockCompetitiveQueue(RankedModeUnlockCompetitiveQueueRequestData data, Action callback)
	{
		UnityWebRequest request = new UnityWebRequest(PlayFabAuthenticatorSettings.MmrApiBaseUrl + "/api/UnlockCompetitiveQueue", "POST");
		byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(data));
		bool retry = false;
		request.uploadHandler = new UploadHandlerRaw(bytes);
		request.downloadHandler = new DownloadHandlerBuffer();
		request.SetRequestHeader("Content-Type", "application/json");
		yield return request.SendWebRequest();
		bool flag;
		if (request.result == UnityWebRequest.Result.Success)
		{
			GTDev.Log("UnlockCompetitiveQueue Success: raw response: " + request.downloadHandler.text);
			OnCompleteUnlockCompetitiveQueue(request.downloadHandler.text, callback);
		}
		else
		{
			if (request.result == UnityWebRequest.Result.ProtocolError)
			{
				long responseCode = request.responseCode;
				if (responseCode >= 500)
				{
					if (responseCode < 600)
					{
						goto IL_0151;
					}
				}
				else if (responseCode == 408 || responseCode == 429)
				{
					goto IL_0151;
				}
				flag = false;
				goto IL_0159;
			}
			retry = true;
		}
		goto IL_0182;
		IL_0151:
		flag = true;
		goto IL_0159;
		IL_0182:
		if (retry)
		{
			if (UnlockCompetitiveQueueRetryCount < MAX_SERVER_RETRIES)
			{
				float time = UnityEngine.Random.Range(0.5f, Mathf.Pow(2f, UnlockCompetitiveQueueRetryCount + 1));
				ValidateMatchJoinRetryCount++;
				yield return new WaitForSecondsRealtime(time);
				UnlockCompetitiveQueueInProgress = false;
				RequestUnlockCompetitiveQueue(data.unlocked, callback);
			}
			else
			{
				UnlockCompetitiveQueueRetryCount = 0;
				OnCompleteUnlockCompetitiveQueue(null, callback);
			}
		}
		yield break;
		IL_0159:
		if (flag)
		{
			retry = true;
		}
		else
		{
			OnCompleteUnlockCompetitiveQueue(request.downloadHandler.text, callback);
		}
		goto IL_0182;
	}

	private void OnCompleteUnlockCompetitiveQueue([CanBeNull] string response, Action callback)
	{
		GTDev.Log("UnlockCompetitiveQueue complete");
		UnlockCompetitiveQueueInProgress = false;
		UnlockCompetitiveQueueRetryCount = 0;
		if (response != null)
		{
			callback?.Invoke();
		}
	}
}
