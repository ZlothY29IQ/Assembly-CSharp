using System.Collections;
using GorillaTagScripts.VirtualStumpCustomMaps;
using UnityEngine;

public class CustomMapsLoadRoomMapButton : GorillaPressableButton
{
	[SerializeField]
	private float pressedTime = 0.2f;

	public override void ButtonActivation()
	{
		base.ButtonActivation();
		StartCoroutine(ButtonPressed_Local());
		if (CustomMapManager.CanLoadRoomMap())
		{
			CustomMapManager.ApproveAndLoadRoomMap();
		}
	}

	private IEnumerator ButtonPressed_Local()
	{
		isOn = true;
		UpdateColor();
		yield return new WaitForSeconds(pressedTime);
		isOn = false;
		UpdateColor();
	}
}
