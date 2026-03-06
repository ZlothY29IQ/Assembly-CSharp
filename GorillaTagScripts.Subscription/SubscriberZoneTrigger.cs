using GorillaLocomotion;
using UnityEngine;

namespace GorillaTagScripts.Subscription;

public class SubscriberZoneTrigger : MonoBehaviour
{
	public SubscriberExclusiveZone parentZone;

	public bool isRestrictedZone;

	private void OnTriggerEnter(Collider other)
	{
		if (GTPlayer.Instance != null && other == GTPlayer.Instance.bodyCollider && parentZone != null)
		{
			parentZone.OnZoneEnter(isRestrictedZone);
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (GTPlayer.Instance != null && other == GTPlayer.Instance.bodyCollider && parentZone != null)
		{
			parentZone.OnZoneExit(isRestrictedZone);
		}
	}
}
