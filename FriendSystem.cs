using System;
using System.Collections.Generic;
using UnityEngine;

public class FriendSystem : MonoBehaviour
{
	public delegate void FriendRequestCallback(GTZone zone, int localId, int friendId, bool success);

	private struct FriendRequestData
	{
		public GTZone zone;

		public int sendingPlayerId;

		public int targetPlayerId;

		public float localTimeSent;

		public FriendRequestCallback completionCallback;
	}

	public delegate void FriendRemovalCallback(int friendId, bool success);

	private struct FriendRemovalData
	{
		public int targetPlayerId;

		public float localTimeSent;

		public FriendRemovalCallback completionCallback;
	}

	private enum FriendRequestStatus
	{
		Pending,
		Succeeded,
		Failed
	}

	public enum PlayerPrivacy
	{
		Visible,
		PublicOnly,
		Hidden
	}

	[OnEnterPlay_SetNull]
	public static volatile FriendSystem Instance;

	[SerializeField]
	private float friendRequestExpirationTime = 10f;

	private PlayerPrivacy localPlayerPrivacy;

	private List<FriendRequestData> pendingFriendRequests = new List<FriendRequestData>();

	private List<FriendRemovalData> pendingFriendRemovals = new List<FriendRemovalData>();

	private List<int> indexesToRemove = new List<int>();

	private float lastFriendsListRefresh;

	public PlayerPrivacy LocalPlayerPrivacy => localPlayerPrivacy;

	public event Action<List<FriendBackendController.Friend>> OnFriendListRefresh;

	public void SetLocalPlayerPrivacy(PlayerPrivacy privacyState)
	{
		localPlayerPrivacy = privacyState;
		FriendBackendController.Instance.SetPrivacyState(privacyState switch
		{
			PlayerPrivacy.PublicOnly => FriendBackendController.PrivacyState.PUBLIC_ONLY, 
			PlayerPrivacy.Hidden => FriendBackendController.PrivacyState.HIDDEN, 
			_ => FriendBackendController.PrivacyState.VISIBLE, 
		});
	}

	public void RefreshFriendsList()
	{
		FriendBackendController.Instance.GetFriends();
	}

	public void SendFriendRequest(NetPlayer targetPlayer, GTZone stationZone, FriendRequestCallback callback)
	{
		FriendRequestData item = new FriendRequestData
		{
			completionCallback = callback,
			sendingPlayerId = NetworkSystem.Instance.LocalPlayer.UserId.GetHashCode(),
			targetPlayerId = targetPlayer.UserId.GetHashCode(),
			localTimeSent = Time.realtimeSinceStartup,
			zone = stationZone
		};
		pendingFriendRequests.Add(item);
		FriendBackendController.Instance.AddFriend(targetPlayer);
	}

	public void RemoveFriend(FriendBackendController.Friend friend, FriendRemovalCallback callback = null)
	{
		pendingFriendRemovals.Add(new FriendRemovalData
		{
			completionCallback = callback,
			targetPlayerId = friend.Presence.FriendLinkId.GetHashCode(),
			localTimeSent = Time.realtimeSinceStartup
		});
		FriendBackendController.Instance.RemoveFriend(friend);
	}

	public bool HasPendingFriendRequest(GTZone zone, int senderId)
	{
		for (int i = 0; i < pendingFriendRequests.Count; i++)
		{
			if (pendingFriendRequests[i].zone == zone && pendingFriendRequests[i].sendingPlayerId == senderId)
			{
				return true;
			}
		}
		return false;
	}

	public bool CheckFriendshipWithPlayer(int targetActorNumber)
	{
		NetPlayer player = NetworkSystem.Instance.GetPlayer(targetActorNumber);
		if (player != null)
		{
			int hashCode = player.UserId.GetHashCode();
			List<FriendBackendController.Friend> friendsList = FriendBackendController.Instance.FriendsList;
			for (int i = 0; i < friendsList.Count; i++)
			{
				if (friendsList[i] != null && friendsList[i].Presence != null && friendsList[i].Presence.FriendLinkId.GetHashCode() == hashCode)
				{
					return true;
				}
			}
		}
		return false;
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

	private void Start()
	{
		FriendBackendController.Instance.OnGetFriendsComplete += OnGetFriendsReturned;
		FriendBackendController.Instance.OnAddFriendComplete += OnAddFriendReturned;
		FriendBackendController.Instance.OnRemoveFriendComplete += OnRemoveFriendReturned;
	}

	private void OnDestroy()
	{
		if (FriendBackendController.Instance != null)
		{
			FriendBackendController.Instance.OnGetFriendsComplete -= OnGetFriendsReturned;
			FriendBackendController.Instance.OnAddFriendComplete -= OnAddFriendReturned;
			FriendBackendController.Instance.OnRemoveFriendComplete -= OnRemoveFriendReturned;
		}
	}

	private void OnGetFriendsReturned(bool succeeded)
	{
		if (succeeded)
		{
			lastFriendsListRefresh = Time.realtimeSinceStartup;
			switch (FriendBackendController.Instance.MyPrivacyState)
			{
			default:
				localPlayerPrivacy = PlayerPrivacy.Visible;
				break;
			case FriendBackendController.PrivacyState.PUBLIC_ONLY:
				localPlayerPrivacy = PlayerPrivacy.PublicOnly;
				break;
			case FriendBackendController.PrivacyState.HIDDEN:
				localPlayerPrivacy = PlayerPrivacy.Hidden;
				break;
			}
			this.OnFriendListRefresh?.Invoke(FriendBackendController.Instance.FriendsList);
		}
	}

	private void OnAddFriendReturned(NetPlayer targetPlayer, bool succeeded)
	{
		int hashCode = targetPlayer.UserId.GetHashCode();
		indexesToRemove.Clear();
		for (int i = 0; i < pendingFriendRequests.Count; i++)
		{
			if (pendingFriendRequests[i].targetPlayerId == hashCode)
			{
				pendingFriendRequests[i].completionCallback?.Invoke(pendingFriendRequests[i].zone, pendingFriendRequests[i].sendingPlayerId, pendingFriendRequests[i].targetPlayerId, succeeded);
				indexesToRemove.Add(i);
			}
			else if (pendingFriendRequests[i].localTimeSent + friendRequestExpirationTime < Time.realtimeSinceStartup)
			{
				indexesToRemove.Add(i);
			}
		}
		for (int num = indexesToRemove.Count - 1; num >= 0; num--)
		{
			pendingFriendRequests.RemoveAt(indexesToRemove[num]);
		}
	}

	private void OnRemoveFriendReturned(FriendBackendController.Friend friend, bool succeeded)
	{
		if (friend == null || friend.Presence == null)
		{
			return;
		}
		int hashCode = friend.Presence.FriendLinkId.GetHashCode();
		indexesToRemove.Clear();
		for (int i = 0; i < pendingFriendRemovals.Count; i++)
		{
			if (pendingFriendRemovals[i].targetPlayerId == hashCode)
			{
				pendingFriendRemovals[i].completionCallback?.Invoke(hashCode, succeeded);
				indexesToRemove.Add(i);
			}
			else if (pendingFriendRemovals[i].localTimeSent + friendRequestExpirationTime < Time.realtimeSinceStartup)
			{
				indexesToRemove.Add(i);
			}
		}
		for (int num = indexesToRemove.Count - 1; num >= 0; num--)
		{
			pendingFriendRemovals.RemoveAt(indexesToRemove[num]);
		}
	}
}
