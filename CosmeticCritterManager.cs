using System;
using System.Collections.Generic;
using Fusion;
using Photon.Pun;
using UnityEngine;

public class CosmeticCritterManager : NetworkSceneObject, ITickSystemTick
{
	private List<CosmeticCritterHoldable> localHoldables;

	private List<CosmeticCritterSpawnerIndependent> localCritterSpawners;

	private List<CosmeticCritterSpawnerIndependent> remoteCritterSpawners;

	private List<CosmeticCritterCatcher> localCritterCatchers;

	private List<CosmeticCritterCatcher> remoteCritterCatchers;

	private List<CosmeticCritter> activeCritters;

	private Dictionary<Type, int> activeCrittersPerType;

	private Dictionary<int, CosmeticCritter> activeCrittersBySeed;

	private Dictionary<Type, Stack<CosmeticCritter>> inactiveCrittersByType;

	private Dictionary<Type, List<ICosmeticCritterTickForEach>> tickForEachCritterOfType;

	public static CosmeticCritterManager Instance { get; private set; }

	public bool TickRunning { get; set; }

	private new void OnEnable()
	{
		NetworkBehaviourUtils.InternalOnEnable(this);
		base.OnEnable();
		TickSystem<object>.AddTickCallback(this);
	}

	private new void OnDisable()
	{
		NetworkBehaviourUtils.InternalOnDisable(this);
		base.OnDisable();
		TickSystem<object>.RemoveTickCallback(this);
	}

	public void RegisterLocalHoldable(CosmeticCritterHoldable holdable)
	{
		localHoldables.Add(holdable);
	}

	public void RegisterIndependentSpawner(CosmeticCritterSpawnerIndependent spawner)
	{
		if (spawner.IsLocal)
		{
			localCritterSpawners.AddIfNew(spawner);
		}
		else
		{
			remoteCritterSpawners.AddIfNew(spawner);
		}
	}

	public void UnregisterIndependentSpawner(CosmeticCritterSpawnerIndependent spawner)
	{
		if (spawner.IsLocal)
		{
			localCritterSpawners.Remove(spawner);
		}
		else
		{
			remoteCritterSpawners.Remove(spawner);
		}
	}

	public void RegisterCatcher(CosmeticCritterCatcher catcher)
	{
		if (catcher.IsLocal)
		{
			localCritterCatchers.AddIfNew(catcher);
		}
		else
		{
			remoteCritterCatchers.AddIfNew(catcher);
		}
	}

	public void UnregisterCatcher(CosmeticCritterCatcher catcher)
	{
		if (catcher.IsLocal)
		{
			localCritterCatchers.Remove(catcher);
		}
		else
		{
			remoteCritterCatchers.Remove(catcher);
		}
	}

	public void RegisterTickForEachCritter(Type type, ICosmeticCritterTickForEach target)
	{
		if (!tickForEachCritterOfType.TryGetValue(type, out var value) || value == null)
		{
			value = new List<ICosmeticCritterTickForEach>();
			tickForEachCritterOfType.Add(type, value);
		}
		value.AddIfNew(target);
	}

	public void UnregisterTickForEachCritter(Type type, ICosmeticCritterTickForEach target)
	{
		if (tickForEachCritterOfType.TryGetValue(type, out var value))
		{
			value?.Remove(target);
		}
	}

	private void ResetLocalCallLimiters()
	{
		int num = 0;
		while (num < localHoldables.Count)
		{
			if (localHoldables[num] == null)
			{
				localHoldables.RemoveAt(num);
				continue;
			}
			localHoldables[num].ResetCallLimiter();
			num++;
		}
	}

