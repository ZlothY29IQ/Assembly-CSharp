using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CompositeTriggerEvents))]
public class VRRigCollection : MonoBehaviour
{
	public readonly List<RigContainer> containedRigs = new List<RigContainer>(20);

	[SerializeField]
	private CompositeTriggerEvents collisionTriggerEvents;

	public Action<RigContainer> playerEnteredCollection;

	public Action<RigContainer> playerLeftCollection;

	public List<RigContainer> Rigs => containedRigs;

	private void OnEnable()
	{
		collisionTriggerEvents.CompositeTriggerEnter += OnRigTriggerEnter;
		collisionTriggerEvents.CompositeTriggerExit += OnRigTriggerExit;
	}

	private void OnDisable()
	{
		for (int num = containedRigs.Count - 1; num >= 0; num--)
		{
			RigDisabled(containedRigs[num]);
		}
		collisionTriggerEvents.CompositeTriggerEnter -= OnRigTriggerEnter;
		collisionTriggerEvents.CompositeTriggerExit -= OnRigTriggerExit;
	}

	private void OnRigTriggerEnter(Collider other)
	{
		Rigidbody attachedRigidbody = other.attachedRigidbody;
		if (!(attachedRigidbody == null) && attachedRigidbody.TryGetComponent<RigContainer>(out var component) && !(other != component.HeadCollider) && !containedRigs.Contains(component))
		{
			component.RigEvents.disableEvent += new Action<RigContainer>(RigDisabled);
			containedRigs.Add(component);
			playerEnteredCollection?.Invoke(component);
		}
	}

	private void OnRigTriggerExit(Collider other)
	{
		Rigidbody attachedRigidbody = other.attachedRigidbody;
		if (!(attachedRigidbody == null) && attachedRigidbody.TryGetComponent<RigContainer>(out var component) && !(other != component.HeadCollider) && containedRigs.Contains(component))
		{
			component.RigEvents.disableEvent -= new Action<RigContainer>(RigDisabled);
			containedRigs.Remove(component);
			playerLeftCollection?.Invoke(component);
		}
	}

	private void RigDisabled(RigContainer rig)
	{
		collisionTriggerEvents.ResetColliderMask(rig.HeadCollider);
		collisionTriggerEvents.ResetColliderMask(rig.BodyCollider);
	}

	private bool HasRig(VRRig rig)
	{
		for (int i = 0; i < containedRigs.Count; i++)
		{
			if (containedRigs[i].Rig == rig)
			{
				return true;
			}
		}
		return false;
	}

	private bool HasRig(NetPlayer player)
	{
		for (int i = 0; i < containedRigs.Count; i++)
		{
			if (containedRigs[i].Creator == player)
			{
				return true;
			}
		}
		return false;
	}
}
