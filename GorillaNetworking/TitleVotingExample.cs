using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Oculus.Platform;
using Oculus.Platform.Models;
using UnityEngine;
using UnityEngine.Networking;

namespace GorillaNetworking;

public class TitleVotingExample : MonoBehaviour
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
	private class FetchPollsResponse
	{
		public int PollId;

		public string Question;

		public List<string> VoteOptions;

		public List<int> VoteCount;

		public List<int> PredictionCount;

		public DateTime StartTime;

		public DateTime EndTime;
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
	private class VoteResponse
	{
		public int PollId { get; set; }

		public string TitleId { get; set; }

		public List<string> VoteOptions { get; set; }

		public List<int> VoteCount { get; set; }

		public List<int> PredictionCount { get; set; }
	}

	private string Nonce = "";

	private int PollId = 5;

	private bool includeInactive = true;

	private int Option;

	private bool isPrediction;

	private int fetchPollsRetryCount;

	private int voteRetryCount;

	private int maxRetriesOnFail = 3;

	public async void Start()
	{
		await WaitForSessionToken();
		FetchPollsAndVote();
	}

	public void Update()
	{
	}

	private async Task WaitForSessionToken()
	{
		while (!PlayFabAuthenticator.instance || PlayFabAuthenticator.instance.GetPlayFabPlayerId().IsNullOrEmpty() || PlayFabAuthenticator.instance.GetPlayFabSessionTicket().IsNullOrEmpty() || PlayFabAuthenticator.instance.userID.IsNullOrEmpty())
		{
			await Task.Yield();
			await Task.Delay(1000);
		}
	}

	public void FetchPollsAndVote()
	{
		StartCoroutine(DoFetchPolls(new FetchPollsRequest
		{
			TitleId = PlayFabAuthenticatorSettings.TitleId,
			PlayFabId = PlayFabAuthenticator.instance.GetPlayFabPlayerId(),
			PlayFabTicket = PlayFabAuthenticator.instance.GetPlayFabSessionTicket(),
			IncludeInactive = includeInactive
		}, OnFetchPollsResponse));
	}

	private void GetNonceForVotingCallback([CanBeNull] Message<UserProof> message)
	{
		if (message != null)
		{
			Nonce = message.Data?.ToString();
		}
		StartCoroutine(DoVote(new VoteRequest
		{
			PollId = PollId,
			TitleId = PlayFabAuthenticatorSettings.TitleId,
			PlayFabId = PlayFabAuthenticator.instance.GetPlayFabPlayerId(),
			OculusId = PlayFabAuthenticator.instance.userID,
			UserPlatform = PlayFabAuthenticator.instance.platform.ToString(),
			UserNonce = Nonce,
			PlayFabTicket = PlayFabAuthenticator.instance.GetPlayFabSessionTicket(),
			OptionIndex = Option,
			IsPrediction = isPrediction
		}, OnVoteSuccess));
	}

	public void Vote()
	{
		GetNonceForVotingCallback(null);
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
			Debug.LogError($"FetchPolls Error: {request.responseCode} -- raw response: " + request.downloadHandler.text);
			long responseCode = request.responseCode;
			if (responseCode >= 500 && responseCode < 600)
			{
				retry = true;
				Debug.LogError($"HTTP {request.responseCode} error: {request.error}");
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
				Debug.LogWarning($"Retrying Title Voting FetchPolls... Retry attempt #{fetchPollsRetryCount + 1}, waiting for {num} seconds");
				fetchPollsRetryCount++;
				yield return new WaitForSeconds(num);
				FetchPollsAndVote();
			}
			else
			{
				Debug.LogError("Maximum FetchPolls retries attempted. Please check your network connection.");
				fetchPollsRetryCount = 0;
				callback(null);
			}
		}
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
			Debug.LogError($"Vote Error: {request.responseCode} -- raw response: " + request.downloadHandler.text);
			long responseCode = request.responseCode;
			if (responseCode >= 500 && responseCode < 600)
			{
				retry = true;
				Debug.LogError($"HTTP {request.responseCode} error: {request.error}");
			}
			else if (request.responseCode == 409)
			{
				Debug.LogWarning("User already voted on this poll!");
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
				Debug.LogWarning($"Retrying Voting... Retry attempt #{voteRetryCount + 1}, waiting for {num} seconds");
				voteRetryCount++;
				yield return new WaitForSeconds(num);
				Vote();
			}
			else
			{
				Debug.LogError("Maximum Vote retries attempted. Please check your network connection.");
				voteRetryCount = 0;
				callback(null);
			}
		}
	}

	private void OnFetchPollsResponse([CanBeNull] List<FetchPollsResponse> response)
	{
		if (response != null)
		{
			Debug.Log("Got polls: " + JsonConvert.SerializeObject(response));
			Vote();
		}
		else
		{
			Debug.LogError("Error: Could not fetch polls!");
		}
	}

	private void OnVoteSuccess([CanBeNull] VoteResponse response)
	{
		if (response != null)
		{
			Debug.Log("Voted! " + JsonConvert.SerializeObject(response));
		}
		else
		{
			Debug.LogError("Error: Could not vote!");
		}
	}
}
