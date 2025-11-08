using System;
using System.Collections.Generic;
using UnityEngine;

public class MazePlayerCollection : MonoBehaviour
{
	public List<VRRig> containedRigs = new List<VRRig>();

	public List<MonkeyeAI> monkeyeAis = new List<MonkeyeAI>();

	private void Start()
	{
		NetworkSystem.Instance.OnPlayerLeft += new Action<NetPlayer>(OnPlayerLeftRoom);
	}

	private void OnDestroy()
	{
		NetworkSystem.Instance.OnPlayerLeft -= new Action<NetPlayer>(OnPlayerLeftRoom);
	}

	public void OnTriggerEnter(Collider other)
	{
		if ((bool)other.GetComponent<SphereCollider>())
		{
			VRRig component = other.attachedRigidbody.gameObject.GetComponent<VRRig>();
			if (!(component == null) && !containedRigs.Contains(component))
			{
				containedRigs.Add(component);
			}
		}
	}

	public void OnTriggerExit(Collider other)
	{
		if ((bool)other.GetComponent<SphereCollider>())
		{
			VRRig component = other.attachedRigidbody.gameObject.GetComponent<VRRig>();
			if (!(component == null) && containedRigs.Contains(component))
			{
				containedRigs.Remove(component);
			}
		}
	}

	public void OnPlayerLeftRoom(NetPlayer otherPlayer)
	{
		containedRigs.RemoveAll((VRRig r) => r?.creator == null || r.creator == otherPlayer);
	}
}
