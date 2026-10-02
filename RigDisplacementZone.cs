using UnityEngine;

public abstract class RigDisplacementZone : MonoBehaviour
{
	protected bool localPlayerInZone;

	protected virtual void OnTriggerEnter(Collider other)
	{
		VRRig component = other.GetComponent<VRRig>();
		if (!(component == null))
		{
			component.SetDisplacementZone(this);
			if (component.isLocal)
			{
				localPlayerInZone = true;
			}
		}
	}

	protected virtual void OnTriggerExit(Collider other)
	{
		VRRig component = other.GetComponent<VRRig>();
		if (!(component == null))
		{
			component.ClearDisplacementZone(this);
			if (component.isLocal)
			{
				localPlayerInZone = false;
			}
		}
	}

	protected virtual void OnDisable()
	{
		if (localPlayerInZone)
		{
			localPlayerInZone = false;
			if (VRRig.LocalRig != null)
			{
				VRRig.LocalRig.ClearDisplacementZone(this);
			}
		}
	}

	public abstract Vector3 GetDisplacementForRig(VRRig rig, Vector3 undisplacedPosition);

	public abstract bool IsDisplacingRig(VRRig rig);
}
