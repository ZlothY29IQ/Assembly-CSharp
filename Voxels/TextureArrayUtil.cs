using UnityEngine;

namespace Voxels;

public class TextureArrayUtil : MonoBehaviour
{
	public TextureEntry[] textureEntries;

	public Texture2DArray diffuseArray;

	public Texture2DArray normalArray;

	public Material material;

	public bool linearNormalMaps = true;

	public string diffuseName = "_Diffuse";

	public string normalName = "_Normal";

	private bool UnreadableTextureFound => !TexturesReadable;

	private bool TexturesReadable
	{
		get
		{
			TextureEntry[] array = textureEntries;
			for (int i = 0; i < array.Length; i++)
			{
				TextureEntry textureEntry = array[i];
				if (textureEntry.Diffuse == null || textureEntry.Normal == null)
				{
					return false;
				}
				if (!textureEntry.Diffuse.isReadable || !textureEntry.Normal.isReadable)
				{
					return false;
				}
			}
			return true;
		}
	}

	public static Texture2DArray CreateTextureArray(Texture2D[] textures)
	{
		if (textures == null || textures.Length == 0)
		{
			return null;
		}
		int width = textures[0].width;
		int height = textures[0].height;
		bool mipChain = textures[0].mipmapCount > 1;
		int num = textures.Length;
		Texture2DArray texture2DArray = new Texture2DArray(width, height, num, textures[0].format, mipChain);
		for (int i = 0; i < num; i++)
		{
			Graphics.CopyTexture(textures[i], 0, texture2DArray, i);
		}
		texture2DArray.Apply(updateMipmaps: false, makeNoLongerReadable: true);
		return texture2DArray;
	}
}
