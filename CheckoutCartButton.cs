using GorillaExtensions;
using GorillaNetworking;
using UnityEngine;

public class CheckoutCartButton : GorillaPressableButton
{
	public CosmeticsController.CosmeticItem currentCosmeticItem;

	[SerializeField]
	private SpriteRenderer currentCosmeticSprite;

	[SerializeField]
	private Sprite blankSprite;

	public string noCosmeticText;

	public override void Start()
	{
		currentCosmeticItem = CosmeticsController.instance.nullItem;
	}

	public override void UpdateColor()
	{
		if (currentCosmeticItem.itemName == "null")
		{
			if (buttonRenderer.IsNotNull())
			{
				buttonRenderer.material = unpressedMaterial;
			}
			if (myText.IsNotNull())
			{
				myText.text = noCosmeticText;
			}
			if (myTmpText.IsNotNull())
			{
				myTmpText.text = noCosmeticText;
			}
			if (myTmpText2.IsNotNull())
			{
				myTmpText2.text = noCosmeticText;
			}
		}
		else if (isOn)
		{
			if (buttonRenderer.IsNotNull())
			{
				buttonRenderer.material = pressedMaterial;
			}
			SetOnText(myText.IsNotNull(), myTmpText.IsNotNull(), myTmpText2.IsNotNull());
		}
		else
		{
			if (buttonRenderer.IsNotNull())
			{
				buttonRenderer.material = unpressedMaterial;
			}
			SetOffText(myText.IsNotNull(), myTmpText.IsNotNull(), myTmpText2.IsNotNull());
		}
	}

	public override void ButtonActivationWithHand(bool isLeftHand)
	{
		base.ButtonActivation();
		CosmeticsController.instance.PressCheckoutCartButton(this, isLeftHand);
	}

	public void SetItem(CosmeticsController.CosmeticItem item, bool isCurrentItemToBuy)
	{
		currentCosmeticItem = item;
		if (currentCosmeticSprite.IsNotNull())
		{
			currentCosmeticSprite.sprite = currentCosmeticItem.itemPicture;
		}
		isOn = isCurrentItemToBuy;
		UpdateColor();
	}

	public void ClearItem()
	{
		currentCosmeticItem = CosmeticsController.instance.nullItem;
		if (currentCosmeticSprite.IsNotNull())
		{
			currentCosmeticSprite.sprite = blankSprite;
		}
		isOn = false;
		UpdateColor();
	}
}
