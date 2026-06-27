using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Voxels;

public static class MeshUtilities
{
	public struct MeshData : IDisposable
	{
		public NativeList<float3> Vertices;

		public NativeList<int> Triangles;

		public NativeList<float3> Normals;

		public void Dispose()
		{
			Vertices.Dispose();
			Triangles.Dispose();
			Normals.Dispose();
		}
	}

	public struct VoxelMeshData : IDisposable
	{
		public NativeList<float3> Vertices;

		public NativeList<byte> Materials;

		public NativeList<int> Triangles;

		public NativeList<float3> Normals;

		public void Dispose()
		{
			Vertices.Dispose();
			Materials.Dispose();
			Triangles.Dispose();
			Normals.Dispose();
		}
	}

	[BurstCompile]
	public struct FaceNormalJob : IJobParallelFor
	{
		[ReadOnly]
		public NativeArray<float3> Verts;

		[ReadOnly]
		public NativeArray<int> Tris;

		[WriteOnly]
		public NativeArray<float3> FaceN;

		public void Execute(int index)
		{
			int num = index * 3;
			float3 float5 = Verts[Tris[num]];
			float3 float6 = Verts[Tris[num + 1]];
			float3 float7 = Verts[Tris[num + 2]];
			FaceN[index] = math.normalize(math.cross(float6 - float5, float7 - float5));
		}
	}

	[BurstCompile]
	public struct SplitJob : IJob
	{
		private struct Bucket
		{
			public int next;

			public int newIdx;

			public float3 repN;
		}

		public float CosThresh;

		[ReadOnly]
		public NativeArray<float3> SrcVerts;

		[ReadOnly]
		public NativeArray<int> SrcTris;

		[ReadOnly]
		public NativeArray<float3> FaceN;

		public NativeList<float3> DstVerts;

		public NativeList<int> DstTris;

		public void Execute()
		{
			int length = SrcVerts.Length;
			int num = SrcTris.Length / 3;
			NativeArray<int> nativeArray = new NativeArray<int>(length, Allocator.Temp);
			for (int i = 0; i < length; i++)
			{
				nativeArray[i] = -1;
			}
			NativeList<Bucket> nativeList = new NativeList<Bucket>(length, Allocator.Temp);
			DstVerts.Clear();
			DstTris.ResizeUninitialized(SrcTris.Length);
			for (int j = 0; j < num; j++)
			{
				float3 float5 = FaceN[j];
				for (int k = 0; k < 3; k++)
				{
					int index = SrcTris[j * 3 + k];
					int num2 = -1;
					for (int num3 = nativeArray[index]; num3 != -1; num3 = nativeList[num3].next)
					{
						Bucket bucket = nativeList[num3];
						if (math.dot(bucket.repN, float5) >= CosThresh)
						{
							num2 = bucket.newIdx;
							break;
						}
					}
					if (num2 == -1)
					{
						num2 = DstVerts.Length;
						DstVerts.Add(SrcVerts[index]);
						Bucket value = new Bucket
						{
							next = nativeArray[index],
							newIdx = num2,
							repN = float5
						};
						nativeArray[index] = nativeList.Length;
						nativeList.Add(in value);
					}
					DstTris[j * 3 + k] = num2;
				}
			}
			nativeArray.Dispose();
			nativeList.Dispose();
		}
	}

	[BurstCompile]
	public struct SplitVoxelMeshJob : IJob
	{
		private struct Bucket
		{
			public int next;

			public int newIdx;

			public float3 repN;
		}

		public float CosThresh;

		[ReadOnly]
		public NativeArray<float3> SrcVerts;

		[ReadOnly]
		public NativeArray<byte> SrcMats;

		[ReadOnly]
		public NativeArray<int> SrcTris;

		[ReadOnly]
		public NativeArray<float3> FaceN;

		public NativeList<float3> DstVerts;

		public NativeList<byte> DstMats;

		public NativeList<int> DstTris;

