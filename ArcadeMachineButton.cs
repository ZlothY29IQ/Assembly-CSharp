using UnityEngine;

public class ArcadeMachineButton : GorillaPressableButton
{
	public delegate void ArcadeMachineButtonEvent(int id, bool state);

	private bool state;

	[SerializeField]
	private int ButtonID;

	public event ArcadeMachineButtonEvent OnStateChange;

	public override void ButtonActivation()
	{
		base.ButtonActivation();
		if (!state)
		{
			state = true;
			if (this.OnStateChange != null)
			{
				this.OnStateChange(ButtonID, state);
			}
		}
	}

	private void OnTriggerExit(Collider collider)
	{
		if (base.enabled && state && !(collider.GetComponentInParent<GorillaTriggerColliderHandIndicator>() == null))
		{
			state = false;
			if (this.OnStateChange != null)
			{
				this.OnStateChange(ButtonID, state);
			}
		}
	}
}
