using GorillaNetworking;
using UnityEngine;
using UnityEngine.UI;

public class CosmeticStand : GorillaPressableButton
{
	public CosmeticsController.CosmeticItem thisCosmeticItem;

	public string thisCosmeticName;

	public HeadModel thisHeadModel;

	public Text slotPriceText;

	public Text addToCartText;

	[Tooltip("If this is true then this cosmetic stand should have already been updated when the 'Update Cosmetic Stands' button was pressed in the CosmeticsController inspector.")]
	public bool skipMe;

	public void InitializeCosmetic()
	{
		thisCosmeticItem = CosmeticsController.instance.allCosmetics.Find((CosmeticsController.CosmeticItem x) => thisCosmeticName == x.displayName || thisCosmeticName == x.overrideDisplayName || thisCosmeticName == x.itemName);
		if (slotPriceText != null)
		{
			slotPriceText.text = thisCosmeticItem.itemCategory.ToString().ToUpper() + " " + thisCosmeticItem.cost;
		}
	}

	public override void ButtonActivation()
	{
		base.ButtonActivation();
		CosmeticsController.instance.PressCosmeticStandButton(this);
	}
}
