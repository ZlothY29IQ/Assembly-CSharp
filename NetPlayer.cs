using System;
using System.Collections.Generic;
using Fusion;
using GorillaTag;
using Photon.Realtime;
using UnityEngine;

[Serializable]
public abstract class NetPlayer : ObjectPoolEvents
{
	public enum SingleCallRPC
	{
		CMS_RequestRoomInitialization,
		CMS_RequestTriggerHistory,
		CMS_SyncTriggerHistory,
		CMS_SyncTriggerCounts,
		RankedSendScoreToLateJoiner,
		Count
	}

	private HashSet<int> SingleCallRPCStatus = new HashSet<int>(5);

	public abstract bool IsValid { get; }

	public abstract int ActorNumber { get; }

	public abstract string UserId { get; }

	public abstract bool IsMasterClient { get; }

	public abstract bool IsLocal { get; }

	public abstract bool IsNull { get; }

	public abstract string NickName { get; }

	public virtual string SanitizedNickName { get; set; } = string.Empty;

	public abstract string DefaultName { get; }

	public abstract bool InRoom { get; }

	public virtual float JoinedTime { get; private set; }

	public virtual float LeftTime { get; private set; }

	public abstract bool Equals(NetPlayer myPlayer, NetPlayer other);

	public virtual void OnReturned()
	{
		LeftTime = Time.time;
		SingleCallRPCStatus?.Clear();
		SanitizedNickName = string.Empty;
	}

	public virtual void OnTaken()
	{
		JoinedTime = Time.time;
		SingleCallRPCStatus?.Clear();
	}

	public virtual bool CheckSingleCallRPC(SingleCallRPC RPCType)
	{
		return SingleCallRPCStatus.Contains((int)RPCType);
	}

	public virtual void ReceivedSingleCallRPC(SingleCallRPC RPCType)
	{
		SingleCallRPCStatus.Add((int)RPCType);
	}

	public Player GetPlayerRef()
	{
		return (this as PunNetPlayer).PlayerRef;
	}

	public string ToStringFull()
	{
		return $"#{ActorNumber: 0:00} '{NickName}', Not sure what to do with inactive yet, Or custom props?";
	}

	public static implicit operator NetPlayer(Player player)
	{
		Utils.Log("Using an implicit cast from Player to NetPlayer. Please make sure this was intended as this has potential to cause errors when switching between network backends");
		return NetworkSystem.Instance?.GetPlayer(player) ?? null;
	}

	public static implicit operator NetPlayer(PlayerRef player)
	{
		Utils.Log("Using an implicit cast from PlayerRef to NetPlayer. Please make sure this was intended as this has potential to cause errors when switching between network backends");
		return NetworkSystem.Instance?.GetPlayer(player) ?? null;
	}

	public static NetPlayer Get(Player player)
	{
		return NetworkSystem.Instance?.GetPlayer(player) ?? null;
	}

	public static NetPlayer Get(PlayerRef player)
	{
		return NetworkSystem.Instance?.GetPlayer(player) ?? null;
	}

	public static NetPlayer Get(int actorNr)
	{
		return NetworkSystem.Instance?.GetPlayer(actorNr) ?? null;
	}
}
