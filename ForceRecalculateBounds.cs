using UnityEngine;

public class ForceRecalculateBounds : MonoBehaviourTick
{
	private SkinnedMeshRenderer skinnedMesh;

	private void Awake()
	{
		skinnedMesh = GetComponent<SkinnedMeshRenderer>();
	}

	public override void Tick()
	{
		if (!(skinnedMesh == null))
		{
			skinnedMesh.bounds = new Bounds(base.transform.position, Vector3.one * 1000f);
		}
	}
}
