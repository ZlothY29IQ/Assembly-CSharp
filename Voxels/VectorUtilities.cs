using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace Voxels;

public static class VectorUtilities
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector3Int ToVectorInt(this int3 v)
	{
		return new Vector3Int(v.x, v.y, v.z);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int3 ToInt3(this Vector3Int v)
	{
		return new int3(v.x, v.y, v.z);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int3 ToInt3(this Vector3 v)
	{
		return new int3((int)v.x, (int)v.y, (int)v.z);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int3 ToInt3(this float3 v)
	{
		return new int3((int)v.x, (int)v.y, (int)v.z);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int3 RoundToInt(this Vector3 v)
	{
		return (int3)math.round(v);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int3 RoundToInt(this float3 v)
	{
		return (int3)math.round(v);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int3 CeilToInt(this float3 v)
	{
		return new int3(Mathf.CeilToInt(v.x), Mathf.CeilToInt(v.y), Mathf.CeilToInt(v.z));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector3 Floor(this Vector3 v)
	{
		return new Vector3(math.floor(v.x), Mathf.Floor(v.y), Mathf.Floor(v.z));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float3 Floor(this float3 v)
	{
		return new float3(math.floor(v.x), math.floor(v.y), math.floor(v.z));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector3 ToVector3(this int3 v)
	{
		return new Vector3(v.x, v.y, v.z);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float3 ToFloat3(this int3 v)
	{
		return new float3(v.x, v.y, v.z);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int3 FloorToMultipleOfX(this Vector3 v, int3 x)
	{
		return (int3)(math.floor(new float3(v.x / (float)x.x, v.y / (float)x.y, v.z / (float)x.z)) * x);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int3 FloorToMultipleOfX(this Vector3Int v, int3 x)
	{
		return (int3)(math.floor(new float3((float)v.x / (float)x.x, (float)v.y / (float)x.y, (float)v.z / (float)x.z)) * x);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int3 FloorToMultipleOfX(this int3 v, int3 x)
	{
		return (int3)(math.floor(new float3((float)v.x / (float)x.x, (float)v.y / (float)x.y, (float)v.z / (float)x.z)) * x);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int3 LocalPositionToChunkId(this Vector3 localWorldPosition, int3 chunkSize)
	{
		return localWorldPosition.FloorToMultipleOfX(chunkSize) / chunkSize;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int3 LocalPositionToChunkId(this Vector3Int localWorldPosition, int3 chunkSize)
	{
		return localWorldPosition.FloorToMultipleOfX(chunkSize) / chunkSize;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int3 LocalPositionToChunkId(this int3 localWorldPosition, int3 chunkSize)
	{
		return localWorldPosition.FloorToMultipleOfX(chunkSize) / chunkSize;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static byte ToByte(this float value)
	{
		return (byte)math.clamp(value * 127f + 128f, 0f, 255f);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float ToFloat(this byte value)
	{
		return (float)(value - 128) / 127f;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsSolid(this byte density)
	{
		return density > 127;
	}

	public static int3[] GetCardinalNeighbours(this int3 center)
	{
		return new int3[6]
		{
			center + new int3(1, 0, 0),
			center + new int3(-1, 0, 0),
			center + new int3(0, 1, 0),
			center + new int3(0, -1, 0),
			center + new int3(0, 0, 1),
			center + new int3(0, 0, -1)
		};
	}

	public static int3 GetClosestCardinalNeighbour(this int3 center, Vector3 target)
	{
		int3[] cardinalNeighbours = center.GetCardinalNeighbours();
		int num = 0;
		float num2 = math.distance(cardinalNeighbours[0], target);
		for (int i = 1; i < cardinalNeighbours.Length; i++)
		{
			float num3 = math.distance(cardinalNeighbours[i], target);
			if (num3 < num2)
			{
				num2 = num3;
				num = i;
			}
		}
		return cardinalNeighbours[num];
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector3Int Min(Vector3Int v1, Vector3Int v2)
	{
		return new Vector3Int(math.min(v1.x, v2.x), math.min(v1.y, v2.y), math.min(v1.z, v2.z));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector3Int Max(Vector3Int v1, Vector3Int v2)
	{
		return new Vector3Int(math.max(v1.x, v2.x), math.max(v1.y, v2.y), math.max(v1.z, v2.z));
	}
}
