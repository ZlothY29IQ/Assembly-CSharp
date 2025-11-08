using GorillaExtensions;
using UnityEngine;

public class CritterVisuals : MonoBehaviour
{
	public int critterType;

	[Header("Visuals")]
	public Transform bodyRoot;

	public MeshRenderer myRenderer;

	public MeshFilter myMeshFilter;

	public Transform hatRoot;

	public GameObject[] hats;

	private CritterAppearance _appearance;

	public CritterAppearance Appearance => _appearance;

	public void SetAppearance(CritterAppearance appearance)
	{
		_appearance = appearance;
		float num = _appearance.size.ClampSafe(0.25f, 1.5f);
		bodyRoot.localScale = new Vector3(num, num, num);
		if (!string.IsNullOrEmpty(appearance.hatName))
		{
			GameObject[] array = hats;
			foreach (GameObject obj in array)
			{
				obj.SetActive(obj.name == _appearance.hatName);
			}
			hatRoot.gameObject.SetActive(value: true);
		}
		else
		{
			hatRoot.gameObject.SetActive(value: false);
		}
	}

	public void ApplyMesh(Mesh newMesh)
	{
		myMeshFilter.sharedMesh = newMesh;
	}

	public void ApplyMaterial(Material mat)
	{
		myRenderer.sharedMaterial = mat;
	}
}
