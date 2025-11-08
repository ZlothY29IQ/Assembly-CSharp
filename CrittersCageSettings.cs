using UnityEngine;

public class CrittersCageSettings : CrittersActorSettings
{
	public Transform cagePoint;

	public Transform grabPoint;

	public override void UpdateActorSettings()
	{
		base.UpdateActorSettings();
		CrittersCage obj = (CrittersCage)parentActor;
		obj.cagePosition = cagePoint;
		obj.grabPosition = grabPoint;
	}
}
