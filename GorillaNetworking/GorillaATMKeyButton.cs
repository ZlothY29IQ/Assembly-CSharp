namespace GorillaNetworking;

public class GorillaATMKeyButton : GorillaKeyButton<GorillaATMKeyBindings>
{
	protected override void OnButtonPressedEvent()
	{
		GameEvents.OnGorrillaATMKeyButtonPressedEvent.Invoke(Binding);
	}
}
