using UnityEngine;

public class GorillaCameraClipPlaneOverrideTrigger : GorillaTriggerBox
{
	private Camera mainCamera;

	public float clipPlaneFarDistanceOverride;

	private void Awake()
	{
		mainCamera = Camera.main;
	}

	public override void OnBoxTriggered()
	{
		mainCamera.farClipPlane = clipPlaneFarDistanceOverride;
	}
}
