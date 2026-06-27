using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Photon.Pun;
using UnityEngine;

namespace MaterialCycler;

[RequireComponent(typeof(PhotonView))]
public class MaterialCyclerManager : MonoBehaviour
{
	private readonly Dictionary<int, List<MaterialCycler>> _cyclers = new Dictionary<int, List<MaterialCycler>>();

	private readonly Dictionary<int, int> _numMaterialsByKey = new Dictionary<int, int>();

	private readonly Dictionary<int, int> _currentMaterialIndexByKey = new Dictionary<int, int>();

	private PhotonView _photonView;

	public static MaterialCyclerManager Instance { get; private set; }

	public float SyncTimeOut { get; private set; } = 1f;

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else if (Instance != this)
		{
			UnityEngine.Object.Destroy(this);
		}
		_photonView = GetComponent<PhotonView>();
	}

	public void RegisterCycler(int key, MaterialCycler cycler)
	{
		if (_cyclers.TryGetValue(key, out var value))
		{
			if (!value.Contains(cycler))
			{
				value.Add(cycler);
			}
			if (cycler.NumMaterials != _numMaterialsByKey[key])
			{
				throw new Exception("New cycler does not match size of other matching cyclers.");
			}
			int num = _currentMaterialIndexByKey[key];
			if (num < 0 || num >= cycler.NumMaterials)
			{
				num = value[0].index;
				_currentMaterialIndexByKey[key] = num;
			}
			cycler.CycleMaterial(num);
		}
		else
		{
			_cyclers[key] = new List<MaterialCycler> { cycler };
			_numMaterialsByKey[key] = cycler.NumMaterials;
			_currentMaterialIndexByKey[key] = cycler.index;
		}
	}

	public void UnregisterCycler(MaterialCycler cycler)
	{
		int keyHash = cycler.KeyHash;
		if (_cyclers.TryGetValue(keyHash, out var value) && value.Remove(cycler) && value.Count == 0)
		{
			_cyclers.Remove(keyHash);
			_numMaterialsByKey.Remove(keyHash);
			_currentMaterialIndexByKey.Remove(keyHash);
		}
	}

	public void CycleKey(int key)
	{
		int num = (_currentMaterialIndexByKey[key] + 1) % _numMaterialsByKey[key];
		_photonView.RPC("RPC_CycleKey", RpcTarget.All, key, num);
	}

	private void RPC_CycleKey(int key, int index, PhotonMessageInfo info)
	{
		if (!_cyclers.TryGetValue(key, out var value))
		{
			return;
		}
		_currentMaterialIndexByKey[key] = index;
		foreach (MaterialCycler item in value)
		{
			item.CycleMaterial(index);
		}
	}

	public void Synchronize(int key, int materialIndex, Color c)
	{
		UpdateMaterialCycler(key, materialIndex, c);
		if (RoomSystem.JoinedRoom)
		{
			int num = PackColor(c);
			_photonView.RPC("RPC_Synchronize", RpcTarget.Others, key, materialIndex, num);
		}
	}

	[PunRPC]
	private void RPC_Synchronize(int key, int materialIndex, int colourPacked, PhotonMessageInfo info)
	{
		if (VRRigCache.Instance.TryGetVrrig(info.Sender, out var playerRig) && FXSystem.CheckCallSpam(playerRig.Rig.fxSettings, 21, info.SentServerTime) && materialIndex >= 0)
		{
			UpdateMaterialCycler(key, materialIndex, UnPackColour(colourPacked));
		}
	}

	private void UpdateMaterialCycler(int key, int materialIndex, Color colour)
	{
		if (!_cyclers.TryGetValue(key, out var value))
		{
			return;
		}
		_currentMaterialIndexByKey[key] = materialIndex;
		foreach (MaterialCycler item in value)
		{
			item.MaterialCyclerNetworked_OnSynchronize(materialIndex, colour);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int PackColor(Color c)
	{
		int num = Mathf.CeilToInt(c.r * 9f);
		int num2 = Mathf.CeilToInt(c.g * 9f);
		int num3 = Mathf.CeilToInt(c.b * 9f);
		return num | (num2 << 8) | (num3 << 16);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Color UnPackColour(int colourPacked)
	{
		int value = colourPacked & 0xFF;
		int value2 = (colourPacked >> 8) & 0xFF;
		int value3 = (colourPacked >> 16) & 0xFF;
		int num = Mathf.Clamp(value, 0, 9);
		value2 = Mathf.Clamp(value2, 0, 9);
		value3 = Mathf.Clamp(value3, 0, 9);
		float r = (float)num / 9f;
		float g = (float)value2 / 9f;
		float b = (float)value3 / 9f;
		return new Color(r, g, b);
	}
}
