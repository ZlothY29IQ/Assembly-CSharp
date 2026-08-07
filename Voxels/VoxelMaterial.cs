using System;
using Pooling;
using UnityEngine;

namespace Voxels;

[Serializable]
public struct VoxelMaterial
{
	public string name;

	public Texture2D texture;

	public int hardness;

	public PoolableFX digFX;

	public PoolableFX digBigFX;
}
