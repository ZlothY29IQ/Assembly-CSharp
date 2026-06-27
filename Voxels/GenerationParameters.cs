using System;

namespace Voxels;

[Serializable]
public struct GenerationParameters
{
	public MeshGenerationMode MeshGenerationMode;

	public float NoiseScale;

	public float GroundLevel;

	public float HeightScale;

	public float HeightCompensation;

	public int Octaves;

	public float Persistence;

	public float IsoLevel;

	public int Seed;

	public float NormalThreshold;

	public bool AreaWeightedNormals;
}
