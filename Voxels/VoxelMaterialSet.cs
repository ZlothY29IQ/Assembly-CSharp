using System.Linq;
using Pooling;
using UnityEngine;

namespace Voxels;

[CreateAssetMenu(fileName = "VoxelMaterialSet", menuName = "Voxels/VoxelMaterialSet")]
public class VoxelMaterialSet : ScriptableObject
{
	public VoxelMaterial[] Materials;

	[SerializeField]
	private float tile = 0.2f;

	[SerializeField]
	private float backlightPower = 4f;

	private bool _initialized;

	private Material _material;

	private int _lastFrame;

	private int _callCount;

	private Texture2DArray TextureArray { get; set; }

	public Material Material
	{
		get
		{
			Init();
			return _material;
		}
		set
		{
			_material = value;
		}
	}

	private void Init()
	{
		if (!_initialized)
		{
			TextureArray = TextureArrayUtil.CreateTextureArray(Materials.Select((VoxelMaterial m) => m.texture).ToArray());
			if ((bool)TextureArray)
			{
				TextureArray.name = "VoxelMaterialTextureArray";
			}
			_material = new Material(Shader.Find("Shader Graphs/TriplanarTextureArrayGraph_Unlit"));
			_material.name = "VoxelMaterial";
			_material.SetFloat("_Tile", tile);
			_material.SetFloat("_BacklightPower", backlightPower);
			_material.SetTexture("_Diffuse", TextureArray);
			_initialized = true;
		}
	}

	public int GetHardness(byte material)
	{
		if (material >= Materials.Length)
		{
			return 1;
		}
		return Materials[material].hardness;
	}

	public void PlayDigFX(Vector3 position, Vector3 normal, int[] amounts)
	{
		int frameCount = Time.frameCount;
		if (_lastFrame != frameCount)
		{
			_callCount = 0;
		}
		if (_callCount >= 5)
		{
			return;
		}
		for (int i = 0; i < Materials.Length; i++)
		{
			int num = amounts[i];
			if (num > 0)
			{
				_lastFrame = frameCount;
				_callCount++;
				((num >= 20) ? Materials[i].digBigFX : Materials[i].digFX).Get(position, Quaternion.LookRotation(normal));
			}
		}
	}
}
