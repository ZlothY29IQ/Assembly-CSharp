using UnityEngine;
using UnityEngine.Rendering;

public static class MeshUtils
{
	public static Mesh CreateReadableMeshCopy(this Mesh sourceMesh)
	{
		Mesh mesh = new Mesh();
		mesh.indexFormat = sourceMesh.indexFormat;
		GraphicsBuffer vertexBuffer = sourceMesh.GetVertexBuffer(0);
		int num = vertexBuffer.stride * vertexBuffer.count;
		byte[] data = new byte[num];
		vertexBuffer.GetData(data);
		mesh.SetVertexBufferParams(sourceMesh.vertexCount, sourceMesh.GetVertexAttributes());
		mesh.SetVertexBufferData(data, 0, 0, num);
		vertexBuffer.Release();
		mesh.subMeshCount = sourceMesh.subMeshCount;
		GraphicsBuffer indexBuffer = sourceMesh.GetIndexBuffer();
		int num2 = indexBuffer.stride * indexBuffer.count;
		byte[] data2 = new byte[num2];
		indexBuffer.GetData(data2);
		mesh.SetIndexBufferParams(indexBuffer.count, sourceMesh.indexFormat);
		mesh.SetIndexBufferData(data2, 0, 0, num2);
		indexBuffer.Release();
		uint num3 = 0u;
		for (int i = 0; i < mesh.subMeshCount; i++)
		{
			uint indexCount = sourceMesh.GetIndexCount(i);
			mesh.SetSubMesh(i, new SubMeshDescriptor((int)num3, (int)indexCount));
			num3 += indexCount;
		}
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		return mesh;
	}
}
