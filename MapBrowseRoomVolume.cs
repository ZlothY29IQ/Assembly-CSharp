using GorillaLocomotion;
using UnityEngine;

public class MapBrowseRoomVolume : MonoBehaviour
{
	public void OnTriggerExit(Collider other)
	{
		if (!(other != GTPlayer.Instance.headCollider))
		{
			CustomMapsTerminal.ReleaseLocalTerminalControl();
		}
	}
}