	private void ResetCosmeticCritters(NetPlayer player)
	{
		if (NetworkSystem.Instance.LocalPlayer == player)
		{
			ResetLocalCallLimiters();
			for (int i = 0; i < activeCritters.Count; i++)
			{
				FreeCritter(activeCritters[i]);
			}
		}
	}

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			UnityEngine.Object.Destroy(this);
			return;
		}
		Instance = this;
		localHoldables = new List<CosmeticCritterHoldable>();
		localCritterSpawners = new List<CosmeticCritterSpawnerIndependent>();
		remoteCritterSpawners = new List<CosmeticCritterSpawnerIndependent>();
		localCritterCatchers = new List<CosmeticCritterCatcher>();
		remoteCritterCatchers = new List<CosmeticCritterCatcher>();
		activeCritters = new List<CosmeticCritter>();
		activeCrittersPerType = new Dictionary<Type, int>();
		activeCrittersBySeed = new Dictionary<int, CosmeticCritter>();
		inactiveCrittersByType = new Dictionary<Type, Stack<CosmeticCritter>>();
		tickForEachCritterOfType = new Dictionary<Type, List<ICosmeticCritterTickForEach>>();
		NetworkSystem.Instance.OnPlayerJoined += new Action<NetPlayer>(ResetCosmeticCritters);
		NetworkSystem.Instance.OnPlayerLeft += new Action<NetPlayer>(ResetCosmeticCritters);
	}

	private void ReuseOrSpawnNewCritter(CosmeticCritterSpawner spawner, int seed, double time)
	{
		Type critterType = spawner.GetCritterType();
		CosmeticCritter result;
		if (!inactiveCrittersByType.TryGetValue(critterType, out var value))
		{
			value = new Stack<CosmeticCritter>();
			inactiveCrittersByType.Add(critterType, value);
			result = UnityEngine.Object.Instantiate(spawner.GetCritterPrefab(), base.transform).GetComponent<CosmeticCritter>();
		}
		else if (value.TryPop(out result))
		{
			result.gameObject.SetActive(value: true);
		}
		else
		{
			result = UnityEngine.Object.Instantiate(spawner.GetCritterPrefab(), base.transform).GetComponent<CosmeticCritter>();
		}
		result.SetSeedSpawnerTypeAndTime(seed, spawner, critterType, time);
		activeCritters.Add(result);
		if (!activeCrittersPerType.ContainsKey(critterType))
		{
			activeCrittersPerType.Add(critterType, 1);
		}
		else
		{
			activeCrittersPerType[critterType]++;
		}
		activeCrittersBySeed.Add(seed, result);
		UnityEngine.Random.State state = UnityEngine.Random.state;
		UnityEngine.Random.InitState(seed);
		spawner.SetRandomVariables(result);
		result.SetRandomVariables();
		UnityEngine.Random.state = state;
		spawner.OnSpawn(result);
		result.OnSpawn();
	}

	private void FreeCritter(CosmeticCritter critter)
	{
		critter.OnDespawn();
		if (critter.Spawner != null)
		{
			critter.Spawner.OnDespawn(critter);
		}
		critter.gameObject.SetActive(value: false);
		Type cachedType = critter.CachedType;
		if (!inactiveCrittersByType.TryGetValue(cachedType, out var value))
		{
			value = new Stack<CosmeticCritter>();
			inactiveCrittersByType.Add(cachedType, value);
		}
		value.Push(critter);
		activeCritters.Remove(critter);
		if (activeCrittersPerType.TryGetValue(cachedType, out var value2))
		{
			activeCrittersPerType[cachedType] = Math.Max(value2 - 1, 0);
		}
		activeCrittersBySeed.Remove(critter.Seed);
	}

	public void Tick()
	{
		for (int i = 0; i < activeCritters.Count; i++)
		{
			CosmeticCritter cosmeticCritter = activeCritters[i];
			if (cosmeticCritter.Expired())
			{
				FreeCritter(cosmeticCritter);
				continue;
			}
			cosmeticCritter.Tick();
			if (tickForEachCritterOfType.TryGetValue(cosmeticCritter.CachedType, out var value))
			{
				for (int j = 0; j < value.Count; j++)
				{
					value[j].TickForEachCritter(cosmeticCritter);
				}
			}
			for (int k = 0; k < localCritterCatchers.Count; k++)
			{
				CosmeticCritterCatcher cosmeticCritterCatcher = localCritterCatchers[k];
				CosmeticCritterAction localCatchAction = cosmeticCritterCatcher.GetLocalCatchAction(cosmeticCritter);
				if (localCatchAction != CosmeticCritterAction.None)
				{
					double num = (PhotonNetwork.InRoom ? PhotonNetwork.Time : Time.timeAsDouble);
					cosmeticCritterCatcher.OnCatch(cosmeticCritter, localCatchAction, num);
					if ((localCatchAction & CosmeticCritterAction.Despawn) != CosmeticCritterAction.None)
					{
						FreeCritter(cosmeticCritter);
						i--;
					}
					if ((localCatchAction & CosmeticCritterAction.SpawnLinked) != CosmeticCritterAction.None && cosmeticCritterCatcher.GetLinkedSpawner() != null)
					{
						ReuseOrSpawnNewCritter(cosmeticCritterCatcher.GetLinkedSpawner(), cosmeticCritter.Seed + 1, num);
					}
					if (PhotonNetwork.InRoom && (localCatchAction & CosmeticCritterAction.RPC) != CosmeticCritterAction.None)
					{
						photonView.RPC("CosmeticCritterRPC", RpcTarget.Others, localCatchAction, cosmeticCritterCatcher.OwnerID, cosmeticCritter.Seed);
					}
					break;
				}
			}
		}
		for (int l = 0; l < localCritterSpawners.Count; l++)
		{
			CosmeticCritterSpawnerIndependent cosmeticCritterSpawnerIndependent = localCritterSpawners[l];
			if ((activeCrittersPerType.TryGetValue(cosmeticCritterSpawnerIndependent.GetCritterType(), out var value2) && value2 >= cosmeticCritterSpawnerIndependent.GetCritter().GetGlobalMaxCritters()) || !cosmeticCritterSpawnerIndependent.CanSpawnLocal())
			{
				continue;
			}
			int num2 = UnityEngine.Random.Range(0, int.MaxValue);
			if (!activeCrittersBySeed.ContainsKey(num2))
			{
				ReuseOrSpawnNewCritter(cosmeticCritterSpawnerIndependent, num2, PhotonNetwork.InRoom ? PhotonNetwork.Time : Time.timeAsDouble);
				if (PhotonNetwork.InRoom)
				{
					photonView.RPC("CosmeticCritterRPC", RpcTarget.Others, CosmeticCritterAction.RPC | CosmeticCritterAction.Spawn, cosmeticCritterSpawnerIndependent.OwnerID, num2);
				}
			}
		}
	}

	[PunRPC]
	private void CosmeticCritterRPC(CosmeticCritterAction action, int holdableID, int seed, PhotonMessageInfo info)
	{
		PhotonMessageInfoWrapped photonMessageInfoWrapped = new PhotonMessageInfoWrapped(info);
		MonkeAgent.IncrementRPCCall(photonMessageInfoWrapped, "CosmeticCritterRPC");
		if ((action & CosmeticCritterAction.RPC) != CosmeticCritterAction.None)
		{
			if (action == (CosmeticCritterAction.RPC | CosmeticCritterAction.Spawn))
			{
				SpawnCosmeticCritterRPC(holdableID, seed, photonMessageInfoWrapped);
			}
			else
			{
				CatchCosmeticCritterRPC(action, holdableID, seed, photonMessageInfoWrapped);
			}
		}
	}

	private void CatchCosmeticCritterRPC(CosmeticCritterAction catchAction, int catcherID, int seed, PhotonMessageInfoWrapped info)
	{
		if (!activeCrittersBySeed.TryGetValue(seed, out var value))
		{
			return;
		}
		for (int i = 0; i < remoteCritterCatchers.Count; i++)
		{
			CosmeticCritterCatcher cosmeticCritterCatcher = remoteCritterCatchers[i];
			if (cosmeticCritterCatcher.OwnerID != catcherID)
			{
				continue;
			}
			if (cosmeticCritterCatcher.OwningPlayerMatches(info) && cosmeticCritterCatcher.ValidateRemoteCatchAction(value, catchAction, info.SentServerTime))
			{
				cosmeticCritterCatcher.OnCatch(value, catchAction, info.SentServerTime);
				if ((catchAction & CosmeticCritterAction.Despawn) != CosmeticCritterAction.None)
				{
					FreeCritter(value);
				}
				if ((catchAction & CosmeticCritterAction.SpawnLinked) != CosmeticCritterAction.None && cosmeticCritterCatcher.GetLinkedSpawner() != null && (!activeCrittersPerType.TryGetValue(cosmeticCritterCatcher.GetLinkedSpawner().GetCritterType(), out var value2) || value2 < cosmeticCritterCatcher.GetLinkedSpawner().GetCritter().GetGlobalMaxCritters() + 1))
				{
					ReuseOrSpawnNewCritter(cosmeticCritterCatcher.GetLinkedSpawner(), seed + 1, info.SentServerTime);
				}
			}
			break;
		}
	}

	private void SpawnCosmeticCritterRPC(int spawnerID, int seed, PhotonMessageInfoWrapped info)
	{
		if (activeCrittersBySeed.ContainsKey(seed))
		{
			return;
		}
		for (int i = 0; i < remoteCritterSpawners.Count; i++)
		{
			CosmeticCritterSpawnerIndependent cosmeticCritterSpawnerIndependent = remoteCritterSpawners[i];
			if (cosmeticCritterSpawnerIndependent.OwnerID == spawnerID)
			{
				if (cosmeticCritterSpawnerIndependent.OwningPlayerMatches(info) && (!activeCrittersPerType.TryGetValue(cosmeticCritterSpawnerIndependent.GetCritterType(), out var value) || value < cosmeticCritterSpawnerIndependent.GetCritter().GetGlobalMaxCritters()) && cosmeticCritterSpawnerIndependent.CanSpawnRemote(info.SentServerTime))
				{
					ReuseOrSpawnNewCritter(cosmeticCritterSpawnerIndependent, seed, info.SentServerTime);
				}
				break;
			}
		}
	}
}
