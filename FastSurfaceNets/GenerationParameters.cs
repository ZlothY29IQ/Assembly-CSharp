using System;
using Unity.Mathematics;

namespace FastSurfaceNets;

[Serializable]
public class GenerationParameters
{
	public bool recalculateNormals;

	public bool customNormals = true;

	public bool useBurst = true;

	public float normalThreshold = 60f;

	public bool areaWeightedNormals = true;

	public bool generateShape = true;

	public int3 shapeMin = new int3(1);

	public int3 shapeMax = new int3(15);

	public float noiseScale = 0.01f;

	public float baseHeight = 10f;

	public float heightScale = 5f;
}
