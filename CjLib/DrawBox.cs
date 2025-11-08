using UnityEngine;

namespace CjLib;

[ExecuteInEditMode]
public class DrawBox : DrawBase
{
	public float Radius = 1f;

	public int NumSegments = 64;

	public float StartAngle;

	public float ArcAngle = 60f;

	private void OnValidate()
	{
		Radius = Mathf.Max(0f, Radius);
		NumSegments = Mathf.Max(0, NumSegments);
	}

	protected override void Draw(Color color, DebugUtil.Style style, bool depthTest)
	{
		Quaternion quaternion = QuaternionUtil.AxisAngle(Vector3.forward, StartAngle * MathUtil.Deg2Rad);
		DebugUtil.DrawArc(base.transform.position, base.transform.rotation * quaternion * Vector3.right, base.transform.rotation * Vector3.forward, ArcAngle * MathUtil.Deg2Rad, Radius, NumSegments, color, depthTest);
	}
}
