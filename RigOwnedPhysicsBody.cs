using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class RigOwnedPhysicsBody : MonoBehaviour
{
	private VRRig rig;

	public RigOwnedTransformView transformView;

	private bool hasTransformView;

	public RigOwnedRigidbodyView rigidbodyView;

	private bool hasRigidbodyView;

	public MonoBehaviourPun[] otherComponents;

	private bool hasRig;

	[Tooltip("To make a rigidbody unaffected by the movement of the holdable part, put this script on the holdable, make the RigOwnedRigidbodyView a child of it, and check this box")]
	[SerializeField]
	private bool detachTransform;

	private void Awake()
	{
		hasTransformView = transformView != null;
		hasRigidbodyView = rigidbodyView != null;
		if (!hasTransformView && !hasRigidbodyView && otherComponents.Length == 0)
		{
			GTDev.LogError("RigOwnedPhysicsBody has nothing to do! No TransformView, RigidbodyView, or otherComponents");
		}
		if (detachTransform)
		{
			if (hasTransformView)
			{
				transformView.transform.parent = null;
			}
			else if (hasRigidbodyView)
			{
				rigidbodyView.transform.parent = null;
			}
		}
	}

	private void OnEnable()
	{
		if (!base.gameObject.activeInHierarchy)
		{
			return;
		}
		NetworkSystem.Instance.OnJoinedRoomEvent += new Action(OnNetConnect);
		NetworkSystem.Instance.OnReturnedToSinglePlayer += new Action(OnNetDisconnect);
		if (!hasRig)
		{
			rig = GetComponentInParent<VRRig>();
			hasRig = rig != null;
		}
		if (detachTransform)
		{
			if (hasTransformView)
			{
				transformView.gameObject.SetActive(value: true);
			}
			else if (hasRigidbodyView)
			{
				rigidbodyView.gameObject.SetActive(value: true);
			}
		}
		if (NetworkSystem.Instance.InRoom)
		{
			OnNetConnect();
		}
		else
		{
			OnNetDisconnect();
		}
	}

	private void OnDisable()
	{
		NetworkSystem.Instance.OnJoinedRoomEvent -= new Action(OnNetConnect);
		NetworkSystem.Instance.OnReturnedToSinglePlayer -= new Action(OnNetDisconnect);
		if (detachTransform)
		{
			if (hasTransformView)
			{
				transformView.gameObject.SetActive(value: false);
			}
			else if (hasRigidbodyView)
			{
				rigidbodyView.gameObject.SetActive(value: false);
			}
		}
		OnNetDisconnect();
	}

	private void OnNetConnect()
	{
		if (hasTransformView)
		{
			transformView.enabled = hasRig;
		}
		if (hasRigidbodyView)
		{
			rigidbodyView.enabled = hasRig;
		}
		MonoBehaviourPun[] array = otherComponents;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].enabled = hasRig;
		}
		if (!hasRig)
		{
			return;
		}
		PhotonView getView = rig.netView.GetView;
		List<Component> observedComponents = getView.ObservedComponents;
		if (hasTransformView)
		{
			transformView.SetIsMine(getView.IsMine);
			if (!observedComponents.Contains(transformView))
			{
				observedComponents.Add(transformView);
			}
		}
		if (hasRigidbodyView)
		{
			rigidbodyView.SetIsMine(getView.IsMine);
			if (!observedComponents.Contains(rigidbodyView))
			{
				observedComponents.Add(rigidbodyView);
			}
		}
		array = otherComponents;
		foreach (MonoBehaviourPun item in array)
		{
			if (!observedComponents.Contains(item))
			{
				observedComponents.Add(item);
			}
		}
	}

	private void OnNetDisconnect()
	{
		if (ApplicationQuittingState.IsQuitting)
		{
			return;
		}
		if (hasTransformView)
		{
			transformView.enabled = false;
		}
		if (hasRigidbodyView)
		{
			rigidbodyView.enabled = false;
		}
		MonoBehaviourPun[] array = otherComponents;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].enabled = false;
		}
		if (hasRig && NetworkSystem.Instance.InRoom)
		{
			List<Component> observedComponents = rig.netView.GetView.ObservedComponents;
			if (hasTransformView)
			{
				observedComponents.Remove(transformView);
			}
			if (hasRigidbodyView)
			{
				observedComponents.Remove(rigidbodyView);
			}
			array = otherComponents;
			foreach (MonoBehaviourPun item in array)
			{
				observedComponents.Remove(item);
			}
		}
	}
}
