using System;

namespace GorillaTagScripts;

public class BuilderOptionButton : GorillaPressableButton
{
	private Action<BuilderOptionButton, bool> onOptionPressed;

	public void Setup(Action<BuilderOptionButton, bool> onPressed)
	{
		onOptionPressed = onPressed;
	}

	public override void ButtonActivationWithHand(bool isLeftHand)
	{
		onOptionPressed?.Invoke(this, isLeftHand);
	}

	public void SetPressed(bool pressed)
	{
		buttonRenderer.material = (pressed ? pressedMaterial : unpressedMaterial);
	}
}
