using System;
using System.Collections.Generic;
using UnityEngine;

public class SimpleKeyboard : ObservableBehavior
{
	private SimpleKeyboardButton lastButton;

	private float pressTime;

	[SerializeField]
	private float coolDown = 0.1f;

	[SerializeField]
	private TypingTarget typingTarget;

	[SerializeField]
	private SimpleKeyboardButton[] buttons;

	[SerializeField]
	private int audioClipIndex = 67;

	private Dictionary<SimpleKeyboardButton, Vector3> btnPos = new Dictionary<SimpleKeyboardButton, Vector3>();

	[SerializeField]
	private float keyTravel = 0.01f;

	private void Start()
	{
		for (int i = 0; i < buttons.Length; i++)
		{
			btnPos.Add(buttons[i], buttons[i].transform.localPosition);
		}
	}

	protected override void UnityOnEnable()
	{
		for (int i = 0; i < buttons.Length; i++)
		{
			SimpleKeyboardButton obj = buttons[i];
			obj.OnKeyPress = (Action<SimpleKeyboardButton, bool>)Delegate.Combine(obj.OnKeyPress, new Action<SimpleKeyboardButton, bool>(buttonPress));
		}
	}

	private void buttonPress(SimpleKeyboardButton b, bool isLeft)
	{
		if (!(Time.time - pressTime < coolDown))
		{
			pressTime = Time.time;
			lastButton = b;
			switch (b.Function)
			{
			case SimpleKeyboardButton.ButtonFunction.DELETE:
				typingTarget.Delete();
				break;
			case SimpleKeyboardButton.ButtonFunction.CURSOR_FWD:
				typingTarget.MoveCursor(1);
				break;
			case SimpleKeyboardButton.ButtonFunction.CURSOR_BACK:
				typingTarget.MoveCursor(-1);
				break;
			default:
				typingTarget.Append(b.KeyValue);
				break;
			}
			if (audioClipIndex > 0)
			{
				GorillaTagger.Instance.offlineVRRig.PlayHandTapLocal(audioClipIndex, isLeft, 0.05f);
				GorillaTagger.Instance.StartVibration(isLeft, GorillaTagger.Instance.tapHapticStrength / 2f, GorillaTagger.Instance.tapHapticDuration);
			}
		}
	}

	protected override void UnityOnDisable()
	{
		for (int i = 0; i < buttons.Length; i++)
		{
			SimpleKeyboardButton obj = buttons[i];
			obj.OnKeyPress = (Action<SimpleKeyboardButton, bool>)Delegate.Remove(obj.OnKeyPress, new Action<SimpleKeyboardButton, bool>(buttonPress));
		}
	}

	protected override void OnLostObservable()
	{
		for (int i = 0; i < buttons.Length; i++)
		{
			buttons[i].transform.localPosition = btnPos[buttons[i]];
		}
	}

	protected override void OnBecameObservable()
	{
		for (int i = 0; i < buttons.Length; i++)
		{
			buttons[i].transform.localPosition = btnPos[buttons[i]];
		}
	}

	protected override void ObservableSliceUpdate()
	{
	}

	private void LateUpdate()
	{
		if (observable && !(lastButton == null))
		{
			if (Time.time - pressTime >= coolDown)
			{
				lastButton.transform.localPosition = btnPos[lastButton];
				lastButton = null;
			}
			else
			{
				lastButton.transform.localPosition = btnPos[lastButton] + new Vector3(0f, (Time.time - pressTime) / coolDown * (0f - keyTravel), 0f);
			}
		}
	}
}
