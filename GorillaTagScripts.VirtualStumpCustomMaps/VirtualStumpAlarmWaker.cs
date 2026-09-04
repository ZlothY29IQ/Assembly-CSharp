using UnityEngine;

namespace GorillaTagScripts.VirtualStumpCustomMaps;

public class VirtualStumpAlarmWaker : MonoBehaviour
{
	public void EnterStumpCustomMode()
	{
		CustomMapManager.Activate(VirtualStumpActivateMode.Custom, hasEntryTeleportNode: false);
	}
}
