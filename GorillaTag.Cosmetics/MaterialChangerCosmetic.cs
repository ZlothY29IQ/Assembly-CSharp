using UnityEngine;

namespace GorillaTag.Cosmetics;

public class MaterialChangerCosmetic : MonoBehaviour
{
	[SerializeField]
	private SkinnedMeshRenderer targetRenderer;

	[SerializeField]
	private int materialIndex;

	public void ChangeMaterial(Material newMaterial)
	{
		if (!(targetRenderer == null) && !(newMaterial == null) && materialIndex >= 0)
		{
			Material[] materials = targetRenderer.materials;
			if (materialIndex >= materials.Length)
			{
				Debug.LogWarning($"Material index {materialIndex} is out of range.");
				return;
			}
			materials[materialIndex] = newMaterial;
			targetRenderer.materials = materials;
		}
	}

	public void ChangeAllMaterials(Material newMat)
	{
		if (!(targetRenderer == null) && !(newMat == null))
		{
			Material[] array = new Material[targetRenderer.materials.Length];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = newMat;
			}
			targetRenderer.materials = array;
		}
	}
}
