using UnityEngine;

namespace GorillaTag.Rendering;

public class FirstPersonMeshCullingDisabler : MonoBehaviour
{
	private Mesh[] meshes;

	private Transform[] xforms;

	protected void Awake()
	{
		MeshFilter[] componentsInChildren = GetComponentsInChildren<MeshFilter>();
		if (componentsInChildren != null)
		{
			meshes = new Mesh[componentsInChildren.Length];
			xforms = new Transform[componentsInChildren.Length];
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				meshes[i] = componentsInChildren[i].mesh;
				xforms[i] = componentsInChildren[i].transform;
			}
		}
	}

	protected void OnEnable()
	{
		Camera main = Camera.main;
		if (!(main == null))
		{
			Transform obj = main.transform;
			Vector3 position = obj.position;
			Vector3 vector = Vector3.Normalize(obj.forward);
			float nearClipPlane = main.nearClipPlane;
			float num = (main.farClipPlane - nearClipPlane) / 2f + nearClipPlane;
			Vector3 position2 = position + vector * num;
			for (int i = 0; i < meshes.Length; i++)
			{
				Vector3 center = xforms[i].InverseTransformPoint(position2);
				meshes[i].bounds = new Bounds(center, Vector3.one);
			}
		}
	}
}
