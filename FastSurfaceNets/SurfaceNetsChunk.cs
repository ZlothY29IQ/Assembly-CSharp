using Cysharp.Threading.Tasks;
using GorillaExtensions;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using Voxels;

namespace FastSurfaceNets;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SurfaceNetsChunk : MonoBehaviour
{
	public int3 Id;

	public GenerationParameters parameters;

	public const int ChunkSize = 32;

	public bool autoGenerate = true;

	private const int Pad = 1;

	private int3 chunkPosition;

	private NativeArray<byte> sdf;

	private int3 min;

	private int3 max;

	private int3 shape;

	private Mesh mesh;

	private void Awake()
	{
		if (autoGenerate)
		{
			BuildChunk();
		}
	}

	private void OnDestroy()
	{
		if (sdf.IsCreated)
		{
			sdf.Dispose();
			sdf = default(NativeArray<byte>);
		}
	}

	public async void BuildChunk()
	{
		chunkPosition = Id * 32;
		shape = new int3(34);
		int num = shape.x * shape.y * shape.z;
		if (sdf.IsCreated)
		{
			sdf.Dispose();
			sdf = default(NativeArray<byte>);
		}
		sdf = new NativeArray<byte>(num, Allocator.Persistent);
		FillChunk();
		Voxels.SurfaceNetsBuffer buffer = new Voxels.SurfaceNetsBuffer(32768, 65536, num);
		min = new int3(0);
		max = shape - 1;
		JobHandle handle = Voxels.SurfaceNets.Generate(sdf, shape, min, max, buffer);
		while (!handle.IsCompleted)
		{
			await UniTask.Yield();
		}
		handle.Complete();
		Debug.Log($"{base.name} generated {buffer.Vertices.Length} vertices, {buffer.Normals.Length} normals, {buffer.Triangles.Length / 3}({buffer.Triangles.Length}) triangles.", this);
		if (buffer.Triangles.Length != 0)
		{
			if (parameters.customNormals)
			{
				MeshUtilities.MeshData meshData = MeshUtilities.SplitByAngle(buffer.Vertices.AsArray(), buffer.Triangles.AsArray(), parameters.normalThreshold, parameters.areaWeightedNormals);
				ref NativeList<float3> vertices = ref buffer.Vertices;
				ref NativeList<float3> vertices2 = ref meshData.Vertices;
				NativeList<float3> vertices3 = meshData.Vertices;
				NativeList<float3> vertices4 = buffer.Vertices;
				vertices = vertices3;
				vertices2 = vertices4;
				vertices = ref buffer.Normals;
				ref NativeList<float3> normals = ref meshData.Normals;
				vertices4 = meshData.Normals;
				vertices3 = buffer.Normals;
				vertices = vertices4;
				normals = vertices3;
				ref NativeList<int> triangles = ref buffer.Triangles;
				ref NativeList<int> triangles2 = ref meshData.Triangles;
				NativeList<int> triangles3 = meshData.Triangles;
				NativeList<int> triangles4 = buffer.Triangles;
				triangles = triangles3;
				triangles2 = triangles4;
				meshData.Dispose();
			}
			mesh = new Mesh
			{
				indexFormat = ((buffer.Vertices.Length > 65535) ? IndexFormat.UInt32 : IndexFormat.UInt16)
			};
			mesh.SetVertices(buffer.Vertices.AsArray());
			mesh.SetTriangles(buffer.Triangles.AsArray().ToArray(), 0, calculateBounds: false);
			if (parameters.recalculateNormals)
			{
				mesh.RecalculateNormals();
			}
			else
			{
				mesh.SetNormals(buffer.Normals.AsArray());
			}
			mesh.RecalculateBounds();
			buffer.Dispose();
			GetComponent<MeshFilter>().sharedMesh = mesh;
			base.gameObject.GetOrAddComponent<MeshCollider>().sharedMesh = mesh;
		}
	}

	private void FillChunk()
	{
		if (parameters.generateShape)
		{
			int x = shape.x;
			int num = shape.x * shape.y;
			int3 int5 = parameters.shapeMin - chunkPosition + min;
			int3 int6 = parameters.shapeMax - chunkPosition + min;
			for (int i = 0; i < shape.z; i++)
			{
				for (int j = 0; j < shape.y; j++)
				{
					int num2 = i * num + j * x;
					for (int k = 0; k < shape.x; k++)
					{
						float value;
						if (parameters.generateShape)
						{
							value = ((k >= int5.x && k <= int6.x && j >= int5.y && j <= int6.y && i >= int5.z && i <= int6.z) ? 1f : (-1f));
						}
						else
						{
							float3 float5 = (chunkPosition + new int3(k, j, i)).ToFloat3();
							value = noise.snoise(float5 * parameters.noiseScale) - float5.y / parameters.heightScale;
						}
						sdf[num2 + k] = value.ToByte();
					}
				}
			}
		}
		else
		{
			IJobParallelForExtensions.Schedule(new FillChunkJob
			{
				sdf = sdf,
				shape = shape,
				chunkPosition = chunkPosition,
				shapeMin = parameters.shapeMin,
				shapeMax = parameters.shapeMax,
				noiseScale = parameters.noiseScale,
				heightScale = parameters.heightScale,
				min = min,
				max = max,
				strideY = shape.x,
				strideZ = shape.x * shape.y
			}, shape.x * shape.y * shape.z, 64).Complete();
		}
	}

	private void OnDrawGizmosSelected()
	{
		if ((bool)mesh && mesh.vertexCount >= 3)
		{
			Gizmos.color = Color.green;
			int vertexCount = mesh.vertexCount;
			Vector3[] vertices = mesh.vertices;
			Vector3[] normals = mesh.normals;
			for (int i = 0; i < vertexCount; i++)
			{
				Gizmos.DrawLine(base.transform.position + vertices[i], base.transform.position + vertices[i] + normals[i] * 0.25f);
			}
		}
	}
}
