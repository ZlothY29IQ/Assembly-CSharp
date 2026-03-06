using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using GorillaExtensions;
using Photon.Pun;
using Photon.Realtime;

namespace GorillaTag;

public static class MonkeAgentCleanup
{
	private static readonly Queue<PhotonView> k_destroyQueue;

	private static readonly HashSet<PhotonView> k_destroyTargets;

	private static readonly TickSystemTimer k_destroyTimer;

	private static readonly Hashtable k_cacheInfo;

	private static readonly RaiseEventOptions k_raiseEventOptions;

	private static readonly object k_viewIdKey;

	static MonkeAgentCleanup()
	{
		k_destroyQueue = new Queue<PhotonView>();
		k_destroyTargets = new HashSet<PhotonView>();
		k_destroyTimer = new TickSystemTimer(1f);
		k_cacheInfo = new Hashtable(1);
		k_raiseEventOptions = new RaiseEventOptions
		{
			CachingOption = EventCaching.RemoveFromRoomCache
		};
		k_viewIdKey = (byte)7;
		k_destroyTimer.callback = CheckDestroyQueue;
		RoomSystem.LeftRoomEvent += new Action(OnLeftRoom);
	}

	public static void RegisterForDestroy(PhotonView target)
	{
		if (!k_destroyTargets.Contains(target))
		{
			if (target.gameObject.activeSelf)
			{
				target.gameObject.Disable();
			}
			if (k_destroyTargets.Add(target))
			{
				k_destroyQueue.Enqueue(target);
			}
			if (!k_destroyTimer.Running && k_destroyQueue.Count > 0)
			{
				k_destroyTimer.Start();
			}
		}
	}

	private static void OnLeftRoom()
	{
		k_destroyQueue.Clear();
		k_destroyTargets.Clear();
		k_destroyTimer.Stop();
	}

	private static void CheckDestroyQueue()
	{
		if (!RoomSystem.JoinedRoom)
		{
			return;
		}
		bool flag = RoomSystem.GetLowestActorNumberPlayer() == NetworkSystem.Instance.LocalPlayer;
		int num = 0;
		while (k_destroyQueue.Count > 0 && num < 10)
		{
			PhotonView photonView = k_destroyQueue.Dequeue();
			if (k_destroyTargets.Remove(photonView) && !photonView.IsNull())
			{
				if ((photonView.IsRoomView && flag) || photonView.IsMine)
				{
					k_cacheInfo[k_viewIdKey] = photonView.InstantiationId;
					PhotonNetwork.NetworkingClient.OpRaiseEvent(202, k_cacheInfo, k_raiseEventOptions, SendOptions.SendReliable);
				}
				PhotonNetwork.RemoveInstantiatedGO(photonView.gameObject, localOnly: true);
				num++;
			}
		}
		if (k_destroyTargets.Count == 0)
		{
			k_destroyTimer.Stop();
		}
	}
}
