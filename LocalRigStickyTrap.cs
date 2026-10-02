using GorillaLocomotion;
using UnityEngine;

public class LocalRigStickyTrap : MonoBehaviour
{
	[SerializeField]
	private Transform[] anchorPoints;

	public void Capture()
	{
		int actorNumber = VRRig.LocalRig.Creator.ActorNumber;
		GTPlayer instance = GTPlayer.Instance;
		if (!(instance == null))
		{
			instance.disableMovement = true;
			instance.SetGravityOverride(base.gameObject, null);
			Transform transform = anchorPoints[actorNumber % anchorPoints.Length];
			instance.TeleportTo(transform.position, transform.rotation, keepVelocity: false, center: true);
		}
	}

	public void Release()
	{
		GTPlayer instance = GTPlayer.Instance;
		if (!(instance == null))
		{
			instance.disableMovement = false;
			instance.UnsetGravityOverride(base.gameObject);
		}
	}
}
