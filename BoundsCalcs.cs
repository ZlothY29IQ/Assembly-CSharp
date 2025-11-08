using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BoundsCalcs : MonoBehaviour
{
	public MeshFilter[] optionalTargets = new MeshFilter[0];

	public bool useRootMeshOnly;

	[Space]
	public List<BoundsInfo> elements = new List<BoundsInfo>();

	[Space]
	public BoundsInfo composite;

	[Space]
	private StateHash _state;

	private static MeshFilter[] singleMesh = new MeshFilter[1];

	public void Compute()
	{
		MeshFilter[] array;
		if (!useRootMeshOnly)
		{
			array = ((optionalTargets == null || optionalTargets.Length == 0) ? GetComponentsInChildren<MeshFilter>() : GetComponentsInChildren<MeshFilter>().Concat(optionalTargets).ToArray());
		}
		else
		{
			singleMesh[0] = GetComponent<MeshFilter>();
			array = singleMesh;
		}
		List<Mesh> list = new List<Mesh>((array.Length + 1) / 2);
		List<Vector3> list2 = new List<Vector3>(array.Length * 512);
		elements.Clear();
		for (int i = 0; i < array.Length; i++)
		{
			Matrix4x4 localToWorldMatrix = array[i].transform.localToWorldMatrix;
			Mesh mesh = array[i].sharedMesh;
			if (!mesh.isReadable)
			{
				Mesh mesh2 = mesh.CreateReadableMeshCopy();
				list.Add(mesh2);
				mesh = mesh2;
			}
			Vector3[] vertices = mesh.vertices;
			for (int j = 0; j < vertices.Length; j++)
			{
				vertices[j] = localToWorldMatrix.MultiplyPoint3x4(vertices[j]);
			}
			BoundsInfo item = BoundsInfo.ComputeBounds(vertices);
			elements.Add(item);
			list2.AddRange(vertices);
		}
		composite = BoundsInfo.ComputeBounds(list2.ToArray());
		list.ForEach(Object.DestroyImmediate);
	}
}
