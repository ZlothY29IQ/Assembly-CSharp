using UnityEngine;

namespace Docking;

public class Dockable : MonoBehaviour
{
	protected Transform potentialDock;

	protected virtual void OnTriggerEnter(Collider other)
	{
		if (other.TryGetComponent<Dock>(out var _))
		{
			potentialDock = other.transform;
		}
	}

	protected virtual void OnTriggerExit(Collider other)
	{
		if (potentialDock == other.transform)
		{
			potentialDock = null;
		}
	}

	public virtual void Dock()
	{
		if (!(potentialDock == null))
		{
			base.transform.position = potentialDock.position;
			base.transform.rotation = potentialDock.rotation;
			potentialDock = null;
		}
	}
}
