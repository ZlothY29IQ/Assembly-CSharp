using System.Collections.Generic;
using Fusion;
using GorillaExtensions;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

[NetworkBehaviourWeaved(0)]
public class GameAgentManager : NetworkComponent, ITickSystemTick
{
	public enum RPC
	{
		ApplyDestination,
		ApplyState,
		ApplyBehaviour,
		ApplyImpact,
		ApplyTarget
	}

	public const float MAX_JUMP_DISTANCE = 25f;

	public GameEntityManager entityManager;

	public PhotonView photonView;

	private List<GameAgent> agents;

	private float lastDestinationSentTime;

	private float destinationCooldown;

	private List<int> netIdsForDestination;

	private List<Vector3> destinationsForDestination;

	private List<int> netIdsForState;

	private List<byte> statesForState;

	private float lastStateSentTime;

	private float stateCooldown;

	private List<int> netIdsForBehavior;

	private List<byte> behaviorsForBehavior;

	private float lastBehaviorSentTime;

	private float behaviorCooldown = 0.25f;

	private const int MAX_UPDATES_PER_FRAME = 4;

	private int nextAgentIndexUpdate;

	private const int MAX_THINK_PER_FRAME = 1;

	private int nextAgentIndexThink;

	public CallLimitersList<CallLimiter, RPC> m_RpcSpamChecks = new CallLimitersList<CallLimiter, RPC>();

	public bool TickRunning { get; set; }

	protected override void Awake()
	{
		agents = new List<GameAgent>(128);
		netIdsForDestination = new List<int>();
		destinationsForDestination = new List<Vector3>();
		netIdsForState = new List<int>();
		statesForState = new List<byte>();
		netIdsForBehavior = new List<int>();
		behaviorsForBehavior = new List<byte>();
		nextAgentIndexUpdate = 0;
		nextAgentIndexThink = 0;
	}

	private new void OnEnable()
	{
		NetworkBehaviourUtils.InternalOnEnable(this);
		TickSystem<object>.AddCallbackTarget(this);
	}

	private new void OnDisable()
	{
		NetworkBehaviourUtils.InternalOnDisable(this);
		TickSystem<object>.RemoveCallbackTarget(this);
	}

	public static GameAgentManager Get(GameEntity gameEntity)
	{
		if (!(gameEntity == null) && !(gameEntity.manager == null))
		{
			return gameEntity.manager.gameAgentManager;
		}
		return null;
	}

	public List<GameAgent> GetAgents()
	{
		return agents;
	}

	public int GetGameAgentCount()
	{
		return agents.Count;
	}

	public void AddGameAgent(GameAgent gameAgent)
	{
		agents.Add(gameAgent);
	}

	public void RemoveGameAgent(GameAgent gameAgent)
	{
		agents.Remove(gameAgent);
	}

	public GameAgent GetGameAgent(GameEntityId id)
	{
		return entityManager.GetGameEntity(id).GetComponent<GameAgent>();
	}

	public void Tick()
	{
		if (IsAuthority())
		{
			int num = Mathf.Min(1, agents.Count);
			for (int i = 0; i < num; i++)
			{
				if (nextAgentIndexThink >= agents.Count)
				{
					nextAgentIndexThink = 0;
				}
				agents[nextAgentIndexThink].OnThink(Time.deltaTime);
				nextAgentIndexThink++;
			}
		}
		for (int j = 0; j < agents.Count; j++)
		{
			if (agents[j] != null)
			{
				agents[j].OnUpdate();
			}
		}
		if (IsAuthority())
		{
			if (netIdsForDestination.Count > 0 && Time.time > lastDestinationSentTime + destinationCooldown)
			{
				lastDestinationSentTime = Time.time;
				SendRPC("ApplyDestinationRPC", RpcTarget.All, netIdsForDestination.ToArray(), destinationsForDestination.ToArray());
				netIdsForDestination.Clear();
				destinationsForDestination.Clear();
			}
			if (netIdsForState.Count > 0 && Time.time > lastStateSentTime + stateCooldown)
			{
				lastStateSentTime = Time.time;
				SendRPC("ApplyStateRPC", RpcTarget.All, netIdsForState.ToArray(), statesForState.ToArray());
				netIdsForState.Clear();
				statesForState.Clear();
			}
			if (netIdsForBehavior.Count > 0 && Time.time > lastBehaviorSentTime + behaviorCooldown)
			{
				lastBehaviorSentTime = Time.time;
				SendRPC("ApplyBehaviorRPC", RpcTarget.All, netIdsForBehavior.ToArray(), behaviorsForBehavior.ToArray());
				netIdsForBehavior.Clear();
				behaviorsForBehavior.Clear();
			}
		}
	}

