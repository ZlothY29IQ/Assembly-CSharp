using UnityEngine.Events;

public class GorillaTriggerBoxEvent : GorillaTriggerBox
{
	public UnityEvent onBoxTriggered;

	public override void OnBoxTriggered()
	{
		if (onBoxTriggered != null)
		{
			onBoxTriggered.Invoke();
		}
	}
}
