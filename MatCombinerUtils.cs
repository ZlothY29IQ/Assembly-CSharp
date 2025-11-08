using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public static class MatCombinerUtils
{
	private const string _k_logPre = "MaterialCombiner: ";

	public static void ApplyExtraFingerprintRules(ref UberShaderMatUsedProps matUsedProps)
	{
		matUsedProps.fingerprint._BaseColor = new int4(100, 100, 100, 100);
		matUsedProps.fingerprint._BaseMap_AtlasSlice = -999;
		if (matUsedProps._EmissionToggle != 0)
		{
			int4 emissionColor = matUsedProps.fingerprint._EmissionColor;
			if ((emissionColor.x != 0 || emissionColor.y != 0 || emissionColor.z != 0) && matUsedProps.fingerprint._EmissionColor.w != 0)
			{
				goto IL_00ab;
			}
		}
		matUsedProps._EmissionToggle = 0;
		matUsedProps._EmissionColor = 0;
		matUsedProps._EmissionUVScrollSpeed = 0;
		matUsedProps.fingerprint._EmissionColor = int4.zero;
		matUsedProps.fingerprint._EmissionUVScrollSpeed = int4.zero;
		matUsedProps.fingerprint._EmissionMap = string.Empty;
		goto IL_00ab;
		IL_00ab:
		MaterialFingerprint fingerprint = matUsedProps.fingerprint;
		if (fingerprint._WaterEffect <= 0 || fingerprint._HeightBasedWaterEffect != 0)
		{
			matUsedProps._WaterEffect = 999;
			matUsedProps.fingerprint._WaterEffect = 999;
			matUsedProps._HeightBasedWaterEffect = 999;
			matUsedProps.fingerprint._HeightBasedWaterEffect = 999;
		}
		if (matUsedProps._UseSpecular == 0 || matUsedProps.fingerprint._Smoothness == 0)
		{
			matUsedProps._UseSpecular = 0;
			matUsedProps.fingerprint._UseSpecular = 0;
			matUsedProps._Smoothness = 0;
			matUsedProps.fingerprint._Smoothness = 0;
		}
	}

	public static Material AverageMaterials(List<Material> oldMats)
	{
		Material material = new Material(oldMats[0]);
		Shader shader = material.shader;
		int propertyCount = shader.GetPropertyCount();
		for (int i = 0; i < propertyCount; i++)
		{
			string propertyName = shader.GetPropertyName(i);
			if (propertyName.EndsWith("_AtlasSlice"))
			{
				continue;
			}
			int propertyNameId = shader.GetPropertyNameId(i);
			if (propertyName == "_HalfLambertToggle")
			{
				material.SetFloat(propertyNameId, 0f);
				material.DisableKeyword("_HALF_LAMBERT_TERM");
				continue;
			}
			if (propertyName == "_UseSpecular")
			{
				material.SetFloat(propertyNameId, 0f);
				material.DisableKeyword("_GT_RIM_LIGHT");
				continue;
			}
			ShaderPropertyType propertyType = shader.GetPropertyType(i);
			switch (propertyType)
			{
			case ShaderPropertyType.Int:
			{
				double num2 = 0.0;
				foreach (Material oldMat in oldMats)
				{
					num2 += (double)oldMat.GetInteger(propertyNameId);
				}
				num2 /= (double)oldMats.Count;
				material.SetInteger(propertyNameId, (int)num2);
				break;
			}
			case ShaderPropertyType.Float:
			case ShaderPropertyType.Range:
			{
				double num = 0.0;
				foreach (Material oldMat2 in oldMats)
				{
					num += (double)oldMat2.GetFloat(propertyNameId);
				}
				num /= (double)oldMats.Count;
				material.SetFloat(propertyNameId, (float)num);
				break;
			}
			case ShaderPropertyType.Color:
			{
				Color black = Color.black;
				foreach (Material oldMat3 in oldMats)
				{
					black += oldMat3.GetColor(propertyNameId);
				}
				black /= (float)oldMats.Count;
				material.SetColor(propertyNameId, black);
				break;
			}
			case ShaderPropertyType.Vector:
			{
				Vector4 zero = Vector4.zero;
				foreach (Material oldMat4 in oldMats)
				{
					zero += oldMat4.GetVector(propertyNameId);
				}
				zero /= (float)oldMats.Count;
				material.SetVector(propertyNameId, zero);
				break;
			}
			default:
				Debug.LogError("ERROR!!! MaterialCombiner: Unknown property type: " + propertyType);
				break;
			case ShaderPropertyType.Texture:
				break;
			}
		}
		material.SetColor(ShaderProps._BaseColor, Color.white);
		return material;
	}
}
