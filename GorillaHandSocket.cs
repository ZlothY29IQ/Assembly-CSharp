using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class GorillaHandSocket : MonoBehaviour
{
	public Collider collider;

	public float attachCooldown = 0.5f;

	public HandSocketConstraint constraint;

	[NonSerialized]
	private GorillaHandNode _attachedHand;

	[NonSerialized]
	private bool _inUse;

	[NonSerialized]
	private TimeSince _sinceSocketStateChange;

	private static readonly Dictionary<Collider, GorillaHandSocket> gColliderToSocket = new Dictionary<Collider, GorillaHandSocket>(64);

	public GorillaHandNode attachedHand => _attachedHand;

	public bool inUse => _inUse;

	public static bool FetchSocket(Collider collider, out GorillaHandSocket socket)
	{
		return gColliderToSocket.TryGetValue(collider, out socket);
	}

	public bool CanAttach()
	{
		if (!_inUse)
		{
			return _sinceSocketStateChange.HasElapsed(attachCooldown, resetOnElapsed: true);
		}
		return false;
	}

	public void Attach(GorillaHandNode hand)
	{
		if (CanAttach() && !(hand == null))
		{
			hand.attachedToSocket = this;
			_attachedHand = hand;
			_inUse = true;
			OnHandAttach();
		}
	}

	public void Detach()
	{
		Detach(out var _);
	}

	public void Detach(out GorillaHandNode hand)
	{
		if (_inUse)
		{
			_inUse = false;
		}
		if (_attachedHand == null)
		{
			hand = null;
			return;
		}
		hand = _attachedHand;
		hand.attachedToSocket = null;
		_attachedHand = null;
		OnHandDetach();
		_sinceSocketStateChange = TimeSince.Now();
	}

	protected virtual void OnHandAttach()
	{
	}

	protected virtual void OnHandDetach()
	{
	}

	protected virtual void OnUpdateAttached()
	{
		_attachedHand.transform.position = base.transform.position;
	}

	private void OnEnable()
	{
		if (!(collider == null))
		{
			gColliderToSocket.TryAdd(collider, this);
		}
	}

	private void OnDisable()
	{
		if (!(collider == null))
		{
			gColliderToSocket.Remove(collider);
		}
	}

	private void Awake()
	{
		Setup();
	}

	private void FixedUpdate()
	{
		if (_inUse && (bool)_attachedHand)
		{
			OnUpdateAttached();
		}
	}

	private void Setup()
	{
		if (collider == null)
		{
			collider = GetComponent<Collider>();
		}
		int num = 0;
		num |= 0x400;
		num |= 0x200000;
		num |= 0x1000000;
		base.gameObject.SetTag(UnityTag.GorillaHandSocket);
		base.gameObject.SetLayer(UnityLayer.GorillaHandSocket);
		collider.isTrigger = true;
		collider.includeLayers = num;
		collider.excludeLayers = ~num;
		_sinceSocketStateChange = TimeSince.Now();
	}
}
