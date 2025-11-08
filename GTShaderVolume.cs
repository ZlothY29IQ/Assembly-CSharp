using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class GTShaderVolume : MonoBehaviour
{
	public const int MAX_VOLUMES = 16;

	private static Matrix4x4[] ShaderData = new Matrix4x4[16];

	[Space]
	private static List<GTShaderVolume> gVolumes = new List<GTShaderVolume>(16);

	private static ShaderHashId _GT_ShaderVolumes = "_GT_ShaderVolumes";

	private static ShaderHashId _GT_ShaderVolumesActive = "_GT_ShaderVolumesActive";

	private void OnEnable()
	{
		if (gVolumes.Count <= 16 && !gVolumes.Contains(this))
		{
			gVolumes.Add(this);
		}
	}

	private void OnDisable()
	{
		gVolumes.Remove(this);
	}

	public static void SyncVolumeData()
	{
		m4x4 m4x5 = default(m4x4);
		int count = gVolumes.Count;
		for (int i = 0; i < 16; i++)
		{
			if (i >= count)
			{
				MatrixUtils.Clear(ref ShaderData[i]);
				continue;
			}
			GTShaderVolume gTShaderVolume = gVolumes[i];
			if (!gTShaderVolume)
			{
				MatrixUtils.Clear(ref ShaderData[i]);
				continue;
			}
			Transform obj = gTShaderVolume.transform;
			Vector4 v = obj.position;
			Vector4 v2 = obj.rotation.ToVector();
			Vector4 v3 = obj.localScale;
			m4x5.SetRow0(ref v);
			m4x5.SetRow1(ref v2);
			m4x5.SetRow2(ref v3);
			m4x5.Push(ref ShaderData[i]);
		}
		Shader.SetGlobalInteger(_GT_ShaderVolumesActive, count);
		Shader.SetGlobalMatrixArray(_GT_ShaderVolumes, ShaderData);
	}
}
