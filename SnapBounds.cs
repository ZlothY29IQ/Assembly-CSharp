using System;
using System.IO;
using UnityEngine;

[Serializable]
public struct SnapBounds
{
	public Vector2Int min;

	public Vector2Int max;

	public SnapBounds(Vector2Int min, Vector2Int max)
	{
		this.min = min;
		this.max = max;
	}

	public SnapBounds(int minX, int minY, int maxX, int maxY)
	{
		min = new Vector2Int(minX, minY);
		max = new Vector2Int(maxX, maxY);
	}

	public void Clear()
	{
		min = new Vector2Int(int.MinValue, int.MinValue);
		max = new Vector2Int(int.MinValue, int.MinValue);
	}

	public void Write(BinaryWriter writer)
	{
		writer.Write(min.x);
		writer.Write(min.y);
		writer.Write(max.x);
		writer.Write(max.y);
	}

	public void Read(BinaryReader reader)
	{
		min.x = reader.ReadInt32();
		min.y = reader.ReadInt32();
		max.x = reader.ReadInt32();
		max.y = reader.ReadInt32();
	}
}
