using UnityEngine;

namespace Voxels;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class ChunkComponent : MonoBehaviour
{
	public MeshFilter meshFilter;

	public MeshRenderer meshRenderer;

	public MeshCollider meshCollider;

	public VoxelWorld World { get; set; }

	private void Reset()
	{
		meshFilter = GetComponent<MeshFilter>();
		meshRenderer = GetComponent<MeshRenderer>();
		meshCollider = GetComponent<MeshCollider>();
	}
}