	public bool IsAuthority()
	{
		return entityManager.IsAuthority();
	}

	public bool IsAuthorityPlayer(NetPlayer player)
	{
		return entityManager.IsAuthorityPlayer(player);
	}

	public bool IsAuthorityPlayer(Player player)
	{
		return entityManager.IsAuthorityPlayer(player);
	}

	public Player GetAuthorityPlayer()
	{
		return entityManager.GetAuthorityPlayer();
	}

	public bool IsZoneActive()
	{
		return entityManager.IsZoneActive();
	}

	public bool IsPositionInManagerBounds(Vector3 pos)
	{
		return entityManager.IsPositionInManagerBounds(pos);
	}

	public bool IsValidClientRPC(Player sender)
	{
		return entityManager.IsValidClientRPC(sender);
	}

	public bool IsValidClientRPC(Player sender, int entityNetId)
	{
		return entityManager.IsValidClientRPC(sender, entityNetId);
	}

	public bool IsValidClientRPC(Player sender, int entityNetId, Vector3 pos)
	{
		return entityManager.IsValidClientRPC(sender, entityNetId, pos);
	}

	public bool IsValidClientRPC(Player sender, Vector3 pos)
	{
		return entityManager.IsValidClientRPC(sender, pos);
	}

	public bool IsValidAuthorityRPC(Player sender)
	{
		return entityManager.IsValidAuthorityRPC(sender);
	}

	public bool IsValidAuthorityRPC(Player sender, int entityNetId)
	{
		return entityManager.IsValidAuthorityRPC(sender, entityNetId);
	}

	public bool IsValidAuthorityRPC(Player sender, int entityNetId, Vector3 pos)
	{
		return entityManager.IsValidAuthorityRPC(sender, entityNetId, pos);
	}

	public bool IsValidAuthorityRPC(Player sender, Vector3 pos)
	{
		return entityManager.IsValidAuthorityRPC(sender, pos);
	}

	public void RequestDestination(GameAgent agent, Vector3 dest)
	{
		if (!IsAuthority())
		{
			Debug.LogError("RequestDestination should only be called from the master client");
			return;
		}
		int netIdFromEntityId = entityManager.GetNetIdFromEntityId(agent.entity.id);
		if (netIdsForDestination.Contains(netIdFromEntityId))
		{
			destinationsForDestination[netIdsForDestination.IndexOf(netIdFromEntityId)] = dest;
			return;
		}
		netIdsForDestination.Add(netIdFromEntityId);
		destinationsForDestination.Add(dest);
	}

	[PunRPC]
	public void ApplyDestinationRPC(int[] netEntityId, Vector3[] dest, PhotonMessageInfo info)
	{
		if (!IsZoneActive() || m_RpcSpamChecks.IsSpamming(RPC.ApplyDestination) || netEntityId == null || dest == null || netEntityId.Length != dest.Length)
		{
			return;
		}
		for (int i = 0; i < netEntityId.Length; i++)
		{
			if (!IsValidClientRPC(info.Sender, netEntityId[i], dest[i]) || !dest[i].IsValid(10000f))
			{
				return;
			}
		}
		for (int j = 0; j < netEntityId.Length; j++)
		{
			GameEntity gameEntity = entityManager.GetGameEntity(entityManager.GetEntityIdFromNetId(netEntityId[j]));
			if (gameEntity == null)
			{
				break;
			}
			GameAgent component = gameEntity.GetComponent<GameAgent>();
			if (component == null)
			{
				break;
			}
			component.ApplyDestination(dest[j]);
		}
	}

