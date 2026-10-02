using GorillaLocomotion;
using GorillaTagScripts.VirtualStumpCustomMaps;
using UnityEngine;

public class VHallwayExitTrigger : MonoBehaviour
{
	private bool armed = true;

	public void OnTriggerEnter(Collider other)
	{
		if (armed && !(other != GTPlayer.Instance.headCollider))
		{
			armed = false;
			CustomMapManager.ExitVHallway();
		}
	}

	public void OnTriggerExit(Collider other)
	{
		if (other == GTPlayer.Instance.headCollider)
		{
			armed = true;
		}
	}
}
