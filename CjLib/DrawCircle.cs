using UnityEngine;

namespace CjLib;

[ExecuteInEditMode]
public class DrawCircle : DrawBase
{
	public float Radius = 1f;

	public int NumSegments = 64;

	private void OnValidate()
	{
		Radius = Mathf.Max(0f, Radius);
		NumSegments = Mathf.Max(0, NumSegments);
	}

	protected override void Draw(Color color, DebugUtil.Style style, bool depthTest)
	{
		DebugUtil.DrawCircle(base.transform.position, base.transform.rotation * Vector3.back, Radius, NumSegments, color, depthTest, style);
	}
}