	public void RequestState(GameAgent agent, byte state)
	{
		if (IsAuthority())
		{
			int netIdFromEntityId = entityManager.GetNetIdFromEntityId(agent.entity.id);
			if (netIdsForState.Contains(netIdFromEntityId))
			{
				statesForState[netIdsForState.IndexOf(netIdFromEntityId)] = state;
				return;
			}
			netIdsForState.Add(netIdFromEntityId);
			statesForState.Add(state);
		}
	}

	[PunRPC]
	public void ApplyStateRPC(int[] netEntityId, byte[] state, PhotonMessageInfo info)
	{
		if (netEntityId == null || state == null || netEntityId.Length != state.Length || m_RpcSpamChecks.IsSpamming(RPC.ApplyState))
		{
			return;
		}
		for (int i = 0; i < netEntityId.Length && IsValidClientRPC(info.Sender, netEntityId[i]); i++)
		{
			GameEntity gameEntity = entityManager.GetGameEntity(entityManager.GetEntityIdFromNetId(netEntityId[i]));
			if (gameEntity == null)
			{
				break;
			}
			GameAgent component = gameEntity.GetComponent<GameAgent>();
			if (component == null)
			{
				break;
			}
			component.OnBodyStateChanged(state[i]);
		}
	}

	public void RequestBehavior(GameAgent agent, byte behavior)
	{
		if (IsAuthority())
		{
			int netIdFromEntityId = entityManager.GetNetIdFromEntityId(agent.entity.id);
			if (netIdsForBehavior.Contains(netIdFromEntityId))
			{
				behaviorsForBehavior[netIdsForBehavior.IndexOf(netIdFromEntityId)] = behavior;
				return;
			}
			netIdsForBehavior.Add(netIdFromEntityId);
			behaviorsForBehavior.Add(behavior);
		}
	}

	[PunRPC]
	public void ApplyBehaviorRPC(int[] netEntityId, byte[] behavior, PhotonMessageInfo info)
	{
		if (netEntityId == null || behavior == null || netEntityId.Length != behavior.Length || m_RpcSpamChecks.IsSpamming(RPC.ApplyBehaviour))
		{
			return;
		}
		for (int i = 0; i < netEntityId.Length && IsValidClientRPC(info.Sender, netEntityId[i]); i++)
		{
			GameEntity gameEntity = entityManager.GetGameEntity(entityManager.GetEntityIdFromNetId(netEntityId[i]));
			if (gameEntity == null)
			{
				break;
			}
			GameAgent component = gameEntity.GetComponent<GameAgent>();
			if (component != null)
			{
				component.OnBehaviorStateChanged(behavior[i]);
			}
		}
	}

	public void RequestTarget(GameAgent agent, NetPlayer player)
	{
		if (player != agent.targetPlayer && IsAuthority() && !(agent == null))
		{
			agent.targetPlayer = player;
			SendRPC("ApplyTargetRPC", RpcTarget.Others, entityManager.GetNetIdFromEntityId(agent.entity.id), player?.GetPlayerRef());
		}
	}

	[PunRPC]
	public void ApplyTargetRPC(int agentNetId, Player player, PhotonMessageInfo info)
	{
		if (!IsValidClientRPC(info.Sender, agentNetId) || m_RpcSpamChecks.IsSpamming(RPC.ApplyTarget) || player == null)
		{
			return;
		}
		GameEntity gameEntity = entityManager.GetGameEntity(entityManager.GetEntityIdFromNetId(agentNetId));
		if (!(gameEntity == null))
		{
			GameAgent component = gameEntity.GetComponent<GameAgent>();
			if (!(component == null))
			{
				component.targetPlayer = NetPlayer.Get(player);
			}
		}
	}

