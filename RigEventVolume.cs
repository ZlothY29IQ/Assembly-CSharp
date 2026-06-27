using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;

public class RigEventVolume : MonoBehaviour
{
	private enum Mode
	{
		RELATIVE,
		ABSOLUTE,
		NONE
	}

	private Dictionary<RigEventVolumeTrigger, int> gameObjects = new Dictionary<RigEventVolumeTrigger, int>();

	[SerializeField]
	private Mode mode = Mode.ABSOLUTE;

	[Range(0.05f, 1f)]
	[SerializeField]
	private float relThreshold = 0.05f;

	[SerializeField]
	private VRRigCollection rigCollection;

	[Range(1f, 20f)]
	[SerializeField]
	private int absThreshold = 1;

	[SerializeField]
	private UnityEvent<VRRig> RigEnters;

	[SerializeField]
	private UnityEvent<VRRig> RigExits;

	[SerializeField]
	private UnityEvent GoesOverThreshold;

	[SerializeField]
	private UnityEvent GoesUnderThreshold;

	[SerializeField]
	private UnityEvent<VRRig> LocalRigEnters;

	[SerializeField]
	private UnityEvent<VRRig> LocalRigExits;

	private List<VRRig> rigs = new List<VRRig>();

	private bool localRigPresent;

	public VRRig[] Rigs => rigs.ToArray();

	public int RigCount => gameObjects.Keys.Count;

	public bool LocalRigPresent => localRigPresent;

	public event Action OnCountChanged;

	private void OnEnable()
	{
		if (mode == Mode.RELATIVE)
		{
			if (rigCollection != null)
			{
				VRRigCollection vRRigCollection = rigCollection;
				vRRigCollection.playerEnteredCollection = (Action<RigContainer>)Delegate.Combine(vRRigCollection.playerEnteredCollection, new Action<RigContainer>(OnJoined));
				VRRigCollection vRRigCollection2 = rigCollection;
				vRRigCollection2.playerLeftCollection = (Action<RigContainer>)Delegate.Combine(vRRigCollection2.playerLeftCollection, new Action<RigContainer>(OnLeft));
			}
			else
			{
				NetworkSystem.Instance.OnPlayerJoined += new Action<NetPlayer>(OnNetJoined);
				NetworkSystem.Instance.OnPlayerLeft += new Action<NetPlayer>(OnNetLeft);
			}
		}
	}

	private void OnDisable()
	{
		OnDestroy();
	}

	private void OnDestroy()
	{
		if (mode == Mode.RELATIVE)
		{
			if (rigCollection != null)
			{
				VRRigCollection vRRigCollection = rigCollection;
				vRRigCollection.playerEnteredCollection = (Action<RigContainer>)Delegate.Remove(vRRigCollection.playerEnteredCollection, new Action<RigContainer>(OnJoined));
				VRRigCollection vRRigCollection2 = rigCollection;
				vRRigCollection2.playerLeftCollection = (Action<RigContainer>)Delegate.Remove(vRRigCollection2.playerLeftCollection, new Action<RigContainer>(OnLeft));
			}
			else
			{
				NetworkSystem.Instance.OnPlayerJoined -= new Action<NetPlayer>(OnNetJoined);
				NetworkSystem.Instance.OnPlayerLeft -= new Action<NetPlayer>(OnNetLeft);
			}
		}
	}

	private void OnNetJoined(NetPlayer np)
	{
		int num = ((PhotonNetwork.CurrentRoom == null) ? 1 : PhotonNetwork.CurrentRoom.PlayerCount);
		countChanged(gameObjects.Count, gameObjects.Count, num - 1, num);
	}

	private void OnNetLeft(NetPlayer np)
	{
		int num = ((PhotonNetwork.CurrentRoom == null) ? 1 : PhotonNetwork.CurrentRoom.PlayerCount);
		countChanged(gameObjects.Count, gameObjects.Count, num + 1, num);
	}

	private void OnJoined(RigContainer rc)
	{
		int num = ((rigCollection == null) ? 1 : rigCollection.Rigs.Count);
		countChanged(gameObjects.Count, gameObjects.Count, num - 1, num);
	}

	private void OnLeft(RigContainer rc)
	{
		int num = ((rigCollection == null) ? 1 : rigCollection.Rigs.Count);
		countChanged(gameObjects.Count, gameObjects.Count, num + 1, num);
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!other.gameObject.TryGetComponent<RigEventVolumeTrigger>(out var component))
		{
			return;
		}
		if (!gameObjects.ContainsKey(component))
		{
			gameObjects.Add(component, 0);
			rigs.Add(component.Rig);
			RigEnters?.Invoke(component.Rig);
			if (component.Rig == VRRig.LocalRig)
			{
				LocalRigEnters?.Invoke(component.Rig);
				localRigPresent = true;
			}
			if (mode != Mode.NONE)
			{
				int num = ((!(rigCollection == null)) ? rigCollection.Rigs.Count : ((PhotonNetwork.CurrentRoom == null) ? 1 : PhotonNetwork.CurrentRoom.PlayerCount));
				countChanged(gameObjects.Count - 1, gameObjects.Count, num, num);
			}
		}
		else
		{
			gameObjects[component]++;
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (!other.gameObject.TryGetComponent<RigEventVolumeTrigger>(out var component))
		{
			return;
		}
		gameObjects[component]--;
		if (gameObjects[component] < 0)
		{
			gameObjects.Remove(component);
			rigs.Remove(component.Rig);
			RigExits?.Invoke(component.Rig);
			if (component.Rig == VRRig.LocalRig)
			{
				LocalRigExits?.Invoke(component.Rig);
				localRigPresent = false;
			}
			if (mode != Mode.NONE)
			{
				int num = ((!(rigCollection == null)) ? rigCollection.Rigs.Count : ((PhotonNetwork.CurrentRoom == null) ? 1 : PhotonNetwork.CurrentRoom.PlayerCount));
				countChanged(gameObjects.Count + 1, gameObjects.Count, num, num);
			}
		}
	}

	private void countChanged(int oldValue, int newValue, int oldPlayerCount, int newPlayerCount)
	{
		if (newValue > oldValue)
		{
			if ((mode == Mode.RELATIVE && (float)newValue / (float)newPlayerCount >= relThreshold && (float)oldValue / (float)oldPlayerCount < relThreshold) || (mode == Mode.ABSOLUTE && newValue >= absThreshold && oldValue < absThreshold))
			{
				GoesOverThreshold?.Invoke();
			}
		}
		else if (newValue < oldValue && ((mode == Mode.RELATIVE && (float)newValue / (float)newPlayerCount < relThreshold && (float)oldValue / (float)oldPlayerCount >= relThreshold) || (mode == Mode.ABSOLUTE && newValue < absThreshold && oldValue >= absThreshold)))
		{
			GoesUnderThreshold?.Invoke();
		}
		this.OnCountChanged?.Invoke();
	}
}
