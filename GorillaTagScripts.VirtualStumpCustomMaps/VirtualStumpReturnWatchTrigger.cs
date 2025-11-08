using GorillaLocomotion;
using GorillaNetworking;
using UnityEngine;

namespace GorillaTagScripts.VirtualStumpCustomMaps;

public class VirtualStumpReturnWatchTrigger : MonoBehaviour
{
	public void OnTriggerEnter(Collider other)
	{
		if (other == GTPlayer.Instance.headCollider)
		{
			VRRig.LocalRig.EnableVStumpReturnWatch(on: false);
		}
	}

	public void OnTriggerExit(Collider other)
	{
		if (other == GTPlayer.Instance.headCollider && GorillaComputer.instance.IsPlayerInVirtualStump())
		{
			VRRig.LocalRig.EnableVStumpReturnWatch(on: true);
		}
	}
}