		public void Execute()
		{
			int length = SrcVerts.Length;
			int num = SrcTris.Length / 3;
			NativeArray<int> nativeArray = new NativeArray<int>(length, Allocator.Temp);
			for (int i = 0; i < length; i++)
			{
				nativeArray[i] = -1;
			}
			NativeList<Bucket> nativeList = new NativeList<Bucket>(length, Allocator.Temp);
			DstVerts.Clear();
			DstMats.Clear();
			DstTris.ResizeUninitialized(SrcTris.Length);
			for (int j = 0; j < num; j++)
			{
				float3 float5 = FaceN[j];
				for (int k = 0; k < 3; k++)
				{
					int index = SrcTris[j * 3 + k];
					int num2 = -1;
					for (int num3 = nativeArray[index]; num3 != -1; num3 = nativeList[num3].next)
					{
						Bucket bucket = nativeList[num3];
						if (math.dot(bucket.repN, float5) >= CosThresh)
						{
							num2 = bucket.newIdx;
							break;
						}
					}
					if (num2 == -1)
					{
						num2 = DstVerts.Length;
						DstVerts.Add(SrcVerts[index]);
						DstMats.Add(SrcMats[index]);
						Bucket value = new Bucket
						{
							next = nativeArray[index],
							newIdx = num2,
							repN = float5
						};
						nativeArray[index] = nativeList.Length;
						nativeList.Add(in value);
					}
					DstTris[j * 3 + k] = num2;
				}
			}
			nativeArray.Dispose();
			nativeList.Dispose();
		}
	}

	[BurstCompile]
	private struct TriNormalJob : IJobParallelFor
	{
		[ReadOnly]
		public NativeArray<float3> V;

		[ReadOnly]
		public NativeArray<int> T;

		[WriteOnly]
		public NativeArray<float3> Out;

		public int AreaWeight;

		public void Execute(int i)
		{
			int num = i * 3;
			float3 float5 = V[T[num]];
			float3 obj = V[T[num + 1]];
			float3 float6 = math.cross(y: V[T[num + 2]] - float5, x: obj - float5);
			Out[i] = ((AreaWeight == 0) ? math.normalize(float6) : float6);
		}
	}

	[BurstCompile]
	private struct BuildAdjJob : IJobParallelFor
	{
		[ReadOnly]
		public NativeArray<int> T;

		public NativeParallelMultiHashMap<int, int>.ParallelWriter MapW;

		public void Execute(int triIdx)
		{
			int num = triIdx * 3;
			MapW.Add(T[num], triIdx);
			MapW.Add(T[num + 1], triIdx);
			MapW.Add(T[num + 2], triIdx);
		}
	}

	[BurstCompile]
	private struct VertexNormalJob : IJobParallelFor
	{
		public int AreaWeight;

		[ReadOnly]
		public NativeArray<float3> TriN;

		[ReadOnly]
		public NativeParallelMultiHashMap<int, int> V2T;

		[WriteOnly]
		public NativeArray<float3> Out;

		public void Execute(int v)
		{
			NativeParallelMultiHashMap<int, int>.Enumerator valuesForKey = V2T.GetValuesForKey(v);
			if (!valuesForKey.MoveNext())
			{
				Out[v] = float3.zero;
				return;
			}
			int current = valuesForKey.Current;
			float3 float5 = TriN[current];
			float3 float6 = ((AreaWeight == 0) ? float5 : math.normalize(float5));
			float3 zero = float3.zero;
			NativeParallelMultiHashMap<int, int>.Enumerator valuesForKey2 = V2T.GetValuesForKey(v);
			while (valuesForKey2.MoveNext())
			{
				float3 float7 = TriN[valuesForKey2.Current];
				if (AreaWeight != 0)
				{
					math.normalize(float7);
				}
				zero += float7;
			}
			Out[v] = ((math.lengthsq(zero) < 1E-09f) ? float6 : math.normalize(zero));
		}
	}

	public static void SplitByAngle(this Mesh mesh, float angleDeg, bool areaWeight = true, Allocator allocator = Allocator.TempJob)
	{
		NativeArray<float3> srcVerts = new NativeArray<float3>(mesh.vertexCount, allocator);
		List<Vector3> list = new List<Vector3>(mesh.vertexCount);
		mesh.GetVertices(list);
		for (int i = 0; i < list.Count; i++)
		{
			srcVerts[i] = list[i];
		}
		new NativeArray<byte>(mesh.vertexCount, allocator);
		NativeArray<int> srcTris = new NativeArray<int>(mesh.triangles, allocator);
		MeshData meshData = SplitByAngle(srcVerts, srcTris, angleDeg, areaWeight, allocator);
		mesh.Clear();
		mesh.SetVertices(meshData.Vertices.AsArray());
		mesh.SetTriangles(meshData.Triangles.AsArray().ToArray(), 0, calculateBounds: false);
		mesh.SetNormals(meshData.Normals.AsArray());
		mesh.RecalculateBounds();
		srcVerts.Dispose();
		srcTris.Dispose();
		meshData.Dispose();
	}

