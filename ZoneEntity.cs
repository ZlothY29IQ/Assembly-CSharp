using System;
using System.Collections;
using System.Collections.Generic;
using GorillaNetworking;
using UnityEngine;

public class ZoneEntity : MonoBehaviour, IGorillaSliceableSimple
{
	[NonSerialized]
	[Space]
	private int? _entityID;

	[SerializeField]
	private string _entityTag;

	[Space]
	[SerializeField]
	private bool _emitTelemetry = true;

	[SerializeField]
	private int _zoneStayEventInterval = 300;

	[Space]
	[SerializeField]
	private VRRig _entityRig;

	[SerializeField]
	private SphereCollider _collider;

	[NonSerialized]
	[Space]
	public GTZone currentZone = GTZone.none;

	[NonSerialized]
	public GTSubZone currentSubZone;

	[NonSerialized]
	private GroupJoinZoneAB currentGroupZone = 0;

	[NonSerialized]
	private GroupJoinZoneAB previousGroupZone = 0;

	[NonSerialized]
	private GroupJoinZoneAB currentExcludeGroupZone = 0;

	private HashSet<BoxCollider> insideBoxes = new HashSet<BoxCollider>();

	private int currentZonePriority;

	private float groupZoneClearAtTimestamp;

	private float groupZoneClearInterval = 0.1f;

	private Coroutine disabledZoneChangesOnTriggerStayCoroutine;

	[NonSerialized]
	[Space]
	public ZoneNode currentNode = ZoneNode.Null;

	[NonSerialized]
	public ZoneNode lastEnteredNode = ZoneNode.Null;

	[NonSerialized]
	public ZoneNode lastExitedNode = ZoneNode.Null;

	[NonSerialized]
	[Space]
	private TimeSince sinceZoneEntered = 0;

	private readonly Dictionary<int, Collider> currentlyEnteredColliderIds = new Dictionary<int, Collider>();

	private Collider[] colliders = new Collider[20];

	public const string ZONE_LAYER = "Zone";

	private LayerMask layerMask = -1;

	private TimeSince gLastStayPoll = 0;

	public string entityTag => _entityTag;

	public int entityID
	{
		get
		{
			int valueOrDefault = _entityID.GetValueOrDefault();
			if (!_entityID.HasValue)
			{
				valueOrDefault = GetInstanceID();
				_entityID = valueOrDefault;
			}
			return _entityID.Value;
		}
	}

	public VRRig entityRig => _entityRig;

	public SphereCollider collider => _collider;

	public GroupJoinZoneAB GroupZone => (currentGroupZone & ~currentExcludeGroupZone) | previousGroupZone;

