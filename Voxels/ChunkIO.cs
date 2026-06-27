using System.IO;
using K4os.Compression.LZ4;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Voxels;

public static class ChunkIO
{
	public const uint MAGIC = 1448040524u;

	public const int VERSION = 5;

	private static readonly string Root = Path.Combine(Application.persistentDataPath, "WorldSaves");

	public static string PathFor(int3 id)
	{
		return Path.Combine(Root, $"{id.x}_{id.y}_{id.z}.vox");
	}

	public static void SaveChunk(ChunkDTO dto)
	{
		string text = PathFor(dto.Id);
		Debug.Log($"Saving chunk {dto.Id} to {text}");
		Save(text, in dto);
	}

	public static bool TryLoadChunk(int3 id, out ChunkDTO dto)
	{
		string arg = PathFor(id);
		if (!File.Exists(PathFor(id)))
		{
			dto = default(ChunkDTO);
			return false;
		}
		dto = Load(PathFor(id));
		if (dto.IsValid)
		{
			Debug.Log($"Loaded chunk {id} from {arg}");
		}
		else
		{
			Debug.Log($"Chunk {id} at {arg} magic or version mismatch.");
		}
		return dto.IsValid;
	}

	public static void Save(string path, in ChunkDTO chunk)
	{
		if (!Directory.Exists(Root))
		{
			Directory.CreateDirectory(Root);
		}
		using FileStream output = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: false);
		using BinaryWriter bw = new BinaryWriter(output);
		WriteChunk(bw, in chunk);
	}

	public static byte[] SerializeChunk(in ChunkDTO chunk)
	{
		using MemoryStream memoryStream = new MemoryStream(4096);
		using BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
		WriteChunk(binaryWriter, in chunk);
		binaryWriter.Flush();
		return memoryStream.ToArray();
	}

	private static void WriteChunk(BinaryWriter bw, in ChunkDTO chunk)
	{
		bw.Write(1448040524u);
		bw.Write(5);
		bw.Write(chunk.WorldId);
		bw.Write(chunk.Id.x);
		bw.Write(chunk.Id.y);
		bw.Write(chunk.Id.z);
		bw.Write(chunk.Size.x);
		bw.Write(chunk.Size.y);
		bw.Write(chunk.Size.z);
		bw.Write(chunk.Dimensions.x);
		bw.Write(chunk.Dimensions.y);
		bw.Write(chunk.Dimensions.z);
		WriteNativeArray(bw, chunk.Density);
		WriteNativeArray(bw, chunk.Material);
	}

	public static ChunkDTO Load(string path, Allocator alloc = Allocator.Persistent)
	{
		using FileStream input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: false);
		using BinaryReader br = new BinaryReader(input);
		return ReadChunk(br, alloc);
	}

	public static bool TryDeserializeChunk(in byte[] data, out ChunkDTO dto)
	{
		dto = DeserializeChunk(in data);
		return dto.IsValid;
	}

	public static ChunkDTO DeserializeChunk(in byte[] data, Allocator alloc = Allocator.Persistent)
	{
		using MemoryStream input = new MemoryStream(data);
		using BinaryReader br = new BinaryReader(input);
		return ReadChunk(br);
	}

	private static ChunkDTO ReadChunk(BinaryReader br, Allocator alloc = Allocator.Persistent)
	{
		uint num = br.ReadUInt32();
		int num2 = br.ReadInt32();
		if (num != 1448040524 || num2 != 5)
		{
			return default(ChunkDTO);
		}
		int worldId = br.ReadInt32();
		int3 id = new int3(br.ReadInt32(), br.ReadInt32(), br.ReadInt32());
		int3 size = new int3(br.ReadInt32(), br.ReadInt32(), br.ReadInt32());
		int3 dimensions = new int3(br.ReadInt32(), br.ReadInt32(), br.ReadInt32());
		NativeArray<byte> density = ReadNativeArray(br, alloc);
		NativeArray<byte> material = ReadNativeArray(br, alloc);
		return new ChunkDTO
		{
			WorldId = worldId,
			Id = id,
			Size = size,
			Dimensions = dimensions,
			Density = density,
			Material = material
		};
	}

	private static void WriteNativeArray(BinaryWriter bw, NativeArray<byte> src)
	{
		byte[] array = LZ4Pickler.Pickle(src.ToArray());
		bw.Write(array.Length);
		bw.Write(array);
	}

	private static NativeArray<byte> ReadNativeArray(BinaryReader br, Allocator alloc = Allocator.Persistent)
	{
		int count = br.ReadInt32();
		byte[] array = LZ4Pickler.Unpickle(br.ReadBytes(count));
		int length = array.Length;
		NativeArray<byte> nativeArray = new NativeArray<byte>(length, alloc);
		NativeArray<byte>.Copy(array, nativeArray, length);
		return nativeArray;
	}

	public static void DeleteWorld()
	{
		if (Directory.Exists(Root))
		{
			Directory.Delete(Root, recursive: true);
		}
		Debug.Log("All chunks deleted.");
	}
}
