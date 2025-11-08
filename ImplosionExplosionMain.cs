using BoingKit;
using UnityEngine;

public class ImplosionExplosionMain : MonoBehaviour
{
	public BoingReactorField ReactorField;

	public GameObject Diamond;

	public int NumDiamonds;

	private static readonly int kNumInstancedBushesPerDrawCall = 1000;

	private Matrix4x4[][] m_aaInstancedDiamondMatrix;

	private MaterialPropertyBlock m_diamondMaterialProps;

	public void Start()
	{
		m_aaInstancedDiamondMatrix = new Matrix4x4[(NumDiamonds + kNumInstancedBushesPerDrawCall - 1) / kNumInstancedBushesPerDrawCall][];
		for (int i = 0; i < m_aaInstancedDiamondMatrix.Length; i++)
		{
			m_aaInstancedDiamondMatrix[i] = new Matrix4x4[kNumInstancedBushesPerDrawCall];
		}
		for (int j = 0; j < NumDiamonds; j++)
		{
			float num = Random.Range(0.1f, 0.4f);
			Vector3 pos = new Vector3(Random.Range(-3.5f, 3.5f), Random.Range(0.5f, 7f), Random.Range(-3.5f, 3.5f));
			Quaternion q = Quaternion.Euler(Random.Range(0f, 360f), Random.Range(0f, 360f), Random.Range(0f, 360f));
			m_aaInstancedDiamondMatrix[j / kNumInstancedBushesPerDrawCall][j % kNumInstancedBushesPerDrawCall].SetTRS(pos, q, num * Vector3.one);
		}
	}

	public void Update()
	{
		Mesh sharedMesh = Diamond.GetComponent<MeshFilter>().sharedMesh;
		Material sharedMaterial = Diamond.GetComponent<MeshRenderer>().sharedMaterial;
		if (m_diamondMaterialProps == null)
		{
			m_diamondMaterialProps = new MaterialPropertyBlock();
		}
		if (ReactorField.UpdateShaderConstants(m_diamondMaterialProps))
		{
			Matrix4x4[][] aaInstancedDiamondMatrix = m_aaInstancedDiamondMatrix;
			foreach (Matrix4x4[] array in aaInstancedDiamondMatrix)
			{
				Graphics.DrawMeshInstanced(sharedMesh, 0, sharedMaterial, array, array.Length, m_diamondMaterialProps);
			}
		}
	}
}
