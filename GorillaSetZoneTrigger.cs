using UnityEngine;

public class GorillaSetZoneTrigger : GorillaTriggerBox
{
	[SerializeField]
	private GTZone[] zones;

	public override void OnBoxTriggered()
	{
		ZoneManagement.SetActiveZones(zones);
	}
}
