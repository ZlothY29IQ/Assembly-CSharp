using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using GorillaNetworking;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Oculus.Platform;
using Oculus.Platform.Models;
using UnityEngine;
using UnityEngine.Networking;

public class MonkeVoteController : MonoBehaviour, IGorillaSliceableSimple
{
	[Serializable]
	private class FetchPollsRequest
	{
		public string TitleId;

		public string PlayFabId;

		public string PlayFabTicket;

		public bool IncludeInactive;
	}

	[Serializable]
	public class FetchPollsResponse
	{
		public int PollId;

		public string Question;

		public List<string> VoteOptions;

		public List<int> VoteCount;

		public List<int> PredictionCount;

		public DateTime StartTime;

		public DateTime EndTime;

		public bool isActive;
	}

	[Serializable]
	private class VoteRequest
	{
		public int PollId;

		public string TitleId;

		public string PlayFabId;

		public string OculusId;

		public string UserNonce;

		public string UserPlatform;

		public int OptionIndex;

		public bool IsPrediction;

		public string PlayFabTicket;
	}

	[Serializable]
	public class VoteResponse
	{
		public int PollId { get; set; }

		public string TitleId { get; set; }

		public List<string> VoteOptions { get; set; }

		public List<int> VoteCount { get; set; }

		public List<int> PredictionCount { get; set; }
	}

	private string Nonce = "";

	private bool includeInactive = true;

	private int fetchPollsRetryCount;

	private int maxRetriesOnFail = 3;

	private int voteRetryCount;

	private FetchPollsResponse lastPollData;

	private FetchPollsResponse currentPollData;

	private VoteResponse lastVoteData;

	private bool isFetchingPoll;

	private bool hasPoll;

	private bool isCurrentPollActive;

	private bool hasCurrentPollCompleted;

	private DateTime currentPollCompletionTime;

	private bool isSendingVote;

	private int pollId = -1;

	private int option;

	private bool isPrediction;

	public static MonkeVoteController instance { get; private set; }

	public event Action OnPollsUpdated;

	public event Action OnVoteAccepted;

	public event Action OnVoteFailed;

	public event Action OnCurrentPollEnded;

	public void Awake()
	{
		if (instance == null)
		{
			instance = this;
		}
		else
		{
			UnityEngine.Object.Destroy(this);
		}
	}

	public void SliceUpdate()
	{
		if (isCurrentPollActive && !hasCurrentPollCompleted && currentPollCompletionTime < DateTime.UtcNow)
		{
			GTDev.Log("Active vote poll completed.");
			hasCurrentPollCompleted = true;
			this.OnCurrentPollEnded?.Invoke();
		}
	}

	public void OnEnable()
	{
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	public void OnDisable()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	public async void RequestPolls()
	{
		if (!isFetchingPoll && (!hasPoll || (isCurrentPollActive && hasCurrentPollCompleted)))
		{
			isFetchingPoll = true;
			await WaitForSessionToken();
			FetchPolls();
		}
		else
		{
			this.OnPollsUpdated?.Invoke();
		}
	}

	private async Task WaitForSessionToken()
	{
		while (!PlayFabAuthenticator.instance || PlayFabAuthenticator.instance.GetPlayFabPlayerId().IsNullOrEmpty() || PlayFabAuthenticator.instance.GetPlayFabSessionTicket().IsNullOrEmpty() || PlayFabAuthenticator.instance.userID.IsNullOrEmpty())
		{
			await Task.Yield();
			await Task.Delay(1000);
		}
	}

	private void FetchPolls()
	{
		StartCoroutine(DoFetchPolls(new FetchPollsRequest
		{
			TitleId = PlayFabAuthenticatorSettings.TitleId,
			PlayFabId = PlayFabAuthenticator.instance.GetPlayFabPlayerId(),
			PlayFabTicket = PlayFabAuthenticator.instance.GetPlayFabSessionTicket(),
			IncludeInactive = includeInactive
		}, OnFetchPollsResponse));
	}

	private IEnumerator DoFetchPolls(FetchPollsRequest data, Action<List<FetchPollsResponse>> callback)
	{
		UnityWebRequest request = new UnityWebRequest(PlayFabAuthenticatorSettings.VotingApiBaseUrl + "/api/FetchPoll", "POST");
		byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(data));
		bool retry = false;
		request.uploadHandler = new UploadHandlerRaw(bytes);
		request.downloadHandler = new DownloadHandlerBuffer();
		request.SetRequestHeader("Content-Type", "application/json");
		yield return request.SendWebRequest();
		if (request.result == UnityWebRequest.Result.Success)
		{
			List<FetchPollsResponse> obj = JsonConvert.DeserializeObject<List<FetchPollsResponse>>(request.downloadHandler.text);
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
			if (fetchPollsRetryCount < maxRetriesOnFail)
			{
				int num = (int)Mathf.Pow(2f, fetchPollsRetryCount + 1);
				fetchPollsRetryCount++;
				yield return new WaitForSecondsRealtime(num);
				FetchPolls();
			}
			else
			{
				GTDev.LogError("Maximum FetchPolls retries attempted. Please check your network connection.");
				fetchPollsRetryCount = 0;
				callback(null);
			}
		}
	}

