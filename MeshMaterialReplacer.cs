using GameObjectScheduling;
using UnityEngine;

public class MeshMaterialReplacer : MonoBehaviour
{
	[SerializeField]
	private MeshMaterialReplacement meshMaterialReplacement;

	private void Start()
	{
		SkinnedMeshRenderer component2;
		if (TryGetComponent<MeshRenderer>(out var component))
		{
			GetComponent<MeshFilter>().mesh = meshMaterialReplacement.mesh;
			component.materials = meshMaterialReplacement.materials;
		}
		else if (TryGetComponent<SkinnedMeshRenderer>(out component2))
		{
			component2.sharedMesh = meshMaterialReplacement.mesh;
			component2.materials = meshMaterialReplacement.materials;
		}
	}
}
