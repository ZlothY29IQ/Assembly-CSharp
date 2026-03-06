using System;
using System.Collections.Generic;
using GorillaExtensions;
using Photon.Pun;
using UnityEngine;

internal static class ProjectileTracker
{
	public struct ProjectileInfo(double newTime, Vector3 newVel, Vector3 origin, float newScale, SlingshotProjectile projectile)
	{
		public double timeLaunched = newTime;

		public Vector3 shotVelocity = newVel;

		public Vector3 launchOrigin = origin;

		public float scale = newScale;

		public SlingshotProjectile projectileInstance = projectile;

		public bool hasImpactOverride = projectile.playerImpactEffectPrefab.IsNotNull();
	}

	private static LoopingArray<ProjectileInfo>.Pool m_projectileInfoPool;

	private static LoopingArray<ProjectileInfo> m_localProjectiles;

	public static readonly Dictionary<NetPlayer, LoopingArray<ProjectileInfo>> m_playerProjectiles;

	static ProjectileTracker()
	{
		m_projectileInfoPool = new LoopingArray<ProjectileInfo>.Pool(50, 9);
		m_localProjectiles = new LoopingArray<ProjectileInfo>(50);
		m_playerProjectiles = new Dictionary<NetPlayer, LoopingArray<ProjectileInfo>>(9);
		RoomSystem.LeftRoomEvent += new Action(ClearProjectiles);
		RoomSystem.PlayerLeftEvent += new Action<NetPlayer>(RemovePlayerProjectiles);
	}

	public static void RemovePlayerProjectiles(NetPlayer player)
	{
		if (m_playerProjectiles.TryGetValue(player, out var value))
		{
			ResetPlayerProjectiles(value);
			m_playerProjectiles.Remove(player);
			m_projectileInfoPool.Return(value);
		}
	}

	private static void ClearProjectiles()
	{
		foreach (LoopingArray<ProjectileInfo> value in m_playerProjectiles.Values)
		{
			ResetPlayerProjectiles(value);
			m_projectileInfoPool.Return(value);
		}
		m_playerProjectiles.Clear();
	}

	private static void ResetPlayerProjectiles(LoopingArray<ProjectileInfo> projectiles)
	{
		for (int i = 0; i < projectiles.Length; i++)
		{
			SlingshotProjectile projectileInstance = projectiles[i].projectileInstance;
			if (!projectileInstance.IsNull() && projectileInstance.projectileOwner != NetworkSystem.Instance.LocalPlayer && projectileInstance.gameObject.activeSelf)
			{
				projectileInstance.Deactivate();
			}
		}
	}

	public static int AddAndIncrementLocalProjectile(SlingshotProjectile projectile, Vector3 intialVelocity, Vector3 initialPosition, float scale)
	{
		SlingshotProjectile projectileInstance = m_localProjectiles[m_localProjectiles.CurrentIndex].projectileInstance;
		if (projectileInstance.IsNotNull() && projectileInstance != projectile && projectileInstance.projectileOwner == NetworkSystem.Instance.LocalPlayer && projectileInstance.gameObject.activeSelf)
		{
			projectileInstance.Deactivate();
		}
		ProjectileInfo value = new ProjectileInfo(PhotonNetwork.Time, intialVelocity, initialPosition, scale, projectile);
		return m_localProjectiles.AddAndIncrement(in value);
	}

	public static void AddRemotePlayerProjectile(NetPlayer player, SlingshotProjectile projectile, int projectileIndex, double timeShot, Vector3 intialVelocity, Vector3 initialPosition, float scale)
	{
		LoopingArray<ProjectileInfo> loopingArray;
		if (!m_playerProjectiles.ContainsKey(player))
		{
			loopingArray = m_projectileInfoPool.Take();
			m_playerProjectiles[player] = loopingArray;
		}
		else
		{
			loopingArray = m_playerProjectiles[player];
		}
		if (projectileIndex < 0 || projectileIndex >= loopingArray.Length)
		{
			MonkeAgent.instance.SendReport("invlProj", player.UserId, player.NickName);
			return;
		}
		SlingshotProjectile projectileInstance = loopingArray[projectileIndex].projectileInstance;
		if (projectileInstance.IsNotNull() && projectileInstance.projectileOwner == player && projectileInstance.gameObject.activeSelf)
		{
			projectileInstance.Deactivate();
		}
		ProjectileInfo value = new ProjectileInfo(timeShot, intialVelocity, initialPosition, scale, projectile);
		loopingArray[projectileIndex] = value;
	}

	public static ProjectileInfo GetLocalProjectile(int index)
	{
		return m_localProjectiles[index];
	}

	public static (bool, ProjectileInfo) GetAndRemoveRemotePlayerProjectile(NetPlayer player, int index)
	{
		(bool, ProjectileInfo) result = (false, default(ProjectileInfo));
		if (index < 0 || index >= m_localProjectiles.Length || !m_playerProjectiles.TryGetValue(player, out var value))
		{
			return result;
		}
		ProjectileInfo item = value[index];
		if (item.projectileInstance.IsNotNull())
		{
			result.Item1 = true;
			result.Item2 = item;
			value[index] = default(ProjectileInfo);
		}
		return result;
	}
}
