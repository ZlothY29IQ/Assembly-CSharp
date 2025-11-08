using System;

namespace GorillaTagScripts;

public class BuilderOptionButton : GorillaPressableButton
{
	private new Action<BuilderOptionButton, bool> onPressed;

	public override void Start()
	{
		base.Start();
	}

	private void OnDestroy()
	{
	}

	public void Setup(Action<BuilderOptionButton, bool> onPressed)
	{
		this.onPressed = onPressed;
	}

	public override void ButtonActivationWithHand(bool isLeftHand)
	{
		onPressed?.Invoke(this, isLeftHand);
	}

	public void SetPressed(bool pressed)
	{
		buttonRenderer.material = (pressed ? pressedMaterial : unpressedMaterial);
	}
}
