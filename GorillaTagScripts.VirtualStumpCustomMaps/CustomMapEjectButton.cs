using System.Collections;
using GT_CustomMapSupportRuntime;
using UnityEngine;

namespace GorillaTagScripts.VirtualStumpCustomMaps;

public class CustomMapEjectButton : GorillaPressableButton
{
	public enum EjectType
	{
		EjectFromVirtualStump,
		ReturnToVirtualStump
	}

	[SerializeField]
	private EjectType ejectType;

	private bool processing;

	public override void ButtonActivation()
	{
		base.ButtonActivation();
		StartCoroutine(ButtonPressed_Local());
		if (!processing)
		{
			HandleTeleport();
		}
	}

	private IEnumerator ButtonPressed_Local()
	{
		isOn = true;
		UpdateColor();
		yield return new WaitForSeconds(debounceTime);
		isOn = false;
		UpdateColor();
	}

	private void HandleTeleport()
	{
		if (!processing)
		{
			processing = true;
			switch (ejectType)
			{
			case EjectType.ReturnToVirtualStump:
				CustomMapManager.ReturnToVirtualStump();
				processing = false;
				break;
			case EjectType.EjectFromVirtualStump:
				CustomMapManager.ExitVirtualStump(FinishTeleport);
				break;
			}
		}
	}

	private void FinishTeleport(bool success = true)
	{
		if (processing)
		{
			processing = false;
		}
	}

	public void CopySettings(CustomMapEjectButtonSettings customMapEjectButtonSettings)
	{
		ejectType = (EjectType)customMapEjectButtonSettings.ejectType;
	}
}