	public void RequestJump(GameAgent agent, Vector3 start, Vector3 end, float heightScale, float speedScale)
	{
		if (IsAuthority() && !(agent == null))
		{
			agent.OnJumpRequested(start, end, heightScale, speedScale);
			SendRPC("ApplyJumpRPC", RpcTarget.Others, entityManager.GetNetIdFromEntityId(agent.entity.id), start, end, heightScale, speedScale);
		}
	}

	[PunRPC]
	public void ApplyJumpRPC(int agentNetId, Vector3 start, Vector3 end, float heightScale, float speedScale, PhotonMessageInfo info)
	{
		if (!IsValidClientRPC(info.Sender, agentNetId) || m_RpcSpamChecks.IsSpamming(RPC.ApplyTarget) || !start.IsValid(10000f) || !end.IsValid(10000f) || !entityManager.IsPositionInManagerBounds(start) || !entityManager.IsPositionInManagerBounds(end) || !entityManager.IsEntityNearPosition(agentNetId, start) || heightScale > 5f || speedScale > 5f || (end - start).sqrMagnitude > 625f)
		{
			return;
		}
		GameEntity gameEntity = entityManager.GetGameEntity(entityManager.GetEntityIdFromNetId(agentNetId));
		if (!(gameEntity == null))
		{
			GameAgent component = gameEntity.GetComponent<GameAgent>();
			if (!(component == null))
			{
				component.OnJumpRequested(start, end, heightScale, speedScale);
			}
		}
	}

	public override void WriteDataFusion()
	{
	}

	public override void ReadDataFusion()
	{
	}

	protected override void WriteDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		int num = Mathf.Min(4, agents.Count);
		stream.SendNext(num);
		for (int i = 0; i < num; i++)
		{
			if (nextAgentIndexUpdate >= agents.Count)
			{
				nextAgentIndexUpdate = 0;
			}
			stream.SendNext(entityManager.GetNetIdFromEntityId(agents[nextAgentIndexUpdate].entity.id));
			long num2 = BitPackUtils.PackWorldPosForNetwork(agents[nextAgentIndexUpdate].transform.position);
			stream.SendNext(num2);
			int num3 = BitPackUtils.PackQuaternionForNetwork(agents[nextAgentIndexUpdate].transform.rotation);
			stream.SendNext(num3);
			nextAgentIndexUpdate++;
		}
	}

	protected override void ReadDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		if (!IsValidClientRPC(info.Sender))
		{
			return;
		}
		int num = (int)stream.ReceiveNext();
		for (int i = 0; i < num; i++)
		{
			int netId = (int)stream.ReceiveNext();
			Vector3 vector = BitPackUtils.UnpackWorldPosFromNetwork((long)stream.ReceiveNext());
			Quaternion rotation = BitPackUtils.UnpackQuaternionFromNetwork((int)stream.ReceiveNext());
			if (IsPositionInManagerBounds(vector) && entityManager.IsValidNetId(netId))
			{
				GameEntityId entityIdFromNetId = entityManager.GetEntityIdFromNetId(netId);
				GameAgent gameAgent = GetGameAgent(entityIdFromNetId);
				if (gameAgent != null)
				{
					gameAgent.ApplyNetworkUpdate(vector, rotation);
				}
			}
		}
	}

	[WeaverGenerated]
	public override void CopyBackingFieldsToState(bool P_0)
	{
		base.CopyBackingFieldsToState(P_0);
	}

	[WeaverGenerated]
	public override void CopyStateToBackingFields()
	{
		base.CopyStateToBackingFields();
	}
}
