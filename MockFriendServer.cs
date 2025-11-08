using System;
using System.Collections.Generic;
using GorillaNetworking;
using Photon.Pun;
using UnityEngine;

public class MockFriendServer : MonoBehaviourPun
{
	public struct FriendPair
	{
		public int publicIdPlayerA;

		public int publicIdPlayerB;

		public int privateIdPlayerA;

		public int privateIdPlayerB;
	}

	public struct PrivateIdEncryptionPlaceholder
	{
		public int playerPublicId;

		public int playerPrivateId;
	}

	public struct FriendRequest
	{
		public int requestorPublicId;

		public int requesteePublicId;

		public float requestTime;

		public float completionTime;
	}

	[OnEnterPlay_SetNull]
	public static volatile MockFriendServer Instance;

	[SerializeField]
	private Vector2 friendRequestCompletionDelayRange = new Vector2(0.5f, 1f);

	[SerializeField]
	private float friendRequestExpirationTime = 10f;

	private List<FriendPair> friendPairList = new List<FriendPair>();

	private List<PrivateIdEncryptionPlaceholder> privateIdLookup = new List<PrivateIdEncryptionPlaceholder>();

	private List<FriendRequest> friendRequests = new List<FriendRequest>();

	private List<int> indexesToRemove = new List<int>();

