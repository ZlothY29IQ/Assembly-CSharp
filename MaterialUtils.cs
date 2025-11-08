using UnityEngine;

public static class MaterialUtils
{
	public static string GetTrimmedMaterialName(Material material)
	{
		return material.name.Replace(" (Instance)", "").Trim();
	}

	public static void SwapMaterial(MeshAndMaterials meshAndMaterial, bool isOnToOff)
	{
		Material[] sharedMaterials = meshAndMaterial.meshRenderer.sharedMaterials;
		for (int i = 0; i < sharedMaterials.Length; i++)
		{
			string trimmedMaterialName = GetTrimmedMaterialName(sharedMaterials[i]);
			string text = ((!isOnToOff) ? ((meshAndMaterial.offMaterial != null) ? GetTrimmedMaterialName(meshAndMaterial.offMaterial) : null) : ((meshAndMaterial.onMaterial != null) ? GetTrimmedMaterialName(meshAndMaterial.onMaterial) : null));
			if (text != null && trimmedMaterialName == text)
			{
				sharedMaterials[i] = (isOnToOff ? meshAndMaterial.offMaterial : meshAndMaterial.onMaterial);
			}
		}
		meshAndMaterial.meshRenderer.sharedMaterials = sharedMaterials;
	}
}
