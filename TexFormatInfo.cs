using UnityEngine;

public struct TexFormatInfo
{
	public bool isValid;

	public int width;

	public int height;

	public TextureFormat format;

	public FilterMode filterMode;

	public int mipmapCount;

	public bool isLinearColor;

	public TexFormatInfo(Texture2D tex2d)
	{
		width = tex2d.width;
		height = tex2d.height;
		format = tex2d.format;
		filterMode = tex2d.filterMode;
		isLinearColor = !tex2d.isDataSRGB;
		mipmapCount = tex2d.mipmapCount;
		isValid = true;
	}

	public override string ToString()
	{
		return "TexFormatInfo(isValid: " + isValid + ", width: " + width + ", height: " + height + ", format: " + format.ToString() + ", filterMode: " + filterMode.ToString() + ", isLinearColor: " + isLinearColor + ", mipmapCount: " + mipmapCount + ")";
	}
}
