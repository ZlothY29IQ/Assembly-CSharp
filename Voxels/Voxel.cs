using System;

namespace Voxels;

[Serializable]
public struct Voxel(byte material, byte density)
{
	public byte Material = material;

	public byte Density = density;
}
