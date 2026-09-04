using System.Collections;
using UnityEngine;

public class GeodeATMPurchaseButton : GorillaPressableButton
{
	public float buttonFadeTime = 0.25f;

	public override void ButtonActivation()
	{
		base.ButtonActivation();
		StartCoroutine(ButtonColorUpdate());
	}

	private IEnumerator ButtonColorUpdate()
	{
		buttonRenderer.sharedMaterial = pressedMaterial;
		yield return new WaitForSeconds(buttonFadeTime);
		buttonRenderer.sharedMaterial = unpressedMaterial;
	}
}
