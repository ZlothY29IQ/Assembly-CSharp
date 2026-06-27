using System;
using UnityEngine;

namespace Voxels;

[Serializable]
public struct VoxelMaterial
{
	public string name;

	public Texture2D texture;

	public int hardness;

	public GameObject digFX;

	public GameObject digBigFX;
}
