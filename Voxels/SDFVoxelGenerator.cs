using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Voxels;

public class SDFVoxelGenerator : VoxelGenerator
{
	public enum Operation
	{
		Add,
		Subtract
	}

	public enum Shape
	{
		Sphere,
		Cube
	}

	[Serializable]
	public struct SDFPrimitive
	{
		public Operation Operation;

		public Shape Shape;

		public float3 Position;

		public float Radius;

		public float3 Size;

		public byte Material;

		private bool ShowRadius => Shape == Shape.Sphere;

		private bool ShowSize => !ShowRadius;

		public Bounds GetBounds()
		{
			Vector3 size = Shape switch
			{
				Shape.Sphere => new Vector3(Radius * 2f, Radius * 2f, Radius * 2f), 
				Shape.Cube => Size, 
				_ => Vector3.zero, 
			};
			return new Bounds(Position, size);
		}
	}

	[BurstCompile]
	public struct VoxelDataJob : IJobParallelFor
	{
		public int3 chunkPosition;

		public int chunkSize;

		public int dimension;

		public bool blocky;

		public float noiseScale;

		public float heightScale;

		public int octaves;

		public float persistence;

		public int seed;

		[WriteOnly]
		public NativeArray<byte> voxels;

		[WriteOnly]
		public NativeArray<byte> materials;

		public byte fill;

		[ReadOnly]
		public NativeArray<SDFPrimitive> operations;

		public void Execute(int index)
		{
			int num = index % dimension;
			int num2 = index / dimension % dimension;
			int num3 = index / (dimension * dimension);
			float3 float5 = new float3(chunkPosition.x * chunkSize + num, chunkPosition.y * chunkSize + num2, chunkPosition.z * chunkSize + num3);
			byte value = fill;
			float num4 = -1f;
			float num5 = float.MaxValue;
			float num6 = float.MaxValue;
			byte material = fill;
			SDFPrimitive sDFPrimitive = default(SDFPrimitive);
			foreach (SDFPrimitive operation in operations)
			{
				float distance = GetDistance(operation, float5);
				if ((operation.Operation != Operation.Subtract || (!(num5 > 0f) && !(distance > 0f))) && (distance <= 0f || distance < num5))
				{
					sDFPrimitive = operation;
					num5 = distance;
					if (operation.Operation == Operation.Add)
					{
						num6 = num5;
						material = operation.Material;
					}
				}
			}
			if (num5 < float.MaxValue)
			{
				num4 = sDFPrimitive.Operation switch
				{
					Operation.Add => 0f - num5, 
					Operation.Subtract => num5, 
					_ => throw new ArgumentOutOfRangeException(), 
				};
				if (heightScale > 0f)
				{
					float3 float6 = new float3((float)seed * 1.7f, (float)seed * 2.3f, (float)seed * 3.1f);
					float3 float7 = float5 + float6;
					float num7 = noise.snoise(float7 * noiseScale) * heightScale;
					float num8 = noiseScale;
					float num9 = 1f;
					for (int i = 0; i < octaves; i++)
					{
						num8 *= 2f;
						num9 *= persistence;
						num7 += noise.snoise(float7 * num8) * num9 * heightScale;
					}
					num4 += num7;
				}
				if (num4 > -1.75f && num6 < float.MaxValue)
				{
					value = material;
				}
			}
			materials[index] = value;
			byte b = num4.ToByte();
			if (blocky)
			{
				b = (byte)(b.IsSolid() ? 255u : 0u);
			}
			voxels[index] = b;
		}

		private static float GetDistance(SDFPrimitive primitive, float3 position)
		{
			switch (primitive.Shape)
			{
			case Shape.Sphere:
				return math.distance(primitive.Position, position) - primitive.Radius;
			case Shape.Cube:
			{
				float3 float5 = primitive.Size * 0.5f;
				float3 x = math.abs(position - primitive.Position) - float5;
				float num = math.length(math.max(x, 0f));
				float num2 = math.min(math.max(x.x, math.max(x.y, x.z)), 0f);
				return num + num2;
			}
			default:
				return float.MaxValue;
			}
		}
	}

	public byte fill;

	public bool Blocky;

