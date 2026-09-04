using GorillaLocomotion;
using UnityEngine;

public class HoverboardAreaTrigger : MonoBehaviour
{
	public void OnTriggerEnter(Collider other)
	{
		if (other == GTPlayer.Instance.headCollider)
		{
			GTPlayer.Instance.AddHoverArea(this);
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (other != GTPlayer.Instance.headCollider)
		{
			GTPlayer.Instance.RemoveHoverArea(this);
		}
	}

	private void OnDisable()
	{
		if (!ApplicationQuittingState.IsQuitting)
		{
			GTPlayer.Instance.RemoveHoverArea(this);
		}
	}
}
