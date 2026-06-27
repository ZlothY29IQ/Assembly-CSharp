using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class GTSignalListener : MonoBehaviour
{
	[Space]
	public GTSignalID signal;

	[Space]
	public VRRig rig;

	[Space]
	public bool deafen;

	[FormerlySerializedAs("listenToRigOnly")]
	public bool listenToSelfOnly;

	public bool ignoreSelf;

	[Space]
	public bool callUnityEvent = true;

	[Space]
	[SerializeField]
	private CallLimiter _callLimits = new CallLimiter(10, 0.25f);

	[Space]
	public UnityEvent onSignalReceived;

	[field: NonSerialized]
	public int rigActorID { get; private set; } = -1;

	private void Awake()
	{
		OnListenerAwake();
	}

	private void OnEnable()
	{
		RefreshActorID();
		OnListenerEnable();
		GTSignalRelay.Register(this);
	}

	private void OnDisable()
	{
		GTSignalRelay.Unregister(this);
		OnListenerDisable();
	}

	private void RefreshActorID()
	{
		rig = GetComponentInParent<VRRig>(includeInactive: true);
		rigActorID = ((rig == null) ? (-1) : (rig.Creator?.ActorNumber ?? (-1)));
	}

	public virtual bool IsReady()
	{
		return _callLimits.CheckCallTime(Time.time);
	}

	protected virtual void OnListenerAwake()
	{
	}

	protected virtual void OnListenerEnable()
	{
	}

	protected virtual void OnListenerDisable()
	{
	}

	public virtual void HandleSignalReceived(int sender, object[] args)
	{
	}
}
