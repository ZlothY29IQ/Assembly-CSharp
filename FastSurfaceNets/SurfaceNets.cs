using System;
using Unity.Mathematics;

namespace FastSurfaceNets;

public static class SurfaceNets
{
	private static readonly int3[] CubeCorners = new int3[8]
	{
		new int3(0, 0, 0),
		new int3(1, 0, 0),
		new int3(0, 1, 0),
		new int3(1, 1, 0),
		new int3(0, 0, 1),
		new int3(1, 0, 1),
		new int3(0, 1, 1),
		new int3(1, 1, 1)
	};

	private static readonly float3[] CornerVectors = new float3[8]
	{
		new float3(0f, 0f, 0f),
		new float3(1f, 0f, 0f),
		new float3(0f, 1f, 0f),
		new float3(1f, 1f, 0f),
		new float3(0f, 0f, 1f),
		new float3(1f, 0f, 1f),
		new float3(0f, 1f, 1f),
		new float3(1f, 1f, 1f)
	};

	private static readonly int2[] CubeEdges = new int2[12]
	{
		new int2(0, 1),
		new int2(0, 2),
		new int2(0, 4),
		new int2(1, 3),
		new int2(1, 5),
		new int2(2, 3),
		new int2(2, 6),
		new int2(3, 7),
		new int2(4, 5),
		new int2(4, 6),
		new int2(5, 7),
		new int2(6, 7)
	};

	public static void Generate(float[] sdf, int3 shape, int3 min, int3 max, SurfaceNetsBuffer output)
	{
		if (sdf == null)
		{
			throw new ArgumentNullException("sdf");
		}
		int num = shape.x * shape.y * shape.z;
		if (sdf.Length < num)
		{
			throw new ArgumentException("SDF array is smaller than shape.", "sdf");
		}
		output.Reset(num);
		int num2 = 1;
		int x = shape.x;
		int num3 = shape.x * shape.y;
		for (int i = min.z; i < max.z; i++)
		{
			for (int j = min.y; j < max.y; j++)
			{
				int num4 = i * num3 + j * x + min.x;
				int num5 = min.x;
				while (num5 < max.x)
				{
					if (EstimateSurfaceInCube(sdf, shape, new int3(num5, j, i), num4, num2, x, num3, output, out var centroid))
					{
						int count = output.Positions.Count;
						output.StrideToIndex[num4] = count;
						output.SurfacePoints.Add(new int3(num5, j, i));
						output.SurfaceStrides.Add(num4);
						output.Positions.Add(new float3(num5, j, i) + centroid);
					}
					num5++;
					num4++;
				}
			}
		}
		MakeAllQuads(sdf, shape, min, max, num2, x, num3, output);
		AccumulateNormals(output);
	}

	private static bool EstimateSurfaceInCube(float[] sdf, int3 shape, int3 voxel, int cubeMinStride, int strideX, int strideY, int strideZ, SurfaceNetsBuffer output, out float3 centroid)
	{
		float[] array = new float[8];
		int num = 0;
		for (int i = 0; i < 8; i++)
		{
			int3 int5 = CubeCorners[i];
			int num2 = cubeMinStride + int5.x * strideX + int5.y * strideY + int5.z * strideZ;
			if ((array[i] = sdf[num2]) < 0f)
			{
				num++;
			}
		}
		if (num == 0 || num == 8)
		{
			centroid = default(float3);
			return false;
		}
		float3 zero = float3.zero;
		int num3 = 0;
		int2[] cubeEdges = CubeEdges;
		for (int j = 0; j < cubeEdges.Length; j++)
		{
			int2 int6 = cubeEdges[j];
			float num4 = array[int6.x];
			float num5 = array[int6.y];
			if (num4 < 0f != num5 < 0f)
			{
				num3++;
				zero += EdgeIntersection(int6.x, int6.y, num4, num5);
			}
		}
		centroid = zero / num3;
		output.Normals.Add(float3.zero);
		return true;
	}

