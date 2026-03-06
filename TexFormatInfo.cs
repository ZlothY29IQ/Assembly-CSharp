using UnityEngine;

public struct TexFormatInfo(Texture2D tex2d)
{
	public bool isValid = true;

	public int width = tex2d.width;

	public int height = tex2d.height;

	public TextureFormat format = tex2d.format;

	public FilterMode filterMode = tex2d.filterMode;

	public int mipmapCount = tex2d.mipmapCount;

	public bool isLinearColor = !tex2d.isDataSRGB;

	public override string ToString()
	{
		return "TexFormatInfo(isValid: " + isValid + ", width: " + width + ", height: " + height + ", format: " + format.ToString() + ", filterMode: " + filterMode.ToString() + ", isLinearColor: " + isLinearColor + ", mipmapCount: " + mipmapCount + ")";
	}
}
