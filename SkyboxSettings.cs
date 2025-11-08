using UnityEngine;

[ExecuteInEditMode]
public class SkyboxSettings : MonoBehaviour
{
	[SerializeField]
	private Material _skyMaterial;

	private void OnEnable()
	{
		if ((bool)_skyMaterial)
		{
			RenderSettings.skybox = _skyMaterial;
		}
	}
}
