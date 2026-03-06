using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GorillaTag;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public static class Utils
{
	private static ObjectPool<PooledList<IPreDisable>> g_listPool = new ObjectPool<PooledList<IPreDisable>>(2, 10);

	private static StringBuilder reusableSB = new StringBuilder();

	public static void Disable(this GameObject target)
	{
		if (!target.activeSelf)
		{
			return;
		}
		PooledList<IPreDisable> pooledList = g_listPool.Take();
		List<IPreDisable> list = pooledList.List;
		target.GetComponents(list);
		int count = list.Count;
		for (int i = 0; i < count; i++)
		{
			try
			{
				list[i].PreDisable();
			}
			catch (Exception)
			{
			}
		}
		target.SetActive(value: false);
		g_listPool.Return(pooledList);
	}

	public static void AddIfNew<T>(this List<T> list, T item)
	{
		if (!list.Contains(item))
		{
			list.Add(item);
		}
	}

	public static void RemoveIfContains<T>(this List<T> list, T item)
	{
		if (list.Contains(item))
		{
			list.Remove(item);
		}
	}

	public static bool InRoom(this NetPlayer player)
	{
		if (NetworkSystem.Instance.InRoom)
		{
			return Enumerable.Contains(NetworkSystem.Instance.AllNetPlayers, player);
		}
		return false;
	}

	public static bool PlayerInRoom(int actorNumber)
	{
		if (NetworkSystem.Instance.InRoom)
		{
			NetPlayer[] allNetPlayers = NetworkSystem.Instance.AllNetPlayers;
			for (int i = 0; i < allNetPlayers.Length; i++)
			{
				if (allNetPlayers[i].ActorNumber == actorNumber)
				{
					return true;
				}
			}
		}
		return false;
	}

	public static bool PlayerInRoom(int actorNumer, out Player photonPlayer)
	{
		photonPlayer = null;
		if (PhotonNetwork.InRoom)
		{
			return PhotonNetwork.CurrentRoom.Players.TryGetValue(actorNumer, out photonPlayer);
		}
		return false;
	}

	public static bool PlayerInRoom(int actorNumber, out NetPlayer player)
	{
		if (NetworkSystem.Instance == null)
		{
			player = null;
			return false;
		}
		player = NetworkSystem.Instance.GetPlayer(actorNumber);
		if (NetworkSystem.Instance.InRoom)
		{
			return player != null;
		}
		return false;
	}

	public static long PackVector3ToLong(Vector3 vector)
	{
		long num = Mathf.Clamp(Mathf.RoundToInt(vector.x * 1024f) + 1048576, 0, 2097151);
		long num2 = Mathf.Clamp(Mathf.RoundToInt(vector.y * 1024f) + 1048576, 0, 2097151);
		long num3 = Mathf.Clamp(Mathf.RoundToInt(vector.z * 1024f) + 1048576, 0, 2097151);
		return num + (num2 << 21) + (num3 << 42);
	}

	public static Vector3 UnpackVector3FromLong(long data)
	{
		long num = data & 0x1FFFFF;
		long num2 = (data >> 21) & 0x1FFFFF;
		long num3 = (data >> 42) & 0x1FFFFF;
		return new Vector3((float)(num - 1048576) * 0.0009765625f, (float)(num2 - 1048576) * 0.0009765625f, (float)(num3 - 1048576) * 0.0009765625f);
	}

	public static bool IsASCIILetterOrDigit(char c)
	{
		if ((c < 'A' || c > 'Z') && (c < '0' || c > '9'))
		{
			if (c >= 'a')
			{
				return c <= 'z';
			}
			return false;
		}
		return true;
	}

	public static void Log(object message)
	{
	}

	public static void Log(object message, UnityEngine.Object context)
	{
	}

	public static bool ValidateServerTime(double time, double maximumLatency)
	{
		double currentTime = PhotonNetwork.CurrentTime;
		double num = 4294967.295 - maximumLatency;
		double num2;
		if (currentTime > maximumLatency || time < maximumLatency)
		{
			if (time > currentTime + 0.5)
			{
				return false;
			}
			num2 = currentTime - time;
		}
		else
		{
			double num3 = num + currentTime;
			if (time > currentTime + 0.5 && time < num3)
			{
				return false;
			}
			num2 = currentTime + (4294967.295 - time);
		}
		if (num2 > maximumLatency)
		{
			return false;
		}
		return true;
	}

	public static double CalculateNetworkDeltaTime(double prevTime, double newTime)
	{
		if (newTime >= prevTime)
		{
			return newTime - prevTime;
		}
		double num = 4294967.295 - prevTime;
		return newTime + num;
	}
}
