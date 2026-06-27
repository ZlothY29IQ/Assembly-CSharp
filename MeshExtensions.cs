using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public static class MeshExtensions
{
	[BurstCompile]
	private struct FaceNormalJob : IJobParallelFor
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
	private struct SplitJob : IJob
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

	public static void SplitByAngle(this Mesh mesh, float angleDeg)
	{
		float num = Mathf.Cos(angleDeg * (MathF.PI / 180f));
		Vector3[] vertices = mesh.vertices;
		int[] triangles = mesh.triangles;
		int num2 = triangles.Length / 3;
		Vector3[] array = new Vector3[num2];
		for (int i = 0; i < num2; i++)
		{
			Vector3 vector = vertices[triangles[i * 3]];
			Vector3 vector2 = vertices[triangles[i * 3 + 1]];
			Vector3 vector3 = vertices[triangles[i * 3 + 2]];
			array[i] = Vector3.Cross(vector2 - vector, vector3 - vector).normalized;
		}
		List<(int, Vector3)>[] array2 = new List<(int, Vector3)>[vertices.Length];
		for (int j = 0; j < array2.Length; j++)
		{
			array2[j] = new List<(int, Vector3)>();
		}
		List<Vector3> list = new List<Vector3>(vertices.Length);
		int[] array3 = new int[triangles.Length];
		for (int k = 0; k < num2; k++)
		{
			for (int l = 0; l < 3; l++)
			{
				int num3 = triangles[k * 3 + l];
				Vector3 vector4 = array[k];
				int num4 = -1;
				foreach (var item in array2[num3])
				{
					var (num5, _) = item;
					if (Vector3.Dot(item.Item2, vector4) >= num)
					{
						num4 = num5;
						break;
					}
				}
				if (num4 < 0)
				{
					num4 = list.Count;
					list.Add(vertices[num3]);
					array2[num3].Add((num4, vector4));
				}
				array3[k * 3 + l] = num4;
			}
		}
		mesh.Clear();
		mesh.SetVertices(list);
		mesh.triangles = array3;
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
	}

	public static void SplitByAngleBurst(this Mesh mesh, float angleDeg, bool areaWeight = true, Allocator allocator = Allocator.TempJob)
	{
		NativeArray<float3> nativeArray = new NativeArray<float3>(mesh.vertexCount, allocator);
		List<Vector3> list = new List<Vector3>(mesh.vertexCount);
		mesh.GetVertices(list);
		for (int i = 0; i < list.Count; i++)
		{
			nativeArray[i] = list[i];
		}
		NativeArray<int> nativeArray2 = new NativeArray<int>(mesh.triangles, allocator);
		NativeArray<float3> faceN = new NativeArray<float3>(nativeArray2.Length / 3, allocator);
		IJobParallelForExtensions.Schedule(new FaceNormalJob
		{
			Verts = nativeArray,
			Tris = nativeArray2,
			FaceN = faceN
		}, faceN.Length, 64).Complete();
		NativeList<float3> nativeList = new NativeList<float3>(nativeArray.Length, allocator);
		NativeList<int> nativeList2 = new NativeList<int>(nativeArray2.Length, allocator);
		new SplitJob
		{
			CosThresh = math.cos(math.radians(angleDeg)),
			SrcVerts = nativeArray,
			SrcTris = nativeArray2,
			FaceN = faceN,
			DstVerts = nativeList,
			DstTris = nativeList2
		}.Run();
		NativeArray<float3> outNormals = new NativeArray<float3>(nativeList.Length, allocator);
		RecalcNormalsJobified(nativeList, nativeList2, areaWeight, allocator, ref outNormals);
		mesh.Clear();
		List<Vector3> list2 = new List<Vector3>(nativeList.Length);
		for (int j = 0; j < nativeList.Length; j++)
		{
			list2.Add(nativeList[j]);
		}
		mesh.SetVertices(list2);
		mesh.triangles = nativeList2.AsArray().ToArray();
		List<Vector3> list3 = new List<Vector3>(outNormals.Length);
		for (int k = 0; k < outNormals.Length; k++)
		{
			list3.Add(outNormals[k]);
		}
		mesh.SetNormals(list3);
		mesh.RecalculateBounds();
		nativeArray.Dispose();
		nativeArray2.Dispose();
		faceN.Dispose();
		nativeList.Dispose();
		nativeList2.Dispose();
		outNormals.Dispose();
	}

	private static void RecalcNormalsJobified(NativeList<float3> verts, NativeList<int> tris, bool areaWeight, Allocator alloc, ref NativeArray<float3> outNormals)
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
			Out = outNormals
		}, length, 64).Complete();
		nativeArray.Dispose();
		v2T.Dispose();
	}
}
