using GorillaLocomotion;
using GorillaTagScripts.VirtualStumpCustomMaps;
using UnityEngine;

public class VStumpDeactivateTrigger : MonoBehaviour
{
	private bool armed = true;

	public void OnTriggerEnter(Collider other)
	{
		if (armed && !(other != GTPlayer.Instance.headCollider))
		{
			armed = false;
			CustomMapManager.Deactivate();
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
