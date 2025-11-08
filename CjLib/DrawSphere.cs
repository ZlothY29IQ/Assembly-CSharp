using UnityEngine;

namespace CjLib;

[ExecuteInEditMode]
public class DrawSphere : DrawBase
{
	public float Radius = 1f;

	public int LatSegments = 12;

	public int LongSegments = 12;

	private void OnValidate()
	{
		Radius = Mathf.Max(0f, Radius);
		LatSegments = Mathf.Max(0, LatSegments);
	}

	protected override void Draw(Color color, DebugUtil.Style style, bool depthTest)
	{
		DebugUtil.DrawSphere(base.transform.position, base.transform.rotation, Radius * base.transform.lossyScale.x, LatSegments, LongSegments, color, depthTest, style);
	}
}
