using UnityEngine;
using UnityEngine.Events;

public class GorillaActionButton : GorillaPressableButton
{
	[SerializeField]
	public UnityEvent onPress;

	public override void ButtonActivation()
	{
		base.ButtonActivation();
		onPress.Invoke();
	}
}
