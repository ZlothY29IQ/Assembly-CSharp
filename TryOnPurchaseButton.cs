using System.Collections;
using GorillaNetworking.Store;
using UnityEngine;

public class TryOnPurchaseButton : GorillaPressableButton
{
	public bool bError;

	public string ErrorText = "ERROR COMPLETING PURCHASE! PLEASE RESTART THE GAME";

	public string AlreadyOwnText;

	public void Update()
	{
		if (NetworkSystem.Instance != null && NetworkSystem.Instance.WrongVersion && !bError)
		{
			base.enabled = false;
			GetComponent<BoxCollider>().enabled = false;
			buttonRenderer.material = pressedMaterial;
			myText.text = "UNAVAILABLE";
		}
	}

	public override void ButtonActivation()
	{
		if (!bError)
		{
			base.ButtonActivation();
			BundleManager.instance.PressPurchaseTryOnBundleButton();
			StartCoroutine(ButtonColorUpdate());
		}
	}

	public void AlreadyOwn()
	{
		if (!bError)
		{
			base.enabled = false;
			GetComponent<BoxCollider>().enabled = false;
			buttonRenderer.material = pressedMaterial;
			myText.text = AlreadyOwnText;
		}
	}

	public void ResetButton()
	{
		if (!bError)
		{
			base.enabled = true;
			GetComponent<BoxCollider>().enabled = true;
			buttonRenderer.material = unpressedMaterial;
			SetOffText(setMyText: true);
		}
	}

	private IEnumerator ButtonColorUpdate()
	{
		buttonRenderer.material = pressedMaterial;
		yield return new WaitForSeconds(debounceTime);
		buttonRenderer.material = (isOn ? pressedMaterial : unpressedMaterial);
	}

	public void ErrorHappened()
	{
		bError = true;
		myText.text = ErrorText;
		buttonRenderer.material = unpressedMaterial;
		base.enabled = false;
		isOn = false;
	}
}
