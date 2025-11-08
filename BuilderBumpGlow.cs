using UnityEngine;

public class BuilderBumpGlow : MonoBehaviour
{
	public MeshRenderer glowRenderer;

	private float blendIn;

	private float intensity;

	public void Awake()
	{
		blendIn = 1f;
		intensity = 0f;
		UpdateRender();
	}

	public void SetIntensity(float intensity)
	{
		this.intensity = intensity;
		UpdateRender();
	}

	public void SetBlendIn(float blendIn)
	{
		this.blendIn = blendIn;
		UpdateRender();
	}

	private void UpdateRender()
	{
	}
}
