using UnityEngine.UI;

public class BuilderKioskButton : GorillaPressableButton
{
	public BuilderSetManager.BuilderSetStoreItem currentPieceSet;

	public BuilderKiosk kiosk;

	public Text setNameText;

	public override void Start()
	{
		currentPieceSet = BuilderKiosk.nullItem;
	}

	public override void UpdateColor()
	{
		if (currentPieceSet.isNullItem)
		{
			buttonRenderer.material = unpressedMaterial;
			myText.text = "";
		}
		else
		{
			base.UpdateColor();
		}
	}

	public override void ButtonActivationWithHand(bool isLeftHand)
	{
		base.ButtonActivation();
	}
}