	public virtual void OnEnable()
	{
		insideBoxes.Clear();
		ZoneGraph.Register(this);
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.FixedUpdate);
	}

	public virtual void OnDisable()
	{
		insideBoxes.Clear();
		ZoneGraph.Unregister(this);
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.FixedUpdate);
	}

	public void SliceUpdate()
	{
		if ((int)layerMask == -1)
		{
			layerMask = LayerMask.GetMask("Zone");
		}
		int num = Physics.OverlapSphereNonAlloc(base.transform.TransformPoint(this.collider.center), this.collider.radius * base.transform.lossyScale.x, colliders, layerMask, QueryTriggerInteraction.Collide);
		HashSet<int> hashSet = new HashSet<int>();
		for (int i = 0; i < num; i++)
		{
			Collider collider = colliders[i];
			int instanceID = collider.GetInstanceID();
			hashSet.Add(instanceID);
			if (currentlyEnteredColliderIds.TryAdd(instanceID, collider))
			{
				ManualTriggerEnter(collider);
			}
			else
			{
				TriggerStayManualInvoke(collider);
			}
		}
		Queue<int> queue = new Queue<int>();
		foreach (KeyValuePair<int, Collider> currentlyEnteredColliderId in currentlyEnteredColliderIds)
		{
			if (!hashSet.Contains(currentlyEnteredColliderId.Key))
			{
				ManualTriggerExit(currentlyEnteredColliderId.Value);
				queue.Enqueue(currentlyEnteredColliderId.Key);
			}
		}
		int result;
		while (queue.TryDequeue(out result))
		{
			currentlyEnteredColliderIds.Remove(result);
		}
	}

	public void EnableZoneChanges()
	{
		_collider.enabled = true;
		if (disabledZoneChangesOnTriggerStayCoroutine != null)
		{
			StopCoroutine(disabledZoneChangesOnTriggerStayCoroutine);
			disabledZoneChangesOnTriggerStayCoroutine = null;
		}
	}

	public void DisableZoneChanges()
	{
		_collider.enabled = false;
		if (insideBoxes.Count > 0 && disabledZoneChangesOnTriggerStayCoroutine == null)
		{
			disabledZoneChangesOnTriggerStayCoroutine = StartCoroutine(DisabledZoneCollider_OnTriggerStay());
		}
	}

	private IEnumerator DisabledZoneCollider_OnTriggerStay()
	{
		ZoneGraph.Instance?.CheckCompiledMaps();
		while (true)
		{
			foreach (BoxCollider insideBox in insideBoxes)
			{
				TriggerStayManualInvoke(insideBox);
			}
			yield return null;
		}
	}

	private void ManualTriggerEnter(Collider c)
	{
		OnZoneTrigger(GTZoneEventType.zone_enter, c);
	}

	private void ManualTriggerExit(Collider c)
	{
		OnZoneTrigger(GTZoneEventType.zone_exit, c);
	}

	protected virtual void TriggerStayManualInvoke(Collider c)
	{
		if (Application.isPlaying && c is BoxCollider c2)
		{
			ZoneDef zoneDef = ZoneGraph.ColliderToZoneDef(c2);
			if (Time.time >= groupZoneClearAtTimestamp)
			{
				previousGroupZone = currentGroupZone & ~currentExcludeGroupZone;
				currentGroupZone = zoneDef.groupZoneAB;
				currentExcludeGroupZone = zoneDef.excludeGroupZoneAB;
				groupZoneClearAtTimestamp = Time.time + groupZoneClearInterval;
			}
			else
			{
				currentGroupZone |= zoneDef.groupZoneAB;
				currentExcludeGroupZone |= zoneDef.excludeGroupZoneAB;
			}
			if (gLastStayPoll.HasElapsed(1f, resetOnElapsed: true))
			{
				OnZoneTrigger(GTZoneEventType.zone_stay, c2);
			}
		}
	}

	protected virtual void OnZoneTrigger(GTZoneEventType zoneEvent, Collider c)
	{
		if (Application.isPlaying && c is BoxCollider box)
		{
			ZoneDef zone = ZoneGraph.ColliderToZoneDef(box);
			OnZoneTrigger(zoneEvent, zone, box);
		}
	}

	private void OnZoneTrigger(GTZoneEventType zoneEvent, ZoneDef zone, BoxCollider box)
	{
		bool flag = false;
		switch (zoneEvent)
		{
		case GTZoneEventType.zone_enter:
		{
			if (zone.zoneId != lastEnteredNode.zoneId)
			{
				sinceZoneEntered = 0;
			}
			lastEnteredNode = ZoneGraph.ColliderToNode(box);
			ZoneDef zoneDef = ZoneGraph.ColliderToZoneDef(box);
			insideBoxes.Add(box);
			if (zoneDef.priority > currentZonePriority)
			{
				currentZone = zone.zoneId;
				currentSubZone = zone.subZoneId;
				currentZonePriority = zoneDef.priority;
			}
			if (zone.subZoneId == GTSubZone.store_register)
			{
				GorillaTelemetry.PostShopEvent(_entityRig, GTShopEventType.register_visit, CosmeticsController.instance.currentCart);
			}
			flag = zone.trackEnter;
			break;
		}
		case GTZoneEventType.zone_exit:
			lastExitedNode = ZoneGraph.ColliderToNode(box);
			insideBoxes.Remove(box);
			if (currentZone == lastExitedNode.zoneId)
			{
				int num = 0;
				ZoneDef zoneDef2 = null;
				foreach (BoxCollider insideBox in insideBoxes)
				{
					ZoneDef zoneDef3 = ZoneGraph.ColliderToZoneDef(insideBox);
					if (zoneDef3.priority > num)
					{
						zoneDef2 = zoneDef3;
						num = zoneDef3.priority;
					}
				}
				if (zoneDef2 != null)
				{
					currentZone = zoneDef2.zoneId;
					currentSubZone = zoneDef2.subZoneId;
					currentZonePriority = zoneDef2.priority;
				}
				else
				{
					currentZone = GTZone.none;
					currentSubZone = GTSubZone.none;
					currentZonePriority = 0;
				}
			}
			if (currentZone == GTZone.forest && currentSubZone == GTSubZone.tree_room)
			{
				zone.subZoneId = GTSubZone.none;
			}
			flag = zone.trackExit;
			break;
		case GTZoneEventType.zone_stay:
		{
			bool flag2 = sinceZoneEntered.secondsElapsedInt >= _zoneStayEventInterval;
			if (flag2)
			{
				sinceZoneEntered = 0;
			}
			flag = zone.trackStay && flag2;
			break;
		}
		}
		if (_emitTelemetry && flag && _entityRig.isOfflineVRRig)
		{
			GorillaTelemetry.EnqueueZoneEvent(zone.zoneId, zone.subZoneId, zoneEvent);
			GorillaTelemetry.LastZone = zone.zoneId;
			GorillaTelemetry.LastSubZone = zone.subZoneId;
			GorillaTelemetry.LastZoneEventType = zoneEvent;
		}
	}

	public static int Compare<T>(T x, T y) where T : ZoneEntity
	{
		if (x == null && y == null)
		{
			return 0;
		}
		if (x == null)
		{
			return 1;
		}
		if (y == null)
		{
			return -1;
		}
		return x.entityID.CompareTo(y.entityID);
	}
}
