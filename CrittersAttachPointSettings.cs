public class CrittersAttachPointSettings : CrittersActorSettings
{
	public bool isLeft;

	public CrittersAttachPoint.AnchoredLocationTypes anchoredLocation;

	public override void UpdateActorSettings()
	{
		base.UpdateActorSettings();
		CrittersAttachPoint obj = (CrittersAttachPoint)parentActor;
		obj.anchorLocation = anchoredLocation;
		obj.rb.isKinematic = true;
		obj.isLeft = isLeft;
	}
}
