using UnityEngine;

public class TapInnerGlow : MonoBehaviour
{
	public Renderer _renderer;

	public float tapLength = 1f;

	[Space]
	private Material _instance;

	private Material targetMaterial
	{
		get
		{
			if (_instance.AsNull() == null)
			{
				return _instance = _renderer.material;
			}
			return _instance;
		}
	}

	public void Tap()
	{
		if ((bool)_renderer)
		{
			Material target = targetMaterial;
			float value = tapLength;
			float time = GTShaderGlobals.Time;
			UberShader.InnerGlowSinePeriod.SetValue(target, value);
			UberShader.InnerGlowSinePhaseShift.SetValue(target, time);
		}
	}
}
