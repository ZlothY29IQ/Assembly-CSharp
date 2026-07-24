using System;
using GorillaNetworking;
using UnityEngine;

public class GorillaFriendColliderHelper : MonoBehaviour
{
	[Serializable]
	public struct FriendColliderPair
	{
		public string ColliderName;

		public GorillaFriendCollider Collider;

		public GorillaNetworkJoinTrigger JoinTrigger;
	}

	public static GorillaFriendColliderHelper Instance;

	public FriendColliderPair[] MappedFriendColliders;

	public void Awake()
	{
		Instance = this;
	}

	public GorillaFriendCollider FindFriendCollider(string search)
	{
		for (int i = 0; i < MappedFriendColliders.Length; i++)
		{
			if (MappedFriendColliders[i].ColliderName.Equals(search, StringComparison.OrdinalIgnoreCase))
			{
				return MappedFriendColliders[i].Collider;
			}
		}
		return null;
	}

	public GorillaNetworkJoinTrigger FindJoinCollider(string search)
	{
		for (int i = 0; i < MappedFriendColliders.Length; i++)
		{
			if (MappedFriendColliders[i].ColliderName.Equals(search, StringComparison.OrdinalIgnoreCase))
			{
				return MappedFriendColliders[i].JoinTrigger;
			}
		}
		return null;
	}
}
