using Unity.Mathematics;
using UnityEngine;
using Voxels;

namespace FastSurfaceNets;

public class SurfaceNetsWorld : MonoBehaviour
{
	public SurfaceNetsChunk chunkPrefab;

	public int3 radius;

	public GenerationParameters parameters;

	private void Awake()
	{
		Generate();
	}

	private void Generate()
	{
		DestroyChildren();
		for (int i = -radius.x; i <= radius.x; i++)
		{
			for (int j = -radius.y; j <= radius.y; j++)
			{
				for (int k = -radius.z; k <= radius.z; k++)
				{
					int3 int5 = new int3(i, j, k);
					SurfaceNetsChunk surfaceNetsChunk = Object.Instantiate(chunkPrefab, base.transform);
					surfaceNetsChunk.Id = int5;
					surfaceNetsChunk.parameters = parameters;
					surfaceNetsChunk.name = $"SurfaceNetsChunk_{int5.x}_{int5.y}_{int5.z}";
					surfaceNetsChunk.transform.localPosition = int5.ToFloat3() * 32f;
					surfaceNetsChunk.BuildChunk();
				}
			}
		}
	}

	private void DestroyChildren()
	{
		for (int num = base.transform.childCount - 1; num >= 0; num--)
		{
			Transform child = base.transform.GetChild(num);
			if (child != null)
			{
				JamUtil.Destroy(child.gameObject);
			}
		}
	}
}
