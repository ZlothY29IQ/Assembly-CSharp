using System.Collections.Generic;
using GorillaTag.Rendering;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class MaterialUVOffsetListSetter : MonoBehaviour, IBuildValidation
{
	[SerializeField]
	private List<Vector2> uvOffsetList = new List<Vector2>();

	private MeshRenderer meshRenderer;

	private MaterialPropertyBlock matPropertyBlock;

	private void Awake()
	{
		matPropertyBlock = new MaterialPropertyBlock();
		meshRenderer = GetComponent<MeshRenderer>();
		meshRenderer.GetPropertyBlock(matPropertyBlock);
	}

	public void SetUVOffset(int listIndex)
	{
		if (listIndex >= uvOffsetList.Count || listIndex < 0)
		{
			Debug.LogError("Invalid uv offset list index provided.");
		}
		else if (matPropertyBlock == null || meshRenderer == null)
		{
			Debug.LogError("MaterialUVOffsetListSetter settings are incorrect somehow, please fix", base.gameObject);
			Awake();
		}
		else
		{
			Vector2 vector = uvOffsetList[listIndex];
			matPropertyBlock.SetVector(ShaderProps._BaseMap_ST, new Vector4(1f, 1f, vector.x, vector.y));
			meshRenderer.SetPropertyBlock(matPropertyBlock);
		}
	}

	public bool BuildValidationCheck()
	{
		if (GetComponent<MeshRenderer>() == null)
		{
			Debug.LogError("missing a mesh renderer for the materialuvoffsetlistsetter", base.gameObject);
			return false;
		}
		if (GetComponentInParent<EdMeshCombinerMono>() != null && GetComponentInParent<EdDoNotMeshCombine>() == null)
		{
			Debug.LogError("the meshrenderer is going to getcombined, that will likely cause issues for the materialuvoffsetlistsetter", base.gameObject);
			return false;
		}
		return true;
	}
}
