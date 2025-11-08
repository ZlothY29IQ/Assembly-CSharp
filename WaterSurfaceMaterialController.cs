using UnityEngine;

[ExecuteAlways]
public class WaterSurfaceMaterialController : MonoBehaviour
{
	public float ScrollX = 0.6f;

	public float ScrollY = 0.6f;

	public float Scale = 1f;

	private Renderer renderer;

	private MaterialPropertyBlock matPropBlock;

	protected void OnEnable()
	{
		renderer = GetComponent<Renderer>();
		matPropBlock = new MaterialPropertyBlock();
		ApplyProperties();
	}

	private void ApplyProperties()
	{
		matPropBlock.SetVector(ShaderProps._ScrollSpeedAndScale, new Vector4(ScrollX, ScrollY, Scale, 0f));
		if ((bool)renderer)
		{
			renderer.SetPropertyBlock(matPropBlock);
		}
	}
}
