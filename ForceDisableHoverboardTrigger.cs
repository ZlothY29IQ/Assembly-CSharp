using GorillaLocomotion;
using GorillaNetworking;
using UnityEngine;

public class ForceDisableHoverboardTrigger : MonoBehaviour
{
	public bool reEnableOnlyInVStump = true;

	public void OnTriggerEnter(Collider other)
	{
		if (other == GTPlayer.Instance.headCollider)
		{
			GTPlayer.Instance.AddHoverDisabler(this);
		}
	}

	public void OnTriggerExit(Collider other)
	{
		if (other == GTPlayer.Instance.headCollider)
		{
			if (reEnableOnlyInVStump && !GorillaComputer.instance.IsPlayerInVirtualStump())
			{
				GTPlayer.Instance.ForceHoverDisallowed();
			}
			else
			{
				GTPlayer.Instance.RemoveHoverDisabler(this);
			}
		}
	}

	private void OnDisable()
	{
		if (!ApplicationQuittingState.IsQuitting)
		{
			GTPlayer instance = GTPlayer.Instance;
			if (instance != null)
			{
				instance.RemoveHoverDisabler(this);
			}
		}
	}
}
