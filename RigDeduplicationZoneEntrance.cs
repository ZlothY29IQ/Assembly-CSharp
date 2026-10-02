using UnityEngine;
using UnityEngine.Events;

public class RigDeduplicationZoneEntrance : MonoBehaviour
{
	[Tooltip("Value to stamp on the local player's PortalShenanigans bit when they enter. Players carrying different values can't see each other inside the crossing.")]
	[SerializeField]
	private bool portalShenanigansBit;

	[Tooltip("Fired whenever the local player enters this entrance.")]
	[SerializeField]
	private UnityEvent OnEnter;

	[Tooltip("Fired when the local player leaves this entrance without having entered the crossing, i.e. when their PortalShenanigans bit is actually reset.")]
	[SerializeField]
	private UnityEvent OnLeavingZone;

	private void OnTriggerEnter(Collider other)
	{
		VRRig component = other.GetComponent<VRRig>();
		if (!(component == null) && component.isLocal)
		{
			component.portalShenanigansBit = portalShenanigansBit;
			OnEnter?.Invoke();
		}
	}

	private void OnTriggerExit(Collider other)
	{
		VRRig component = other.GetComponent<VRRig>();
		if (!(component == null) && component.isLocal && !component.IsInDisplacementZone)
		{
			component.portalShenanigansBit = false;
			OnLeavingZone?.Invoke();
		}
	}
}
