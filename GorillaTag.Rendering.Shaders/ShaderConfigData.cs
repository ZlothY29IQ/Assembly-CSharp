using System;
using UnityEngine;

namespace GorillaTag.Rendering.Shaders;

public class ShaderConfigData
{
	[Serializable]
	public struct ShaderConfig(string shadName, Material fMat, string[] intNames, int[] intVals, string[] floatNames, float[] floatVals, string[] matrixNames, Matrix4x4[] matrixVals, string[] vectorNames, Vector4[] vectorVals, string[] textureNames, Texture[] textureVals)
	{
		public string shaderName = shadName;

		public Material firstMat = fMat;

		public MatPropInt[] ints = convertInts(intNames, intVals);

		public MatPropFloat[] floats = convertFloats(floatNames, floatVals);

		public MatPropMatrix[] matrices = convertMatrices(matrixNames, matrixVals);

		public MatPropVector[] vectors = convertVectors(vectorNames, vectorVals);

		public MatPropTexture[] textures = convertTextures(textureNames, textureVals);
	}

	[Serializable]
	public struct MatPropInt
	{
		public string intName;

		public int intVal;
	}

	[Serializable]
	public struct MatPropFloat
	{
		public string floatName;

		public float floatVal;
	}

	[Serializable]
	public struct MatPropMatrix
	{
		public string matrixName;

		public Matrix4x4 matrixVal;
	}

	[Serializable]
	public struct MatPropVector
	{
		public string vectorName;

		public Vector4 vectorVal;
	}

	[Serializable]
	public struct MatPropTexture
	{
		public string textureName;

		public Texture textureVal;
	}

	[Serializable]
	public struct RenderersForShaderWithSameProperties
	{
		public MeshRenderer[] renderers;
	}

	public static MatPropInt[] convertInts(string[] names, int[] vals)
	{
		MatPropInt[] array = new MatPropInt[names.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = new MatPropInt
			{
				intName = names[i],
				intVal = vals[i]
			};
		}
		return array;
	}

	public static MatPropFloat[] convertFloats(string[] names, float[] vals)
	{
		MatPropFloat[] array = new MatPropFloat[names.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = new MatPropFloat
			{
				floatName = names[i],
				floatVal = vals[i]
			};
		}
		return array;
	}

	public static MatPropMatrix[] convertMatrices(string[] names, Matrix4x4[] vals)
	{
		MatPropMatrix[] array = new MatPropMatrix[names.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = new MatPropMatrix
			{
				matrixName = names[i],
				matrixVal = vals[i]
			};
		}
		return array;
	}

	public static MatPropVector[] convertVectors(string[] names, Vector4[] vals)
	{
		MatPropVector[] array = new MatPropVector[names.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = new MatPropVector
			{
				vectorName = names[i],
				vectorVal = vals[i]
			};
		}
		return array;
	}

	public static MatPropTexture[] convertTextures(string[] names, Texture[] vals)
	{
		MatPropTexture[] array = new MatPropTexture[names.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = new MatPropTexture
			{
				textureName = names[i],
				textureVal = vals[i]
			};
		}
		return array;
	}

	public static string GetShaderPropertiesStringFromMaterial(Material mat, bool excludeMainTexData)
	{
		string text = "";
		string[] propertyNames = mat.GetPropertyNames(MaterialPropertyType.Int);
		int[] array = new int[propertyNames.Length];
		for (int i = 0; i < propertyNames.Length; i++)
		{
			array[i] = mat.GetInteger(propertyNames[i]);
			text += array[i];
		}
		propertyNames = mat.GetPropertyNames(MaterialPropertyType.Float);
		float[] array2 = new float[propertyNames.Length];
		for (int j = 0; j < propertyNames.Length; j++)
		{
			if (excludeMainTexData || !propertyNames[j].Contains("_BaseMap"))
			{
				array2[j] = mat.GetFloat(propertyNames[j]);
				text += array2[j];
			}
		}
		propertyNames = mat.GetPropertyNames(MaterialPropertyType.Matrix);
		Matrix4x4[] array3 = new Matrix4x4[propertyNames.Length];
		for (int k = 0; k < propertyNames.Length; k++)
		{
			array3[k] = mat.GetMatrix(propertyNames[k]);
			text += array3[k].ToString();
		}
		propertyNames = mat.GetPropertyNames(MaterialPropertyType.Vector);
		Vector4[] array4 = new Vector4[propertyNames.Length];
		for (int l = 0; l < propertyNames.Length; l++)
		{
			if (excludeMainTexData || !propertyNames[l].Contains("_BaseMap"))
			{
				array4[l] = mat.GetVector(propertyNames[l]);
				text += array4[l].ToString();
			}
		}
		propertyNames = mat.GetPropertyNames(MaterialPropertyType.Texture);
		Texture[] array5 = new Texture[propertyNames.Length];
		for (int m = 0; m < propertyNames.Length; m++)
		{
			if (!propertyNames[m].Contains("_BaseMap"))
			{
				array5[m] = mat.GetTexture(propertyNames[m]);
				if (array5[m] != null)
				{
					text += array5[m].ToString();
				}
			}
		}
		return text;
	}

