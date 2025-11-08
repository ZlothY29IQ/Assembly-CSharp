using UnityEngine;

namespace CjLib;

[ExecuteInEditMode]
public class DrawLine : DrawBase
{
	public Vector3 LocalEndVector = Vector3.right;

	private void OnValidate()
	{
		Wireframe = true;
		Style = DebugUtil.Style.Wireframe;
	}

	protected override void Draw(Color color, DebugUtil.Style style, bool depthTest)
	{
		DebugUtil.DrawLine(base.transform.position, base.transform.position + base.transform.TransformVector(LocalEndVector), color, depthTest);
	}
}
