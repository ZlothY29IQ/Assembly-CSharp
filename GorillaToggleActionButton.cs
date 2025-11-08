using System;
using UnityEngine.Events;

public class GorillaToggleActionButton : GorillaPressableButton
{
	public ComponentFunctionReference<bool> ToggleAction;

	private Func<bool> toggleFunc;

	public override void Start()
	{
		BindToggleAction();
	}

	private void BindToggleAction()
	{
		if (ToggleAction != null && ToggleAction.IsValid)
		{
			ToggleAction.Cache();
			onPressButton = new UnityEvent();
			onPressButton.AddListener(ExecuteToggleAction);
		}
	}

	private void ExecuteToggleAction()
	{
		isOn = ToggleAction?.Invoke() ?? false;
		UpdateColor();
	}
}