	public static ShaderConfig GetConfigDataFromMaterial(Material mat, bool includeMainTexData)
	{
		string[] propertyNames = mat.GetPropertyNames(MaterialPropertyType.Int);
		string[] array = propertyNames;
		int[] array2 = new int[array.Length];
		bool flag = mat.IsKeywordEnabled("_WATER_EFFECT");
		bool flag2 = mat.IsKeywordEnabled("_MAINTEX_ROTATE");
		bool num = mat.IsKeywordEnabled("_UV_WAVE_WARP");
		bool flag3 = mat.IsKeywordEnabled("_EMISSION_USE_UV_WAVE_WARP");
		bool flag4 = num || flag3;
		bool flag5 = mat.IsKeywordEnabled("_LIQUID_CONTAINER");
		bool flag6 = mat.IsKeywordEnabled("_LIQUID_VOLUME") && !flag5;
		bool flag7 = mat.IsKeywordEnabled("_CRYSTAL_EFFECT");
		bool flag8 = mat.IsKeywordEnabled("_EMISSION") || flag7;
		bool flag9 = mat.IsKeywordEnabled("_REFLECTIONS");
		mat.IsKeywordEnabled("_REFLECTIONS_MATCAP");
		bool flag10 = mat.IsKeywordEnabled("_UV_SHIFT");
		for (int i = 0; i < array.Length; i++)
		{
			array2[i] = mat.GetInteger(propertyNames[i]);
			if (!flag10 && (propertyNames[i] == "_UvShiftSteps" || propertyNames[i] == "_UvShiftOffset"))
			{
				array2[i] = 0;
			}
		}
		propertyNames = mat.GetPropertyNames(MaterialPropertyType.Float);
		string[] array3 = propertyNames;
		float[] array4 = new float[array3.Length];
		for (int j = 0; j < propertyNames.Length; j++)
		{
			if (includeMainTexData || !propertyNames[j].Contains("_BaseMap"))
			{
				array4[j] = mat.GetFloat(propertyNames[j]);
			}
			if ((!flag && propertyNames[j] == "_HeightBasedWaterEffect") || (!flag2 && propertyNames[j] == "_RotateSpeed") || (!flag4 && (propertyNames[j] == "_WaveAmplitude" || propertyNames[j] == "_WaveFrequency" || propertyNames[j] == "_WaveScale")) || (!flag6 && (propertyNames[j] == "_LiquidFill" || propertyNames[j] == "_LiquidSwayX" || propertyNames[j] == "_LiquidSwayY")) || (!flag7 && propertyNames[j] == "_CrystalPower") || (!flag8 && propertyNames[j].StartsWith("_Emission")) || (!flag9 && (propertyNames[j] == "_ReflectOpacity" || propertyNames[j] == "_ReflectExposure" || propertyNames[j] == "_ReflectRotate")) || (!flag10 && propertyNames[j] == "_UvShiftRate"))
			{
				array4[j] = 0f;
			}
		}
		propertyNames = mat.GetPropertyNames(MaterialPropertyType.Matrix);
		string[] array5 = propertyNames;
		Matrix4x4[] array6 = new Matrix4x4[array5.Length];
		for (int k = 0; k < propertyNames.Length; k++)
		{
			array6[k] = mat.GetMatrix(propertyNames[k]);
		}
		propertyNames = mat.GetPropertyNames(MaterialPropertyType.Vector);
		string[] array7 = propertyNames;
		Vector4[] array8 = new Vector4[array7.Length];
		for (int l = 0; l < propertyNames.Length; l++)
		{
			if (includeMainTexData || !propertyNames[l].Contains("_BaseMap"))
			{
				array8[l] = mat.GetVector(propertyNames[l]);
			}
			if ((!flag6 && (propertyNames[l] == "_LiquidFillNormal" || propertyNames[l] == "_LiquidSurfaceColor")) || (!flag5 && (propertyNames[l] == "_LiquidPlanePosition" || propertyNames[l] == "_LiquidPlaneNormal")) || (!flag7 && propertyNames[l] == "_CrystalRimColor") || (!flag8 && propertyNames[l].StartsWith("_Emission")) || (!flag9 && (propertyNames[l] == "_ReflectTint" || propertyNames[l] == "_ReflectOffset" || propertyNames[l] == "_ReflectScale")))
			{
				array8[l] = Vector4.zero;
			}
		}
		propertyNames = mat.GetPropertyNames(MaterialPropertyType.Texture);
		string[] array9 = propertyNames;
		Texture[] array10 = new Texture[array9.Length];
		for (int m = 0; m < propertyNames.Length; m++)
		{
			if (!propertyNames[m].Contains("_BaseMap"))
			{
				array10[m] = mat.GetTexture(propertyNames[m]);
			}
		}
		return new ShaderConfig(mat.shader.name, mat, array, array2, array3, array4, array5, array6, array7, array8, array9, array10);
	}
}
