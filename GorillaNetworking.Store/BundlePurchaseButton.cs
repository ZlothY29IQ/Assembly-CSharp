using System.Collections;
using Cosmetics;
using UnityEngine;

namespace GorillaNetworking.Store;

public class BundlePurchaseButton : GorillaPressableButton, IGorillaSliceableSimple
{
	private const string MONKE_BLOCKS_BUNDLE_ALREADY_OWN_KEY = "MONKE_BLOCKS_BUNDLE_ALREADY_OWN";

	private const string MONKE_BLOCKS_BUNDLE_UNAVAILABLE_KEY = "MONKE_BLOCKS_BUNDLE_UNAVAILABLE";

	private const string MONKE_BLOCKS_BUNDLE_ERROR_KEY = "MONKE_BLOCKS_BUNDLE_ERROR";

	public bool bError;

	public string ErrorText = "ERROR COMPLETING PURCHASE! PLEASE RESTART THE GAME";

	public string AlreadyOwnText = "YOU OWN THE BUNDLE ALREADY! THANK YOU!";

	public string UnavailableText = "UNAVAILABLE";

	public string playfabID = "";

	public ICreatorCodeProvider codeProvider;

	public new void OnEnable()
	{
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	public new void OnDisable()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	public void SliceUpdate()
	{
		if (NetworkSystem.Instance != null && NetworkSystem.Instance.WrongVersion && !bError)
		{
			base.enabled = false;
			GetComponent<BoxCollider>().enabled = false;
			buttonRenderer.material = pressedMaterial;
			myText.text = UnavailableText;
		}
	}

	public override void ButtonActivation()
	{
		if (!bError)
		{
			base.ButtonActivation();
			BundleManager.instance.BundlePurchaseButtonPressed(playfabID, codeProvider);
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
			onText = AlreadyOwnText;
			myText.text = AlreadyOwnText;
			isOn = true;
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
			isOn = false;
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
		offText = ErrorText;
		onText = ErrorText;
		isOn = false;
	}

	public void InitializeData()
	{
		if (!bError)
		{
			SetOffText(setMyText: true);
			buttonRenderer.material = unpressedMaterial;
			base.enabled = true;
			isOn = false;
		}
	}

	public void UpdatePurchaseButtonText(string purchaseText)
	{
		if (!bError)
		{
			offText = purchaseText;
			UpdateColor();
		}
	}
}
