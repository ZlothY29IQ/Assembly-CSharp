using System;
using System.Collections.Generic;
using System.Linq;
using BoingKit;
using UnityEngine;

public class BushFieldReactorFieldMain : MonoBehaviour
{
	public GameObject Bush;

	public GameObject Blossom;

	public GameObject Sphere;

	public BoingReactorField ReactorField;

	public int NumBushes;

	public Vector2 BushScaleRange;

	public int NumBlossoms;

	public Vector2 BlossomScaleRange;

	public Vector2 FieldBounds;

	public int NumSpheresPerCircle;

	public int NumCircles;

	public float MaxCircleRadius;

	public float CircleSpeed;

	private List<BoingEffector> m_aSphere;

	private float m_basePhase;

	private static readonly int kNumInstancedBushesPerDrawCall = 1000;

	private Matrix4x4[][] m_aaInstancedBushMatrix;

	private MaterialPropertyBlock m_bushMaterialProps;

	public void Start()
	{
		UnityEngine.Random.InitState(0);
		if (Bush.GetComponent<BoingReactorFieldGPUSampler>() == null)
		{
			for (int i = 0; i < NumBushes; i++)
			{
				GameObject obj = UnityEngine.Object.Instantiate(Bush);
				float num = UnityEngine.Random.Range(BushScaleRange.x, BushScaleRange.y);
				obj.transform.position = new Vector3(UnityEngine.Random.Range(-0.5f * FieldBounds.x, 0.5f * FieldBounds.x), 0.2f * num, UnityEngine.Random.Range(-0.5f * FieldBounds.y, 0.5f * FieldBounds.y));
				obj.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
				obj.transform.localScale = num * Vector3.one;
				BoingReactorFieldCPUSampler component = obj.GetComponent<BoingReactorFieldCPUSampler>();
				if (component != null)
				{
					component.ReactorField = ReactorField;
				}
				BoingReactorFieldGPUSampler component2 = obj.GetComponent<BoingReactorFieldGPUSampler>();
				if (component2 != null)
				{
					component2.ReactorField = ReactorField;
				}
			}
		}
		else
		{
			m_aaInstancedBushMatrix = new Matrix4x4[(NumBushes + kNumInstancedBushesPerDrawCall - 1) / kNumInstancedBushesPerDrawCall][];
			for (int j = 0; j < m_aaInstancedBushMatrix.Length; j++)
			{
				m_aaInstancedBushMatrix[j] = new Matrix4x4[kNumInstancedBushesPerDrawCall];
			}
			for (int k = 0; k < NumBushes; k++)
			{
				float num2 = UnityEngine.Random.Range(BushScaleRange.x, BushScaleRange.y);
				Vector3 pos = new Vector3(UnityEngine.Random.Range(-0.5f * FieldBounds.x, 0.5f * FieldBounds.x), 0.2f * num2, UnityEngine.Random.Range(-0.5f * FieldBounds.y, 0.5f * FieldBounds.y));
				Quaternion q = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
				m_aaInstancedBushMatrix[k / kNumInstancedBushesPerDrawCall][k % kNumInstancedBushesPerDrawCall].SetTRS(pos, q, num2 * Vector3.one);
			}
		}
		for (int l = 0; l < NumBlossoms; l++)
		{
			GameObject obj2 = UnityEngine.Object.Instantiate(Blossom);
			float num3 = UnityEngine.Random.Range(BlossomScaleRange.x, BlossomScaleRange.y);
			obj2.transform.position = new Vector3(UnityEngine.Random.Range(-0.5f * FieldBounds.x, 0.5f * FieldBounds.y), 0.2f * num3, UnityEngine.Random.Range(-0.5f * FieldBounds.y, 0.5f * FieldBounds.y));
			obj2.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
			obj2.transform.localScale = num3 * Vector3.one;
			obj2.GetComponent<BoingReactorFieldCPUSampler>().ReactorField = ReactorField;
		}
		m_aSphere = new List<BoingEffector>(NumSpheresPerCircle * NumCircles);
		for (int m = 0; m < NumCircles; m++)
		{
			for (int n = 0; n < NumSpheresPerCircle; n++)
			{
				GameObject gameObject = UnityEngine.Object.Instantiate(Sphere);
				m_aSphere.Add(gameObject.GetComponent<BoingEffector>());
			}
		}
		BoingReactorField component3 = ReactorField.GetComponent<BoingReactorField>();
		component3.Effectors = ((component3.Effectors != null) ? component3.Effectors.Concat(m_aSphere.ToArray()).ToArray() : m_aSphere.ToArray());
		m_basePhase = 0f;
	}

	public void Update()
	{
		int num = 0;
		for (int i = 0; i < NumCircles; i++)
		{
			float num2 = MaxCircleRadius / (float)(i + 1);
			for (int j = 0; j < NumSpheresPerCircle; j++)
			{
				float num3 = m_basePhase + (float)j / (float)NumSpheresPerCircle * 2f * MathF.PI;
				num3 *= ((i % 2 == 0) ? 1f : (-1f));
				m_aSphere[num].transform.position = new Vector3(num2 * Mathf.Cos(num3), 0.2f, num2 * Mathf.Sin(num3));
				num++;
			}
		}
		m_basePhase -= CircleSpeed / MaxCircleRadius * Time.deltaTime;
		if (m_aaInstancedBushMatrix == null)
		{
			return;
		}
		Mesh sharedMesh = Bush.GetComponent<MeshFilter>().sharedMesh;
		Material sharedMaterial = Bush.GetComponent<MeshRenderer>().sharedMaterial;
		if (m_bushMaterialProps == null)
		{
			m_bushMaterialProps = new MaterialPropertyBlock();
		}
		if (ReactorField.UpdateShaderConstants(m_bushMaterialProps))
		{
			Matrix4x4[][] aaInstancedBushMatrix = m_aaInstancedBushMatrix;
			foreach (Matrix4x4[] array in aaInstancedBushMatrix)
			{
				Graphics.DrawMeshInstanced(sharedMesh, 0, sharedMaterial, array, array.Length, m_bushMaterialProps);
			}
		}
	}
}
