using System.Collections;
using GorillaNetworking;
using UnityEngine;

public class WardrobeFunctionButton : GorillaPressableButton
{
	public string function;

	public float buttonFadeTime = 0.25f;

	public override void ButtonActivation()
	{
		base.ButtonActivation();
		CosmeticsController.instance.PressWardrobeFunctionButton(function);
		StartCoroutine(ButtonColorUpdate());
	}

	public override void UpdateColor()
	{
	}

	private IEnumerator ButtonColorUpdate()
	{
		buttonRenderer.material = pressedMaterial;
		yield return new WaitForSeconds(buttonFadeTime);
		buttonRenderer.material = unpressedMaterial;
	}
}
