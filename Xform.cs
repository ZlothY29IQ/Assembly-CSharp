using Drawing;
using Unity.Mathematics;
using UnityEngine;

[ExecuteAlways]
public class Xform : MonoBehaviour
{
	public Transform parent;

	[Space]
	public Color displayColor = SRand.New().NextColor();

	[Space]
	public float3 localPosition = float3.zero;

	public float3 localScale = Vector3.one;

	public Quaternion localRotation = quaternion.identity;

	private static readonly float3 F3_ONE = 1f;

	private static readonly float2 F2_ONE = 1f;

	private static readonly float3 AXIS_ZB_FW = new float3(0f, 0f, 1f);

	private static readonly float3 AXIS_YG_UP = new float3(0f, 1f, 0f);

	private static readonly float3 AXIS_XR_RT = new float3(1f, 0f, 0f);

	private static readonly Color CR = new Color(1f, 0f, 0f, 0.24f);

	private static readonly Color CG = new Color(0f, 1f, 0f, 0.24f);

	private static readonly Color CB = new Color(0f, 0f, 1f, 0.24f);

	public float3 localExtents => localScale * 0.5f;

	public Matrix4x4 LocalTRS()
	{
		return Matrix4x4.TRS(localPosition, localRotation, localScale);
	}

	public Matrix4x4 TRS()
	{
		if (parent.AsNull() == null)
		{
			return LocalTRS();
		}
		return parent.localToWorldMatrix * LocalTRS();
	}

	private void Update()
	{
		Matrix4x4 matrix = TRS();
		CommandBuilder ingame = Draw.ingame;
		using (ingame.WithMatrix(matrix))
		{
			using (ingame.WithLineWidth(2f))
			{
				ingame.PlaneWithNormal(AXIS_XR_RT * 0.5f, AXIS_XR_RT, F2_ONE, CR);
				ingame.PlaneWithNormal(AXIS_YG_UP * 0.5f, AXIS_YG_UP, F2_ONE, CG);
				ingame.PlaneWithNormal(AXIS_ZB_FW * 0.5f, AXIS_ZB_FW, F2_ONE, CB);
				ingame.WireBox(float3.zero, quaternion.identity, 1f, displayColor);
			}
		}
	}
}
