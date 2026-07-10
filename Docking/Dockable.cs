using UnityEngine;

namespace Docking;

public class Dockable : MonoBehaviour
{
	protected Dock currentDock;

	protected Dock potentialDock;

	private float undockTime;

	protected bool rotate = true;

	protected virtual void OnTriggerEnter(Collider other)
	{
		potentialDock = other.GetComponent<Dock>();
	}

	protected virtual void OnTriggerExit(Collider other)
	{
		if (potentialDock != null && potentialDock.transform == other.transform)
		{
			potentialDock = null;
		}
	}

	public virtual void Dock()
	{
		if (potentialDock == null)
		{
			return;
		}
		base.transform.position = potentialDock.transform.position;
		base.transform.rotation = potentialDock.transform.rotation;
		potentialDock.NotifyDocked();
		if (potentialDock.Moveable)
		{
			currentDock = potentialDock;
			if (currentDock.ForceUndockTime > 0f)
			{
				undockTime = Time.time + currentDock.ForceUndockTime;
			}
		}
		potentialDock = null;
	}

	public virtual void UnDock()
	{
		currentDock.NotifyUnDocked();
		currentDock = null;
		potentialDock = null;
		undockTime = 0f;
	}

	private void LateUpdate()
	{
		if (currentDock != null)
		{
			base.transform.position = currentDock.transform.position;
			if (rotate)
			{
				base.transform.rotation = currentDock.transform.rotation;
			}
			if (undockTime > 0f && undockTime < Time.time)
			{
				UnDock();
			}
		}
	}
}
