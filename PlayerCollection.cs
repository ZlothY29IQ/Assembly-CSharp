using System;
using System.Collections.Generic;
using GorillaTag;
using UnityEngine;

public class PlayerCollection : MonoBehaviour
{
	[NonSerialized]
	[DebugReadout]
	public readonly List<VRRig> containedRigs = new List<VRRig>(20);

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
		SphereCollider component = other.GetComponent<SphereCollider>();
		if (!component)
		{
			return;
		}
		VRRig component2 = other.attachedRigidbody.gameObject.GetComponent<VRRig>();
		if (component2 == null || !containedRigs.Contains(component2))
		{
			return;
		}
		Collider[] components = GetComponents<Collider>();
		for (int i = 0; i < components.Length; i++)
		{
			if (Physics.ComputePenetration(components[i], base.transform.position, base.transform.rotation, component, component.transform.position, component.transform.rotation, out var _, out var _))
			{
				return;
			}
		}
		containedRigs.Remove(component2);
	}

	public void OnPlayerLeftRoom(NetPlayer otherPlayer)
	{
		containedRigs.RemoveAll((VRRig r) => r.creator == null || r.creator == otherPlayer);
	}
}