	[Header("Noise")]
	public float NoiseScale;

	public float Frequency = 0.1f;

	public int Octaves = 1;

	public float Persistence = 0.5f;

	public SDFPrimitive[] Primitives;

	public override ChunkTask CreateVoxelDataJob(Chunk chunk)
	{
		NativeArray<SDFPrimitive> opBuffer = new NativeArray<SDFPrimitive>(Primitives, Allocator.TempJob);
		if (!chunk.Density.IsCreated)
		{
			chunk.Density = new NativeArray<byte>(chunk.VoxelCount, Allocator.Persistent);
		}
		if (!chunk.Material.IsCreated)
		{
			chunk.Material = new NativeArray<byte>(chunk.VoxelCount, Allocator.Persistent);
		}
		JobHandle handle = IJobParallelForExtensions.Schedule(new VoxelDataJob
		{
			chunkPosition = chunk.Id,
			chunkSize = chunk.Size.x,
			dimension = chunk.Dimensions.x,
			blocky = Blocky,
			seed = Seed,
			voxels = chunk.Density,
			materials = chunk.Material,
			fill = fill,
			operations = opBuffer,
			heightScale = NoiseScale,
			noiseScale = Frequency,
			octaves = Octaves,
			persistence = Persistence
		}, chunk.VoxelCount, 64);
		Action onComplete = delegate
		{
			chunk.IsDataGenerated = true;
			chunk.IsMeshGenerated = false;
			chunk.IsDirty = true;
			opBuffer.Dispose();
		};
		return new ChunkTask(chunk, handle, onComplete);
	}

	public override void DrawGizmos(VoxelWorld world)
	{
		Transform root = world.Root;
		Gizmos.matrix = Matrix4x4.TRS(root.position, root.rotation, root.lossyScale * world.Scale);
		SDFPrimitive[] primitives = Primitives;
		for (int i = 0; i < primitives.Length; i++)
		{
			SDFPrimitive sDFPrimitive = primitives[i];
			Gizmos.color = ((sDFPrimitive.Operation == Operation.Add) ? Color.green : Color.red);
			switch (sDFPrimitive.Shape)
			{
			case Shape.Sphere:
				Gizmos.DrawWireSphere(sDFPrimitive.Position, sDFPrimitive.Radius);
				break;
			case Shape.Cube:
				Gizmos.DrawWireCube(sDFPrimitive.Position, sDFPrimitive.Size);
				Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.1f);
				Gizmos.DrawCube(sDFPrimitive.Position, sDFPrimitive.Size);
				break;
			default:
				throw new ArgumentOutOfRangeException();
			}
		}
	}

	public override UnityEngine.BoundsInt GetWorldBounds()
	{
		if (Primitives.Length < 1)
		{
			return default(UnityEngine.BoundsInt);
		}
		Bounds bounds = Primitives[0].GetBounds();
		SDFPrimitive[] primitives = Primitives;
		foreach (SDFPrimitive sDFPrimitive in primitives)
		{
			bounds.Encapsulate(sDFPrimitive.GetBounds());
		}
		bounds.size += Vector3.one * (2f + math.max(NoiseScale * 2f, 0f));
		return new UnityEngine.BoundsInt(bounds.min.FloorToVectorInt(), bounds.size.CeilToVectorInt());
	}

	public override void ShiftWorldBounds(Vector3Int worldShift)
	{
		float3 float5 = new float3(worldShift.x, worldShift.y, worldShift.z);
		for (int i = 0; i < Primitives.Length; i++)
		{
			Primitives[i].Position += float5;
		}
	}

	public override void InitWorld(VoxelWorld world)
	{
		world.SetWorldBounds(GetWorldBounds());
	}

	public void SetPrimitive(UnityEngine.BoundsInt worldBounds, byte material = 0)
	{
		Vector3 vector = ((Vector3)worldBounds.min + (Vector3)worldBounds.max) / 2f;
		Primitives = new SDFPrimitive[1];
		Primitives[0] = new SDFPrimitive
		{
			Operation = Operation.Add,
			Shape = Shape.Cube,
			Position = vector,
			Size = (Vector3)worldBounds.size,
			Material = material
		};
	}
}
