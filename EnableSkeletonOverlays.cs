using UnityEngine;

public class EnableSkeletonOverlays : MonoBehaviour
{
	[SerializeField]
	private Material bodyMaterial;

	[SerializeField]
	private Material skeletonMaterial;

	private ShaderHashId _BlackAndWhite = "_GreyZoneActive";

	private void OnEnable()
	{
		Shader.SetGlobalFloat(_BlackAndWhite, 1f);
		GorillaBodyRenderer.EnableSkeletonOverlays(bodyMaterial, skeletonMaterial);
	}

	private void OnDisable()
	{
		Shader.SetGlobalFloat(_BlackAndWhite, 0f);
		GorillaBodyRenderer.DisableSkeletonOverlays();
	}
}