	public static MeshData SplitByAngle(NativeArray<float3> srcVerts, NativeArray<int> srcTris, float angleDeg, bool areaWeight = true, Allocator allocator = Allocator.TempJob)
	{
		NativeArray<float3> faceN = new NativeArray<float3>(srcTris.Length / 3, allocator);
		IJobParallelForExtensions.Schedule(new FaceNormalJob
		{
			Verts = srcVerts,
			Tris = srcTris,
			FaceN = faceN
		}, faceN.Length, 64).Complete();
		NativeList<float3> nativeList = new NativeList<float3>(srcVerts.Length, allocator);
		NativeList<int> nativeList2 = new NativeList<int>(srcTris.Length, allocator);
		new SplitJob
		{
			CosThresh = math.cos(math.radians(angleDeg)),
			SrcVerts = srcVerts,
			SrcTris = srcTris,
			FaceN = faceN,
			DstVerts = nativeList,
			DstTris = nativeList2
		}.Run();
		NativeList<float3> outNormals = new NativeList<float3>(nativeList.Length, allocator);
		outNormals.ResizeUninitialized(nativeList.Length);
		RecalcNormalsJobified(nativeList, nativeList2, areaWeight, allocator, ref outNormals);
		faceN.Dispose();
		return new MeshData
		{
			Vertices = nativeList,
			Triangles = nativeList2,
			Normals = outNormals
		};
	}

	public static VoxelMeshData SplitByAngle(NativeArray<float3> srcVerts, NativeArray<byte> srcMats, NativeArray<int> srcTris, float angleDeg, bool areaWeight = true, Allocator allocator = Allocator.TempJob)
	{
		NativeArray<float3> faceN = new NativeArray<float3>(srcTris.Length / 3, allocator);
		IJobParallelForExtensions.Schedule(new FaceNormalJob
		{
			Verts = srcVerts,
			Tris = srcTris,
			FaceN = faceN
		}, faceN.Length, 64).Complete();
		NativeList<float3> nativeList = new NativeList<float3>(srcVerts.Length, allocator);
		NativeList<byte> nativeList2 = new NativeList<byte>(srcVerts.Length, allocator);
		NativeList<int> nativeList3 = new NativeList<int>(srcTris.Length, allocator);
		new SplitVoxelMeshJob
		{
			CosThresh = math.cos(math.radians(angleDeg)),
			SrcVerts = srcVerts,
			SrcMats = srcMats,
			SrcTris = srcTris,
			FaceN = faceN,
			DstVerts = nativeList,
			DstMats = nativeList2,
			DstTris = nativeList3
		}.Run();
		NativeList<float3> outNormals = new NativeList<float3>(nativeList.Length, allocator);
		outNormals.ResizeUninitialized(nativeList.Length);
		RecalcNormalsJobified(nativeList, nativeList3, areaWeight, allocator, ref outNormals);
		faceN.Dispose();
		return new VoxelMeshData
		{
			Vertices = nativeList,
			Materials = nativeList2,
			Triangles = nativeList3,
			Normals = outNormals
		};
	}

	private static void RecalcNormalsJobified(NativeList<float3> verts, NativeList<int> tris, bool areaWeight, Allocator alloc, ref NativeList<float3> outNormals)
	{
		int length = verts.Length;
		int num = tris.Length / 3;
		NativeArray<float3> nativeArray = new NativeArray<float3>(num, alloc);
		IJobParallelForExtensions.Schedule(new TriNormalJob
		{
			V = verts.AsArray(),
			T = tris.AsArray(),
			Out = nativeArray,
			AreaWeight = (areaWeight ? 1 : 0)
		}, num, 64).Complete();
		NativeParallelMultiHashMap<int, int> v2T = new NativeParallelMultiHashMap<int, int>(tris.Length, alloc);
		IJobParallelForExtensions.Schedule(new BuildAdjJob
		{
			T = tris.AsArray(),
			MapW = v2T.AsParallelWriter()
		}, num, 64).Complete();
		IJobParallelForExtensions.Schedule(new VertexNormalJob
		{
			AreaWeight = (areaWeight ? 1 : 0),
			TriN = nativeArray,
			V2T = v2T,
			Out = outNormals.AsArray()
		}, length, 64).Complete();
		nativeArray.Dispose();
		v2T.Dispose();
	}
}
