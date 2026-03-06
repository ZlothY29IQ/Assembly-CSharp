using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using GorillaNetworking;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

public class FriendBackendController : MonoBehaviour
{
	public class Friend
	{
		public FriendPresence Presence { get; set; }

		public DateTime Created { get; set; }
	}

	public class FriendPresence
	{
		public string FriendLinkId { get; set; }

		public string UserName { get; set; }

		public string RoomId { get; set; }

		public string Zone { get; set; }

		public string Region { get; set; }

		public bool? IsPublic { get; set; }
	}

	public class FriendLink
	{
		public string my_playfab_id { get; set; }

		public string my_mothership_id { get; set; }

		public string my_friendlink_id { get; set; }

		public string friend_playfab_id { get; set; }

		public string friend_mothership_id { get; set; }

		public string friend_friendlink_id { get; set; }

		public DateTime created { get; set; }
	}

	public class FriendIdResponse
	{
		public string? PlayFabId { get; set; }

		public string? MothershipId { get; set; } = "";
	}

	public class FriendRequestRequest
	{
		public string PlayFabId { get; set; }

		public string MothershipId { get; set; } = "";

		public string PlayFabTicket { get; set; }

		public string MothershipToken { get; set; }

		public string MyFriendLinkId { get; set; }

		public string FriendFriendLinkId { get; set; }
	}

	public class GetFriendsRequest
	{
		public string PlayFabId { get; set; }

		public string MothershipId { get; set; } = "";

		public string MothershipToken { get; set; }

		public string PlayFabTicket { get; set; }
	}

	public class GetFriendsResponse
	{
		[CanBeNull]
		public GetFriendsResult Result { get; set; }

		public int StatusCode { get; set; }

		public string? Error { get; set; }
	}

	public class GetFriendsResult
	{
		public List<Friend> Friends { get; set; }

		public PrivacyState MyPrivacyState { get; set; }
	}

	public class SetPrivacyStateRequest
	{
		public string PlayFabId { get; set; }

		public string PlayFabTicket { get; set; }

		public string PrivacyState { get; set; }
	}

	public class SetPrivacyStateResponse
	{
		public int StatusCode { get; set; }

		public string? Error { get; set; }
	}

	public class RemoveFriendRequest
	{
		public string PlayFabId { get; set; }

		public string MothershipId { get; set; } = "";

		public string PlayFabTicket { get; set; }

		public string MothershipToken { get; set; }

		public string MyFriendLinkId { get; set; }

		public string FriendFriendLinkId { get; set; }
	}

	public enum PendingRequestStatus
	{
		I_REQUESTED,
		THEY_REQUESTED,
		CONFIRMED,
		NOT_FOUND
	}

	public enum PrivacyState
	{
		VISIBLE,
		PUBLIC_ONLY,
		HIDDEN
	}

	[OnEnterPlay_SetNull]
	public static volatile FriendBackendController Instance;

	private int maxRetriesOnFail = 3;

	private int getFriendsRetryCount;

	private int setPrivacyStateRetryCount;

	private int addFriendRetryCount;

	private int removeFriendRetryCount;

	private bool getFriendsInProgress;

	private GetFriendsResponse lastGetFriendsResponse;

	private List<Friend> lastFriendsList = new List<Friend>();

	private bool setPrivacyStateInProgress;

	private PrivacyState setPrivacyStateState;

	private SetPrivacyStateResponse lastPrivacyStateResponse;

	private Queue<PrivacyState> setPrivacyStateQueue = new Queue<PrivacyState>();

	private PrivacyState lastPrivacyState;

	private bool addFriendInProgress;

	private int addFriendTargetIdHash;

	private NetPlayer addFriendTargetPlayer;

	private Queue<(int, NetPlayer)> addFriendRequestQueue = new Queue<(int, NetPlayer)>();

	private bool removeFriendInProgress;

	private int removeFriendTargetIdHash;

	private Friend removeFriendTarget;

	private Queue<(int, Friend)> removeFriendRequestQueue = new Queue<(int, Friend)>();

	[SerializeField]
	private int netPlayerIndexToAddFriend;

	[SerializeField]
	private int friendListIndexToRemoveFriend;

	[SerializeField]
	private PrivacyState privacyStateToSet;

	public List<Friend> FriendsList => lastFriendsList;

	public PrivacyState MyPrivacyState => lastPrivacyState;