	private static float3 EdgeIntersection(int c1, int c2, float v1, float v2)
	{
		float num = v1 / (v1 - v2);
		return CornerVectors[c1] * (1f - num) + CornerVectors[c2] * num;
	}

	private static float3 CentralDifferenceGradient(float[] sdf, int3 shape, int3 v, int sx, int sy, int sz)
	{
		int num = math.max(v.x - 1, 0);
		int num2 = math.min(v.x + 1, shape.x - 1);
		int num3 = math.max(v.y - 1, 0);
		int num4 = math.min(v.y + 1, shape.y - 1);
		int num5 = math.max(v.z - 1, 0);
		int num6 = math.min(v.z + 1, shape.z - 1);
		float x = sdf[num2 + v.y * sy + v.z * sz] - sdf[num + v.y * sy + v.z * sz];
		float y = sdf[v.x + num4 * sy + v.z * sz] - sdf[v.x + num3 * sy + v.z * sz];
		float z = sdf[v.x + v.y * sy + num6 * sz] - sdf[v.x + v.y * sy + num5 * sz];
		return new float3(x, y, z);
	}

	private static void MakeAllQuads(float[] sdf, int3 shape, int3 min, int3 max, int sx, int sy, int sz, SurfaceNetsBuffer outBuf)
	{
		for (int i = 0; i < outBuf.SurfacePoints.Count; i++)
		{
			int3 int5 = outBuf.SurfacePoints[i];
			int num = outBuf.SurfaceStrides[i];
			if (int5.y != min.y && int5.z != min.z && int5.x != max.x - 1)
			{
				MaybeQuad(sdf, outBuf, num, num + sx, sy, sz);
			}
			if (int5.x != min.x && int5.z != min.z && int5.y != max.y - 1)
			{
				MaybeQuad(sdf, outBuf, num, num + sy, sz, sx);
			}
			if (int5.x != min.x && int5.y != min.y && int5.z != max.z - 1)
			{
				MaybeQuad(sdf, outBuf, num, num + sz, sx, sy);
			}
		}
	}

	private static void MaybeQuad(float[] sdf, SurfaceNetsBuffer b, int p1, int p2, int strideB, int strideC)
	{
		float num = sdf[p1];
		float num2 = sdf[p2];
		bool flag = num < 0f && num2 >= 0f;
		if (!flag && (!(num2 < 0f) || !(num >= 0f)))
		{
			return;
		}
		int num3 = b.StrideToIndex[p1];
		int num4 = b.StrideToIndex[p1 - strideB];
		int num5 = b.StrideToIndex[p1 - strideC];
		int num6 = b.StrideToIndex[p1 - strideB - strideC];
		if ((num3 | num4 | num5 | num6) == int.MaxValue)
		{
			return;
		}
		float3 obj = b.Positions[num3];
		float3 float5 = b.Positions[num4];
		float3 float6 = b.Positions[num5];
		float3 float7 = b.Positions[num6];
		if (math.lengthsq(obj - float7) < math.lengthsq(float5 - float6))
		{
			if (flag)
			{
				b.Indices.AddRange(new int[6] { num3, num6, num4, num3, num5, num6 });
			}
			else
			{
				b.Indices.AddRange(new int[6] { num3, num4, num6, num3, num6, num5 });
			}
		}
		else if (flag)
		{
			b.Indices.AddRange(new int[6] { num4, num5, num6, num4, num3, num5 });
		}
		else
		{
			b.Indices.AddRange(new int[6] { num4, num6, num5, num4, num5, num3 });
		}
	}

	private static void AccumulateNormals(SurfaceNetsBuffer b)
	{
		for (int i = 0; i < b.Indices.Count; i += 3)
		{
			int index = b.Indices[i];
			int index2 = b.Indices[i + 1];
			int index3 = b.Indices[i + 2];
			float3 float5 = b.Positions[index];
			float3 obj = b.Positions[index2];
			float3 float6 = math.cross(y: b.Positions[index3] - float5, x: obj - float5);
			b.Normals[index] += float6;
			b.Normals[index2] += float6;
			b.Normals[index3] += float6;
		}
	}
}
