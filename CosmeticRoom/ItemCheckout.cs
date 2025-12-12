using System.Collections.Generic;
using GorillaExtensions;
using GorillaNetworking;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CosmeticRoom;

public class ItemCheckout : MonoBehaviour
{
	public CheckoutCartButton[] checkoutCartButtons;

	public PurchaseItemButton leftPurchaseButton;

	public PurchaseItemButton rightPurchaseButton;

	[HideInInspector]
	public Text purchaseText;

	public TMP_Text purchaseTextTMP;

	public HeadModel checkoutHeadModel;

	public Collider checkoutTryOnArea;

	public GameObject checkoutCounterMesh;

	public GameObject purchaseScreenMesh;

	private Scene originalScene;

	private int iterator;

	public bool addOnEnable;

	private void OnEnable()
	{
		if (addOnEnable)
		{
			CosmeticsController.instance.AddItemCheckout(this);
		}
	}

	private void OnDisable()
	{
		if (addOnEnable)
		{
			CosmeticsController.instance.RemoveItemCheckout(this);
		}
	}

	public void InitializeForCustomMap(CompositeTriggerEvents customMapTryOnArea, Scene customMapScene, bool useCustomCounterMesh = true)
	{
		checkoutCounterMesh?.SetActive(!useCustomCounterMesh);
		purchaseScreenMesh?.SetActive(useCustomCounterMesh);
		originalScene = customMapScene;
		customMapTryOnArea.AddCollider(checkoutTryOnArea);
		CosmeticsController.instance.AddItemCheckout(this);
	}

	public void RemoveFromCustomMap(CompositeTriggerEvents customMapTryOnArea)
	{
		if (!customMapTryOnArea.IsNull())
		{
			customMapTryOnArea.RemoveCollider(checkoutTryOnArea);
		}
	}

	public void UpdateFromCart(List<CosmeticsController.CosmeticItem> currentCart, CosmeticsController.CosmeticItem itemToBuy)
	{
		for (iterator = 0; iterator < checkoutCartButtons.Length; iterator++)
		{
			if (iterator < currentCart.Count)
			{
				bool isCurrentItemToBuy = currentCart[iterator].itemName == itemToBuy.itemName;
				checkoutCartButtons[iterator].SetItem(currentCart[iterator], isCurrentItemToBuy);
			}
			else
			{
				checkoutCartButtons[iterator].ClearItem();
			}
		}
	}

	public void UpdatePurchaseText(string newText, string leftPurchaseButtonText, string rightPurchaseButtonText, bool leftButtonOn, bool rightButtonOn)
	{
		if (purchaseText.IsNotNull())
		{
			purchaseText.text = newText;
		}
		if (purchaseTextTMP.IsNotNull())
		{
			purchaseTextTMP.text = newText;
		}
		if (!leftPurchaseButtonText.IsNullOrEmpty())
		{
			leftPurchaseButton.SetText(leftPurchaseButtonText);
			leftPurchaseButton.buttonRenderer.material = (leftButtonOn ? leftPurchaseButton.pressedMaterial : leftPurchaseButton.unpressedMaterial);
		}
		if (!rightPurchaseButtonText.IsNullOrEmpty())
		{
			rightPurchaseButton.SetText(rightPurchaseButtonText);
			rightPurchaseButton.buttonRenderer.material = (rightButtonOn ? rightPurchaseButton.pressedMaterial : rightPurchaseButton.unpressedMaterial);
		}
	}

	public bool IsFromScene(Scene unloadingScene)
	{
		return unloadingScene == originalScene;
	}
}
