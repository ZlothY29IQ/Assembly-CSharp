using System;
using UnityEngine;
using UnityEngine.Rendering;

public class GTMeshData
{
	public Mesh mesh;

	public Vector3[] vertices;

	public Vector3[] normals;

	public Vector4[] tangents;

	public Color32[] colors32;

	public int[] triangles;

	public BoneWeight[] boneWeights;

	public Vector2[] uv;

	public Vector2[] uv2;

	public Vector2[] uv3;

	public Vector2[] uv4;

	public Vector2[] uv5;

	public Vector2[] uv6;

	public Vector2[] uv7;

	public Vector2[] uv8;

	public int subMeshCount;

	public GTMeshData(Mesh m)
	{
		mesh = m;
		subMeshCount = m.subMeshCount;
		vertices = m.vertices;
		triangles = m.triangles;
		normals = m.normals;
		tangents = m.tangents;
		colors32 = m.colors32;
		boneWeights = m.boneWeights;
		uv = m.uv;
		uv2 = m.uv2;
		uv3 = m.uv3;
		uv4 = m.uv4;
		uv5 = m.uv5;
		uv6 = m.uv6;
		uv7 = m.uv7;
		uv8 = m.uv8;
	}

	public Mesh ExtractSubmesh(int subMeshIndex, bool optimize = false)
	{
		if (subMeshIndex < 0 || subMeshIndex >= subMeshCount)
		{
			throw new IndexOutOfRangeException("subMeshIndex");
		}
		SubMeshDescriptor subMesh = this.mesh.GetSubMesh(subMeshIndex);
		int firstVertex = subMesh.firstVertex;
		int vertexCount = subMesh.vertexCount;
		MeshTopology topology = subMesh.topology;
		int[] indices = this.mesh.GetIndices(subMeshIndex, applyBaseVertex: false);
		for (int i = 0; i < indices.Length; i++)
		{
			indices[i] -= firstVertex;
		}
		Mesh mesh = new Mesh();
		mesh.indexFormat = ((vertexCount > 65535) ? IndexFormat.UInt32 : IndexFormat.UInt16);
		mesh.SetVertices(vertices, firstVertex, vertexCount);
		mesh.SetIndices(indices, topology, 0);
		mesh.SetNormals(normals, firstVertex, vertexCount);
		mesh.SetTangents(tangents, firstVertex, vertexCount);
		if (!uv.IsNullOrEmpty())
		{
			mesh.SetUVs(0, uv, firstVertex, vertexCount);
		}
		if (!uv2.IsNullOrEmpty())
		{
			mesh.SetUVs(1, uv2, firstVertex, vertexCount);
		}
		if (!uv3.IsNullOrEmpty())
		{
			mesh.SetUVs(2, uv3, firstVertex, vertexCount);
		}
		if (!uv4.IsNullOrEmpty())
		{
			mesh.SetUVs(3, uv4, firstVertex, vertexCount);
		}
		if (!uv5.IsNullOrEmpty())
		{
			mesh.SetUVs(4, uv5, firstVertex, vertexCount);
		}
		if (!uv6.IsNullOrEmpty())
		{
			mesh.SetUVs(5, uv6, firstVertex, vertexCount);
		}
		if (!uv7.IsNullOrEmpty())
		{
			mesh.SetUVs(6, uv7, firstVertex, vertexCount);
		}
		if (!uv8.IsNullOrEmpty())
		{
			mesh.SetUVs(7, uv8, firstVertex, vertexCount);
		}
		if (optimize)
		{
			mesh.Optimize();
			mesh.OptimizeIndexBuffers();
		}
		mesh.RecalculateBounds();
		return mesh;
	}

	public static GTMeshData Parse(Mesh mesh)
	{
		if (mesh == null)
		{
			throw new ArgumentNullException("mesh");
		}
		return new GTMeshData(mesh);
	}
}
