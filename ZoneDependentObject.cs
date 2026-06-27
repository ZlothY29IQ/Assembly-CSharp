using System.Collections.Generic;
using UnityEngine;

public class ZoneDependentObject : MonoBehaviour
{
	public List<GTZone> zones = new List<GTZone> { GTZone.forest };

	private void Awake()
	{
		ZoneEntityBSP.onPlayerZoneChange += OnPlayerZoneChange;
		UpdateObjectState();
	}

	private void OnDestroy()
	{
		ZoneEntityBSP.onPlayerZoneChange -= OnPlayerZoneChange;
	}

	private void OnPlayerZoneChange(VRRig rig, GTZone fromZone, GTZone toZone)
	{
		Debug.Log($"PlayerZoneChange: Player[{rig.Creator?.ActorNumber}] {fromZone}->{toZone}");
		UpdateObjectState();
	}

	private void UpdateObjectState()
	{
		if (zones.IsAnyPlayerInZones() != base.gameObject.activeSelf)
		{
			base.gameObject.SetActive(zones.IsAnyPlayerInZones());
		}
	}
}
