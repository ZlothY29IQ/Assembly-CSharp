using System;
using Photon.Pun;
using UnityEngine;

public abstract class CosmeticCritter : MonoBehaviour
{
	[Tooltip("After this many seconds the critter will forcibly despawn.")]
	[SerializeField]
	protected float lifetime;

	[Tooltip("The maximum number of this kind of critter that can be in the room at any given time.")]
	[SerializeField]
	private int globalMaxCritters;

	protected double startTime;

	public int Seed { get; protected set; }

	public CosmeticCritterSpawner Spawner { get; protected set; }

	public Type CachedType { get; private set; }

	public int GetGlobalMaxCritters()
	{
		return globalMaxCritters;
	}

	public void SetSeedSpawnerTypeAndTime(int seed, CosmeticCritterSpawner spawner, Type type, double time)
	{
		Seed = seed;
		Spawner = spawner;
		CachedType = type;
		startTime = time;
	}

	public virtual void OnSpawn()
	{
	}

	public virtual void OnDespawn()
	{
	}

	public virtual void SetRandomVariables()
	{
	}

	public abstract void Tick();

	protected double GetAliveTime()
	{
		if (!PhotonNetwork.InRoom)
		{
			return Time.timeAsDouble - startTime;
		}
		return PhotonNetwork.Time - startTime;
	}

	public virtual bool Expired()
	{
		if (!(GetAliveTime() > (double)lifetime))
		{
			return GetAliveTime() < 0.0;
		}
		return true;
	}
}
