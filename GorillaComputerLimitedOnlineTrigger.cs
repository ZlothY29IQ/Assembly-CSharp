using GorillaNetworking;

public class GorillaComputerLimitedOnlineTrigger : GorillaTriggerBox
{
	public override void OnBoxTriggered()
	{
		GorillaComputer.instance.SetLimitOnlineScreens(isLimited: true);
	}

	public override void OnBoxExited()
	{
		GorillaComputer.instance.SetLimitOnlineScreens(isLimited: false);
	}
}
