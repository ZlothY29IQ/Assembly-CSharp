using System.Collections;
using GorillaNetworking.Store;
using UnityEngine;

public class PurchaseCurrencyButton : GorillaPressableButton
{
	public string purchaseCurrencySize;

	public float buttonFadeTime = 0.25f;

	public override void ButtonActivation()
	{
		base.ButtonActivation();
		ATM_Manager.instance.PressCurrencyPurchaseButton(GetComponentInParent<ATM_UI>(), purchaseCurrencySize);
		StartCoroutine(ButtonColorUpdate());
	}

	private IEnumerator ButtonColorUpdate()
	{
		buttonRenderer.sharedMaterial = pressedMaterial;
		yield return new WaitForSeconds(buttonFadeTime);
		buttonRenderer.sharedMaterial = unpressedMaterial;
	}
}
