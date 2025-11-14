using UnityEngine;

public class PlayerPrefFlagButton : GorillaPressableButton
{
	private enum ButtonMode
	{
		SET_VALUE,
		TOGGLE
	}

	[SerializeField]
	private PlayerPrefFlags.Flag flag;

	[SerializeField]
	private ButtonMode mode;

	[SerializeField]
	private bool value;

	protected override void OnEnable()
	{
		base.OnEnable();
		isOn = PlayerPrefFlags.Check(flag);
		UpdateColor();
	}

	public override void ButtonActivation()
	{
		switch (mode)
		{
		case ButtonMode.SET_VALUE:
			PlayerPrefFlags.Set(flag, value);
			isOn = value;
			UpdateColor();
			break;
		case ButtonMode.TOGGLE:
			isOn = PlayerPrefFlags.Flip(flag);
			UpdateColor();
			break;
		}
	}
}
