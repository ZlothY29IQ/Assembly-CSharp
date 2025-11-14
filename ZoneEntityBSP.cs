using System;
using UnityEngine;

public class ZoneEntityBSP : MonoBehaviour, IGorillaSliceableSimple
{
	[Space]
	[SerializeField]
	private bool _emitTelemetry = true;

	[Space]
	[SerializeField]
	private VRRig _entityRig;

	[NonSerialized]
	[Space]
	public ZoneDef currentNode;

	[NonSerialized]
	public ZoneDef lastEnteredNode;

	[NonSerialized]
	public ZoneDef lastExitedNode;

	private bool isUpdateDisabled;

	public VRRig entityRig => _entityRig;

	public GTZone currentZone => currentNode?.zoneId ?? GTZone.none;

	public GTSubZone currentSubZone => currentNode?.subZoneId ?? GTSubZone.none;

	public GroupJoinZoneAB GroupZone => currentNode?.groupZoneAB ?? default(GroupJoinZoneAB);

	private void Start()
	{
		if (!_entityRig.isOfflineVRRig)
		{
			_emitTelemetry = false;
		}
		SliceUpdate();
	}

	public virtual void OnEnable()
	{
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.FixedUpdate);
	}

	public virtual void OnDisable()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.FixedUpdate);
	}

	public void SliceUpdate()
	{
		if (isUpdateDisabled)
		{
			return;
		}
		ZoneDef zoneDef = ZoneGraphBSP.Instance.FindZoneAtPoint(base.transform.position);
		if (!zoneDef.IsSameZone(currentNode))
		{
			lastExitedNode = currentNode;
			currentNode = zoneDef;
			lastEnteredNode = zoneDef;
			if (_emitTelemetry)
			{
				ZoneDef zoneDef2 = lastEnteredNode;
				if ((object)zoneDef2 != null && zoneDef2.trackEnter)
				{
					GorillaTelemetry.EnqueueZoneEvent(lastEnteredNode, GTZoneEventType.zone_enter);
				}
				ZoneDef zoneDef3 = lastExitedNode;
				if ((object)zoneDef3 != null && zoneDef3.trackExit)
				{
					GorillaTelemetry.EnqueueZoneEvent(lastExitedNode, GTZoneEventType.zone_exit);
				}
			}
		}
		else if (_emitTelemetry)
		{
			ZoneDef zoneDef4 = currentNode;
			if ((object)zoneDef4 != null && zoneDef4.trackStay)
			{
				GorillaTelemetry.EnqueueZoneEvent(currentNode, GTZoneEventType.zone_stay);
			}
		}
	}

	public void EnableZoneChanges()
	{
		isUpdateDisabled = false;
	}

	public void DisableZoneChanges()
	{
		isUpdateDisabled = true;
	}
}
