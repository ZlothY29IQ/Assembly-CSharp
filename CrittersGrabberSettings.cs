using UnityEngine;

public class CrittersGrabberSettings : CrittersActorSettings
{
	public Transform _grabPosition;

	public float _grabDistance;

	public override void UpdateActorSettings()
	{
		base.UpdateActorSettings();
		CrittersGrabber obj = (CrittersGrabber)parentActor;
		obj.grabPosition = _grabPosition;
		obj.grabDistance = _grabDistance;
	}
}
