using System.Collections.Generic;
using GorillaTagScripts;
using UnityEngine;

public class BuilderRoomBoundary : GorillaTriggerBox
{
	[SerializeField]
	private List<SizeChangerTrigger> enableOnEnterTrigger;

	[SerializeField]
	private SizeChangerTrigger disableOnExitTrigger;

	private VRRig rigRef;

	private void Awake()
	{
		foreach (SizeChangerTrigger item in enableOnEnterTrigger)
		{
			item.OnEnter += OnEnteredBoundary;
		}
		disableOnExitTrigger.OnExit += OnExitedBoundary;
	}

	private void OnDestroy()
	{
		foreach (SizeChangerTrigger item in enableOnEnterTrigger)
		{
			item.OnEnter -= OnEnteredBoundary;
		}
		disableOnExitTrigger.OnExit -= OnExitedBoundary;
	}

	public void OnEnteredBoundary(Collider other)
	{
		if (!(other.attachedRigidbody == null))
		{
			rigRef = other.attachedRigidbody.gameObject.GetComponent<VRRig>();
			if (!(rigRef == null) && rigRef.isOfflineVRRig && BuilderTable.TryGetBuilderTableForZone(rigRef.zoneEntity.currentZone, out var _) && ZoneManagement.instance.IsZoneActive(GTZone.monkeBlocks))
			{
				rigRef.EnableBuilderResizeWatch(on: true);
			}
		}
	}

	public void OnExitedBoundary(Collider other)
	{
		if (!(other.attachedRigidbody == null))
		{
			rigRef = other.attachedRigidbody.gameObject.GetComponent<VRRig>();
			if (!(rigRef == null) && rigRef.isOfflineVRRig)
			{
				rigRef.EnableBuilderResizeWatch(on: false);
			}
		}
	}
}
