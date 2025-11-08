using Liv.Lck.GorillaTag;
using UnityEngine;

namespace Docking;

public class LivCameraDockable : Dockable
{
	private LivCameraDock livDock;

	protected override void OnTriggerEnter(Collider other)
	{
		if (other.TryGetComponent<LivCameraDock>(out var component))
		{
			livDock = component;
			potentialDock = other.transform;
		}
	}

	protected override void OnTriggerExit(Collider other)
	{
		if (livDock != null && other.transform == potentialDock.transform)
		{
			potentialDock = null;
			livDock = null;
		}
	}

	public override void Dock()
	{
		base.Dock();
		if (!(livDock == null))
		{
			GTLckController gTLckController = GetComponent<GTLckController>() ?? GetComponentInParent<GTLckController>();
			if (gTLckController != null)
			{
				gTLckController.ApplyCameraSettings(livDock.cameraSettings);
			}
			livDock = null;
		}
	}
}
