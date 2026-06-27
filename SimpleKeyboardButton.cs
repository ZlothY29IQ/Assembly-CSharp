using System;
using UnityEngine;
using UnityEngine.Events;

public class SimpleKeyboardButton : SimpleButton
{
	public enum ButtonFunction
	{
		NONE,
		CURSOR_BACK,
		CURSOR_FWD,
		DELETE
	}

	[SerializeField]
	private string keyValue;

	[SerializeField]
	private ButtonFunction buttonFunction;

	[SerializeField]
	private UnityEvent<string> KeyPress;

	public Action<SimpleKeyboardButton, bool> OnKeyPress;

	public string KeyValue => keyValue;

	public ButtonFunction Function => buttonFunction;

	protected override void handlePress(bool isLeft)
	{
		KeyPress?.Invoke(keyValue);
		OnKeyPress?.Invoke(this, isLeft);
	}
}
