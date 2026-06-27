using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Voxels;

[BurstCompile]
public struct SurfaceNetsJob : IJob
{
	[ReadOnly]
	public NativeArray<byte> sdf;

	[ReadOnly]
	public NativeArray<byte> material;

	public int3 shape;

	public int3 min;

	public int3 max;

	public byte isoLevel;

	public SurfaceNetsBuffer buffer;

	private static readonly int3[] cubeCorners = new int3[8]
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

	private static readonly float3[] cornerVecs = new float3[8]
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

	private static readonly int2[] cubeEdges = new int2[12]
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

	public void Execute()
	{
		buffer.Reset(shape.x * shape.y * shape.z);
		int x = shape.x;
		int num = shape.x * shape.y;
		for (int i = min.z; i < max.z; i++)
		{
			for (int j = min.y; j < max.y; j++)
			{
				int value = i * num + j * x + min.x;
				int num2 = min.x;
				while (num2 < max.x)
				{
					if (EstimateSurfaceInCube(new int3(num2, j, i), value, 1, x, num, out var centroid))
					{
						int length = buffer.Vertices.Length;
						buffer.StrideToIndex[value] = length;
						buffer.SurfacePoints.Add(new int3(num2, j, i));
						buffer.SurfaceStrides.Add(in value);
						buffer.Vertices.Add(new float3(num2, j, i) + centroid);
						buffer.Normals.Add(in float3.zero);
						buffer.Materials.Add(material[value]);
					}
					num2++;
					value++;
				}
			}
		}
		MakeAllQuads(1, x, num);
		AccumulateNormals();
	}

	private bool EstimateSurfaceInCube(int3 voxel, int cubeMin, int sx, int sy, int sz, out float3 centroid)
	{
		int num = 0;
		NativeArray<float> nativeArray = new NativeArray<float>(8, Allocator.Temp);
		for (int i = 0; i < 8; i++)
		{
			int3 int5 = cubeCorners[i];
			float num2 = (nativeArray[i] = sdf[cubeMin + int5.x * sx + int5.y * sy + int5.z * sz].ToFloat());
			if (num2 < 0f)
			{
				num++;
			}
		}
		if (num == 0 || num == 8)
		{
			centroid = default(float3);
			nativeArray.Dispose();
			return false;
		}
		float3 zero = float3.zero;
		int num4 = 0;
		for (int j = 0; j < 12; j++)
		{
			int2 int6 = cubeEdges[j];
			float num5 = nativeArray[int6.x];
			float num6 = nativeArray[int6.y];
			if ((num5 < 0f) ^ (num6 < 0f))
			{
				num4++;
				zero += EdgeIntersection(int6.x, int6.y, num5, num6);
			}
		}
		centroid = zero / num4;
		nativeArray.Dispose();
		return true;
	}

	private static float3 EdgeIntersection(int c1, int c2, float v1, float v2)
	{
		float num = v1 / (v1 - v2);
		return cornerVecs[c1] * (1f - num) + cornerVecs[c2] * num;
	}

	private void MakeAllQuads(int sx, int sy, int sz)
	{
		int3 int5 = min;
		int3 int6 = max;
		for (int i = 0; i < buffer.SurfacePoints.Length; i++)
		{
			int3 int7 = buffer.SurfacePoints[i];
			int num = buffer.SurfaceStrides[i];
			if (int7.y != int5.y && int7.z != int5.z && int7.x != int6.x - 1)
			{
				TryQuad(num, num + sx, sy, sz);
			}
			if (int7.x != int5.x && int7.z != int5.z && int7.y != int6.y - 1)
			{
				TryQuad(num, num + sy, sz, sx);
			}
			if (int7.x != int5.x && int7.y != int5.y && int7.z != int6.z - 1)
			{
				TryQuad(num, num + sz, sx, sy);
			}
		}
	}

	private void TryQuad(int p1, int p2, int strideB, int strideC)
	{
		float num = sdf[p1].ToFloat();
		float num2 = sdf[p2].ToFloat();
		bool flag = num < 0f && num2 >= 0f;
		if (!flag && (!(num2 < 0f) || !(num >= 0f)))
		{
			return;
		}
		int value = buffer.StrideToIndex[p1];
		int value2 = buffer.StrideToIndex[p1 - strideB];
		int value3 = buffer.StrideToIndex[p1 - strideC];
		int value4 = buffer.StrideToIndex[p1 - strideB - strideC];
		if ((value | value2 | value3 | value4) == int.MaxValue)
		{
			return;
		}
		float3 obj = buffer.Vertices[value];
		float3 float5 = buffer.Vertices[value2];
		float3 float6 = buffer.Vertices[value3];
		float3 float7 = buffer.Vertices[value4];
		if (math.lengthsq(obj - float7) < math.lengthsq(float5 - float6))
		{
			if (flag)
			{
				buffer.Triangles.Add(in value);
				buffer.Triangles.Add(in value4);
				buffer.Triangles.Add(in value2);
				buffer.Triangles.Add(in value);
				buffer.Triangles.Add(in value3);
				buffer.Triangles.Add(in value4);
			}
			else
			{
				buffer.Triangles.Add(in value);
				buffer.Triangles.Add(in value2);
				buffer.Triangles.Add(in value4);
				buffer.Triangles.Add(in value);
				buffer.Triangles.Add(in value4);
				buffer.Triangles.Add(in value3);
			}
		}
		else if (flag)
		{
			buffer.Triangles.Add(in value2);
			buffer.Triangles.Add(in value3);
			buffer.Triangles.Add(in value4);
			buffer.Triangles.Add(in value2);
			buffer.Triangles.Add(in value);
			buffer.Triangles.Add(in value3);
		}
		else
		{
			buffer.Triangles.Add(in value2);
			buffer.Triangles.Add(in value4);
			buffer.Triangles.Add(in value3);
			buffer.Triangles.Add(in value2);
			buffer.Triangles.Add(in value3);
			buffer.Triangles.Add(in value);
		}
	}

	private void AccumulateNormals()
	{
		for (int i = 0; i < buffer.Triangles.Length; i += 3)
		{
			int index = buffer.Triangles[i];
			int index2 = buffer.Triangles[i + 1];
			int index3 = buffer.Triangles[i + 2];
			float3 float5 = buffer.Vertices[index];
			float3 obj = buffer.Vertices[index2];
			float3 float6 = math.cross(y: buffer.Vertices[index3] - float5, x: obj - float5);
			buffer.Normals[index] += float6;
			buffer.Normals[index2] += float6;
			buffer.Normals[index3] += float6;
		}
	}
}
