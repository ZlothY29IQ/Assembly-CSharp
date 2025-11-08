using BoingKit;
using UnityEngine;

public class LiquidMain : MonoBehaviour
{
	public Material PlaneMaterial;

	public BoingReactorField ReactorField;

	public GameObject Effector;

	private static readonly float kPlaneMeshCellSize = 0.25f;

	private static readonly int kNumInstancedPlaneCellPerDrawCall = 1000;

	private static readonly int kNumMovingEffectors = 5;

	private static readonly float kMovingEffectorPhaseSpeed = 0.5f;

	private static int kNumPlaneCells;

	private static readonly int kPlaneMeshResolution = 64;

	private Mesh m_planeMesh;

	private Matrix4x4[][] m_aaInstancedPlaneCellMatrix;

	private GameObject[] m_aMovingEffector;

	private float[] m_aMovingEffectorPhase;

	private void ResetEffector(GameObject obj)
	{
		obj.transform.position = new Vector3(Random.Range(-0.3f, 0.3f), -100f, Random.Range(-0.3f, 0.3f)) * kPlaneMeshCellSize * kPlaneMeshResolution;
	}

	public void Start()
	{
		m_planeMesh = new Mesh();
		m_planeMesh.vertices = new Vector3[4]
		{
			new Vector3(-0.5f, 0f, -0.5f) * kPlaneMeshCellSize,
			new Vector3(-0.5f, 0f, 0.5f) * kPlaneMeshCellSize,
			new Vector3(0.5f, 0f, 0.5f) * kPlaneMeshCellSize,
			new Vector3(0.5f, 0f, -0.5f) * kPlaneMeshCellSize
		};
		m_planeMesh.normals = new Vector3[4]
		{
			new Vector3(0f, 1f, 0f),
			new Vector3(0f, 1f, 0f),
			new Vector3(0f, 1f, 0f),
			new Vector3(0f, 1f, 0f)
		};
		m_planeMesh.SetIndices(new int[6] { 0, 1, 2, 0, 2, 3 }, MeshTopology.Triangles, 0);
		kNumPlaneCells = kPlaneMeshResolution * kPlaneMeshResolution;
		m_aaInstancedPlaneCellMatrix = new Matrix4x4[(kNumPlaneCells + kNumInstancedPlaneCellPerDrawCall - 1) / kNumInstancedPlaneCellPerDrawCall][];
		for (int i = 0; i < m_aaInstancedPlaneCellMatrix.Length; i++)
		{
			m_aaInstancedPlaneCellMatrix[i] = new Matrix4x4[kNumInstancedPlaneCellPerDrawCall];
		}
		Vector3 vector = new Vector3(-0.5f, 0f, -0.5f) * kPlaneMeshCellSize * kPlaneMeshResolution;
		for (int j = 0; j < kPlaneMeshResolution; j++)
		{
			for (int k = 0; k < kPlaneMeshResolution; k++)
			{
				int num = j * kPlaneMeshResolution + k;
				Matrix4x4 matrix4x = Matrix4x4.TRS(new Vector3(k, 0f, j) * kPlaneMeshCellSize + vector, Quaternion.identity, Vector3.one);
				m_aaInstancedPlaneCellMatrix[num / kNumInstancedPlaneCellPerDrawCall][num % kNumInstancedPlaneCellPerDrawCall] = matrix4x;
			}
		}
		m_aMovingEffector = new GameObject[kNumMovingEffectors];
		m_aMovingEffectorPhase = new float[kNumMovingEffectors];
		BoingEffector[] array = new BoingEffector[kNumMovingEffectors];
		for (int l = 0; l < kNumMovingEffectors; l++)
		{
			GameObject gameObject = Object.Instantiate(Effector);
			m_aMovingEffector[l] = gameObject;
			ResetEffector(gameObject);
			m_aMovingEffectorPhase[l] = 0f - MathUtil.HalfPi + (float)l / (float)kNumMovingEffectors * MathUtil.Pi;
			array[l] = gameObject.GetComponent<BoingEffector>();
		}
		ReactorField.Effectors = array;
	}

	public void Update()
	{
		ReactorField.UpdateShaderConstants(PlaneMaterial);
		int num = kNumPlaneCells;
		for (int i = 0; i < m_aaInstancedPlaneCellMatrix.Length; i++)
		{
			Matrix4x4[] matrices = m_aaInstancedPlaneCellMatrix[i];
			Graphics.DrawMeshInstanced(m_planeMesh, 0, PlaneMaterial, matrices, Mathf.Min(num, kNumInstancedPlaneCellPerDrawCall));
			num -= kNumInstancedPlaneCellPerDrawCall;
		}
		for (int j = 0; j < kNumMovingEffectors; j++)
		{
			GameObject gameObject = m_aMovingEffector[j];
			float num2 = m_aMovingEffectorPhase[j];
			num2 += MathUtil.TwoPi * kMovingEffectorPhaseSpeed * Time.deltaTime;
			float num3 = num2;
			num2 = Mathf.Repeat(num2 + MathUtil.HalfPi, MathUtil.Pi) - MathUtil.HalfPi;
			m_aMovingEffectorPhase[j] = num2;
			if (num2 < num3 - 0.01f)
			{
				ResetEffector(gameObject);
			}
			Vector3 position = gameObject.transform.position;
			position.y = Mathf.Tan(Mathf.Clamp(num2, 0f - MathUtil.HalfPi + 0.2f, MathUtil.HalfPi - 0.2f)) + 3.5f;
			gameObject.transform.position = position;
		}
	}
}
