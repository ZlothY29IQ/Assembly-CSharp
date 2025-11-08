using GorillaLocomotion;
using GorillaNetworking;
using UnityEngine;

public class ForceDisableHoverboardTrigger : MonoBehaviour
{
	[Tooltip("If TRUE and the Hoverboard was enabled when the player entered this trigger, it will be re-enabled when they exit.")]
	public bool reEnableOnExit = true;

	public bool reEnableOnlyInVStump = true;

	private bool wasEnabled;

	public void OnTriggerEnter(Collider other)
	{
		if (other == GTPlayer.Instance.headCollider)
		{
			wasEnabled = GTPlayer.Instance.isHoverAllowed;
			GTPlayer.Instance.SetHoverAllowed(allowed: false, force: true);
		}
	}

	public void OnTriggerExit(Collider other)
	{
		if (reEnableOnExit && wasEnabled && (!reEnableOnlyInVStump || GorillaComputer.instance.IsPlayerInVirtualStump()) && other == GTPlayer.Instance.headCollider)
		{
			GTPlayer.Instance.SetHoverAllowed(allowed: true);
		}
	}
}
