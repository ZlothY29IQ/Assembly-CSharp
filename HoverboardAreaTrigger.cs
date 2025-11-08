using GorillaLocomotion;
using UnityEngine;

public class HoverboardAreaTrigger : MonoBehaviour
{
	public void OnTriggerEnter(Collider other)
	{
		if (other == GTPlayer.Instance.headCollider)
		{
			GTPlayer.Instance.SetHoverAllowed(allowed: true);
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (other == GTPlayer.Instance.headCollider)
		{
			GTPlayer.Instance.SetHoverAllowed(allowed: false);
		}
	}
}
