using UnityEngine;

public class DayNightResetForBaking : MonoBehaviour
{
	public BetterDayNightManager dayNightManager;

	public void SetMaterialsForBaking()
	{
		Material[] dayNightSupportedMaterials = dayNightManager.dayNightSupportedMaterials;
		foreach (Material material in dayNightSupportedMaterials)
		{
			if (material != null)
			{
				material.shader = dayNightManager.standard;
			}
			else
			{
				Debug.LogError("a material is missing from day night supported materials in the daynightmanager! something might have gotten deleted inappropriately, or an entry should be manually removed.", base.gameObject);
			}
		}
		dayNightSupportedMaterials = dayNightManager.dayNightSupportedMaterialsCutout;
		foreach (Material material2 in dayNightSupportedMaterials)
		{
			if (material2 != null)
			{
				material2.shader = dayNightManager.standardCutout;
			}
			else
			{
				Debug.LogError("a material is missing from day night supported materials cutout in the daynightmanager! something might have gotten deleted inappropriately, or an entry should be manually removed.", base.gameObject);
			}
		}
	}

	public void SetMaterialsForGame()
	{
		Material[] dayNightSupportedMaterials = dayNightManager.dayNightSupportedMaterials;
		foreach (Material material in dayNightSupportedMaterials)
		{
			if (material != null)
			{
				material.shader = dayNightManager.gorillaUnlit;
			}
			else
			{
				Debug.LogError("a material is missing from day night supported materials in the daynightmanager! something might have gotten deleted inappropriately, or an entry should be manually removed.", base.gameObject);
			}
		}
		dayNightSupportedMaterials = dayNightManager.dayNightSupportedMaterialsCutout;
		foreach (Material material2 in dayNightSupportedMaterials)
		{
			if (material2 != null)
			{
				material2.shader = dayNightManager.gorillaUnlitCutout;
			}
			else
			{
				Debug.LogError("a material is missing from day night supported materialsc cutout in the daynightmanager! something might have gotten deleted inappropriately, or an entry should be manually removed.", base.gameObject);
			}
		}
	}
}
