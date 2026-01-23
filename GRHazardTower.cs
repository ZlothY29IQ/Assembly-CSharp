using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class GRHazardTower : MonoBehaviour, IGameEntityComponent, IGameProjectileLauncher
{
	public GameEntity gameEntity;

	public GRSenseNearby senseNearby;

	public GRSenseLineOfSight senseLineOfSight;

	public float projectileSpeed;

	public GameEntity projectilePrefab;

	public Transform fireFrom;

	public float fireChargeTime;

	public float fireCooldownTime;

	private double nextFireTime;

	private static List<VRRig> tempRigs = new List<VRRig>(16);

	public void OnEntityInit()
	{
		gameEntity.MinTimeBetweenTicks = 0.5f;
		GameEntity obj = gameEntity;
		obj.OnTick = (Action)Delegate.Combine(obj.OnTick, new Action(OnThink));
		senseNearby.Setup(fireFrom, gameEntity);
	}

	public void OnEntityDestroy()
	{
	}

	public void OnEntityStateChange(long prevState, long nextState)
	{
	}

	public void OnThink()
	{
		if (!gameEntity.IsAuthority())
		{
			return;
		}
		double timeAsDouble = Time.timeAsDouble;
		if (!(timeAsDouble < nextFireTime))
		{
			tempRigs.Clear();
			tempRigs.Add(VRRig.LocalRig);
			VRRigCache.Instance.GetAllUsedRigs(tempRigs);
			senseNearby.UpdateNearby(tempRigs, senseLineOfSight);
			float outDistanceSq;
			VRRig vRRig = senseNearby.PickClosest(out outDistanceSq);
			if (!(vRRig == null))
			{
				Vector3 position = vRRig.transform.position;
				Vector3 vector = Vector3.up * 0.1f;
				position += vector;
				GhostReactorManager.Get(gameEntity).RequestFireProjectile(gameEntity.id, fireFrom.position, position, PhotonNetwork.Time + 0.0);
				nextFireTime = timeAsDouble + (double)fireCooldownTime;
			}
		}
	}

	public void OnFire(Vector3 fireFromPos, Vector3 fireAtPos, double fireAtTime)
	{
		if (gameEntity.IsAuthority() && GREnemyRanged.CalculateLaunchDirection(fireFromPos, fireAtPos, projectileSpeed, out var direction))
		{
			gameEntity.manager.RequestCreateItem(projectilePrefab.name.GetStaticHash(), fireFromPos, Quaternion.LookRotation(direction, Vector3.up), gameEntity.GetNetId());
		}
		double timeAsDouble = Time.timeAsDouble;
		nextFireTime = timeAsDouble + (double)fireCooldownTime;
	}

	public void OnProjectileInit(GRRangedEnemyProjectile projectile)
	{
	}

	public void OnProjectileHit(GRRangedEnemyProjectile projectile, Collision collision)
	{
	}
}
