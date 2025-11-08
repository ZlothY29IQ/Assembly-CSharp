using System;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;

public class ConditionalTrigger : MonoBehaviour, IRigAware
{
	[Space]
	[SerializeField]
	private TriggerCondition _tracking;

	[Space]
	[SerializeField]
	private Transform _from;

	[SerializeField]
	private Transform _to;

	[SerializeField]
	private float _maxDistance;

	[NonSerialized]
	private float _distance;

	[Space]
	public UnityEvent onMaxDistance;

	[SerializeField]
	private float _interval = 1f;

	[NonSerialized]
	private TimeSince _timeSince;

	[Space]
	public UnityEvent onTimeElapsed;

	[Space]
	private VRRig _rig;

	private int intValue => (int)_tracking;

	public void SetProximityFromRig()
	{
		if (_rig.AsNull() == null)
		{
			FindRig(out _rig);
		}
		if ((bool)_rig)
		{
			_from = _rig.transform;
		}
	}

	public void SetProximityToRig()
	{
		if (_rig.AsNull() == null)
		{
			FindRig(out _rig);
		}
		if ((bool)_rig)
		{
			_to = _rig.transform;
		}
	}

	public void SetProximityFrom(Transform from)
	{
		_from = from;
	}

	public void SetProxmityTo(Transform to)
	{
		_to = to;
	}

	public void TrackedSet(TriggerCondition conditions)
	{
		_tracking = conditions;
	}

	public void TrackedAdd(TriggerCondition conditions)
	{
		_tracking |= conditions;
	}

	public void TrackedRemove(TriggerCondition conditions)
	{
		_tracking &= ~conditions;
	}

	public void TrackedSet(int conditions)
	{
		_tracking = (TriggerCondition)conditions;
	}

	public void TrackedAdd(int conditions)
	{
		_tracking |= (TriggerCondition)conditions;
	}

	public void TrackedRemove(int conditions)
	{
		_tracking &= (TriggerCondition)(~conditions);
	}

	public void TrackedClear()
	{
		_tracking = TriggerCondition.None;
	}

	private void OnEnable()
	{
		_timeSince = 0f;
	}

	private void Update()
	{
		if (IsTracking(TriggerCondition.TimeElapsed))
		{
			TrackTimeElapsed();
		}
		if (IsTracking(TriggerCondition.Proximity))
		{
			TrackProximity();
		}
		else
		{
			_distance = 0f;
		}
	}

	private void TrackTimeElapsed()
	{
		if (_timeSince.HasElapsed(_interval, resetOnElapsed: true))
		{
			onTimeElapsed?.Invoke();
		}
	}

	private void TrackProximity()
	{
		if (!_from || !_to)
		{
			_distance = 0f;
			return;
		}
		_distance = Vector3.Distance(_to.position, _from.position);
		if (_distance >= _maxDistance)
		{
			onMaxDistance?.Invoke();
		}
	}

	private bool IsTracking(TriggerCondition condition)
	{
		return (_tracking & condition) == condition;
	}

	private static void FindRig(out VRRig rig)
	{
		if (PhotonNetwork.InRoom)
		{
			rig = GorillaGameManager.StaticFindRigForPlayer(NetPlayer.Get(PhotonNetwork.LocalPlayer));
		}
		else
		{
			rig = VRRig.LocalRig;
		}
	}

	public void SetRig(VRRig rig)
	{
		_rig = rig;
	}
}