	public event Action<bool> OnGetFriendsComplete;

	public event Action<bool> OnSetPrivacyStateComplete;

	public event Action<NetPlayer, bool> OnAddFriendComplete;

	public event Action<Friend, bool> OnRemoveFriendComplete;

	public void GetFriends()
	{
		if (!getFriendsInProgress)
		{
			getFriendsInProgress = true;
			GetFriendsInternal();
		}
	}

	public void SetPrivacyState(PrivacyState state)
	{
		if (!setPrivacyStateInProgress)
		{
			setPrivacyStateInProgress = true;
			setPrivacyStateState = state;
			SetPrivacyStateInternal();
		}
		else
		{
			setPrivacyStateQueue.Enqueue(state);
		}
	}

	public void AddFriend(NetPlayer target)
	{
		if (target != null)
		{
			int hashCode = target.UserId.GetHashCode();
			if (!addFriendInProgress)
			{
				addFriendInProgress = true;
				addFriendTargetIdHash = hashCode;
				addFriendTargetPlayer = target;
				AddFriendInternal();
			}
			else if (hashCode != addFriendTargetIdHash && !addFriendRequestQueue.Contains((hashCode, target)))
			{
				addFriendRequestQueue.Enqueue((hashCode, target));
			}
		}
	}

	public void RemoveFriend(Friend target)
	{
		if (target != null)
		{
			int hashCode = target.Presence.FriendLinkId.GetHashCode();
			if (!removeFriendInProgress)
			{
				removeFriendInProgress = true;
				removeFriendTargetIdHash = hashCode;
				removeFriendTarget = target;
				RemoveFriendInternal();
			}
			else if (hashCode != addFriendTargetIdHash && !removeFriendRequestQueue.Contains((hashCode, target)))
			{
				removeFriendRequestQueue.Enqueue((hashCode, target));
			}
		}
	}

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			UnityEngine.Object.Destroy(this);
		}
	}

	private void GetFriendsInternal()
	{
		StartCoroutine(SendGetFriendsRequest(new GetFriendsRequest
		{
			PlayFabId = PlayFabAuthenticator.instance.GetPlayFabPlayerId(),
			PlayFabTicket = PlayFabAuthenticator.instance.GetPlayFabSessionTicket(),
			MothershipId = ""
		}, GetFriendsComplete));
	}

	private IEnumerator SendGetFriendsRequest(GetFriendsRequest data, Action<GetFriendsResponse> callback)
	{
		UnityWebRequest request = new UnityWebRequest(PlayFabAuthenticatorSettings.FriendApiBaseUrl + "/api/GetFriendsV2", "POST");
		byte[] bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data));
		request.uploadHandler = new UploadHandlerRaw(bytes);
		request.downloadHandler = new DownloadHandlerBuffer();
		request.SetRequestHeader("Content-Type", "application/json");
		yield return request.SendWebRequest();
		bool flag = false;
		if (request.result == UnityWebRequest.Result.Success)
		{
			GetFriendsResponse obj = JsonConvert.DeserializeObject<GetFriendsResponse>(request.downloadHandler.text);
			callback(obj);
		}
		else
		{
			long responseCode = request.responseCode;
			if (responseCode >= 500 && responseCode < 600)
			{
				flag = true;
			}
			else if (request.result == UnityWebRequest.Result.ConnectionError)
			{
				flag = true;
			}
		}
		if (flag)
		{
			if (getFriendsRetryCount < maxRetriesOnFail)
			{
				int num = (int)Mathf.Pow(2f, getFriendsRetryCount + 1);
				getFriendsRetryCount++;
				yield return new WaitForSecondsRealtime(num);
				GetFriendsInternal();
			}
			else
			{
				GTDev.LogError("Maximum GetFriends retries attempted. Please check your network connection.");
				getFriendsRetryCount = 0;
				callback(null);
			}
		}
		else
		{
			getFriendsInProgress = false;
		}
	}

	private void GetFriendsComplete([CanBeNull] GetFriendsResponse response)
	{
		getFriendsInProgress = false;
		if (response != null)
		{
			lastGetFriendsResponse = response;
			if (lastGetFriendsResponse.Result != null)
			{
				lastPrivacyState = lastGetFriendsResponse.Result.MyPrivacyState;
				if (lastGetFriendsResponse.Result.Friends != null)
				{
					lastFriendsList.Clear();
					foreach (Friend friend in lastGetFriendsResponse.Result.Friends)
					{
						lastFriendsList.Add(friend);
					}
				}
			}
			this.OnGetFriendsComplete?.Invoke(obj: true);
		}
		else
		{
			this.OnGetFriendsComplete?.Invoke(obj: false);
		}
	}

	public void CreateTestFriends()
	{
		Debug.Log("Adding test friends");
		for (int i = 0; i < 15; i++)
		{
			FriendPresence friendPresence = new FriendPresence();
			friendPresence.FriendLinkId = i.ToString();
			friendPresence.UserName = i.ToString();
			friendPresence.RoomId = i.ToString();
			friendPresence.Zone = "TreeHouse";
			friendPresence.Region = "Jungle";
			friendPresence.IsPublic = true;
			Friend friend = new Friend();
			friend.Presence = friendPresence;
			friend.Created = DateTime.Now;
			FriendsList.Add(friend);
		}
	}

	private void SetPrivacyStateInternal()
	{
		StartCoroutine(SendSetPrivacyStateRequest(new SetPrivacyStateRequest
		{
			PlayFabId = PlayFabAuthenticator.instance.GetPlayFabPlayerId(),
			PlayFabTicket = PlayFabAuthenticator.instance.GetPlayFabSessionTicket(),
			PrivacyState = setPrivacyStateState.ToString()
		}, SetPrivacyStateComplete));
	}

	private IEnumerator SendSetPrivacyStateRequest(SetPrivacyStateRequest data, Action<SetPrivacyStateResponse> callback)
	{
		UnityWebRequest request = new UnityWebRequest(PlayFabAuthenticatorSettings.FriendApiBaseUrl + "/api/SetPrivacyState", "POST");
		byte[] bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data));
		request.uploadHandler = new UploadHandlerRaw(bytes);
		request.downloadHandler = new DownloadHandlerBuffer();
		request.SetRequestHeader("Content-Type", "application/json");
		yield return request.SendWebRequest();
		bool flag = false;
		if (request.result == UnityWebRequest.Result.Success)
		{
			SetPrivacyStateResponse obj = JsonConvert.DeserializeObject<SetPrivacyStateResponse>(request.downloadHandler.text);
			callback(obj);
		}
		else
		{
			long responseCode = request.responseCode;
			if (responseCode >= 500 && responseCode < 600)
			{
				flag = true;
			}
			else if (request.result == UnityWebRequest.Result.ConnectionError)
			{
				flag = true;
			}
		}
		if (flag)
		{
			if (setPrivacyStateRetryCount < maxRetriesOnFail)
			{
				int num = (int)Mathf.Pow(2f, setPrivacyStateRetryCount + 1);
				setPrivacyStateRetryCount++;
				yield return new WaitForSecondsRealtime(num);
				SetPrivacyStateInternal();
			}
			else
			{
				GTDev.LogError("Maximum SetPrivacyState retries attempted. Please check your network connection.");
				setPrivacyStateRetryCount = 0;
				callback(null);
			}
		}
		else
		{
			setPrivacyStateInProgress = false;
		}
	}

	private void SetPrivacyStateComplete([CanBeNull] SetPrivacyStateResponse response)
	{
		setPrivacyStateInProgress = false;
		if (response != null)
		{
			lastPrivacyStateResponse = response;
			this.OnSetPrivacyStateComplete?.Invoke(obj: true);
		}
		else
		{
			this.OnSetPrivacyStateComplete?.Invoke(obj: false);
		}
		if (setPrivacyStateQueue.Count > 0)
		{
			PrivacyState privacyState = setPrivacyStateQueue.Dequeue();
			SetPrivacyState(privacyState);
		}
	}

	private void AddFriendInternal()
	{
		StartCoroutine(SendAddFriendRequest(new FriendRequestRequest
		{
			PlayFabId = PlayFabAuthenticator.instance.GetPlayFabPlayerId(),
			MothershipId = "",
			PlayFabTicket = PlayFabAuthenticator.instance.GetPlayFabSessionTicket(),
			MothershipToken = "",
			MyFriendLinkId = NetworkSystem.Instance.LocalPlayer.UserId,
			FriendFriendLinkId = addFriendTargetPlayer.UserId
		}, AddFriendComplete));
	}

	private IEnumerator SendAddFriendRequest(FriendRequestRequest data, Action<bool> callback)
	{
		UnityWebRequest request = new UnityWebRequest(PlayFabAuthenticatorSettings.FriendApiBaseUrl + "/api/RequestFriend", "POST");
		byte[] bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data));
		request.uploadHandler = new UploadHandlerRaw(bytes);
		request.downloadHandler = new DownloadHandlerBuffer();
		request.SetRequestHeader("Content-Type", "application/json");
		yield return request.SendWebRequest();
		bool flag = false;
		if (request.result == UnityWebRequest.Result.Success)
		{
			callback(obj: true);
		}
		else
		{
			if (request.responseCode == 409)
			{
				flag = false;
			}
			long responseCode = request.responseCode;
			if (responseCode >= 500 && responseCode < 600)
			{
				flag = true;
			}
			else if (request.result == UnityWebRequest.Result.ConnectionError)
			{
				flag = true;
			}
		}
		if (flag)
		{
			if (addFriendRetryCount < maxRetriesOnFail)
			{
				int num = (int)Mathf.Pow(2f, addFriendRetryCount + 1);
				addFriendRetryCount++;
				yield return new WaitForSecondsRealtime(num);
				AddFriendInternal();
			}
			else
			{
				GTDev.LogError("Maximum AddFriend retries attempted. Please check your network connection.");
				addFriendRetryCount = 0;
				callback(obj: false);
			}
		}
		else
		{
			addFriendInProgress = false;
		}
	}

	private void AddFriendComplete([CanBeNull] bool success)
	{
		if (success)
		{
			this.OnAddFriendComplete?.Invoke(addFriendTargetPlayer, arg2: true);
		}
		else
		{
			this.OnAddFriendComplete?.Invoke(addFriendTargetPlayer, arg2: false);
		}
		addFriendInProgress = false;
		addFriendTargetIdHash = 0;
		addFriendTargetPlayer = null;
		if (addFriendRequestQueue.Count > 0)
		{
			AddFriend(addFriendRequestQueue.Dequeue().Item2);
		}
	}

	private void RemoveFriendInternal()
	{
		StartCoroutine(SendRemoveFriendRequest(new RemoveFriendRequest
		{
			PlayFabId = PlayFabAuthenticator.instance.GetPlayFabPlayerId(),
			MothershipId = "",
			PlayFabTicket = PlayFabAuthenticator.instance.GetPlayFabSessionTicket(),
			MyFriendLinkId = NetworkSystem.Instance.LocalPlayer.UserId,
			FriendFriendLinkId = removeFriendTarget.Presence.FriendLinkId
		}, RemoveFriendComplete));
	}

	private IEnumerator SendRemoveFriendRequest(RemoveFriendRequest data, Action<bool> callback)
	{
		UnityWebRequest request = new UnityWebRequest(PlayFabAuthenticatorSettings.FriendApiBaseUrl + "/api/RemoveFriend", "POST");
		byte[] bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data));
		request.uploadHandler = new UploadHandlerRaw(bytes);
		request.downloadHandler = new DownloadHandlerBuffer();
		request.SetRequestHeader("Content-Type", "application/json");
		yield return request.SendWebRequest();
		bool flag = false;
		if (request.result == UnityWebRequest.Result.Success)
		{
			callback(obj: true);
		}
		else
		{
			long responseCode = request.responseCode;
			if (responseCode >= 500 && responseCode < 600)
			{
				flag = true;
			}
			else if (request.result == UnityWebRequest.Result.ConnectionError)
			{
				flag = true;
			}
		}
		if (flag)
		{
			if (removeFriendRetryCount < maxRetriesOnFail)
			{
				int num = (int)Mathf.Pow(2f, removeFriendRetryCount + 1);
				removeFriendRetryCount++;
				yield return new WaitForSecondsRealtime(num);
				AddFriendInternal();
			}
			else
			{
				GTDev.LogError("Maximum AddFriend retries attempted. Please check your network connection.");
				removeFriendRetryCount = 0;
				callback(obj: false);
			}
		}
		else
		{
			removeFriendInProgress = false;
		}
	}

	private void RemoveFriendComplete([CanBeNull] bool success)
	{
		if (success)
		{
			this.OnRemoveFriendComplete?.Invoke(removeFriendTarget, arg2: true);
		}
		else
		{
			this.OnRemoveFriendComplete?.Invoke(removeFriendTarget, arg2: false);
		}
		removeFriendInProgress = false;
		removeFriendTargetIdHash = 0;
		removeFriendTarget = null;
		if (removeFriendRequestQueue.Count > 0)
		{
			RemoveFriend(removeFriendRequestQueue.Dequeue().Item2);
		}
	}

	private void LogNetPlayersInRoom()
	{
		Debug.Log("Local Player PlayfabId: " + PlayFabAuthenticator.instance.GetPlayFabPlayerId());
		int num = 0;
		NetPlayer[] allNetPlayers = NetworkSystem.Instance.AllNetPlayers;
		foreach (NetPlayer netPlayer in allNetPlayers)
		{
			Debug.Log($"[{num}] Player: {netPlayer.NickName}, ActorNumber: {netPlayer.ActorNumber}, UserID: {netPlayer.UserId}, IsMasterClient: {netPlayer.IsMasterClient}");
			num++;
		}
	}

	private void TestAddFriend()
	{
		OnAddFriendComplete -= TestAddFriendCompleteCallback;
		OnAddFriendComplete += TestAddFriendCompleteCallback;
		NetPlayer target = null;
		if (netPlayerIndexToAddFriend >= 0 && netPlayerIndexToAddFriend < NetworkSystem.Instance.AllNetPlayers.Length)
		{
			target = NetworkSystem.Instance.AllNetPlayers[netPlayerIndexToAddFriend];
		}
		AddFriend(target);
	}

	private void TestAddFriendCompleteCallback(NetPlayer player, bool success)
	{
		if (success)
		{
			Debug.Log("FriendBackend: TestAddFriendCompleteCallback returned with success = true");
		}
		else
		{
			Debug.Log("FriendBackend: TestAddFriendCompleteCallback returned with success = false");
		}
	}

	private void TestRemoveFriend()
	{
		OnRemoveFriendComplete -= TestRemoveFriendCompleteCallback;
		OnRemoveFriendComplete += TestRemoveFriendCompleteCallback;
		Friend target = null;
		if (friendListIndexToRemoveFriend >= 0 && friendListIndexToRemoveFriend < FriendsList.Count)
		{
			target = FriendsList[friendListIndexToRemoveFriend];
		}
		RemoveFriend(target);
	}

	private void TestRemoveFriendCompleteCallback(Friend friend, bool success)
	{
		if (success)
		{
			Debug.Log("FriendBackend: TestRemoveFriendCompleteCallback returned with success = true");
		}
		else
		{
			Debug.Log("FriendBackend: TestRemoveFriendCompleteCallback returned with success = false");
		}
	}

	private void TestGetFriends()
	{
		OnGetFriendsComplete -= TestGetFriendsCompleteCallback;
		OnGetFriendsComplete += TestGetFriendsCompleteCallback;
		GetFriends();
	}

	private void TestGetFriendsCompleteCallback(bool success)
	{
		if (success)
		{
			Debug.Log("FriendBackend: TestGetFriendsCompleteCallback returned with success = true");
			if (FriendsList != null)
			{
				string text = $"Friend Count: {FriendsList.Count} Friends: \n";
				for (int i = 0; i < FriendsList.Count; i++)
				{
					text = ((FriendsList[i] == null || FriendsList[i].Presence == null) ? (text + "null friend\n") : (text + FriendsList[i].Presence.UserName + ", " + FriendsList[i].Presence.FriendLinkId + ", " + FriendsList[i].Presence.RoomId + ", " + FriendsList[i].Presence.Region + ", " + FriendsList[i].Presence.Zone + "\n"));
				}
				Debug.Log(text);
			}
		}
		else
		{
			Debug.Log("FriendBackend: TestGetFriendsCompleteCallback returned with success = false");
		}
	}

	private void TestSetPrivacyState()
	{
		OnSetPrivacyStateComplete -= TestSetPrivacyStateCompleteCallback;
		OnSetPrivacyStateComplete += TestSetPrivacyStateCompleteCallback;
		SetPrivacyState(privacyStateToSet);
	}

	private void TestSetPrivacyStateCompleteCallback(bool success)
	{
		if (success)
		{
			Debug.Log($"SetPrivacyState Success: Status: {lastPrivacyStateResponse.StatusCode} Error: {lastPrivacyStateResponse.Error}");
		}
		else
		{
			Debug.Log($"SetPrivacyState Failed: Status: {lastPrivacyStateResponse.StatusCode} Error: {lastPrivacyStateResponse.Error}");
		}
	}
}
