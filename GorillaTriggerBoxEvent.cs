using UnityEngine.Events;

public class GorillaTriggerBoxEvent : GorillaTriggerBox
{
	public UnityEvent onBoxTriggered;

	public UnityEvent onBoxExited;

	public override void OnBoxTriggered()
	{
		onBoxTriggered?.Invoke();
	}

	public override void OnBoxExited()
	{
		onBoxExited?.Invoke();
	}
}
