using UnityEngine;

public class CopyMaterialScript : MonoBehaviour
{
	public SkinnedMeshRenderer sourceToCopyMaterialFrom;

	public SkinnedMeshRenderer mySkinnedMeshRenderer;

	private void Start()
	{
		if (Application.platform == RuntimePlatform.Android)
		{
			base.gameObject.SetActive(value: false);
		}
	}

	private void Update()
	{
		if (sourceToCopyMaterialFrom.material != mySkinnedMeshRenderer.material)
		{
			mySkinnedMeshRenderer.material = sourceToCopyMaterialFrom.material;
		}
	}
}
