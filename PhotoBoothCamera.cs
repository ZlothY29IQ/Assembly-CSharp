using System;
using System.Collections.Generic;
using System.IO;
using GorillaNetworking;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

public class PhotoBoothCamera : MonoBehaviour
{
	[SerializeField]
	private Camera cam;

	[SerializeField]
	private RenderTexture renderTexture;

	[SerializeField]
	private string saveName = "img";

	[SerializeField]
	private bool appendDateToFile;

	[SerializeField]
	private string imageDescription = "";

	[SerializeField]
	private Texture2D overlay;

	[SerializeField]
	private UnityEvent OnCaptureImage;

	[SerializeField]
	private UnityEvent OnSaveImage;

	private List<RenderTexture> rt = new List<RenderTexture>();

	public Action<Texture, int> OnCapture;

	private bool saveImageToDevice;

	public void SetSaveImageToDevice(bool b)
	{
		saveImageToDevice = b;
	}

	public void Clear()
	{
		rt.Clear();
	}

	public void Capture(float FOV = 60f)
	{
		cam.fieldOfView = FOV;
		cam.Render();
		rt.Add(new RenderTexture(renderTexture.width, renderTexture.height, 1));
		Graphics.Blit(renderTexture, rt[rt.Count - 1]);
		OnCapture?.Invoke(rt[rt.Count - 1], rt.Count - 1);
		OnCaptureImage?.Invoke();
	}

	private void _print()
	{
		string fileName = saveName;
		if (appendDateToFile)
		{
			DateTime dateTime = DateTime.UtcNow;
			if (GorillaComputer.instance != null)
			{
				dateTime = GorillaComputer.instance.GetServerTime();
			}
			fileName += dateTime.ToString("yyyyMMddHHmmss");
		}
		Texture2D print = new Texture2D(renderTexture.width, renderTexture.height * rt.Count);
		for (int i = 0; i < rt.Count; i++)
		{
			Graphics.CopyTexture(rt[i], 0, 0, 0, 0, rt[i].width, rt[i].height, print, 0, 0, 0, rt[i].height * i);
		}
		NativeArray<Color32> narray = new NativeArray<Color32>(print.width * print.height, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
		AsyncGPUReadback.RequestIntoNativeArray(ref narray, print, 0, delegate(AsyncGPUReadbackRequest request)
		{
			if (!request.hasError)
			{
				if ((bool)overlay)
				{
					Color32[] pixels = overlay.GetPixels32();
					for (int j = 0; j < narray.Length; j++)
					{
						if (j < pixels.Length && pixels[j].a > 0)
						{
							narray[j] = pixels[j];
						}
					}
				}
				SaveImage(print, narray, fileName, imageDescription);
			}
			narray.Dispose();
			OnSaveImage?.Invoke();
		});
	}

	public void Print()
	{
		if (saveImageToDevice)
		{
			_print();
		}
	}

	private void SaveImage(Texture rt, NativeArray<Color32> narray, string fileName, string desc)
	{
		NativeArray<byte> nativeArray = ImageConversion.EncodeNativeArrayToJPG(narray, rt.graphicsFormat, (uint)rt.width, (uint)rt.height);
		File.WriteAllBytes(Path.Combine(Application.persistentDataPath, fileName + ".jpg"), nativeArray.ToArray());
		nativeArray.Dispose();
	}
}
