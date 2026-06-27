using Unity.Mathematics;
using UnityEngine.Rendering;

namespace Voxels;

public struct MeshVertexData(float3 position, float3 normal, float4 tangent, float4 materials, float4 blend)
{
	public float3 position = position;

	public float3 normal = normal;

	public float4 tangent = tangent;

	public float4 materials = materials;

	public float4 blend = blend;

	public static readonly VertexAttributeDescriptor[] VertexBufferMemoryLayout = new VertexAttributeDescriptor[5]
	{
		new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0),
		new VertexAttributeDescriptor(VertexAttribute.Normal),
		new VertexAttributeDescriptor(VertexAttribute.Tangent, VertexAttributeFormat.Float32, 4),
		new VertexAttributeDescriptor(VertexAttribute.TexCoord1, VertexAttributeFormat.Float32, 4),
		new VertexAttributeDescriptor(VertexAttribute.TexCoord2, VertexAttributeFormat.Float32, 4)
	};

	public override string ToString()
	{
		return $"({position:F2} x {normal:F2})";
	}
}