	private void OnFetchPollsResponse([CanBeNull] List<FetchPollsResponse> response)
	{
		isFetchingPoll = false;
		hasPoll = false;
		lastPollData = null;
		currentPollData = null;
		isCurrentPollActive = false;
		hasCurrentPollCompleted = false;
		if (response != null)
		{
			DateTime minValue = DateTime.MinValue;
			foreach (FetchPollsResponse item in response)
			{
				if (item.isActive)
				{
					hasPoll = true;
					currentPollData = item;
					if (currentPollData.EndTime > DateTime.UtcNow)
					{
						isCurrentPollActive = true;
						hasCurrentPollCompleted = false;
						currentPollCompletionTime = currentPollData.EndTime;
						currentPollCompletionTime = currentPollCompletionTime.AddMinutes(1.0);
					}
				}
				if (!item.isActive && item.EndTime > minValue && item.EndTime < DateTime.UtcNow)
				{
					lastPollData = item;
				}
			}
		}
		else
		{
			GTDev.LogError("Error: Could not fetch polls!");
		}
		this.OnPollsUpdated?.Invoke();
	}

	public void Vote(int pollId, int option, bool isPrediction)
	{
		if (hasPoll && !isSendingVote)
		{
			isSendingVote = true;
			this.pollId = pollId;
			this.option = option;
			this.isPrediction = isPrediction;
			SendVote();
		}
	}

	private void SendVote()
	{
		GetNonceForVotingCallback(null);
	}

	private void GetNonceForVotingCallback([CanBeNull] Message<UserProof> message)
	{
		if (message != null)
		{
			Nonce = message.Data?.Value;
		}
		StartCoroutine(DoVote(new VoteRequest
		{
			PollId = pollId,
			TitleId = PlayFabAuthenticatorSettings.TitleId,
			PlayFabId = PlayFabAuthenticator.instance.GetPlayFabPlayerId(),
			OculusId = PlayFabAuthenticator.instance.userID,
			UserPlatform = PlayFabAuthenticator.instance.platform.ToString(),
			UserNonce = Nonce,
			PlayFabTicket = PlayFabAuthenticator.instance.GetPlayFabSessionTicket(),
			OptionIndex = option,
			IsPrediction = isPrediction
		}, OnVoteSuccess));
	}

	private IEnumerator DoVote(VoteRequest data, Action<VoteResponse> callback)
	{
		UnityWebRequest request = new UnityWebRequest(PlayFabAuthenticatorSettings.VotingApiBaseUrl + "/api/Vote", "POST");
		byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(data));
		bool retry = false;
		request.uploadHandler = new UploadHandlerRaw(bytes);
		request.downloadHandler = new DownloadHandlerBuffer();
		request.SetRequestHeader("Content-Type", "application/json");
		yield return request.SendWebRequest();
		if (request.result == UnityWebRequest.Result.Success)
		{
			VoteResponse obj = JsonConvert.DeserializeObject<VoteResponse>(request.downloadHandler.text);
			callback(obj);
		}
		else
		{
			long responseCode = request.responseCode;
			if (responseCode >= 500 && responseCode < 600)
			{
				retry = true;
			}
			else if (request.responseCode == 429)
			{
				GTDev.LogWarning("User already voted on this poll!");
				callback(null);
			}
			else if (request.result == UnityWebRequest.Result.ConnectionError)
			{
				retry = true;
			}
		}
		if (retry)
		{
			if (voteRetryCount < maxRetriesOnFail)
			{
				int num = (int)Mathf.Pow(2f, voteRetryCount + 1);
				voteRetryCount++;
				yield return new WaitForSecondsRealtime(num);
				SendVote();
			}
			else
			{
				GTDev.LogError("Maximum Vote retries attempted. Please check your network connection.");
				voteRetryCount = 0;
				callback(null);
			}
		}
		else
		{
			isSendingVote = false;
		}
	}

	private void OnVoteSuccess([CanBeNull] VoteResponse response)
	{
		isSendingVote = false;
		if (response != null)
		{
			lastVoteData = response;
			this.OnVoteAccepted?.Invoke();
		}
		else
		{
			this.OnVoteFailed?.Invoke();
		}
	}

	public FetchPollsResponse GetLastPollData()
	{
		return lastPollData;
	}

	public FetchPollsResponse GetCurrentPollData()
	{
		return currentPollData;
	}

	public VoteResponse GetVoteData()
	{
		return lastVoteData;
	}

	public int GetLastVotePollId()
	{
		return pollId;
	}

	public int GetLastVoteSelectedOption()
	{
		return option;
	}

	public bool GetLastVoteWasPrediction()
	{
		return isPrediction;
	}

	public DateTime GetCurrentPollCompletionTime()
	{
		return currentPollCompletionTime;
	}
}
