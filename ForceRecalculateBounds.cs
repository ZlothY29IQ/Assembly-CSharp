using UnityEngine;

public class ForceRecalculateBounds : MonoBehaviourTick
{
	private SkinnedMeshRenderer skinnedMesh;

	private Transform mainCamera;

	private Vector3 bounds;

	private void Awake()
	{
		skinnedMesh = GetComponentInChildren<SkinnedMeshRenderer>();
		bounds = Vector3.one * 1000f;
		mainCamera = Camera.main.transform;
	}

	public override void Tick()
	{
		if (!(skinnedMesh == null))
		{
			if (mainCamera == null)
			{
				mainCamera = Camera.main.transform;
			}
			else
			{
				skinnedMesh.bounds = new Bounds(mainCamera.position, bounds);
			}
		}
	}
}
