using System.Collections;
using UnityEngine;

namespace GameObjectScheduling.DeepLinks;

public class DeepLinkButton : GorillaPressableButton
{
	[SerializeField]
	private ulong deepLinkAppID;

	[SerializeField]
	private string deepLinkPayload = "";

	[SerializeField]
	private float pressedTime = 0.2f;

	private bool sendingDeepLink;

	public override void ButtonActivation()
	{
		base.ButtonActivation();
		sendingDeepLink = DeepLinkSender.SendDeepLink(deepLinkAppID, deepLinkPayload, OnDeepLinkSent);
		StartCoroutine(ButtonPressed_Local());
	}

	private void OnDeepLinkSent(string message)
	{
		sendingDeepLink = false;
		if (!isOn)
		{
			UpdateColor();
		}
	}

	private IEnumerator ButtonPressed_Local()
	{
		isOn = true;
		UpdateColor();
		yield return new WaitForSeconds(pressedTime);
		isOn = false;
		if (!sendingDeepLink)
		{
			UpdateColor();
		}
	}
}
