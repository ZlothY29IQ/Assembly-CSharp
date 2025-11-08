using System;
using System.IO;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public static class TextureUtils
{
	public static Vector4 GetTexelSize(this Texture tex)
	{
		if (tex.AsNull() == null)
		{
			return Vector4.zero;
		}
		Vector2 texelSize = tex.texelSize;
		float num = Mathf.Max(texelSize.x, 1f / texelSize.x);
		float num2 = Mathf.Max(texelSize.y, 1f / texelSize.y);
		return new Vector4(1f / num, 1f / num2, num, num2);
	}

	public static Color32 CalcAverageColor(Texture2D tex)
	{
		if (tex == null)
		{
			return default(Color32);
		}
		Color32[] pixels = tex.GetPixels32();
		int num = pixels.Length;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		for (int i = 0; i < num; i++)
		{
			num2 += pixels[i].r;
			num3 += pixels[i].g;
			num4 += pixels[i].b;
		}
		return new Color32((byte)(num2 / num), (byte)(num3 / num), (byte)(num4 / num), byte.MaxValue);
	}

	public static void SaveToFile(Texture source, string filePath, int width, int height, SaveTextureFileFormat fileFormat = SaveTextureFileFormat.PNG, int jpgQuality = 95, bool asynchronous = true, Action<bool> done = null)
	{
		if (!(source is Texture2D) && !(source is RenderTexture))
		{
			done?.Invoke(obj: false);
			return;
		}
		if (width < 0 || height < 0)
		{
			width = source.width;
			height = source.height;
		}
		RenderTexture resizeRT = RenderTexture.GetTemporary(width, height, 0);
		Graphics.Blit(source, resizeRT);
		NativeArray<byte> narray = new NativeArray<byte>(width * height * 4, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
		AsyncGPUReadbackRequest asyncGPUReadbackRequest = AsyncGPUReadback.RequestIntoNativeArray(ref narray, resizeRT, 0, delegate(AsyncGPUReadbackRequest request)
		{
			if (!request.hasError)
			{
				NativeArray<byte> nativeArray = fileFormat switch
				{
					SaveTextureFileFormat.EXR => ImageConversion.EncodeNativeArrayToEXR(narray, resizeRT.graphicsFormat, (uint)width, (uint)height), 
					SaveTextureFileFormat.JPG => ImageConversion.EncodeNativeArrayToJPG(narray, resizeRT.graphicsFormat, (uint)width, (uint)height, 0u, jpgQuality), 
					SaveTextureFileFormat.TGA => ImageConversion.EncodeNativeArrayToTGA(narray, resizeRT.graphicsFormat, (uint)width, (uint)height), 
					_ => ImageConversion.EncodeNativeArrayToPNG(narray, resizeRT.graphicsFormat, (uint)width, (uint)height), 
				};
				File.WriteAllBytes(filePath, nativeArray.ToArray());
				nativeArray.Dispose();
			}
			narray.Dispose();
			done?.Invoke(!request.hasError);
		});
		if (!asynchronous)
		{
			asyncGPUReadbackRequest.WaitForCompletion();
		}
	}

	public static Texture2D CreateCopy(Texture2D tex)
	{
		if (tex == null)
		{
			throw new ArgumentNullException("tex");
		}
		RenderTexture temporary = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
		Graphics.Blit(tex, temporary);
		RenderTexture active = RenderTexture.active;
		RenderTexture.active = temporary;
		Texture2D texture2D = new Texture2D(tex.width, tex.height);
		texture2D.ReadPixels(new Rect(0f, 0f, temporary.width, temporary.height), 0, 0);
		texture2D.Apply();
		RenderTexture.active = active;
		RenderTexture.ReleaseTemporary(temporary);
		return texture2D;
	}
}