	public int LocalPlayerId => PhotonNetwork.LocalPlayer.UserId.GetHashCode();

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
			PhotonNetwork.AddCallbackTarget(this);
			NetworkSystem.Instance.OnMultiplayerStarted += new Action(OnMultiplayerStarted);
		}
		else
		{
			UnityEngine.Object.Destroy(this);
		}
	}

	private void OnMultiplayerStarted()
	{
		RegisterLocalPlayer(LocalPlayerId);
	}

	private void Update()
	{
		if (!PhotonNetwork.InRoom || !base.photonView.IsMine)
		{
			return;
		}
		indexesToRemove.Clear();
		for (int i = 0; i < friendRequests.Count; i++)
		{
			if (friendRequests[i].requestTime + friendRequestExpirationTime < Time.time)
			{
				indexesToRemove.Add(i);
			}
		}
		for (int j = 0; j < indexesToRemove.Count; j++)
		{
			friendRequests.RemoveAt(indexesToRemove[j]);
		}
		indexesToRemove.Clear();
		for (int k = 0; k < friendRequests.Count; k++)
		{
			if (friendRequests[k].requestTime + friendRequestExpirationTime < Time.time)
			{
				indexesToRemove.Add(k);
			}
			else
			{
				if (!(friendRequests[k].completionTime < Time.time))
				{
					continue;
				}
				for (int l = k + 1; l < friendRequests.Count; l++)
				{
					if (friendRequests[l].completionTime < Time.time && friendRequests[k].requestorPublicId == friendRequests[l].requesteePublicId && friendRequests[k].requesteePublicId == friendRequests[l].requestorPublicId && TryLookupPrivateId(friendRequests[k].requestorPublicId, out var privateId) && TryLookupPrivateId(friendRequests[k].requesteePublicId, out var privateId2))
					{
						AddFriend(friendRequests[k].requestorPublicId, friendRequests[k].requesteePublicId, privateId, privateId2);
						indexesToRemove.Add(l);
						indexesToRemove.Add(k);
						base.photonView.RPC("AddFriendPairRPC", RpcTarget.Others, friendRequests[k].requestorPublicId, friendRequests[k].requesteePublicId, privateId, privateId2);
						break;
					}
				}
			}
		}
		for (int m = 0; m < indexesToRemove.Count; m++)
		{
			friendRequests.RemoveAt(indexesToRemove[m]);
		}
	}

	public void RegisterLocalPlayer(int localPlayerPublicId)
	{
		int hashCode = PlayFabAuthenticator.instance.GetPlayFabPlayerId().GetHashCode();
		if (base.photonView.IsMine)
		{
			RegisterLocalPlayerInternal(localPlayerPublicId, hashCode);
			return;
		}
		base.photonView.RPC("RegisterLocalPlayerRPC", RpcTarget.MasterClient, localPlayerPublicId, hashCode);
	}

	public void RequestAddFriend(int targetPlayerId)
	{
		if (base.photonView.IsMine)
		{
			RequestAddFriendInternal(LocalPlayerId, targetPlayerId);
			return;
		}
		base.photonView.RPC("RequestAddFriendRPC", RpcTarget.MasterClient, LocalPlayerId, targetPlayerId);
	}

	public void RequestRemoveFriend(int targetPlayerId)
	{
		if (base.photonView.IsMine)
		{
			RequestRemoveFriendInternal(LocalPlayerId, targetPlayerId);
			return;
		}
		base.photonView.RPC("RequestRemoveFriendRPC", RpcTarget.MasterClient, LocalPlayerId, targetPlayerId);
	}

	public void GetFriendList(List<int> friendListResult)
	{
		int localPlayerId = LocalPlayerId;
		friendListResult.Clear();
		for (int i = 0; i < friendPairList.Count; i++)
		{
			if (friendPairList[i].publicIdPlayerA == localPlayerId)
			{
				friendListResult.Add(friendPairList[i].publicIdPlayerB);
			}
			else if (friendPairList[i].publicIdPlayerB == localPlayerId)
			{
				friendListResult.Add(friendPairList[i].publicIdPlayerA);
			}
		}
	}

	private void RequestAddFriendInternal(int localPlayerPublicId, int otherPlayerPublicId)
	{
		if (!base.photonView.IsMine)
		{
			return;
		}
		bool flag = false;
		for (int i = 0; i < friendRequests.Count; i++)
		{
			if (friendRequests[i].requestorPublicId == localPlayerPublicId && friendRequests[i].requesteePublicId == otherPlayerPublicId)
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			float time = Time.time;
			float num = UnityEngine.Random.Range(friendRequestCompletionDelayRange.x, friendRequestCompletionDelayRange.y);
			friendRequests.Add(new FriendRequest
			{
				requestorPublicId = localPlayerPublicId,
				requesteePublicId = otherPlayerPublicId,
				requestTime = time,
				completionTime = time + num
			});
		}
	}

	[PunRPC]
	public void RequestAddFriendRPC(int localPlayerPublicId, int otherPlayerPublicId, PhotonMessageInfo info)
	{
		RequestAddFriendInternal(localPlayerPublicId, otherPlayerPublicId);
	}

	private void RequestRemoveFriendInternal(int localPlayerPublicId, int otherPlayerPublicId)
	{
		if (base.photonView.IsMine && TryLookupPrivateId(localPlayerPublicId, out var privateId) && TryLookupPrivateId(otherPlayerPublicId, out var privateId2))
		{
			RemoveFriend(privateId, privateId2);
		}
	}

	[PunRPC]
	public void RequestRemoveFriendRPC(int localPlayerPublicId, int otherPlayerPublicId, PhotonMessageInfo info)
	{
		RequestRemoveFriendInternal(localPlayerPublicId, otherPlayerPublicId);
	}

	private void RegisterLocalPlayerInternal(int publicId, int privateId)
	{
		if (!base.photonView.IsMine)
		{
			return;
		}
		bool flag = false;
		for (int i = 0; i < privateIdLookup.Count; i++)
		{
			if (publicId == privateIdLookup[i].playerPublicId || privateId == privateIdLookup[i].playerPrivateId)
			{
				PrivateIdEncryptionPlaceholder value = privateIdLookup[i];
				value.playerPublicId = publicId;
				value.playerPrivateId = privateId;
				privateIdLookup[i] = value;
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			privateIdLookup.Add(new PrivateIdEncryptionPlaceholder
			{
				playerPublicId = publicId,
				playerPrivateId = privateId
			});
		}
	}

	[PunRPC]
	public void RegisterLocalPlayerRPC(int playerPublicId, int playerPrivateId, PhotonMessageInfo info)
	{
		RegisterLocalPlayerInternal(playerPublicId, playerPrivateId);
	}

	[PunRPC]
	public void AddFriendPairRPC(int publicIdA, int publicIdB, int privateIdA, int privateIdB, PhotonMessageInfo info)
	{
		AddFriend(publicIdA, publicIdB, privateIdA, privateIdB);
	}

	private void AddFriend(int publicIdA, int publicIdB, int privateIdA, int privateIdB)
	{
		for (int i = 0; i < friendPairList.Count; i++)
		{
			if ((friendPairList[i].privateIdPlayerA == privateIdA && friendPairList[i].privateIdPlayerB == privateIdB) || (friendPairList[i].privateIdPlayerA == privateIdB && friendPairList[i].privateIdPlayerB == privateIdA))
			{
				return;
			}
		}
		friendPairList.Add(new FriendPair
		{
			publicIdPlayerA = publicIdA,
			publicIdPlayerB = publicIdB,
			privateIdPlayerA = privateIdA,
			privateIdPlayerB = privateIdB
		});
	}

	private void RemoveFriend(int privateIdA, int privateIdB)
	{
		indexesToRemove.Clear();
		for (int i = 0; i < friendPairList.Count; i++)
		{
			if ((friendPairList[i].privateIdPlayerA == privateIdA && friendPairList[i].privateIdPlayerB == privateIdB) || (friendPairList[i].privateIdPlayerA == privateIdB && friendPairList[i].privateIdPlayerB == privateIdA))
			{
				indexesToRemove.Add(i);
			}
		}
		for (int j = 0; j < friendPairList.Count; j++)
		{
			friendPairList.RemoveAt(indexesToRemove[j]);
		}
	}

	private bool TryLookupPrivateId(int publicId, out int privateId)
	{
		for (int i = 0; i < privateIdLookup.Count; i++)
		{
			if (privateIdLookup[i].playerPublicId == publicId)
			{
				privateId = privateIdLookup[i].playerPrivateId;
				return true;
			}
		}
		privateId = -1;
		return false;
	}
}
