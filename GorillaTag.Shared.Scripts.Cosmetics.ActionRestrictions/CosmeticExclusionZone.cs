using UnityEngine;

namespace GorillaTag.Shared.Scripts.Cosmetics.ActionRestrictions;

[RequireComponent(typeof(Collider))]
public class CosmeticExclusionZone : MonoBehaviour
{
	private Collider zoneCollider;

	private void Awake()
	{
		zoneCollider = GetComponent<Collider>();
		zoneCollider.isTrigger = true;
		CosmeticExclusionZoneRegistryUtility.RegisterZone(zoneCollider);
	}

	private void OnDestroy()
	{
		CosmeticExclusionZoneRegistryUtility.UnregisterZone(zoneCollider);
	}

	private void OnTriggerEnter(Collider other)
	{
		VRRig componentInParent = other.GetComponentInParent<VRRig>();
		if (componentInParent != null)
		{
			CosmeticExclusionZoneRegistry.Enter(componentInParent);
		}
	}

	private void OnTriggerExit(Collider other)
	{
		VRRig componentInParent = other.GetComponentInParent<VRRig>();
		if (componentInParent != null)
		{
			CosmeticExclusionZoneRegistry.Exit(componentInParent);
		}
	}
}
