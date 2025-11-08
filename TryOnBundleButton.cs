using GorillaNetworking.Store;

public class TryOnBundleButton : GorillaPressableButton
{
	public int buttonIndex;

	public string playfabBundleID = "NULL";

	public override void ButtonActivationWithHand(bool isLeftHand)
	{
		base.ButtonActivationWithHand(isLeftHand);
		BundleManager.instance.PressTryOnBundleButton(this, isLeftHand);
	}

	public override void UpdateColor()
	{
		if (playfabBundleID == "NULL")
		{
			buttonRenderer.material = unpressedMaterial;
			if (myText != null)
			{
				myText.text = "";
			}
		}
		else
		{
			base.UpdateColor();
		}
	}
}
